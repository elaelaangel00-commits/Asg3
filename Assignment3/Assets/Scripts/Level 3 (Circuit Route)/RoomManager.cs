using UnityEngine;
using UnityEngine.SceneManagement;

// Runs the whole circuit room: activates sections in order, lights up the power box,
// makes the exit door appear, keeps the objective UI up to date, and loads the next
// scene when the player touches the door or looks at it and presses E.
public class RoomManager : MonoBehaviour
{
    [Header("Sections (in order)")]
    public WireSection[] sections;

    [Header("Power box beside the door")]
    public Renderer[] powerLights;          // one light per section, same order
    public Material lightOffMaterial;
    public Material lightOnMaterial;

    [Header("Exit door")]
    public GameObject exitDoor;             // hidden at the start, appears when all sections are powered
    public string nextSceneName = "Level 4";  // must match the scene name in Build Profiles / Build Settings
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 2.5f;      // how far away you can press E on the door


    int current;                            // index of the active section
    bool doorOpen;
    bool loading;
    Collider doorCollider;
    Transform player;

    void Start()
    {
        foreach (WireSection s in sections)
        {
            s.Changed += OnSectionChanged;
            s.Completed += OnSectionCompleted;
        }
        foreach (Renderer r in powerLights) r.sharedMaterial = lightOffMaterial;

        if (exitDoor != null)
        {
            doorCollider = exitDoor.GetComponentInChildren<Collider>(true);
            exitDoor.SetActive(false);   // door only appears once the room is solved
        }

        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) player = pm.transform;

        ActivateSection(0);
    }

    void OnDestroy()
    {
        foreach (WireSection s in sections)
        {
            if (s == null) continue;
            s.Changed -= OnSectionChanged;
            s.Completed -= OnSectionCompleted;
        }
    }

    void Update()
    {
        if (!doorOpen || loading || Level3UIManager.IsPaused) return;

        if (PlayerPressedEOnDoor())
        {
            loading = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }

    // ---------------- sections ----------------

    void ActivateSection(int index)
    {
        current = index;
        if (index < sections.Length) sections[index].IsActive = true;
        RefreshUI();
    }

    void OnSectionChanged(WireSection s) => RefreshUI();

    void OnSectionCompleted(WireSection s)
    {
        int index = System.Array.IndexOf(sections, s);

        if (index < powerLights.Length) powerLights[index].sharedMaterial = lightOnMaterial;
        Level3AudioManager.SectionPowered();
        Level3UIManager.Message("Section " + (index + 1) + " powered!", Color.green);

        if (index + 1 < sections.Length) ActivateSection(index + 1);
        else ShowDoor();
    }

    // ---------------- exit door ----------------

    void ShowDoor()
    {
        current = sections.Length;
        doorOpen = true;
        if (exitDoor != null) exitDoor.SetActive(true);
        Level3AudioManager.DoorAppear();
        Level3UIManager.Message("All power restored! The exit door has appeared", Color.green, 3f);
        RefreshUI();
    }


    bool PlayerPressedEOnDoor()
    {
        if (!Input.GetKeyDown(interactKey) || exitDoor == null) return false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        return Physics.Raycast(ray, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide)
               && hit.collider.transform.IsChildOf(exitDoor.transform);
    }

    // ---------------- UI ----------------

    void RefreshUI()
    {
        Level3UIManager ui = Level3UIManager.Instance;
        if (ui == null) return;

        int powered = 0;
        foreach (WireSection s in sections) if (s.IsComplete) powered++;
        string power = "Power: " + powered + "/" + sections.Length;

        if (current >= sections.Length)
        {
            ui.SetObjective("The exit door has appeared - go through it!", power);
            return;
        }

        WireSection active = sections[current];
        ui.SetObjective(active.objective, power + "\nWires: " + active.ConnectedCount + "/" + active.Total);
    }
}