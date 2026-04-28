using System.Collections.Generic;
using UnityEngine;

namespace BigDreamLab.Interaction
{
    public sealed class SelectionOutline : MonoBehaviour
    {
        [SerializeField] string highlightLayerName = "Outline";
        [SerializeField] bool includeChildren = true;

        readonly List<Transform> m_Targets = new List<Transform>();
        readonly List<int> m_DefaultLayers = new List<int>();

        int m_HighlightLayer = -1;
        bool m_CacheBuilt;
        bool m_IsHighlighted;
        bool m_WarnedAboutMissingLayer;

        public bool IsHighlighted => m_IsHighlighted;

        void Awake()
        {
            BuildCache();
        }

        void OnDisable()
        {
            ToggleOutline(false);
        }

        public void ToggleOutline(bool isHighlighted)
        {
            if (m_IsHighlighted == isHighlighted)
                return;

            BuildCache();
            if (m_HighlightLayer < 0)
            {
                WarnMissingLayer();
                return;
            }

            for (var i = 0; i < m_Targets.Count; i++)
            {
                var target = m_Targets[i];
                if (target == null)
                    continue;

                target.gameObject.layer = isHighlighted ? m_HighlightLayer : m_DefaultLayers[i];
            }

            m_IsHighlighted = isHighlighted;
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
    }
}
