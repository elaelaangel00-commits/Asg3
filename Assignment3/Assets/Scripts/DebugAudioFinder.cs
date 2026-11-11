using System.Collections;
using UnityEngine;

/// <summary>
/// TEMPORARY debug helper. Put it on any object, press Play, and read the Console.
/// It lists every AudioSource that is playing right after the scene starts.
/// Delete it when you have found the problem.
/// </summary>
public class DebugAudioFinder : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Wait a moment so everything has finished starting up
        yield return new WaitForSecondsRealtime(0.2f);

        AudioSource[] all = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource s in all)
        {
            if (s.isPlaying)
            {
                string clipName = s.clip != null ? s.clip.name : "none";
                // The last argument makes the log clickable: it highlights the object in the Hierarchy
                Debug.Log("PLAYING: " + s.gameObject.name + " | clip: " + clipName, s.gameObject);
            }
        }
    }
}