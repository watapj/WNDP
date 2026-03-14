using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Samples
{
    [DisallowMultipleComponent]
    public sealed class MeshCombineSampleMarker : WorldPassMarker
    {
        [SerializeField]
        private bool _includeInactiveChildren;

        [SerializeField]
        private bool _disableSourceRenderers = true;

        [SerializeField]
        private bool _addMeshCollider;

        [SerializeField]
        private string _generatedObjectName = "Combined Mesh (Generated)";

        public Transform TargetRoot => transform;

        public bool IncludeInactiveChildren => _includeInactiveChildren;

        public bool DisableSourceRenderers => _disableSourceRenderers;

        public bool AddMeshCollider => _addMeshCollider;

        public string GeneratedObjectName => string.IsNullOrWhiteSpace(_generatedObjectName)
            ? "Combined Mesh (Generated)"
            : _generatedObjectName;
    }
}
