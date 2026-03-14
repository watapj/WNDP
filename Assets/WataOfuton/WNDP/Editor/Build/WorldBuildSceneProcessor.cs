using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    /// <summary>
    /// Build / Play 両方の Scene 前処理エントリポイント。
    /// Build 時は <see cref="WataWorldBuildRequestedCallback"/> が事前に session を登録し、
    /// Play 時は Unity が内部生成した Scene コピーに対して自動的に呼ばれる。
    /// </summary>
    public sealed class WorldBuildSceneProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (TryProcessBuildSession(scene, report))
            {
                return;
            }

            TryProcessPlaySession(scene, report);
        }

        /// <summary>
        /// VRChat SDK Build 経由で <see cref="WorldBuildSessionState"/> に session が登録されている場合の処理。
        /// </summary>
        private static bool TryProcessBuildSession(Scene scene, BuildReport report)
        {
            if (!WorldBuildSessionState.TryGetActive(out var session))
            {
                return false;
            }

            if (session.RequestedBuildType != VRCSDKRequestedBuildType.Scene)
            {
                return false;
            }

            if (!session.TryMarkSceneProcessed())
            {
                return false;
            }

            try
            {
                WorldBuildOrchestrator.ProcessScene(session, scene, report);
            }
            finally
            {
                WorldBuildSessionState.Clear();
            }

            return true;
        }

        /// <summary>
        /// Play Mode 進入時に Unity が呼ぶ <c>OnProcessScene</c> を検出し、
        /// Scene 内に <see cref="WorldPassMarker"/> が存在すれば Play session として処理する。
        /// </summary>
        private static void TryProcessPlaySession(Scene scene, BuildReport report)
        {
            // 実ビルド（BuildReport あり）は Build path で処理されるべきなので skip
            if (report != null)
            {
                return;
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // Build 側と同じ single-scene 制約を適用する
            if (SceneManager.sceneCount != 1)
            {
                return;
            }

            if (!HasMarkersInScene(scene))
            {
                return;
            }

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (!WorldBuildPlatformUtility.IsSupported(buildTarget))
            {
                return;
            }

            var session = new WorldBuildSession(
                Guid.NewGuid().ToString("N"),
                VRCSDKRequestedBuildType.Scene,
                buildTarget,
                scene.path,
                WorldSessionKind.Play);

            Debug.Log($"[WNDP] Play session detected. Processing scene '{scene.name}' ({buildTarget}).");

            WorldBuildOrchestrator.ProcessScene(session, scene, null);
        }

        private static bool HasMarkersInScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                if (rootGameObject.GetComponentInChildren<WorldPassMarker>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
