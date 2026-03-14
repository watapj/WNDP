using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    public sealed class WorldBuildContext
    {
        private readonly List<WorldPassMarker> _markers = new List<WorldPassMarker>();
        private readonly List<Action> _cleanupActions = new List<Action>();

        internal WorldBuildContext(
            WorldBuildSession session,
            Scene scene,
            BuildTarget effectiveBuildTarget,
            WorldBuildExecutionReport executionReport)
        {
            SessionId = session.SessionId;
            RequestedBuildType = session.RequestedBuildType;
            SessionKind = session.Kind;
            BuildTarget = effectiveBuildTarget;
            TargetPlatformMask = WorldBuildPlatformUtility.GetPlatformMask(effectiveBuildTarget);
            SourceScenePath = session.SourceScenePath;
            Scene = scene;
            ExecutionReport = executionReport;
            TransientAssets = new WorldTransientAssets(executionReport);
            TransientObjects = new WorldTransientObjects(scene);
            PassContext = new WorldPassContext(this, TransientAssets, TransientObjects);
        }

        public string SessionId { get; }

        public VRCSDKRequestedBuildType RequestedBuildType { get; }

        public WorldSessionKind SessionKind { get; }

        public UnityEditor.BuildTarget BuildTarget { get; }

        public WorldBuildPlatformMask TargetPlatformMask { get; }

        public string SourceScenePath { get; }

        public Scene Scene { get; }

        public WorldBuildExecutionReport ExecutionReport { get; }

        public WorldTransientAssets TransientAssets { get; }

        public WorldTransientObjects TransientObjects { get; }

        public WorldPassContext PassContext { get; }

        public IReadOnlyList<WorldPassMarker> Markers => _markers;

        internal void RefreshMarkers()
        {
            _markers.Clear();

            if (!Scene.IsValid())
            {
                return;
            }

            foreach (var rootGameObject in Scene.GetRootGameObjects())
            {
                _markers.AddRange(rootGameObject.GetComponentsInChildren<WorldPassMarker>(true));
            }
        }

        public IEnumerable<TMarker> GetMarkers<TMarker>() where TMarker : WorldPassMarker
        {
            return _markers.OfType<TMarker>();
        }

        public void RegisterCleanup(Action cleanupAction)
        {
            if (cleanupAction == null)
            {
                throw new ArgumentNullException(nameof(cleanupAction));
            }

            _cleanupActions.Add(cleanupAction);
        }

        public void DestroyMarkerAfterProcessing(WorldPassMarker marker)
        {
            if (marker == null || !marker.DestroyAfterProcessing)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(marker);
        }

        internal void RunCleanupActions()
        {
            Exception firstException = null;

            for (var index = _cleanupActions.Count - 1; index >= 0; index--)
            {
                try
                {
                    _cleanupActions[index].Invoke();
                }
                catch (Exception exception)
                {
                    firstException ??= exception;
                }
            }

            _cleanupActions.Clear();

            if (firstException != null)
            {
                throw firstException;
            }
        }
    }
}
