using System;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    internal sealed class WorldBuildSession
    {
        public WorldBuildSession(
            string sessionId,
            VRCSDKRequestedBuildType requestedBuildType,
            BuildTarget buildTarget,
            string sourceScenePath,
            WorldSessionKind kind)
        {
            SessionId = sessionId;
            RequestedBuildType = requestedBuildType;
            BuildTarget = buildTarget;
            SourceScenePath = sourceScenePath;
            Kind = kind;
            TargetPlatformMask = WorldBuildPlatformUtility.GetPlatformMask(buildTarget);
        }

        public string SessionId { get; }

        public WorldSessionKind Kind { get; }

        public VRCSDKRequestedBuildType RequestedBuildType { get; }

        public BuildTarget BuildTarget { get; }

        public string SourceScenePath { get; }

        public WorldBuildPlatformMask TargetPlatformMask { get; }

        public bool HasProcessedScene { get; private set; }

        public bool TryMarkSceneProcessed()
        {
            if (HasProcessedScene)
            {
                return false;
            }

            HasProcessedScene = true;
            return true;
        }
    }

    internal static class WorldBuildSessionState
    {
        private static WorldBuildSession _activeSession;

        public static bool TryBegin(
            VRCSDKRequestedBuildType requestedBuildType,
            BuildTarget buildTarget,
            string sourceScenePath,
            out WorldBuildSession session,
            out string errorMessage)
        {
            if (_activeSession != null)
            {
                session = null;
                errorMessage = "A build session is already active.";
                return false;
            }

            session = new WorldBuildSession(
                Guid.NewGuid().ToString("N"),
                requestedBuildType,
                buildTarget,
                sourceScenePath,
                WorldSessionKind.Build);

            _activeSession = session;
            errorMessage = null;
            return true;
        }

        public static bool TryGetActive(out WorldBuildSession session)
        {
            session = _activeSession;
            return session != null;
        }

        public static void Clear()
        {
            _activeSession = null;
        }
    }
}
