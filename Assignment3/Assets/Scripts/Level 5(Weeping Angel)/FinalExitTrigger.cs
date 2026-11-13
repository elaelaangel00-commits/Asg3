using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class FinalExitTrigger : MonoBehaviour
{
    [Header("Next Level (leave EMPTY if this is the last level)")]
    public string nextSceneName = "";

    [Header("Game Complete (shown when there is no next scene)")]
    public GameObject gameCompletePanel;

  
    private bool unlocked = false;
    private bool finished = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Unlock()
    {
        if (unlocked) return;
        unlocked = true;
        Debug.Log("[EXIT] Door unlocked! Player can enter.");
    }

    private void OnTriggerEnter(Collider other)
    {
        TryFinish(other);
    }

    private void TryFinish(Collider other)
    {
        if (finished) return;
        if (!unlocked) return;

        finished = true;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // There is a next level: load it
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Last level: show the Game Complete screen
            Debug.Log("[EXIT] Game complete!");

            if (gameCompletePanel != null)
                gameCompletePanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;   // free the mouse so buttons work
            Cursor.visible = true;
            Time.timeScale = 0f;                      // freeze the game
        }
    }
}
