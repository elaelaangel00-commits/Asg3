using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Wire : MonoBehaviour
{
    [Header("Parts")]
    public Transform plug;             
    public Renderer plugRenderer;

    [Header("Rope")]
    public int segmentsPerSpan = 16; //rope smoothness
    public float slack = 1.2f; //rope tightness
    public float minLength = 0.3f;
    [Range(0.9f, 1f)] public float damping = 0.98f; //rope gravity
    public int iterations = 15;//rope stiffness, less stretchy rope

    [Header("Length limits")]
    public float maxLength = 10f;          
    public float connectedSlack = 1.05f; //once connected, rope loose by this much

    [Header("Floor collision")]
    public LayerMask floorMask = ~0;
    public float thickness = 0.03f;

    [Header("Obstacle collision")]
    public bool collideWithObstacles = true;
    public LayerMask obstacleMask = ~0;  
    public float collisionRadius = 0.05f;  
    [Range(1, 10)] public int collisionEvery = 3; // check obstacles 

    [Header("Wrap coil")]
    public float coilHeight = 0.08f;
    public float coilThickness = 0.02f;

    [Header("Wire Glow")]
    public bool glow = true;
    [Range(0f, 10f)] public float glowIntensity = 2.5f; 

    public WireNode Source { get; private set; }
    public WireRod LastRod => rods.Count > 0 ? rods[rods.Count - 1] : null;

    class Span
    {
        public Vector3[] pos; //current point positions
        public Vector3[] prev;//positions last frame (velocity = pos - prev)
        public float lockedLength = -1f;

        public Span(int points, Vector3 start)
        {
            pos = new Vector3[points];
            prev = new Vector3[points];
            for (int i = 0; i < points; i++) pos[i] = prev[i] = start;
        }
    }

    readonly List<Span> spans = new List<Span>(); // always rods.Count + 1 span
    readonly List<WireRod> rods = new List<WireRod>();
    readonly List<GameObject> coils = new List<GameObject>();
    LineRenderer lr;
    Vector3 plugScale;
    Color colour; // copies plug's colour material

    static SphereCollider probe;
    static readonly Collider[] overlaps = new Collider[8];
    static readonly Dictionary<Collider, bool> ignoreCache = new Dictionary<Collider, bool>();

    public void Init(WireNode source)
    {
        Source = source;
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;

        foreach (Collider col in GetComponentsInChildren<Collider>(true)) Destroy(col);

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

    //---------------- Rods ----------------

    public bool HasRod(WireRod rod) => rods.Contains(rod);

    public void Wrap(WireRod rod)
    {
        rods.Add(rod);
        LockSpan(spans.Count - 1);  
        spans.Add(new Span(segmentsPerSpan + 1, rod.WrapPoint));  
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
        spans[spans.Count - 1].lockedLength = -1f;
        return true;
    }

    void LockSpan(int s)
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

    //---------------- Plug ----------------

    public void HoldInHand(Transform cam, Vector3 localOffset, float scale)
    {
        plug.SetParent(cam, false);
        plug.localPosition = localOffset;
        plug.localRotation = Quaternion.Euler(90f, 0f, 0f);
        plug.localScale = plugScale * scale;
    }

    public void PlugInto(WireNode socket)
    {
        plug.SetParent(transform, false);
        plug.localScale = plugScale;
        plug.SetPositionAndRotation(socket.TopPoint + Vector3.up * plugScale.y, Quaternion.identity);
        LockSpan(spans.Count - 1); 
    }

    //---------------- Simulation ----------------

    void LateUpdate()
    {
        if (Source == null || spans.Count == 0) return;
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        if (dt <= 0f) return; // game paused (timeScale = 0)

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
            ? span.lockedLength                                   
            : Mathf.Clamp(straight * slack, minLength, maxLength); 
        ropeLength = Mathf.Max(ropeLength, straight);  
        float segLength = ropeLength / n;

        Vector3 gravity = Physics.gravity * dt * dt;
        for (int i = 1; i < n; i++)
        {
            Vector3 velocity = (pos[i] - prev[i]) * damping;
            prev[i] = pos[i];
            pos[i] += velocity + gravity;
        }

        //Pin both ends, keep each segment at segment length + obstacle check
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

            if (collideWithObstacles && (k + 1) % collisionEvery == 0)
                PushOutOfObstacles(pos, prev, n);
        }

        if (collideWithObstacles) PushOutOfObstacles(pos, prev, n);

        //Make rope stay on floor, no glitch in floor
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

        //Make rope not fly up and glitch ceiling
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
    void PushOutOfObstacles(Vector3[] pos, Vector3[] prev, int n)
    {
        SphereCollider s = GetProbe();
        s.radius = collisionRadius;

        for (int i = 1; i < n; i++) 
        {
            int count = Physics.OverlapSphereNonAlloc(pos[i], collisionRadius, overlaps,
                                                      obstacleMask, QueryTriggerInteraction.Ignore);
            for (int c = 0; c < count; c++)
            {
                Collider other = overlaps[c];
                if (ShouldIgnore(other)) continue;

                if (Physics.ComputePenetration(s, pos[i], Quaternion.identity,
                                               other, other.transform.position, other.transform.rotation,
                                               out Vector3 dir, out float depth))
                {
                    Vector3 push = dir * depth;
                    pos[i] += push;
                    prev[i] += push;
                }
            }
        }
    }

    //Things the wire is allowed to overlap and glitch in
    static bool ShouldIgnore(Collider c)
    {
        if (ignoreCache.TryGetValue(c, out bool ignore)) return ignore;

        ignore = c.GetComponentInParent<PlayerMovement>() != null
              || c.GetComponentInParent<WireRod>() != null
              || c.GetComponentInParent<WireNode>() != null
              || c.GetComponentInParent<Wire>() != null;

        ignoreCache[c] = ignore;
        return ignore;
    }

    static SphereCollider GetProbe()
    {
        if (probe != null) return probe;

        GameObject go = new GameObject("WireCollisionProbe");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.position = new Vector3(0f, -10000f, 0f); 
        probe = go.AddComponent<SphereCollider>();
        probe.isTrigger = true;
        ignoreCache.Clear();//reset colliders
        return probe;
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