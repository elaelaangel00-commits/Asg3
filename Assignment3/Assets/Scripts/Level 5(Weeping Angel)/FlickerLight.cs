using UnityEngine;

// Put this on any Light. It makes the light flicker like a dying bulb.
public class FlickerLight : MonoBehaviour
{
    [SerializeField] private Light targetLight;
    [SerializeField] private float minIntensity = 0.1f;
    [SerializeField] private float maxIntensity = 1.2f;
    [SerializeField] private float flickerSpeed = 8f;

    private void Start()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();
    }

    private void Update()
    {
        if (targetLight == null) return;

        // Smooth random noise makes the flicker feel organic, not regular
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}