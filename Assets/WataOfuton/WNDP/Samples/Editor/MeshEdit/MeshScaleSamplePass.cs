using UnityEngine;
using WataOfuton.Tool.WNDP;
using WataOfuton.Tool.WNDP.Editor;

namespace WataOfuton.Tool.WNDP.Samples.Editor
{
    [WorldBuildPass(WorldBuildPhase.Transforming, 50, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class MeshScaleSamplePass : WorldMarkerPass<MeshScaleSampleMarker>
    {
        public override string DisplayName => "Mesh Scale Sample";

        protected override void ValidateMarker(
            WorldPassContext context,
            MeshScaleSampleMarker marker,
            IWorldValidationSink sink)
        {
            var meshFilter = marker.TargetMeshFilter;
            if (meshFilter == null)
            {
                sink.Error("MeshScaleSampleMarker requires a MeshFilter on the same GameObject as the marker.", marker);
                return;
            }

            if (meshFilter.sharedMesh == null)
            {
                sink.Error("MeshScaleSampleMarker target MeshFilter must reference a mesh.", meshFilter);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, MeshScaleSampleMarker marker)
        {
            var meshFilter = marker.TargetMeshFilter;
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            var sourceMesh = meshFilter.sharedMesh;
            var clonedMesh = CloneForSession(
                context,
                sourceMesh,
                meshFilter,
                $"Scale_{marker.VertexScale.x:0.##}_{marker.VertexScale.y:0.##}_{marker.VertexScale.z:0.##}");
            if (clonedMesh == null)
            {
                return;
            }

            ScaleMeshVertices(clonedMesh, marker.VertexScale, marker.RecalculateNormals);
            meshFilter.sharedMesh = clonedMesh;

            if (marker.UpdateMeshCollider)
            {
                foreach (var meshCollider in meshFilter.GetComponents<MeshCollider>())
                {
                    if (meshCollider != null && (meshCollider.sharedMesh == null || meshCollider.sharedMesh == sourceMesh))
                    {
                        meshCollider.sharedMesh = clonedMesh;
                    }
                }
            }

            Debug.Log(
                $"[WNDP Samples] Scaled mesh '{sourceMesh.name}' on '{meshFilter.name}' by {marker.VertexScale} for {CurrentSessionKind(context)} session '{CurrentSessionId(context)}'.");
        }

        private static void ScaleMeshVertices(Mesh mesh, Vector3 vertexScale, bool recalculateNormals)
        {
            var vertices = mesh.vertices;
            var center = mesh.bounds.center;

            for (var index = 0; index < vertices.Length; index++)
            {
                var localOffset = vertices[index] - center;
                vertices[index] = center + Vector3.Scale(localOffset, vertexScale);
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            if (recalculateNormals)
            {
                mesh.RecalculateNormals();
            }
        }
    }
}
