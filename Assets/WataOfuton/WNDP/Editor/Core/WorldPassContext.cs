using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    public sealed class WorldPassContext
    {
        private readonly WorldBuildContext _buildContext;

        internal WorldPassContext(
            WorldBuildContext buildContext,
            WorldTransientAssets transientAssets,
            WorldTransientObjects transientObjects)
        {
            _buildContext = buildContext ?? throw new ArgumentNullException(nameof(buildContext));
            TransientAssets = transientAssets ?? throw new ArgumentNullException(nameof(transientAssets));
            TransientObjects = transientObjects ?? throw new ArgumentNullException(nameof(transientObjects));
        }

        public string SessionId => _buildContext.SessionId;

        public WorldSessionKind SessionKind => _buildContext.SessionKind;

        public VRCSDKRequestedBuildType RequestedBuildType => _buildContext.RequestedBuildType;

        public UnityEditor.BuildTarget BuildTarget => _buildContext.BuildTarget;

        public WorldBuildPlatformMask TargetPlatformMask => _buildContext.TargetPlatformMask;

        public string SourceScenePath => _buildContext.SourceScenePath;

        public Scene Scene => _buildContext.Scene;

        public WorldBuildExecutionReport ExecutionReport => _buildContext.ExecutionReport;

        public WorldTransientAssets TransientAssets { get; }

        public WorldTransientObjects TransientObjects { get; }

        public IEnumerable<TMarker> GetMarkers<TMarker>() where TMarker : WorldPassMarker
        {
            return _buildContext.GetMarkers<TMarker>();
        }

        public void RegisterCleanup(Action cleanupAction)
        {
            _buildContext.RegisterCleanup(cleanupAction);
        }

        public void DestroyMarkerAfterProcessing(WorldPassMarker marker)
        {
            _buildContext.DestroyMarkerAfterProcessing(marker);
        }
    }
}
