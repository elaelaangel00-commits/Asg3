using TMPro;
using UnityEngine;

public class PuzzleStatusUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField, Min(0f)] private float messageDuration = 3f;

    private string objectInfo = string.Empty;
    private string temporaryMessage = string.Empty;
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
            temporaryMessage = string.Empty;
            RefreshText();
        }
    }

    public void ShowMessage(string message)
    {
        temporaryMessage = message;
        messageTimer = messageDuration;

        RefreshText();
    }

    public void SetObjectInfo(string message)
    {
        if (objectInfo == message)
        {
            return;
        }

        objectInfo = message;
        RefreshText();
    }

    private void RefreshText()
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = messageTimer > 0f ? temporaryMessage : objectInfo;
    }
}
