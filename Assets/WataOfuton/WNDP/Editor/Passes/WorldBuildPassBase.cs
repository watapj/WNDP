using System;

namespace WataOfuton.Tool.WNDP.Editor
{
    public abstract class WorldBuildPassBase : IWorldBuildPass
    {
        private WorldBuildPassAttribute _attribute;

        public abstract string DisplayName { get; }

        public WorldBuildPhase Phase => GetAttribute().Phase;

        public int Order => GetAttribute().Order;

        public WorldBuildPassTargets Targets => GetAttribute().Targets;

        public virtual bool AppliesTo(WorldBuildContext context)
        {
            return true;
        }

        public virtual void Validate(WorldBuildContext context, IWorldValidationSink sink)
        {
        }

        public abstract void Execute(WorldBuildContext context);

        private WorldBuildPassAttribute GetAttribute()
        {
            if (_attribute == null)
            {
                _attribute = (WorldBuildPassAttribute)Attribute.GetCustomAttribute(
                    GetType(),
                    typeof(WorldBuildPassAttribute));
            }

            if (_attribute == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName} must be annotated with {nameof(WorldBuildPassAttribute)}.");
            }

            return _attribute;
        }
    }
}
