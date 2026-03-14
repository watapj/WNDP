using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;

namespace WataOfuton.Tool.WNDP.Editor
{
    public sealed class WataWorldBuildRequestedCallback : IVRCSDKBuildRequestedCallback, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            WorldBuildSessionState.Clear();

            if (requestedBuildType != VRCSDKRequestedBuildType.Scene)
            {
                return true;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!WorldBuildSessionState.TryBegin(
                    requestedBuildType,
                    EditorUserBuildSettings.activeBuildTarget,
                    activeScene.path,
                    out var session,
                    out var errorMessage))
            {
                Debug.LogError($"[WNDP] {errorMessage}");
                return false;
            }

            if (!WorldBuildOrchestrator.RunPreflight(session, out errorMessage))
            {
                Debug.LogError($"[WNDP] {errorMessage}");
                WorldBuildSessionState.Clear();
                return false;
            }

            Debug.Log($"[WNDP] Build session started: {session.SessionId}");
            return true;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            WorldBuildSessionState.Clear();
        }
    }
}
