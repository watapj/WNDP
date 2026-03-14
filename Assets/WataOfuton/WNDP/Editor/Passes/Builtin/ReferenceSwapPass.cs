using UnityEditor;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    [WorldBuildPass(WorldBuildPhase.Resolving, 100, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class ReferenceSwapPass : WorldMarkerPass<ReferenceSwapMarker>
    {
        public override string DisplayName => "Reference Swap";

        protected override void ValidateMarker(WorldPassContext context, ReferenceSwapMarker marker, IWorldValidationSink sink)
        {
            if (marker.TargetComponent == null)
            {
                sink.Error("ReferenceSwapMarker requires a target component.", marker);
                return;
            }

            if (string.IsNullOrWhiteSpace(marker.PropertyPath))
            {
                sink.Error("ReferenceSwapMarker requires a serialized property path.", marker);
                return;
            }

            var serializedObject = new SerializedObject(marker.TargetComponent);
            var property = serializedObject.FindProperty(marker.PropertyPath);
            if (property == null)
            {
                sink.Error(
                    $"ReferenceSwapMarker could not find property '{marker.PropertyPath}' on '{marker.TargetComponent.GetType().Name}'.",
                    marker);
                return;
            }

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                sink.Error(
                    $"ReferenceSwapMarker property '{marker.PropertyPath}' must be an object reference.",
                    marker);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, ReferenceSwapMarker marker)
        {
            if (marker.TargetComponent == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(marker.TargetComponent);
            var property = serializedObject.FindProperty(marker.PropertyPath);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return;
            }

            property.objectReferenceValue = marker.GetReplacement(TargetPlatform(context));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
