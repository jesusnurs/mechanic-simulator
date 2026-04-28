using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigDreamLab
{
    public class SocketTask : MonoBehaviour
    {
        public enum DisablingType
        {
            BehaviourOnly = 0,
            GameObject = 1,
        }

        public bool logMessages;
        public UnityEngine.Object socket;
        public bool disableOutline;
        public bool disableInteractableColliders;
        public DisablingType disablingType;

        readonly List<Collider> m_TargetColliders = new List<Collider>();
        readonly List<bool> m_InitialColliderStates = new List<bool>();
        readonly List<Behaviour> m_OutlineBehaviours = new List<Behaviour>();
        readonly List<bool> m_InitialOutlineStates = new List<bool>();

        bool m_CacheBuilt;
        bool m_InitialSocketBehaviourState = true;
        bool m_InitialTargetActiveState = true;

        void Awake()
        {
            CacheTargets();
            DisableInteractable();
        }

        void OnValidate()
        {
            m_CacheBuilt = false;
        }

        public void EnableInteractable()
        {
            SetInteractableState(true);
        }

        public void DisableInteractable()
        {
            SetInteractableState(false);
        }

        public void ToggleInteractable()
        {
            var targetBehaviour = GetSocketBehaviour();
            if (targetBehaviour != null)
            {
                SetInteractableState(!targetBehaviour.enabled);
                return;
            }

            var targetGameObject = GetTargetGameObject();
            if (targetGameObject != null)
                SetInteractableState(!targetGameObject.activeSelf);
        }

        void CacheTargets()
        {
            if (m_CacheBuilt)
                return;

            m_CacheBuilt = true;
            m_TargetColliders.Clear();
            m_InitialColliderStates.Clear();
            m_OutlineBehaviours.Clear();
            m_InitialOutlineStates.Clear();

            var targetGameObject = GetTargetGameObject();
            var targetBehaviour = GetSocketBehaviour();

            if (targetBehaviour != null)
                m_InitialSocketBehaviourState = targetBehaviour.enabled;

            if (targetGameObject != null)
                m_InitialTargetActiveState = targetGameObject.activeSelf;

            if (disableInteractableColliders && targetGameObject != null)
            {
                var colliders = targetGameObject.GetComponentsInChildren<Collider>(true);
                foreach (var targetCollider in colliders)
                {
                    m_TargetColliders.Add(targetCollider);
                    m_InitialColliderStates.Add(targetCollider.enabled);
                }
            }

            if (disableOutline && targetGameObject != null)
            {
                var behaviours = targetGameObject.GetComponentsInChildren<Behaviour>(true);
                foreach (var behaviour in behaviours)
                {
                    if (!IsOutlineBehaviour(behaviour))
                        continue;

                    m_OutlineBehaviours.Add(behaviour);
                    m_InitialOutlineStates.Add(behaviour.enabled);
                }
            }
        }

        void SetInteractableState(bool enabled)
        {
            CacheTargets();

            var targetGameObject = GetTargetGameObject();
            var targetBehaviour = GetSocketBehaviour();
            var canToggleGameObject = targetGameObject != null && targetGameObject != gameObject;

            if (disablingType == DisablingType.GameObject && canToggleGameObject)
            {
                targetGameObject.SetActive(enabled && m_InitialTargetActiveState);
            }
            else if (targetBehaviour != null)
            {
                targetBehaviour.enabled = enabled && m_InitialSocketBehaviourState;
            }

            for (var i = 0; i < m_TargetColliders.Count; i++)
            {
                var targetCollider = m_TargetColliders[i];
                if (targetCollider == null)
                    continue;

                targetCollider.enabled = enabled && m_InitialColliderStates[i];
            }

            for (var i = 0; i < m_OutlineBehaviours.Count; i++)
            {
                var outlineBehaviour = m_OutlineBehaviours[i];
                if (outlineBehaviour == null)
                    continue;

                outlineBehaviour.enabled = enabled && m_InitialOutlineStates[i];
            }

            if (logMessages)
                Debug.Log($"{nameof(SocketTask)} on {name} set interactable state to {(enabled ? "enabled" : "disabled")}.", this);
        }

        GameObject GetTargetGameObject()
        {
            if (socket is GameObject targetGameObject)
                return targetGameObject;

            if (socket is Component targetComponent)
                return targetComponent.gameObject;

            return gameObject;
        }

        Behaviour GetSocketBehaviour()
        {
            return socket as Behaviour;
        }

        static bool IsOutlineBehaviour(Behaviour behaviour)
        {
            return behaviour != null &&
                behaviour.GetType().Name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
