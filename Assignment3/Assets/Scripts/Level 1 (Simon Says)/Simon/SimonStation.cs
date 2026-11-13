using System;
using System.Collections;
using Backrooms.Core;
using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    // one lamp + one target.
    // the lamp shows the sequence, the target is what the player shoots.
    // the station doesn't know the puzzle rules. it only shows things and reports hits.
    public class SimonStation : MonoBehaviour, IShootable
    {
        [Header("Parts (drag in from this prefab's children)")]
        [SerializeField] private Renderer lampRenderer;
        [SerializeField] private Light lampLight;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private AudioSource audioSource;   // 3d sound, so each note comes from its own lamp

        [Header("Look")]
        [SerializeField] private float litLightIntensity = 3f;
        [SerializeField] private float emissionStrength = 4f;
        [Range(0f, 1f)] [SerializeField] private float offBrightness = 0.25f;   // how dim the lamp is when off
        [SerializeField] private float confirmDuration = 0.2f;

        public event Action<int> Shot;   // sends this station's id to the puzzle

        public int Id { get; private set; } = -1;
        public Color Colour { get; private set; } = Color.white;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private Material lampMaterial;
        private AudioClip tone;
        private Coroutine effect;   // the blink/flicker that's running, so we can stop it

        // the lamp colour but darker (for when it's off)
        private Color DimColour => new Color(Colour.r * offBrightness, Colour.g * offBrightness, Colour.b * offBrightness, 1f);

        private void Awake()
        {
            lampMaterial = lampRenderer.material;      // .material = a copy for this station only
            lampMaterial.EnableKeyword("_EMISSION");   // allow the lamp to glow
        }

        // called once by SimonPuzzle at the start.
        // id, colour and note come from code, so all 5 prefabs stay the same.
        public void Setup(int id, Color colour, AudioClip toneClip)
        {
            Id = id;
            Colour = colour;
            tone = toneClip;

            if (targetRenderer != null)
                targetRenderer.material.color = Color.Lerp(Color.white, colour, 0.4f);   // light tint of the lamp colour

            SetLit(false);
        }

        // called by PlayerShooter when this target gets hit
        public void OnShot(RaycastHit hit)
        {
            Shot?.Invoke(Id);   // just report "i was hit", the puzzle decides if it's right
        }

        // lamp fully on or dim, no sound
        public void SetLit(bool lit)
        {
            StopEffect();
            ApplyLamp(lit ? Colour : DimColour, lit);
        }

        // lamp on + its note. used while showing the sequence.
        public void Flash()
        {
            SetLit(true);
            PlayTone();
        }

        // quick blink + note after a correct shot
        public void Confirm()
        {
            StopEffect();
            PlayTone();
            effect = StartCoroutine(BlinkRoutine(Colour, confirmDuration));
        }

        // flicker in the fail colour (red)
        public void FailFlicker(Color failColour, int flickers = 3, float interval = 0.12f)
        {
            StopEffect();
            effect = StartCoroutine(FlickerRoutine(failColour, flickers, interval));
        }

        private void PlayTone()
        {
            if (audioSource != null && tone != null) audioSource.PlayOneShot(tone);
        }

        // sets the lamp colour, glow and point light all at once
        private void ApplyLamp(Color colour, bool lit)
        {
            if (lampMaterial == null) return;

            lampMaterial.color = colour;
            lampMaterial.SetColor(EmissionColorId, lit ? colour * emissionStrength : Color.black);

            if (lampLight != null)
            {
                lampLight.color = colour;
                lampLight.intensity = lit ? litLightIntensity : 0f;
            }
        }

        // coroutine: on, wait, off
        private IEnumerator BlinkRoutine(Color colour, float duration)
        {
            ApplyLamp(colour, true);
            yield return new WaitForSeconds(duration);
            ApplyLamp(DimColour, false);
            effect = null;
        }

        // coroutine: on/off a few times
        private IEnumerator FlickerRoutine(Color colour, int flickers, float interval)
        {
            for (int i = 0; i < flickers; i++)
            {
                ApplyLamp(colour, true);
                yield return new WaitForSeconds(interval);
                ApplyLamp(DimColour, false);
                yield return new WaitForSeconds(interval);
            }
            effect = null;
        }

        // stop any blink/flicker still running so two effects don't fight
        private void StopEffect()
        {
            if (effect == null) return;
            StopCoroutine(effect);
            effect = null;
        }
    }
}
