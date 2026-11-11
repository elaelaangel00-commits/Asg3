using UnityEngine;

// Put this on the Player (the root object that moves). Handles looking at things
// (raycast from the crosshair), grabbing plugs, wrapping wires on rods, inserting and dropping.
public class PlayerWireHand : MonoBehaviour
{
    [Header("Setup")]
    public Camera playerCamera;
    [UnityEngine.Serialization.FormerlySerializedAs("cablePrefab")]
    public Wire wirePrefab;

    [Header("Controls")]
    public KeyCode interactKey = KeyCode.E;   // grab / wrap around rod / insert
    public KeyCode undoKey = KeyCode.R;       // unwrap the last rod
    public KeyCode dropKey = KeyCode.Q;       // drop the plug (wire goes back to the source)

    [Header("Interaction")]
    public float interactRange = 2.5f;
    public LayerMask interactMask = ~0;       // everything except the Player layer

    [Header("Held plug")]
    public Vector3 heldOffset = new Vector3(0.35f, -0.3f, 0.8f);   // bottom-right of the screen
    public float heldScale = 0.5f;

    Wire held;

    // Used by KillZone: falling drops whatever plug you're holding (it goes back to its source)
    public void DropHeldPlug()
    {
        if (held != null) Drop();
    }

    void Start()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Update()
    {
        if (Level3UIManager.IsPaused) return;   // no grabbing / inserting while paused

        Collider looked = LookAt();
        WireNode node = looked != null ? looked.GetComponentInParent<WireNode>() : null;
        WireRod rod = looked != null ? looked.GetComponentInParent<WireRod>() : null;
        UpdatePrompt(node, rod);

        if (held != null)
        {
            if (Input.GetKeyDown(dropKey)) { Drop(); return; }
            if (Input.GetKeyDown(undoKey) && held.UndoLastRod())
            {
                Level3AudioManager.Unwrap(transform.position);
                Level3UIManager.Message("Unwrapped the last rod", Color.white);
                return;
            }
        }

        if (!Input.GetKeyDown(interactKey)) return;

        if (held == null)
        {
            if (node != null && node.isSource) Grab(node);
        }
        else if (rod != null) Wrap(rod);
        else if (node != null && !node.isSource) Insert(node);
    }

    // What the crosshair is pointing at, within range
    Collider LookAt()
    {
        if (playerCamera == null) return null;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Ignore))
            return hit.collider;
        return null;
    }

    void Grab(WireNode source)
    {
        WireSection section = source.section;
        if (!section.IsActive) { Level3UIManager.Message("No power to this section yet", Color.yellow); return; }
        if (section.IsComplete) { Level3UIManager.Message("This section is already powered", Color.yellow); return; }

        if (source.wire != null) section.Unplug(source);   // redo a connected wire

        held = Instantiate(wirePrefab);
        held.Init(source);
        held.HoldInHand(playerCamera.transform, heldOffset, heldScale);
        source.wire = held;
        Level3AudioManager.Grab(source.TopPoint);
    }

    void Wrap(WireRod rod)
    {
        WireRod[] needed = held.Source.requiredRods;
        if (needed == null || System.Array.IndexOf(needed, rod) < 0)
        {
            Level3UIManager.Message("This rod isn't for this wire", Color.yellow);
            return;
        }
        if (held.HasRod(rod))
        {
            Level3UIManager.Message("Already wrapped around this rod", Color.yellow);
            return;
        }

        held.Wrap(rod);
        Level3AudioManager.Wrap(rod.WrapPoint);
        int done = 0;
        foreach (WireRod r in needed) if (held.HasRod(r)) done++;
        Level3UIManager.Message("Wire wrapped (" + done + "/" + needed.Length + " rods)", Color.green);
    }

    void Insert(WireNode socket)
    {
        if (socket.connected) { Level3UIManager.Message("That socket is already in use", Color.yellow); return; }
        if (socket.section != held.Source.section) { Level3UIManager.Message("This wire doesn't reach that circuit", Color.yellow); return; }

        if (socket.section.TryConnect(held, socket)) held = null;
        // if it was wrong and the section reset, the wire is destroyed and 'held' becomes null next frame
    }

    void Drop()
    {
        held.Source.section.Unplug(held.Source);
        held = null;
        Level3AudioManager.Drop();
    }

    void UpdatePrompt(WireNode node, WireRod rod)
    {
        if (Level3UIManager.Instance == null) return;
        string text = "";

        if (held == null)
        {
            if (node != null && node.isSource)
                text = node.connected ? "\n[" + interactKey + "] Unplug and redo" : "\n[" + interactKey + "] Grab plug";
        }
        else
        {
            text = "\n[" + dropKey + "] Drop plug";
            if (held.LastRod != null) text = "[" + undoKey + "] Unwrap last rod    " + text;

            if (rod != null) text = "\n[" + interactKey + "] Wrap wire around rod    " + text;
            else if (node != null && !node.isSource) text = "\n[" + interactKey + "] Insert plug    " + text;
        }

        Level3UIManager.Instance.SetPrompt(text);
    }
}