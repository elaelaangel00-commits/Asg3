using System.Collections.Generic;
using UnityEngine;

public class WireDrawer : MonoBehaviour
{
    [Header("Setup")]
    public WireNode[] sources;          // the source cylinders (one per pair)
    public GameObject exitDoor;         // disabled when all wires are connected
    public Camera playerCamera;         // drag your player's camera here (best); otherwise it searches for one

    [Header("Controls")]
    public KeyCode interactKey = KeyCode.E;   // grab plug at source / insert plug at socket
    public KeyCode cancelKey = KeyCode.Q;     // drop the plug (cable returns to the source)

    [Header("Tuning")]
    public float interactRange = 2.0f;                            // how close to grab / insert
    public Vector2 screenPosition = new Vector2(0.85f, 0.15f);    // where the held plug sits on screen (0,0 = bottom left, 1,1 = top right)
    public float holdDistance = 0.8f;                             // how far in front of the camera
    public float heldScale = 0.5f;      // plug size multiplier while held
    public float wireWidth = 0.08f;
    public float sag = 1.0f;            // how much the cable droops (0 = straight)
    public float plugSize = 0.3f;
    public float plugInsertDepth = 0f;  // sink the plug into the cylinder's top (0 = sits on top, centred)

    class Cable
    {
        public WireNode source;
        public LineRenderer lr;
        public Transform plug;
    }

    Dictionary<WireNode, Cable> cables = new Dictionary<WireNode, Cable>();
    List<WireNode> validSources = new List<WireNode>();
    Cable held;
    string message = "";
    float messageTimer;

    void Start()
    {
        // prefer the player's own camera, then fall back to the scene's main camera
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogWarning("WireDrawer: no camera found. Drag your player's camera into 'Player Camera'.");

        foreach (var s in sources)
        {
            if (s == null)
            {
                Debug.LogError("WireDrawer: a 'sources' slot is empty.");
                continue;
            }
            if (s.partner == null)
            {
                Debug.LogError("WireDrawer: '" + s.name + "' has no Partner assigned (is it a socket, or did you forget to drag it in?)", s);
                continue;
            }
            validSources.Add(s);
        }
    }

    void Update()
    {
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;

        // ---- empty-handed: E near a source grabs its plug ----
        if (held == null)
        {
            if (Input.GetKeyDown(interactKey)) TryGrab();
            return;
        }

        // ---- holding a plug ----
        if (Input.GetKeyDown(cancelKey)) { Drop(); return; }

        if (Input.GetKeyDown(interactKey))
        {
            WireNode socket = NearestSocket();
            if (socket == null) return;

            if (socket == held.source.partner) Connect(socket);
            else Say("Wrong socket!");
        }
    }

    void LateUpdate()
    {
        // the plug stays at the bottom right of the camera; the cable stretches from the source to it
        if (held == null) return;

        Vector3 plugPos;
        Quaternion rot;
        if (playerCamera != null)
        {
            // a point on screen, so it stays bottom right no matter how the player is set up
            plugPos = playerCamera.ViewportToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, holdDistance));
            rot = playerCamera.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            plugPos = transform.position + transform.forward * holdDistance;
            rot = transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        }

        held.plug.SetPositionAndRotation(plugPos, rot);
        DrawCable(held, plugPos);
    }

    // ---------------- grab / insert / drop ----------------

    void TryGrab()
    {
        WireNode best = null;
        float bestDist = interactRange;
        foreach (var s in validSources)
        {
            float d = FlatDistance(s.transform.position);
            if (d < bestDist) { bestDist = d; best = s; }
        }
        if (best == null) return;

        // grabbing an already-connected source unplugs it so you can redo it
        if (cables.ContainsKey(best)) RemoveCable(best);

        Cable c = new Cable { source = best };

        // the cable
        GameObject go = new GameObject("Cable_" + best.name);
        c.lr = go.AddComponent<LineRenderer>();
        c.lr.widthMultiplier = wireWidth;

        // the plug (plain cylinder)
        GameObject plug = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plug.name = "Plug";
        Collider col = plug.GetComponent<Collider>();
        if (col != null) Destroy(col);
        plug.transform.SetParent(go.transform, false);
        SetPlugScale(plug.transform, heldScale);
        c.plug = plug.transform;

        cables[best] = c;
        held = c;
    }

    void Connect(WireNode socket)
    {
        // plug goes in the centre of the cylinder
        Vector3 plugPos = SocketPlugPosition(socket);
        SetPlugScale(held.plug, 1f);
        held.plug.SetPositionAndRotation(plugPos, Quaternion.identity);
        DrawCable(held, plugPos);

        held.source.connected = true;
        socket.connected = true;
        held = null;
        CheckAllConnected();
    }

    void Drop()
    {
        RemoveCable(held.source);
        held = null;
    }

    void RemoveCable(WireNode src)
    {
        if (cables.TryGetValue(src, out Cable c))
        {
            if (c.lr != null) Destroy(c.lr.gameObject);
            cables.Remove(src);
        }
        src.connected = false;
        if (src.partner != null) src.partner.connected = false;
    }

    void CheckAllConnected()
    {
        foreach (var s in validSources)
            if (!s.connected) return;

        Debug.Log("All circuits connected!");
        if (exitDoor != null) exitDoor.SetActive(false); // replace with your own door-open logic
    }

    // ---------------- helpers ----------------

    void SetPlugScale(Transform plug, float scale)
    {
        plug.localScale = new Vector3(plugSize * scale, plugSize * 0.25f * scale, plugSize * scale);
    }

    // Draws a gently drooping cable from the source to the given end point
    void DrawCable(Cable c, Vector3 end)
    {
        Vector3 start = TopOf(c.source);
        const int segments = 20;
        c.lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y -= sag * 4f * t * (1f - t);
            c.lr.SetPosition(i, pos);
        }
    }

    // centre of the cylinder's top face
    Vector3 TopOf(WireNode n)
    {
        Collider col = n.GetComponent<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }
        // no collider: assume a default Unity cylinder (height = 2 x scale)
        return n.transform.position + Vector3.up * n.transform.lossyScale.y;
    }

    // where the inserted plug sits: centre of the cylinder
    Vector3 SocketPlugPosition(WireNode n)
    {
        Vector3 top = TopOf(n);
        return top + Vector3.up * (plugSize * 0.25f - plugInsertDepth);
    }

    float FlatDistance(Vector3 other)
    {
        Vector3 d = transform.position - other;
        d.y = 0f;
        return d.magnitude;
    }

    // nearest socket (any pair) within range
    WireNode NearestSocket()
    {
        WireNode best = null;
        float bestDist = interactRange;
        foreach (var s in validSources)
        {
            float d = FlatDistance(s.partner.transform.position);
            if (d < bestDist) { bestDist = d; best = s.partner; }
        }
        return best;
    }

    void Say(string text)
    {
        message = text;
        messageTimer = 1.2f;
    }

    // ---------------- UI ----------------

    void OnGUI()
    {
        GUIStyle prompt = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        prompt.normal.textColor = Color.white;

        float boxW = 500f, boxH = 40f;
        Rect centre = new Rect((Screen.width - boxW) / 2f, Screen.height * 0.65f, boxW, boxH);

        // ---- wrong socket message (only after pressing E on the wrong one) ----
        if (messageTimer > 0f)
        {
            prompt.normal.textColor = Color.red;
            GUI.Label(centre, message, prompt);
            return;
        }

        // ---- holding a plug: same prompt at every socket ----
        if (held != null)
        {
            if (NearestSocket() != null)
                GUI.Label(centre, "Press " + interactKey + " to insert plug", prompt);
            return;
        }

        // ---- empty-handed: prompt near a source ----
        WireNode near = null;
        float bestDist = interactRange;
        foreach (var s in validSources)
        {
            float d = FlatDistance(s.transform.position);
            if (d < bestDist) { bestDist = d; near = s; }
        }
        if (near == null) return;

        string text = near.connected
            ? "Press " + interactKey + " to unplug and redo"
            : "Press " + interactKey + " to grab plug";
        GUI.Label(centre, text, prompt);
    }
}