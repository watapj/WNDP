namespace WataOfuton.Tool.WNDP.Editor
{
    public interface IWorldBuildPass
    {
        string DisplayName { get; }

        WorldBuildPhase Phase { get; }

        int Order { get; }

        WorldBuildPassTargets Targets { get; }

        bool AppliesTo(WorldBuildContext context);

        void Validate(WorldBuildContext context, IWorldValidationSink sink);

        void Execute(WorldBuildContext context);
    }
}
