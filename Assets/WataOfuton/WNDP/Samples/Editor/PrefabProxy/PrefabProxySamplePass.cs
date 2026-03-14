using UnityEditor;
using UnityEngine;
using WataOfuton.Tool.WNDP;
using WataOfuton.Tool.WNDP.Editor;

namespace WataOfuton.Tool.WNDP.Samples.Editor
{
    [WorldBuildPass(WorldBuildPhase.Generating, 80, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class PrefabProxySamplePass : WorldMarkerPass<PrefabProxySampleMarker>
    {
        public override string DisplayName => "Prefab Proxy Sample";

        protected override void ValidateMarker(
            WorldPassContext context,
            PrefabProxySampleMarker marker,
            IWorldValidationSink sink)
        {
            if (marker.PrefabAsset == null)
            {
                sink.Error("PrefabProxySampleMarker requires a prefab asset.", marker);
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(marker.PrefabAsset))
            {
                sink.Error("PrefabProxySampleMarker only supports prefab assets.", marker);
            }

            if (marker.ParentUnderMarker && marker.SourceHandling != PrefabProxySampleMarker.SourceHandlingMode.Keep)
            {
                sink.Error(
                    "PrefabProxySampleMarker requires Source Handling to remain Keep when the proxy is parented under the source object.",
                    marker);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, PrefabProxySampleMarker marker)
        {
            var targetObject = marker.TargetObject;
            if (targetObject == null || marker.PrefabAsset == null)
            {
                return;
            }

            var existingGenerated = FindExistingGeneratedObject(targetObject, marker);
            if (existingGenerated != null)
            {
                DestroyObject(context, existingGenerated);
            }

            var parent = marker.ParentUnderMarker ? targetObject.transform : targetObject.transform.parent;
            var proxyObject = InstantiatePrefab(context, marker.PrefabAsset, parent, marker.GeneratedObjectName);
            if (proxyObject == null)
            {
                return;
            }

            var proxyTransform = proxyObject.transform;
            if (marker.ParentUnderMarker)
            {
                ResetLocalTransform(context, proxyTransform);
            }
            else
            {
                CopyTransform(context, targetObject.transform, proxyTransform);
            }

            switch (marker.SourceHandling)
            {
                case PrefabProxySampleMarker.SourceHandlingMode.Disable:
                    SetObjectActive(context, targetObject, false);
                    break;

                case PrefabProxySampleMarker.SourceHandlingMode.Destroy:
                    DestroyObject(context, targetObject);
                    break;
            }

            Debug.Log(
                $"[WNDP Samples] Instantiated prefab proxy '{proxyObject.name}' for '{targetObject.name}' in {CurrentSessionKind(context)} session '{CurrentSessionId(context)}'.");
        }

        private static GameObject FindExistingGeneratedObject(
            GameObject targetObject,
            PrefabProxySampleMarker marker)
        {
            var parent = marker.ParentUnderMarker ? targetObject.transform : targetObject.transform.parent;
            if (parent != null)
            {
                var existingChild = parent.Find(marker.GeneratedObjectName);
                return existingChild != null ? existingChild.gameObject : null;
            }

            foreach (var rootObject in targetObject.scene.GetRootGameObjects())
            {
                if (rootObject != null && rootObject != targetObject && rootObject.name == marker.GeneratedObjectName)
                {
                    return rootObject;
                }
            }

            return null;
        }
    }
}
