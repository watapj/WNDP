using UnityEditor;
using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    [WorldBuildPass(WorldBuildPhase.Generating, 0, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class BuildOnlyObjectPass : WorldMarkerPass<BuildOnlyObjectMarker>
    {
        public override string DisplayName => "Build Only Object";

        protected override void ValidateMarker(WorldPassContext context, BuildOnlyObjectMarker marker, IWorldValidationSink sink)
        {
            if (marker.Prefab == null)
            {
                sink.Error("BuildOnlyObjectMarker requires a prefab asset.", marker);
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(marker.Prefab))
            {
                sink.Error("BuildOnlyObjectMarker only supports prefab assets.", marker);
            }

            if (marker.ParentUnderMarker && marker.DestroyAnchorAfterInstantiation)
            {
                sink.Error(
                    "BuildOnlyObjectMarker cannot destroy its anchor when the prefab is parented under that anchor.",
                    marker);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, BuildOnlyObjectMarker marker)
        {
            if (marker.Prefab == null)
            {
                return;
            }

            var parent = marker.ParentUnderMarker ? marker.transform : marker.transform.parent;
            var instance = InstantiatePrefab(context, marker.Prefab, parent, marker.Prefab.name);
            if (instance == null)
            {
                return;
            }

            var instanceTransform = instance.transform;
            if (marker.ParentUnderMarker)
            {
                ResetLocalTransform(context, instanceTransform);
            }
            else
            {
                CopyTransform(context, marker.transform, instanceTransform);
            }

            if (marker.DestroyAnchorAfterInstantiation)
            {
                DestroyObject(context, marker.gameObject);
            }
        }

        protected override bool ShouldDestroyMarkerAfterExecute(WorldPassContext context, BuildOnlyObjectMarker marker)
        {
            return !marker.DestroyAnchorAfterInstantiation;
        }
    }
}
