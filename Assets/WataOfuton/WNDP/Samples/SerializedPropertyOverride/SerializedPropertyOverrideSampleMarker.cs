using System;
using System.Linq;
using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Samples
{
    [DisallowMultipleComponent]
    public sealed class SerializedPropertyOverrideSampleMarker : WorldPassMarker
    {
        public enum OverrideValueKind
        {
            Boolean,
            Integer,
            Float,
            String,
            Vector3,
            Color
        }

        [SerializeField]
        private string _targetComponentTypeName = "UnityEngine.Transform";

        [SerializeField]
        private int _targetComponentOccurrenceIndex;

        [SerializeField]
        private string _propertyPath = "m_LocalScale";

        [SerializeField]
        private OverrideValueKind _valueKind = OverrideValueKind.Vector3;

        [SerializeField]
        private bool _boolValue = true;

        [SerializeField]
        private int _intValue = 1;

        [SerializeField]
        private float _floatValue = 2.0f;

        [SerializeField]
        private string _stringValue = string.Empty;

        [SerializeField]
        private Vector3 _vector3Value = new Vector3(1.0f, 1.5f, 1.0f);

        [SerializeField]
        private Color _colorValue = new Color(1.0f, 0.75f, 0.25f, 1.0f);

        public Component TargetComponent
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_targetComponentTypeName))
                {
                    return null;
                }

                var matchingComponents = GetSelectableComponents()
                    .Where(component => component.GetType().FullName == _targetComponentTypeName)
                    .ToArray();

                if (matchingComponents.Length == 0)
                {
                    return null;
                }

                var resolvedOccurrenceIndex = Mathf.Clamp(_targetComponentOccurrenceIndex, 0, matchingComponents.Length - 1);
                return matchingComponents[resolvedOccurrenceIndex];
            }
        }

        public Component[] GetSelectableComponents()
        {
            return GetComponents<Component>()
                .Where(component => component != null)
                .Where(component => !(component is WorldPassMarker))
                .ToArray();
        }

        public int GetResolvedTargetComponentIndex()
        {
            var components = GetSelectableComponents();
            var targetComponent = TargetComponent;
            return targetComponent == null ? -1 : Array.IndexOf(components, targetComponent);
        }

        public string PropertyPath => _propertyPath;

        public OverrideValueKind ValueKind => _valueKind;

        public bool BoolValue => _boolValue;

        public int IntValue => _intValue;

        public float FloatValue => _floatValue;

        public string StringValue => _stringValue;

        public Vector3 Vector3Value => _vector3Value;

        public Color ColorValue => _colorValue;
    }
}
