using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Samples
{
    [DisallowMultipleComponent]
    public sealed class ProxyColliderSampleMarker : WorldPassMarker
    {
        public enum ProxyColliderShape
        {
            Box,
            Sphere
        }

        [SerializeField]
        private ProxyColliderShape _shape = ProxyColliderShape.Box;

        [SerializeField]
        private Vector3 _padding = new Vector3(0.05f, 0.05f, 0.05f);

        [SerializeField]
        private bool _disableExistingColliders = true;

        [SerializeField]
        private bool _isTrigger;

        [SerializeField]
        private PhysicMaterial _sharedMaterial;

        [SerializeField]
        private string _generatedChildName = "Proxy Collider (Generated)";

        public GameObject TargetObject => gameObject;

        public ProxyColliderShape Shape => _shape;

        public Vector3 Padding => new Vector3(
            Mathf.Max(0.0f, _padding.x),
            Mathf.Max(0.0f, _padding.y),
            Mathf.Max(0.0f, _padding.z));

        public bool DisableExistingColliders => _disableExistingColliders;

        public bool IsTrigger => _isTrigger;

        public PhysicMaterial SharedMaterial => _sharedMaterial;

        public string GeneratedChildName => string.IsNullOrWhiteSpace(_generatedChildName)
            ? "Proxy Collider (Generated)"
            : _generatedChildName;
    }
}
