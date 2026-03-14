using System;

namespace WataOfuton.Tool.WNDP.Editor
{
    [Flags]
    public enum WorldBuildPassTargets
    {
        None = 0,
        Build = 1 << 0,
        Play = 1 << 1,
        BuildAndPlay = Build | Play
    }
}
