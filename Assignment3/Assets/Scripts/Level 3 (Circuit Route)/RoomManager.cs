using UnityEngine;
using UnityEngine.SceneManagement;

// Activates sections in order, lights up the power box,
// scene change when the player looks at the end door and presses E.
public class RoomManager : MonoBehaviour
{
    [Header("Sections (in order)")]
    public WireSection[] sections;

    [Header("Power box beside the door")]
    public Renderer[] powerLights; // one light per section, same order
    public Material lightOffMaterial;
    public Material lightOnMaterial;

    [Header("Exit door")]
    public GameObject exitDoor;
    public string nextSceneName = "Level4";
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 2.5f;
    public string doorPrompt = "Open door to the next room";

    int current;
    bool doorOpen;
    bool loading;

    void Start()
    {
        for (int i = 0; i < sections.Length; i++)
        {
            sections[i].Index = i;   // section number
            sections[i].Changed += OnSectionChanged;
            sections[i].Completed += OnSectionCompleted;
        }
        foreach (Renderer r in powerLights) r.sharedMaterial = lightOffMaterial;

        if (exitDoor != null)
        {
            exitDoor.SetActive(false);
        }

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

        bool pressedE = Input.GetKeyDown(interactKey) && LookingAtDoor();
        if (pressedE)
        {
            loading = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void LateUpdate()
    {
        if (!doorOpen || loading || Level3UIManager.IsPaused || Level3UIManager.Instance == null) return;
        if (LookingAtDoor())
            Level3UIManager.Instance.SetPrompt("[" + interactKey + "] " + doorPrompt);
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

        Level3AudioManager audio = Level3AudioManager.Instance;

        if (index < powerLights.Length)
        {
            powerLights[index].sharedMaterial = lightOnMaterial;
            audio?.PlaySFXAt(audio.SectionPowered, powerLights[index].transform.position);
        }

        if (s.gateToOpen != null)
            audio?.PlaySFXAt(audio.GateOpens != null ? audio.GateOpens : audio.SectionPowered, s.gateToOpen.transform.position);

        Level3UIManager.Message("Section " + (index + 1) + " powered!", Color.green);

        if (index + 1 < sections.Length) ActivateSection(index + 1);
        else ShowDoor();
    }

    // ---------------- exit door ----------------

    void ShowDoor()
    {
        current = sections.Length;
        doorOpen = true;
        if (exitDoor != null)
        {
            exitDoor.SetActive(true);
            Level3AudioManager.Instance?.PlaySFXAt(Level3AudioManager.Instance.DoorAppears, exitDoor.transform.position);
        }
        Level3UIManager.Message("All power restored! The exit door has appeared", Color.green, 3f);
        RefreshUI();
    }


    bool LookingAtDoor() // Is the crosshair within range from door?
    {
        if (exitDoor == null) return false;

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