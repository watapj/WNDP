using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    [WorldBuildPass(WorldBuildPhase.PlatformFinish, 1000, Targets = WorldBuildPassTargets.Build)]
    public sealed class StripFromBuildPass : WorldMarkerPass<StripFromBuildMarker>
    {
        public override string DisplayName => "Strip From Build";

        protected override System.Collections.Generic.IEnumerable<StripFromBuildMarker> EnumerateMarkers(
            WorldBuildContext buildContext,
            WorldPassContext passContext)
        {
            return base.EnumerateMarkers(buildContext, passContext)
                .OrderByDescending(marker => GetDepth(marker.gameObject));
        }

        protected override bool ShouldDestroyMarkerAfterExecute(WorldPassContext context, StripFromBuildMarker marker)
        {
            return false;
        }

        protected override void ExecuteMarker(WorldPassContext context, StripFromBuildMarker marker)
        {
            if (marker != null && marker.gameObject != null)
            {
                Object.DestroyImmediate(marker.gameObject);
            }
        }

        private static int GetDepth(GameObject gameObject)
        {
            var depth = 0;
            var current = gameObject.transform;

            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }
    }
}
