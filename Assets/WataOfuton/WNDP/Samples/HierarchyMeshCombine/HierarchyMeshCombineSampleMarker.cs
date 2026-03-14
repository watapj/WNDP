using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Samples
{
    [DisallowMultipleComponent]
    public sealed class HierarchyMeshCombineSampleMarker : WorldPassMarker
    {
        [SerializeField]
        private bool _includeInactiveChildren;

        [SerializeField]
        private bool _disableSourceRenderers = true;

        [SerializeField]
        private bool _addMeshCollider;

        [SerializeField]
        private bool _destroySourceObjects;

        [SerializeField]
        private string _generatedMeshName = "Hierarchy Combined Mesh";

        public Transform TargetRoot => transform;

        public bool IncludeInactiveChildren => _includeInactiveChildren;

        public bool DisableSourceRenderers => _disableSourceRenderers;

        public bool AddMeshCollider => _addMeshCollider;

        public bool DestroySourceObjects => _destroySourceObjects;

        public string GeneratedMeshName => string.IsNullOrWhiteSpace(_generatedMeshName)
            ? "Hierarchy Combined Mesh"
            : _generatedMeshName;
    }
}
