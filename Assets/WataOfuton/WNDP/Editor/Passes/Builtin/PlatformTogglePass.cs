using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    [WorldBuildPass(WorldBuildPhase.PlatformInit, 0, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class PlatformTogglePass : WorldMarkerPass<PlatformToggleMarker>
    {
        public override string DisplayName => "Platform Toggle";

        protected override void ExecuteMarker(WorldPassContext context, PlatformToggleMarker marker)
        {
            marker.gameObject.SetActive(marker.ShouldBeActive(TargetPlatform(context)));
        }
    }
}
