using System;
using System.Collections.Generic;
using UnityEngine;

// One area of the circuit room. Put this on an empty GameObject and make all of that
// section's sources, sockets and decoy sockets its CHILDREN - they are found automatically.
public class WireSection : MonoBehaviour
{
    [Header("Info")]
    [TextArea] public string objective = "Objective: Connect the matching coloured wires";

    [Header("Rules")]
    public bool resetOnWrongSocket = false;   // wrong socket = short circuit, every plug in this section pops out

    [Header("When complete")]
    public GameObject gateToOpen;             // shutter/wall blocking the next section (gets disabled)

    [Header("Feedback (optional)")]
    public ParticleSystem sparkPrefab;        // sounds are in CircuitAudio

    public event Action<WireSection> Changed;     // progress changed
    public event Action<WireSection> Completed;   // every pair connected

    public bool IsActive { get; set; }            // set by CircuitRoomManager
    public bool IsComplete { get; private set; }

    public int Total => sources.Count;
    public int ConnectedCount
    {
        get
        {
            int n = 0;
            foreach (WireNode s in sources) if (s.connected) n++;
            return n;
        }
    }

    readonly List<WireNode> sources = new List<WireNode>();

    void Awake()
    {
        foreach (WireNode node in GetComponentsInChildren<WireNode>())
        {
            node.section = this;
            if (!node.isSource) continue;

            if (node.partner == null)
                Debug.LogError("WireSection: source '" + node.name + "' has no partner socket.", node);
            else
                sources.Add(node);
        }
    }

    // Returns true if the plug went in
    public bool TryConnect(Wire wire, WireNode socket)
    {
        WireNode source = wire.Source;

        if (socket == source.partner)
        {
            int missing = MissingRods(wire);
            if (missing > 0)
            {
                Level3UIManager.Message("This wire still needs to go around " + missing + " more rod" + (missing == 1 ? "" : "s"), Color.yellow);
                return false;
            }

            wire.PlugInto(socket);
            source.connected = true;
            socket.connected = true;
            Level3AudioManager.Connect(socket.TopPoint);
            Changed?.Invoke(this);

            if (ConnectedCount == Total) Complete();
            return true;
        }

        // wrong socket (includes decoys)
        if (sparkPrefab != null) Instantiate(sparkPrefab, socket.TopPoint, Quaternion.identity);
        if (resetOnWrongSocket)
        {
            Level3AudioManager.ShortCircuit(socket.TopPoint);
            ResetSection();
            Level3UIManager.Message("Short circuit! Section reset", Color.red);
        }
        else
        {
            Level3AudioManager.Wrong(socket.TopPoint);
            Level3UIManager.Message("Wrong socket!", Color.red);
        }
        return false;
    }

    int MissingRods(Wire wire)
    {
        WireRod[] needed = wire.Source.requiredRods;
        if (needed == null) return 0;

        int missing = 0;
        foreach (WireRod rod in needed)
            if (rod != null && !wire.HasRod(rod)) missing++;
        return missing;
    }

    public void Unplug(WireNode source)
    {
        if (source.wire != null) Destroy(source.wire.gameObject);
        source.wire = null;
        source.connected = false;
        source.partner.connected = false;
        Changed?.Invoke(this);
    }

    public void ResetSection()
    {
        foreach (WireNode s in sources) Unplug(s);
    }

    void Complete()
    {
        IsComplete = true;
        if (gateToOpen != null) gateToOpen.SetActive(false);
        Completed?.Invoke(this);
    }
}