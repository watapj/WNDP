using System;

namespace WataOfuton.Tool.WNDP
{
    [Flags]
    public enum WorldBuildPlatformMask
    {
        None = 0,
        StandaloneWindows = 1 << 0,
        Android = 1 << 1,
        All = StandaloneWindows | Android
    }
}
