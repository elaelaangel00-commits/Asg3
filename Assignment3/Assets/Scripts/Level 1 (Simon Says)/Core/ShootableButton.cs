using System;
using UnityEngine;

namespace Backrooms.Core
{
    // a button the player shoots (the START button).
    // glows green when it can be used, goes dark when locked.
    public class ShootableButton : MonoBehaviour, IShootable
    {
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Color readyColour = new Color(0.2f, 1f, 0.3f);
        [SerializeField] private Color lockedColour = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private float emissionStrength = 2f;

        public event Action Shot;   // the puzzle listens to this
        public bool Interactable { get; private set; } = true;

        // looking up a shader property by id is faster than by name every time
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private Material material;

        private void Awake()
        {
            material = buttonRenderer.material;       // .material = a copy just for this button
            material.EnableKeyword("_EMISSION");      // allow it to glow
            Apply();
        }

        public void SetInteractable(bool value)
        {
            Interactable = value;
            Apply();
        }

        // called by PlayerShooter when this button gets hit
        public void OnShot(RaycastHit hit)
        {
            if (Interactable) Shot?.Invoke();   // only counts when unlocked
        }

        // update the colour and glow to match the state
        private void Apply()
        {
            if (material == null) return;
            Color colour = Interactable ? readyColour : lockedColour;
            material.color = colour;
            material.SetColor(EmissionColorId, Interactable ? colour * emissionStrength : Color.black);
        }
    }
}
