using TMPro;
using UnityEngine;

public class PuzzleStatusUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float messageDuration = 3f;

    private float messageTimer;

    private void Update()
    {
        if (messageTimer <= 0f)
        {
            return;
        }

        messageTimer -= Time.deltaTime;

        if (messageTimer <= 0f)
        {
            statusText.text = string.Empty;
        }
    }

    public void ShowMessage(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        messageTimer = messageDuration;
    }
}
