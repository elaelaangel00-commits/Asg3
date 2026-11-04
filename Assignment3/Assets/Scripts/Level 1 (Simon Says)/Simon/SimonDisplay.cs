using TMPro;
using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// The text screen on the wall above the lamps ("WATCH", "YOUR TURN", round and timer).
    /// It is in the world, not on the HUD, so it does not clash with the team's HUD.
    /// </summary>
    public class SimonDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text infoText;

        public void SetPrompt(string message, Color colour)
        {
            if (promptText == null) return;
            promptText.text = message;
            promptText.color = colour;
        }

        public void SetInfo(string message)
        {
            if (infoText != null) infoText.text = message;
        }
    }
}
