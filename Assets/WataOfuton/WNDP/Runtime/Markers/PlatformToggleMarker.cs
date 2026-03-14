using UnityEngine;

namespace WataOfuton.Tool.WNDP
{
    public sealed class PlatformToggleMarker : WorldPassMarker
    {
        [SerializeField]
        private WorldBuildPlatformMask _activePlatforms = WorldBuildPlatformMask.All;

        public WorldBuildPlatformMask ActivePlatforms => _activePlatforms;

        public bool ShouldBeActive(WorldBuildPlatformMask currentPlatform)
        {
            return (_activePlatforms & currentPlatform) != 0;
        }
    }
}
