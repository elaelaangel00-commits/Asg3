using TMPro;
using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    // the text screen on the wall above the lamps ("WATCH", "YOUR TURN", round and timer).
    // it's in the world, not on the hud, so it doesn't clash with the team's ui.
    public class SimonDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;   // big text: what to do
        [SerializeField] private TMP_Text infoText;     // small text: round, progress, timer

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
