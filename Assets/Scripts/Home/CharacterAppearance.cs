using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Home
{
    /// <summary>
    /// On the character root (next to CharacterMotor). Holds the three models as children — each with its own Animator,
    /// because the models are Generic rigs and their clips bind by bone paths — and shows one of them with the picked
    /// colour texture and bow (one of Bow Colors, or none). Everyone who drives the character's animations asks <see cref="Animator"/> here
    /// instead of keeping their own reference.
    /// The colour goes through a MaterialPropertyBlock, not the materials: CharacterWorkingPresenter swaps the
    /// materials arrays (grey "at work" overlay) and would wipe a texture set on a material instance.
    /// </summary>
    public class CharacterAppearance : MonoBehaviour
    {
        [Serializable]
        public class ModelSlot
        {
            [Tooltip("Just for the inspector: \"Аксолотль\".")]
            public string title;
            [Tooltip("The model instance under the character root. Only the picked one stays active.")]
            public GameObject root;
            [Tooltip("The model's own Animator (on the model root). Controller = an Animator Override Controller of the shared one.")]
            public Animator animator;
            [Tooltip("Renderers that get the colour texture. Empty = every Renderer under Root except the accessories.")]
            public Renderer[] bodyRenderers = Array.Empty<Renderer>();
            [Tooltip("Colour options 1, 2, 3 — same order as on the creation screen. Empty element = the model's own texture.")]
            public Texture[] colorTextures = new Texture[3];
            [Tooltip("Placed by hand on the neck bone. Optional. Its colour comes from Bow Colors.")]
            public GameObject bow;
        }

        /// <summary>One bow option on the creation screen: texture × tint (white tint = the texture as is).</summary>
        [Serializable]
        public class BowColor
        {
            [Tooltip("Just for the inspector: \"Синяя\".")]
            public string title;
            public Texture texture;
            public Color tint = Color.white;
        }

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private ModelSlot[] models = Array.Empty<ModelSlot>();

        [Tooltip("Bow options 1, 2, 3 — same order as the bow cards on the creation screen. Shared by every model.")]
        [SerializeField] private BowColor[] bowColors = Array.Empty<BowColor>();

        [Tooltip("Parts of the old placeholder model — switched off at start.")]
        [SerializeField] private GameObject[] hideObjects = Array.Empty<GameObject>();

        private readonly Dictionary<ModelSlot, Renderer[]> _bodyRenderers = new Dictionary<ModelSlot, Renderer[]>();
        private MaterialPropertyBlock _block;
        private int _current;

        public int ModelCount => models.Length;

        /// <summary>The Animator of the model shown right now (null if the slot has none).</summary>
        public Animator Animator => _current < models.Length ? models[_current].animator : null;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();

            foreach (GameObject go in hideObjects)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
            }

            foreach (ModelSlot slot in models)
            {
                _bodyRenderers[slot] = CollectBodyRenderers(slot);
            }

            // Only one model visible from the very first frame; the saved look comes in Start.
            Show(0, 0, CharacterAccessory.None, 0);
        }

        private void Start()
        {
            EventBus.Subscribe<CharacterChangedEvent>(OnCharacterChanged);
            ShowSaved();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterChangedEvent>(OnCharacterChanged);
        }

        private void OnCharacterChanged(CharacterChangedEvent e) => ShowSaved();

        private void ShowSaved()
        {
            PlayerDataService data = ServiceLocator.Get<PlayerDataService>();
            if (data.IsCharacterCreated)
            {
                Show(data.CharacterModel, data.CharacterColor, data.CharacterAccessory, data.CharacterBowColor);
            }
        }

        public int BowColorCount => bowColors.Length;

        /// <summary>Used for the live preview during creation and for the saved look. Out-of-range indices are clamped.</summary>
        public void Show(int model, int color, CharacterAccessory accessory, int bowColor)
        {
            if (models.Length == 0)
            {
                return;
            }

            int next = Mathf.Clamp(model, 0, models.Length - 1);
            List<(int hash, bool value)> bools = next != _current ? ReadBools(Animator) : null;
            _current = next;

            for (int i = 0; i < models.Length; i++)
            {
                if (models[i].root != null)
                {
                    models[i].root.SetActive(i == _current);
                }
            }

            ModelSlot slot = models[_current];
            SetActive(slot.bow, accessory == CharacterAccessory.Bow);
            ApplyBowColor(slot, bowColor);
            ApplyColor(slot, color);

            WriteBools(Animator, bools);
        }

        private void ApplyColor(ModelSlot slot, int color)
        {
            Texture texture = slot.colorTextures != null && color >= 0 && color < slot.colorTextures.Length
                ? slot.colorTextures[color]
                : null;

            foreach (Renderer r in _bodyRenderers[slot])
            {
                if (r == null)
                {
                    continue;
                }

                if (texture == null)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }

                _block.Clear();
                // URP Lit reads _BaseMap, older/other shaders _MainTex; setting a property a shader lacks is harmless.
                _block.SetTexture(BaseMapId, texture);
                _block.SetTexture(MainTexId, texture);
                r.SetPropertyBlock(_block);
            }
        }

        private void ApplyBowColor(ModelSlot slot, int bowColor)
        {
            if (slot.bow == null || bowColors.Length == 0)
            {
                return;
            }

            BowColor option = bowColors[Mathf.Clamp(bowColor, 0, bowColors.Length - 1)];
            foreach (Renderer r in slot.bow.GetComponentsInChildren<Renderer>(true))
            {
                _block.Clear();
                if (option.texture != null)
                {
                    _block.SetTexture(BaseMapId, option.texture);
                    _block.SetTexture(MainTexId, option.texture);
                }

                _block.SetColor(BaseColorId, option.tint);
                _block.SetColor(ColorId, option.tint);
                r.SetPropertyBlock(_block);
            }
        }

        private static Renderer[] CollectBodyRenderers(ModelSlot slot)
        {
            if (slot.bodyRenderers != null && slot.bodyRenderers.Length > 0)
            {
                return slot.bodyRenderers;
            }

            if (slot.root == null)
            {
                return Array.Empty<Renderer>();
            }

            var result = new List<Renderer>();
            foreach (Renderer r in slot.root.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsUnder(r.transform, slot.bow))
                {
                    result.Add(r);
                }
            }

            return result.ToArray();
        }

        private static bool IsUnder(Transform t, GameObject accessory) => accessory != null && t.IsChildOf(accessory.transform);

        /// <summary>Keeps "is dancing / talking" when the model is swapped mid-state (e.g. debug reset while the boombox plays). Read before the old model is switched off.</summary>
        private static List<(int hash, bool value)> ReadBools(Animator animator)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            {
                return null;
            }

            var result = new List<(int hash, bool value)>();
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool)
                {
                    result.Add((p.nameHash, animator.GetBool(p.nameHash)));
                }
            }

            return result;
        }

        private static void WriteBools(Animator animator, List<(int hash, bool value)> bools)
        {
            if (bools == null || animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                foreach ((int hash, bool value) in bools)
                {
                    if (p.nameHash == hash)
                    {
                        animator.SetBool(hash, value);
                    }
                }
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
