using System;

namespace WataOfuton.Tool.WNDP.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WorldBuildPassAttribute : Attribute
    {
        public WorldBuildPassAttribute(WorldBuildPhase phase, int order = 0)
        {
            Phase = phase;
            Order = order;
            Targets = WorldBuildPassTargets.BuildAndPlay;
        }

        public WorldBuildPhase Phase { get; }

        public int Order { get; }

        public WorldBuildPassTargets Targets { get; set; }
    }
}
