using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Samples
{
    [DisallowMultipleComponent]
    public sealed class MeshScaleSampleMarker : WorldPassMarker
    {
        [SerializeField]
        private Vector3 _vertexScale = new Vector3(1.0f, 1.75f, 1.0f);

        [SerializeField]
        private bool _updateMeshCollider = true;

        [SerializeField]
        private bool _recalculateNormals = true;

        public MeshFilter TargetMeshFilter => GetComponent<MeshFilter>();

        public Vector3 VertexScale => _vertexScale;

        public bool UpdateMeshCollider => _updateMeshCollider;

        public bool RecalculateNormals => _recalculateNormals;
    }
}
