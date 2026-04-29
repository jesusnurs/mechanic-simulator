using System.Collections.Generic;
using UnityEngine;

namespace BigDreamLab.Interaction
{
    public sealed class SelectionOutline : MonoBehaviour
    {
        [SerializeField] string highlightLayerName = "Outline";
        [SerializeField] bool includeChildren = true;
        [SerializeField] Color hintColor = Color.yellow;

        readonly List<Transform> m_Targets = new List<Transform>();
        readonly List<int> m_DefaultLayers = new List<int>();

        static Material s_OutlineMaterial;
        static Color s_DefaultOutlineColor;
        static bool s_HasDefaultOutlineColor;
        static int s_ActiveHintCount;

        int m_HighlightLayer = -1;
        bool m_CacheBuilt;
        bool m_IsFocusHighlighted;
        bool m_IsHintHighlighted;
        bool m_IsLayerHighlighted;
        bool m_WarnedAboutMissingLayer;

        public bool IsHighlighted => m_IsFocusHighlighted || m_IsHintHighlighted;

        void Awake()
        {
            BuildCache();
        }

        void OnDisable()
        {
            ToggleHintOutline(false);
            ToggleOutline(false);
        }

        public void ToggleOutline(bool isHighlighted)
        {
            if (m_IsFocusHighlighted == isHighlighted)
                return;

            m_IsFocusHighlighted = isHighlighted;
            ApplyHighlightState();
        }

        public void ToggleHintOutline(bool isHighlighted)
        {
            if (m_IsHintHighlighted == isHighlighted)
                return;

            m_IsHintHighlighted = isHighlighted;
            s_ActiveHintCount += isHighlighted ? 1 : -1;
            if (s_ActiveHintCount < 0)
                s_ActiveHintCount = 0;

            ApplyOutlineColor(s_ActiveHintCount > 0 ? hintColor : (Color?)null);
            ApplyHighlightState();
        }

        void ApplyHighlightState()
        {
            BuildCache();
            if (m_HighlightLayer < 0)
            {
                WarnMissingLayer();
                return;
            }

            var shouldHighlight = IsHighlighted;
            if (m_IsLayerHighlighted == shouldHighlight)
                return;

            for (var i = 0; i < m_Targets.Count; i++)
            {
                var target = m_Targets[i];
                if (target == null)
                    continue;

                target.gameObject.layer = shouldHighlight ? m_HighlightLayer : m_DefaultLayers[i];
            }

            m_IsLayerHighlighted = shouldHighlight;
        }

        void BuildCache()
        {
            if (m_CacheBuilt)
                return;

            m_CacheBuilt = true;
            m_HighlightLayer = LayerMask.NameToLayer(highlightLayerName);
            m_Targets.Clear();
            m_DefaultLayers.Clear();

            if (includeChildren)
            {
                var childTransforms = GetComponentsInChildren<Transform>(true);
                foreach (var childTransform in childTransforms)
                    AddTarget(childTransform);
            }
            else
            {
                AddTarget(transform);
            }
        }

        void AddTarget(Transform target)
        {
            if (target == null)
                return;

            m_Targets.Add(target);
            m_DefaultLayers.Add(target.gameObject.layer);
        }

        void WarnMissingLayer()
        {
            if (m_WarnedAboutMissingLayer)
                return;

            m_WarnedAboutMissingLayer = true;
            Debug.LogWarning($"{nameof(SelectionOutline)} needs a Unity layer named '{highlightLayerName}'.", this);
        }

        static void ApplyOutlineColor(Color? color)
        {
            var outlineMaterial = GetOutlineMaterial();
            if (outlineMaterial == null || !outlineMaterial.HasProperty("_Color"))
                return;

            outlineMaterial.SetColor("_Color", color ?? s_DefaultOutlineColor);
        }

        static Material GetOutlineMaterial()
        {
            if (s_OutlineMaterial != null)
                return s_OutlineMaterial;

            var materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var material in materials)
            {
                if (material == null || material.name != "OutlineMaterial")
                    continue;

                s_OutlineMaterial = material;
                if (material.HasProperty("_Color"))
                {
                    s_DefaultOutlineColor = material.GetColor("_Color");
                    s_HasDefaultOutlineColor = true;
                }

                break;
            }

            if (!s_HasDefaultOutlineColor)
                s_DefaultOutlineColor = Color.white;

            return s_OutlineMaterial;
        }
    }
}
