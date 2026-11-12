using System.Collections.Generic;
using UnityEngine;

// A realistic hanging wire using Verlet rope simulation.
// The rope is made of "spans": the piece between the source and the first rod,
// between each pair of rods, and from the last rod to the plug. Each span is a
// chain of points: gravity pulls them down, then neighbouring points are pulled
// back to the right distance apart, with both ends of the span pinned.
// Span length grows with distance, so the wire pays out and droops naturally.
// The LineRenderer only DRAWS the points - the movement comes from the simulation.
[RequireComponent(typeof(LineRenderer))]
public class Wire : MonoBehaviour
{
    [Header("Parts")]
    public Transform plug;                 // child cylinder (no collider)
    public Renderer plugRenderer;

    [Header("Rope")]
    public int segmentsPerSpan = 16;       // more = smoother but more work
    public float slack = 1.2f;             // 1 = tight, higher = longer rope = more droop
    public float minLength = 0.3f;
    [Range(0.9f, 1f)] public float damping = 0.98f;   // lower = settles faster, less swing
    public int iterations = 15;            // more = stiffer, less stretchy rope

    [Header("Length limits")]
    public float maxLength = 10f;          // while carrying: the wire never gets longer than this per span (it goes taut instead)
    public float connectedSlack = 1.05f;   // once plugged in / wrapped: length is FIXED at straight distance x this (a little loose)

    [Header("Floor collision")]
    public LayerMask floorMask = ~0;       // set to your environment layer - NOT the Player layer
    public float thickness = 0.03f;

    [Header("Wrap coil (visual ring on the rod)")]
    public float coilHeight = 0.08f;
    public float coilThickness = 0.02f;

    [Header("Glow (needs Bloom on the camera)")]
    public bool glow = true;
    [Range(0f, 10f)] public float glowIntensity = 2.5f;   // higher = brighter glow

    public WireNode Source { get; private set; }
    public WireRod LastRod => rods.Count > 0 ? rods[rods.Count - 1] : null;

    class Span
    {
        public Vector3[] pos;   
        public Vector3[] prev;  
        public float lockedLength = -1f;   // > 0 = this span's length is fixed (it no longer pays out)

        public Span(int points, Vector3 start)
        {
            pos = new Vector3[points];
            prev = new Vector3[points];
            for (int i = 0; i < points; i++) pos[i] = prev[i] = start;
        }
    }

    readonly List<Span> spans = new List<Span>();     // always rods.Count + 1 spans
    readonly List<WireRod> rods = new List<WireRod>();
    readonly List<GameObject> coils = new List<GameObject>();
    LineRenderer lr;
    Vector3 plugScale;
    Color colour;   // copied from the source cylinder's material

    public void Init(WireNode source)
    {
        Source = source;
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;

        // the wire and plug are visual only: remove any collider so the plug
        // (which follows the camera) can't push or trap the player
        foreach (Collider col in GetComponentsInChildren<Collider>(true)) Destroy(col);

        // wire, plug and coils match whatever material is on the source cylinder
        Renderer sourceRenderer = source.GetComponent<Renderer>();
        colour = sourceRenderer != null ? sourceRenderer.sharedMaterial.color : Color.white;
        Tint(lr.material);
        if (plugRenderer != null) Tint(plugRenderer.material);
        plugScale = plug.localScale;

        spans.Add(new Span(segmentsPerSpan + 1, source.TopPoint));
    }

    void Tint(Material m)
    {
        if (!glow)
        {
            m.color = colour;
            return;
        }

        Color hdr = colour * glowIntensity;  
        hdr.a = 1f;

        if (m.HasProperty("_EmissionColor"))  
        {
            m.color = colour;
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", hdr);
        }
        else                                  
        {
            m.color = hdr;
        }
    }

    // ---------------- rods ----------------

    public bool HasRod(WireRod rod) => rods.Contains(rod);

    public void Wrap(WireRod rod)
    {
        rods.Add(rod);
        LockSpan(spans.Count - 1);   // the span that now ends at this rod stops growing
        spans.Add(new Span(segmentsPerSpan + 1, rod.WrapPoint));   // new span from this rod to the plug
        coils.Add(MakeCoil(rod));
    }

    public bool UndoLastRod()
    {
        if (rods.Count == 0) return false;
        int last = rods.Count - 1;
        rods.RemoveAt(last);
        spans.RemoveAt(spans.Count - 1);
        Destroy(coils[last]);
        coils.RemoveAt(last);
        spans[spans.Count - 1].lockedLength = -1f;   // this span ends at the plug again, so it can pay out
        return true;
    }

    void LockSpan(int s) // Fix a span's length, to prevent it to overgrow, glitch up to ceiling
    {
        SpanEnds(s, out Vector3 a, out Vector3 b);
        spans[s].lockedLength = Mathf.Max(Vector3.Distance(a, b) * connectedSlack, minLength);
    }

    void SpanEnds(int s, out Vector3 a, out Vector3 b)
    {
        a = s == 0 ? Source.TopPoint : rods[s - 1].WrapPoint;
        b = s < rods.Count ? rods[s].WrapPoint : plug.position;
    }

    GameObject MakeCoil(WireRod rod)
    {
        GameObject coil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coil.name = "Coil_" + rod.name;
        Destroy(coil.GetComponent<Collider>());
        coil.transform.SetParent(transform, false);
        float diameter = (rod.Radius + coilThickness) * 2f;
        coil.transform.position = rod.WrapPoint;
        coil.transform.localScale = new Vector3(diameter, coilHeight * 0.5f, diameter);
        Tint(coil.GetComponent<Renderer>().material);
        return coil;
    }

    // ---------------- plug ----------------

    public void HoldInHand(Transform cam, Vector3 localOffset, float scale)
    {
        plug.SetParent(cam, false);
        plug.localPosition = localOffset;
        plug.localRotation = Quaternion.Euler(90f, 0f, 0f);
        plug.localScale = plugScale * scale;
    }

    public void PlugInto(WireNode socket) // Plug sits on top of the socket
    {
        plug.SetParent(transform, false);
        plug.localScale = plugScale;
        plug.SetPositionAndRotation(socket.TopPoint + Vector3.up * plugScale.y, Quaternion.identity);
        LockSpan(spans.Count - 1);   // connected: the last span stops paying out and just hangs loosely
    }

    // ---------------- simulation ----------------

    void LateUpdate()
    {
        if (Source == null || spans.Count == 0) return;
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        if (dt <= 0f) return;   // game paused (timeScale = 0)

        for (int s = 0; s < spans.Count; s++)
        {
            SpanEnds(s, out Vector3 a, out Vector3 b);
            Simulate(spans[s], a, b, dt);
        }
        Draw();
    }

    void Simulate(Span span, Vector3 a, Vector3 b, float dt)
    {
        Vector3[] pos = span.pos;
        Vector3[] prev = span.prev;
        int n = pos.Length - 1;

        float straight = Vector3.Distance(a, b);
        float ropeLength = span.lockedLength > 0f
            ? span.lockedLength                                   // connected: fixed length
            : Mathf.Clamp(straight * slack, minLength, maxLength); // carrying: pays out, up to maxLength
        ropeLength = Mathf.Max(ropeLength, straight);             // never shorter than the gap (avoids fighting/jitter)
        float segLength = ropeLength / n;

        Vector3 gravity = Physics.gravity * dt * dt;
        for (int i = 1; i < n; i++)
        {
            Vector3 velocity = (pos[i] - prev[i]) * damping;
            prev[i] = pos[i];
            pos[i] += velocity + gravity;
        }

        for (int k = 0; k < iterations; k++)
        {
            pos[0] = a;
            pos[n] = b;

            for (int i = 0; i < n; i++)
            {
                Vector3 delta = pos[i + 1] - pos[i];
                float dist = delta.magnitude;
                if (dist < 0.0001f) continue;

                Vector3 correction = delta * ((dist - segLength) / dist);
                if (i == 0) pos[i + 1] -= correction;
                else if (i + 1 == n) pos[i] += correction;
                else
                {
                    pos[i] += correction * 0.5f;
                    pos[i + 1] -= correction * 0.5f;
                }
            }
        }

        for (int i = 1; i < n; i++)
        {
            if (Physics.Raycast(pos[i] + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit,
                                0.5f + thickness, floorMask, QueryTriggerInteraction.Ignore))
            {
                float minY = hit.point.y + thickness;
                if (pos[i].y < minY)
                {
                    pos[i].y = minY;
                    prev[i].y = minY;
                }
            }
        }

        float maxY = Mathf.Max(a.y, b.y) + 0.05f;
        for (int i = 1; i < n; i++)
        {
            if (float.IsNaN(pos[i].x) || float.IsNaN(pos[i].y) || float.IsNaN(pos[i].z))
            {
                pos[i] = prev[i] = Vector3.Lerp(a, b, i / (float)n);
                continue;
            }
            if (pos[i].y > maxY)
            {
                pos[i].y = maxY;
                prev[i].y = maxY;
            }
        }
    }

    void Draw()
    {
        int perSpan = segmentsPerSpan + 1;
        lr.positionCount = spans.Count * perSpan;
        int index = 0;
        foreach (Span span in spans)
            for (int i = 0; i < perSpan; i++)
                lr.SetPosition(index++, span.pos[i]);
    }

    void OnDestroy()
    {
        if (plug != null) Destroy(plug.gameObject);
    }
}