using System;
using System.Collections;
using Backrooms.Core;
using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// One lamp + one target. The lamp shows the sequence; the target is what the player shoots.
    /// The station does not know the rules of the puzzle. It only displays things and reports hits.
    /// </summary>
    public class SimonStation : MonoBehaviour, IShootable
    {
        [Header("Parts (drag in from this prefab's children)")]
        [SerializeField] private Renderer lampRenderer;
        [SerializeField] private Light lampLight;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private AudioSource audioSource;

        [Header("Look")]
        [SerializeField] private float litLightIntensity = 3f;
        [SerializeField] private float emissionStrength = 4f;
        [Range(0f, 1f)] [SerializeField] private float offBrightness = 0.25f;
        [SerializeField] private float confirmDuration = 0.2f;

        public event Action<int> Shot;

        public int Id { get; private set; } = -1;
        public Color Colour { get; private set; } = Color.white;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private Material lampMaterial;
        private AudioClip tone;
        private Coroutine effect;

        private Color DimColour => new Color(Colour.r * offBrightness, Colour.g * offBrightness, Colour.b * offBrightness, 1f);

        private void Awake()
        {
            lampMaterial = lampRenderer.material;      // .material = a copy for this station only
            lampMaterial.EnableKeyword("_EMISSION");
        }

        /// <summary>Called once by SimonPuzzle. Ids and colours come from code, not typed per prefab.</summary>
        public void Setup(int id, Color colour, AudioClip toneClip)
        {
            Id = id;
            Colour = colour;
            tone = toneClip;

            if (targetRenderer != null)
                targetRenderer.material.color = Color.Lerp(Color.white, colour, 0.4f);

            SetLit(false);
        }

        public void OnShot(RaycastHit hit)
        {
            Shot?.Invoke(Id);
        }

        /// <summary>Lamp fully on or dim, with no sound.</summary>
        public void SetLit(bool lit)
        {
            StopEffect();
            ApplyLamp(lit ? Colour : DimColour, lit);
        }

        /// <summary>Lamp on + this station's tone. Used while showing the sequence.</summary>
        public void Flash()
        {
            SetLit(true);
            PlayTone();
        }

        /// <summary>Short blink + tone after a correct shot.</summary>
        public void Confirm()
        {
            StopEffect();
            PlayTone();
            effect = StartCoroutine(BlinkRoutine(Colour, confirmDuration));
        }

        /// <summary>Flickers in the fail colour.</summary>
        public void FailFlicker(Color failColour, int flickers = 3, float interval = 0.12f)
        {
            StopEffect();
            effect = StartCoroutine(FlickerRoutine(failColour, flickers, interval));
        }

        private void PlayTone()
        {
            if (audioSource != null && tone != null) audioSource.PlayOneShot(tone);
        }

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

        private IEnumerator BlinkRoutine(Color colour, float duration)
        {
            ApplyLamp(colour, true);
            yield return new WaitForSeconds(duration);
            ApplyLamp(DimColour, false);
            effect = null;
        }

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

        private void StopEffect()
        {
            if (effect == null) return;
            StopCoroutine(effect);
            effect = null;
        }
    }
}
