using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BigDreamLab
{
    public class SceneLoader : MonoBehaviour
    {
        public string environmentName;
        public string actionName;
        public bool loadAtStart;

        bool m_IsLoading;

        void Start()
        {
            if (loadAtStart && Application.isPlaying)
                LoadEnvironmentAndAction();
        }

        public void LoadEnvironmentAndAction()
        {
            if (!Application.isPlaying || m_IsLoading)
                return;

            StartCoroutine(LoadEnvironmentAndActionRoutine());
        }

        IEnumerator LoadEnvironmentAndActionRoutine()
        {
            m_IsLoading = true;

            var scenesToLoad = new List<SceneLoadTarget>();
            TryAddScene(environmentName, scenesToLoad);
            TryAddScene(actionName, scenesToLoad);

            if (scenesToLoad.Count == 0)
            {
                Debug.LogWarning($"{nameof(SceneLoader)} on {name} does not have any scenes configured.", this);
                m_IsLoading = false;
                yield break;
            }

            var loadMode = LoadSceneMode.Single;
            foreach (var sceneTarget in scenesToLoad)
            {
                if (!sceneTarget.isResolvable)
                {
                    Debug.LogError($"{nameof(SceneLoader)} could not find scene '{sceneTarget.sceneName}'.", this);
                    continue;
                }

                AsyncOperation loadOperation = null;

#if UNITY_EDITOR
                if (!sceneTarget.inBuildSettings && !string.IsNullOrEmpty(sceneTarget.scenePath))
                {
                    loadOperation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                        sceneTarget.scenePath,
                        new LoadSceneParameters(loadMode));
                }
                else
#endif
                {
                    loadOperation = SceneManager.LoadSceneAsync(sceneTarget.sceneName, loadMode);
                }

                if (loadOperation == null)
                {
                    Debug.LogError($"{nameof(SceneLoader)} failed to start loading scene '{sceneTarget.sceneName}'.", this);
                    continue;
                }

                while (!loadOperation.isDone)
                    yield return null;

                loadMode = LoadSceneMode.Additive;
            }

            m_IsLoading = false;
        }

        void TryAddScene(string sceneName, List<SceneLoadTarget> scenesToLoad)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            foreach (var existingTarget in scenesToLoad)
            {
                if (string.Equals(existingTarget.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            scenesToLoad.Add(ResolveScene(sceneName));
        }

        SceneLoadTarget ResolveScene(string sceneName)
        {
            var sceneTarget = new SceneLoadTarget
            {
                sceneName = sceneName,
            };

            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(Path.GetFileNameWithoutExtension(scenePath), sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    sceneTarget.scenePath = scenePath;
                    sceneTarget.inBuildSettings = true;
                    sceneTarget.isResolvable = true;
                    return sceneTarget;
                }
            }

#if UNITY_EDITOR
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (var sceneGuid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                if (string.Equals(Path.GetFileNameWithoutExtension(scenePath), sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    sceneTarget.scenePath = scenePath;
                    sceneTarget.isResolvable = true;
                    return sceneTarget;
                }
            }
#endif

            return sceneTarget;
        }

        struct SceneLoadTarget
        {
            public string sceneName;
            public string scenePath;
            public bool inBuildSettings;
            public bool isResolvable;
        }
    }
}
