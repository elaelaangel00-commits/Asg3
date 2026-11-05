using System;
using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// A button the player shoots (used as the Simon Says START button).
    /// Glows when it can be used, goes dark when locked.
    /// </summary>
    public class ShootableButton : MonoBehaviour, IShootable
    {
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Color readyColour = new Color(0.2f, 1f, 0.3f);
        [SerializeField] private Color lockedColour = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private float emissionStrength = 2f;

        public event Action Shot;
        public bool Interactable { get; private set; } = true;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private Material material;

        private void Awake()
        {
            material = buttonRenderer.material;       // copy of the material for this button only
            material.EnableKeyword("_EMISSION");
            Apply();
        }

        public void SetInteractable(bool value)
        {
            Interactable = value;
            Apply();
        }

        public void OnShot(RaycastHit hit)
        {
            if (Interactable) Shot?.Invoke();
        }

        private void Apply()
        {
            if (material == null) return;
            Color colour = Interactable ? readyColour : lockedColour;
            material.color = colour;
            material.SetColor(EmissionColorId, Interactable ? colour * emissionStrength : Color.black);
        }
    }
}
