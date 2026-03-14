using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Samples.Editor
{
    [CustomEditor(typeof(SerializedPropertyOverrideSampleMarker))]
    public sealed class SerializedPropertyOverrideSampleMarkerEditor : UnityEditor.Editor
    {
        private static readonly GUIContent QuickComponentLabel =
            new GUIContent("Quick Select Component", "Select a component on the same GameObject as this marker.");

        private static readonly GUIContent QuickPropertyLabel =
            new GUIContent("Quick Select Property", "Pick a supported serialized property from the current target component.");

        private SerializedProperty _enabledProperty;
        private SerializedProperty _platformMaskProperty;
        private SerializedProperty _destroyAfterProcessingProperty;
        private SerializedProperty _targetComponentTypeNameProperty;
        private SerializedProperty _targetComponentOccurrenceIndexProperty;
        private SerializedProperty _propertyPathProperty;
        private SerializedProperty _valueKindProperty;
        private SerializedProperty _boolValueProperty;
        private SerializedProperty _intValueProperty;
        private SerializedProperty _floatValueProperty;
        private SerializedProperty _stringValueProperty;
        private SerializedProperty _vector3ValueProperty;
        private SerializedProperty _colorValueProperty;

        private void OnEnable()
        {
            _enabledProperty = serializedObject.FindProperty("_enabled");
            _platformMaskProperty = serializedObject.FindProperty("_platformMask");
            _destroyAfterProcessingProperty = serializedObject.FindProperty("_destroyAfterProcessing");
            _targetComponentTypeNameProperty = serializedObject.FindProperty("_targetComponentTypeName");
            _targetComponentOccurrenceIndexProperty = serializedObject.FindProperty("_targetComponentOccurrenceIndex");
            _propertyPathProperty = serializedObject.FindProperty("_propertyPath");
            _valueKindProperty = serializedObject.FindProperty("_valueKind");
            _boolValueProperty = serializedObject.FindProperty("_boolValue");
            _intValueProperty = serializedObject.FindProperty("_intValue");
            _floatValueProperty = serializedObject.FindProperty("_floatValue");
            _stringValueProperty = serializedObject.FindProperty("_stringValue");
            _vector3ValueProperty = serializedObject.FindProperty("_vector3Value");
            _colorValueProperty = serializedObject.FindProperty("_colorValue");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_enabledProperty);
            EditorGUILayout.PropertyField(_platformMaskProperty);
            EditorGUILayout.PropertyField(_destroyAfterProcessingProperty);
            EditorGUILayout.Space();

            DrawQuickComponentPicker();
            EditorGUILayout.Space();

            DrawQuickPropertyPicker();
            EditorGUILayout.PropertyField(_propertyPathProperty);
            DrawDetectedPropertyInfo();
            EditorGUILayout.PropertyField(_valueKindProperty);
            DrawValueField();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawQuickComponentPicker()
        {
            var marker = (SerializedPropertyOverrideSampleMarker)target;
            var selectableComponents = marker.GetComponents<Component>()
                .Where(component => component != null)
                .Where(component => !(component is WorldPassMarker))
                .ToArray();

            if (selectableComponents.Length == 0)
            {
                EditorGUILayout.HelpBox("No selectable components were found on this GameObject.", MessageType.Info);
                return;
            }

            var currentIndex = marker.GetResolvedTargetComponentIndex();
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            var selectedIndex = EditorGUILayout.Popup(
                QuickComponentLabel,
                currentIndex,
                selectableComponents.Select((component, index) => BuildComponentLabel(selectableComponents, index)).ToArray());
            if (selectedIndex >= 0 && selectedIndex < selectableComponents.Length)
            {
                AssignSelectedComponent(selectableComponents, selectedIndex);
            }
        }

        private void DrawQuickPropertyPicker()
        {
            var marker = (SerializedPropertyOverrideSampleMarker)target;
            var targetComponent = marker.TargetComponent;
            if (targetComponent == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a component from the same GameObject first.",
                    MessageType.Info);
                return;
            }

            var propertyOptions = CollectSupportedProperties(targetComponent);
            if (propertyOptions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"No supported serialized properties were found on '{targetComponent.GetType().Name}'.",
                    MessageType.Warning);
                return;
            }

            var currentPath = _propertyPathProperty.stringValue;
            var options = new List<string> { "Keep current / custom path" };
            options.AddRange(propertyOptions.Select(option => option.Label));

            var currentIndex = 0;
            var propertyIndex = propertyOptions.FindIndex(option => option.Path == currentPath);
            if (propertyIndex >= 0)
            {
                currentIndex = propertyIndex + 1;
            }

            var selectedIndex = EditorGUILayout.Popup(QuickPropertyLabel, currentIndex, options.ToArray());
            if (selectedIndex > 0)
            {
                var selectedOption = propertyOptions[selectedIndex - 1];
                _propertyPathProperty.stringValue = selectedOption.Path;
                _valueKindProperty.enumValueIndex = (int)MapValueKind(selectedOption.PropertyType);
            }
        }

        private void DrawDetectedPropertyInfo()
        {
            var marker = (SerializedPropertyOverrideSampleMarker)target;
            var targetComponent = marker.TargetComponent;
            if (targetComponent == null || string.IsNullOrWhiteSpace(_propertyPathProperty.stringValue))
            {
                return;
            }

            var propertyType = TryGetSupportedPropertyType(targetComponent, _propertyPathProperty.stringValue, out var detectedType)
                ? detectedType.ToString()
                : "Not found / unsupported";

            EditorGUILayout.HelpBox(
                $"Detected property type: {propertyType}",
                detectedType == SerializedPropertyType.Generic ? MessageType.Warning : MessageType.Info);
        }

        private void DrawValueField()
        {
            var selectedValueKind = (SerializedPropertyOverrideSampleMarker.OverrideValueKind)_valueKindProperty.enumValueIndex;

            switch (selectedValueKind)
            {
                case SerializedPropertyOverrideSampleMarker.OverrideValueKind.Boolean:
                    EditorGUILayout.PropertyField(_boolValueProperty);
                    break;

                case SerializedPropertyOverrideSampleMarker.OverrideValueKind.Integer:
                    EditorGUILayout.PropertyField(_intValueProperty);
                    break;

                case SerializedPropertyOverrideSampleMarker.OverrideValueKind.Float:
                    EditorGUILayout.PropertyField(_floatValueProperty);
                    break;

                case SerializedPropertyOverrideSampleMarker.OverrideValueKind.String:
                    EditorGUILayout.PropertyField(_stringValueProperty);
                    break;

                case SerializedPropertyOverrideSampleMarker.OverrideValueKind.Vector3:
                    EditorGUILayout.PropertyField(_vector3ValueProperty);
                    break;

                case SerializedPropertyOverrideSampleMarker.OverrideValueKind.Color:
                    EditorGUILayout.PropertyField(_colorValueProperty);
                    break;
            }
        }

        private static List<PropertyOption> CollectSupportedProperties(Component targetComponent)
        {
            var options = new List<PropertyOption>();
            var serializedObject = new SerializedObject(targetComponent);
            var iterator = serializedObject.GetIterator();

            if (!iterator.NextVisible(true))
            {
                return options;
            }

            do
            {
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                if (!TryMapSupportedPropertyType(iterator.propertyType, out _))
                {
                    continue;
                }

                options.Add(new PropertyOption(
                    iterator.propertyPath,
                    $"{iterator.displayName} ({iterator.propertyPath})",
                    iterator.propertyType));
            } while (iterator.NextVisible(false));

            return options;
        }

        private static bool TryGetSupportedPropertyType(
            Component targetComponent,
            string propertyPath,
            out SerializedPropertyType propertyType)
        {
            var property = new SerializedObject(targetComponent).FindProperty(propertyPath);
            if (property != null && TryMapSupportedPropertyType(property.propertyType, out _))
            {
                propertyType = property.propertyType;
                return true;
            }

            propertyType = SerializedPropertyType.Generic;
            return false;
        }

        private static bool TryMapSupportedPropertyType(
            SerializedPropertyType propertyType,
            out SerializedPropertyOverrideSampleMarker.OverrideValueKind valueKind)
        {
            switch (propertyType)
            {
                case SerializedPropertyType.Boolean:
                    valueKind = SerializedPropertyOverrideSampleMarker.OverrideValueKind.Boolean;
                    return true;

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    valueKind = SerializedPropertyOverrideSampleMarker.OverrideValueKind.Integer;
                    return true;

                case SerializedPropertyType.Float:
                    valueKind = SerializedPropertyOverrideSampleMarker.OverrideValueKind.Float;
                    return true;

                case SerializedPropertyType.String:
                    valueKind = SerializedPropertyOverrideSampleMarker.OverrideValueKind.String;
                    return true;

                case SerializedPropertyType.Vector3:
                    valueKind = SerializedPropertyOverrideSampleMarker.OverrideValueKind.Vector3;
                    return true;

                case SerializedPropertyType.Color:
                    valueKind = SerializedPropertyOverrideSampleMarker.OverrideValueKind.Color;
                    return true;

                default:
                    valueKind = SerializedPropertyOverrideSampleMarker.OverrideValueKind.Float;
                    return false;
            }
        }

        private static SerializedPropertyOverrideSampleMarker.OverrideValueKind MapValueKind(SerializedPropertyType propertyType)
        {
            return TryMapSupportedPropertyType(propertyType, out var valueKind)
                ? valueKind
                : SerializedPropertyOverrideSampleMarker.OverrideValueKind.Float;
        }

        private static string BuildComponentLabel(Component component)
        {
            return $"{component.GetType().Name} ({component.gameObject.name})";
        }

        private void AssignSelectedComponent(Component[] selectableComponents, int selectedIndex)
        {
            var selectedComponent = selectableComponents[selectedIndex];
            var selectedTypeName = selectedComponent.GetType().FullName;
            var selectedOccurrenceIndex = 0;

            for (var index = 0; index < selectedIndex; index++)
            {
                if (selectableComponents[index].GetType() == selectedComponent.GetType())
                {
                    selectedOccurrenceIndex++;
                }
            }

            _targetComponentTypeNameProperty.stringValue = selectedTypeName;
            _targetComponentOccurrenceIndexProperty.intValue = selectedOccurrenceIndex;
        }

        private static string BuildComponentLabel(Component[] selectableComponents, int index)
        {
            var component = selectableComponents[index];
            var typeName = component.GetType().Name;
            var occurrenceIndex = 0;

            for (var current = 0; current < index; current++)
            {
                if (selectableComponents[current].GetType() == component.GetType())
                {
                    occurrenceIndex++;
                }
            }

            return occurrenceIndex == 0 ? typeName : $"{typeName} #{occurrenceIndex + 1}";
        }

        private readonly struct PropertyOption
        {
            public PropertyOption(string path, string label, SerializedPropertyType propertyType)
            {
                Path = path;
                Label = label;
                PropertyType = propertyType;
            }

            public string Path { get; }

            public string Label { get; }

            public SerializedPropertyType PropertyType { get; }
        }
    }
}
