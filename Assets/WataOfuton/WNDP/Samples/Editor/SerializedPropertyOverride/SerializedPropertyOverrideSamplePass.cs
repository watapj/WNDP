using UnityEditor;
using UnityEngine;
using WataOfuton.Tool.WNDP;
using WataOfuton.Tool.WNDP.Editor;

namespace WataOfuton.Tool.WNDP.Samples.Editor
{
    [WorldBuildPass(WorldBuildPhase.Transforming, 80, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class SerializedPropertyOverrideSamplePass : WorldMarkerPass<SerializedPropertyOverrideSampleMarker>
    {
        public override string DisplayName => "Serialized Property Override Sample";

        protected override void ValidateMarker(
            WorldPassContext context,
            SerializedPropertyOverrideSampleMarker marker,
            IWorldValidationSink sink)
        {
            var targetComponent = marker.TargetComponent;
            if (targetComponent == null)
            {
                sink.Error(
                    "SerializedPropertyOverrideSampleMarker requires a selectable target component on the same GameObject as the marker.",
                    marker);
                return;
            }

            if (string.IsNullOrWhiteSpace(marker.PropertyPath))
            {
                sink.Error("SerializedPropertyOverrideSampleMarker requires a property path.", marker);
                return;
            }

            var property = new SerializedObject(targetComponent).FindProperty(marker.PropertyPath);
            if (property == null)
            {
                sink.Error(
                    $"SerializedPropertyOverrideSampleMarker could not find property path '{marker.PropertyPath}' on '{targetComponent.GetType().Name}'.",
                    targetComponent);
                return;
            }

            if (!TryValidatePropertyType(marker.ValueKind, property.propertyType, out var validationMessage))
            {
                sink.Error(validationMessage, targetComponent);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, SerializedPropertyOverrideSampleMarker marker)
        {
            var targetComponent = marker.TargetComponent;
            if (targetComponent == null || string.IsNullOrWhiteSpace(marker.PropertyPath))
            {
                return;
            }

            var serializedObject = new SerializedObject(targetComponent);
            var property = serializedObject.FindProperty(marker.PropertyPath);
            if (property == null)
            {
                return;
            }

            ApplyMarkerValue(marker, property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log(
                $"[WNDP Samples] Overrode serialized property '{marker.PropertyPath}' on '{targetComponent.name}' for {CurrentSessionKind(context)} session '{CurrentSessionId(context)}'.");
        }

        private static bool TryValidatePropertyType(
            SerializedPropertyOverrideSampleMarker.OverrideValueKind valueKind,
            SerializedPropertyType propertyType,
            out string validationMessage)
        {
            switch (propertyType)
            {
                case SerializedPropertyType.Boolean:
                    validationMessage = valueKind == SerializedPropertyOverrideSampleMarker.OverrideValueKind.Boolean
                        ? null
                        : "SerializedPropertyOverrideSampleMarker value kind must be Boolean for a boolean property.";
                    return validationMessage == null;

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    validationMessage = valueKind == SerializedPropertyOverrideSampleMarker.OverrideValueKind.Integer
                        ? null
                        : "SerializedPropertyOverrideSampleMarker value kind must be Integer for an integer or enum property.";
                    return validationMessage == null;

                case SerializedPropertyType.Float:
                    validationMessage = valueKind == SerializedPropertyOverrideSampleMarker.OverrideValueKind.Float
                        ? null
                        : "SerializedPropertyOverrideSampleMarker value kind must be Float for a float property.";
                    return validationMessage == null;

                case SerializedPropertyType.String:
                    validationMessage = valueKind == SerializedPropertyOverrideSampleMarker.OverrideValueKind.String
                        ? null
                        : "SerializedPropertyOverrideSampleMarker value kind must be String for a string property.";
                    return validationMessage == null;

                case SerializedPropertyType.Vector3:
                    validationMessage = valueKind == SerializedPropertyOverrideSampleMarker.OverrideValueKind.Vector3
                        ? null
                        : "SerializedPropertyOverrideSampleMarker value kind must be Vector3 for a Vector3 property.";
                    return validationMessage == null;

                case SerializedPropertyType.Color:
                    validationMessage = valueKind == SerializedPropertyOverrideSampleMarker.OverrideValueKind.Color
                        ? null
                        : "SerializedPropertyOverrideSampleMarker value kind must be Color for a Color property.";
                    return validationMessage == null;

                default:
                    validationMessage =
                        $"SerializedPropertyOverrideSampleMarker does not support SerializedPropertyType '{propertyType}'.";
                    return false;
            }
        }

        private static void ApplyMarkerValue(
            SerializedPropertyOverrideSampleMarker marker,
            SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    property.boolValue = marker.BoolValue;
                    break;

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    property.intValue = marker.IntValue;
                    break;

                case SerializedPropertyType.Float:
                    property.floatValue = marker.FloatValue;
                    break;

                case SerializedPropertyType.String:
                    property.stringValue = marker.StringValue;
                    break;

                case SerializedPropertyType.Vector3:
                    property.vector3Value = marker.Vector3Value;
                    break;

                case SerializedPropertyType.Color:
                    property.colorValue = marker.ColorValue;
                    break;
            }
        }
    }
}
