using UnityEngine;

public class PlayerWireHand : MonoBehaviour
{
    [Header("Setup")]
    public Camera playerCamera;
    [UnityEngine.Serialization.FormerlySerializedAs("cablePrefab")]
    public Wire wirePrefab;

    [Header("Controls")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode undoKey = KeyCode.R;
    public KeyCode dropKey = KeyCode.Q;

    [Header("Interaction")]
    public float interactRange = 2.5f;
    public LayerMask interactMask = ~0;

    [Header("Held plug")]
    public Vector3 heldOffset = new Vector3(0.35f, -0.3f, 0.8f);
    public float heldScale = 0.5f;

    Wire held;

    public void DropHeldPlug() // Used by KillZone, falling drops whatever plug you're holding (it goes back to its source)
    {
        if (held != null) Drop(); // plays the Return Wire sound too, on top of the splash
    }

    void Start()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Update()
    {
        if (Level3UIManager.IsPaused) return;

        Collider looked = LookAt();
        WireNode node = looked != null ? looked.GetComponentInParent<WireNode>() : null;
        WireRod rod = looked != null ? looked.GetComponentInParent<WireRod>() : null;
        UpdatePrompt(node, rod);

        if (held != null)
        {
            if (Input.GetKeyDown(dropKey)) { Drop(); return; }
            if (Input.GetKeyDown(undoKey) && held.UndoLastRod())
            {
                Level3AudioManager.Instance?.PlaySFX(Level3AudioManager.Instance.Unwrap);
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


    Collider LookAt() // What the crosshair is pointing at, within range
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

        if (source.wire != null) section.Unplug(source); // redo a connected wire

        held = Instantiate(wirePrefab);
        held.Init(source);
        held.HoldInHand(playerCamera.transform, heldOffset, heldScale);
        source.wire = held;
        Level3AudioManager.Instance?.PlaySFX(Level3AudioManager.Instance.Grab);
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
        Level3AudioManager.Instance?.PlaySFX(Level3AudioManager.Instance.Wrap);
        int done = 0;
        foreach (WireRod r in needed) if (held.HasRod(r)) done++;
        Level3UIManager.Message("Wire wrapped (" + done + "/" + needed.Length + " rods)", Color.green);
    }

    void Insert(WireNode socket)
    {
        if (socket.connected) { Level3UIManager.Message("That socket is already in use", Color.yellow); return; }
        if (socket.section != held.Source.section) { Level3UIManager.Message("This wire doesn't reach that circuit", Color.yellow); return; }

        if (socket.section.TryConnect(held, socket)) held = null;
    }

    void Drop(bool playSound = true)
    {
        held.Source.section.Unplug(held.Source);
        held = null;
        if (playSound) Level3AudioManager.Instance?.PlaySFX(Level3AudioManager.Instance.ReturnWire);
    }

    void UpdatePrompt(WireNode node, WireRod rod)
    {
        if (Level3UIManager.Instance == null) return;
        string text = "";

        if (held == null)
        {
            if (node != null && node.isSource)
                text = node.connected ? "[" + interactKey + "] Unplug and redo" : "[" + interactKey + "] Grab plug";
        }
        else
        {
            text = "[" + dropKey + "] Drop plug";
            if (held.LastRod != null) text = "[" + undoKey + "] Unwrap last rod\n" + text;

            if (rod != null) text = "[" + interactKey + "] Wrap wire around rod\n" + text;
            else if (node != null && !node.isSource) text = "[" + interactKey + "] Insert plug\n" + text;
        }

        Level3UIManager.Instance.SetPrompt(text);
    }
}