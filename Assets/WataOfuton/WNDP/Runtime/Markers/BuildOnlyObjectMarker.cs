using UnityEngine;

namespace WataOfuton.Tool.WNDP
{
    public sealed class BuildOnlyObjectMarker : WorldPassMarker
    {
        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private bool _parentUnderMarker;

        [SerializeField]
        private bool _destroyAnchorAfterInstantiation = true;

        public GameObject Prefab => _prefab;

        public bool ParentUnderMarker => _parentUnderMarker;

        public bool DestroyAnchorAfterInstantiation => _destroyAnchorAfterInstantiation;
    }
}
