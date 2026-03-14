using UnityEditor;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    internal static class WorldBuildPlatformUtility
    {
        public static WorldBuildPlatformMask GetPlatformMask(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return WorldBuildPlatformMask.Android;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return WorldBuildPlatformMask.StandaloneWindows;
                default:
                    return WorldBuildPlatformMask.None;
            }
        }

        public static bool IsSupported(BuildTarget buildTarget)
        {
            return GetPlatformMask(buildTarget) != WorldBuildPlatformMask.None;
        }
    }
}
