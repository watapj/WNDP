using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Samples
{
    [DisallowMultipleComponent]
    public sealed class PrefabProxySampleMarker : WorldPassMarker
    {
        public enum SourceHandlingMode
        {
            Keep,
            Disable,
            Destroy
        }

        [SerializeField]
        private GameObject _prefabAsset;

        [SerializeField]
        private bool _parentUnderMarker = true;

        [SerializeField]
        private SourceHandlingMode _sourceHandling = SourceHandlingMode.Keep;

        [SerializeField]
        private string _generatedObjectName = "Prefab Proxy (Generated)";

        public GameObject TargetObject => gameObject;

        public GameObject PrefabAsset => _prefabAsset;

        public bool ParentUnderMarker => _parentUnderMarker;

        public SourceHandlingMode SourceHandling => _sourceHandling;

        public string GeneratedObjectName => string.IsNullOrWhiteSpace(_generatedObjectName)
            ? "Prefab Proxy (Generated)"
            : _generatedObjectName;
    }
}
