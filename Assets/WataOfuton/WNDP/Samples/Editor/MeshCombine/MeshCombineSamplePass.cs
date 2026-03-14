using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using WataOfuton.Tool.WNDP;
using WataOfuton.Tool.WNDP.Editor;

namespace WataOfuton.Tool.WNDP.Samples.Editor
{
    [WorldBuildPass(WorldBuildPhase.Optimizing, 110, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class MeshCombineSamplePass : WorldMarkerPass<MeshCombineSampleMarker>
    {
        private readonly struct CombinableSubMesh
        {
            public CombinableSubMesh(MeshFilter meshFilter, MeshRenderer meshRenderer, Mesh mesh, Material material, int subMeshIndex)
            {
                MeshFilter = meshFilter;
                MeshRenderer = meshRenderer;
                Mesh = mesh;
                Material = material;
                SubMeshIndex = subMeshIndex;
            }

            public MeshFilter MeshFilter { get; }

            public MeshRenderer MeshRenderer { get; }

            public Mesh Mesh { get; }

            public Material Material { get; }

            public int SubMeshIndex { get; }
        }

        public override string DisplayName => "Mesh Combine Sample";

        protected override void ValidateMarker(
            WorldPassContext context,
            MeshCombineSampleMarker marker,
            IWorldValidationSink sink)
        {
            var root = marker.TargetRoot;
            var subMeshes = CollectCombinableSubMeshes(root, marker.IncludeInactiveChildren);
            if (subMeshes.Count == 0)
            {
                sink.Error(
                    "MeshCombineSampleMarker requires at least one MeshFilter + MeshRenderer pair with a valid mesh and material.",
                    root);
                return;
            }

            if (subMeshes.Select(subMesh => subMesh.MeshFilter).Distinct().Count() < 2)
            {
                sink.Error(
                    "MeshCombineSampleMarker sample is intended for two or more source MeshFilters under the target root.",
                    root);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, MeshCombineSampleMarker marker)
        {
            var root = marker.TargetRoot;
            var subMeshes = CollectCombinableSubMeshes(root, marker.IncludeInactiveChildren);
            if (subMeshes.Count == 0)
            {
                return;
            }

            var groupedByMaterial = new Dictionary<Material, List<CombineInstance>>();
            var materialOrder = new List<Material>();

            foreach (var subMesh in subMeshes)
            {
                if (!groupedByMaterial.TryGetValue(subMesh.Material, out var combineInstances))
                {
                    combineInstances = new List<CombineInstance>();
                    groupedByMaterial.Add(subMesh.Material, combineInstances);
                    materialOrder.Add(subMesh.Material);
                }

                combineInstances.Add(new CombineInstance
                {
                    mesh = subMesh.Mesh,
                    subMeshIndex = subMesh.SubMeshIndex,
                    transform = root.worldToLocalMatrix * subMesh.MeshFilter.transform.localToWorldMatrix
                });
            }

            var temporaryMeshes = new List<Mesh>();

            try
            {
                var materialGroupMeshes = new List<Mesh>(materialOrder.Count);

                foreach (var material in materialOrder)
                {
                    var groupedMesh = new Mesh
                    {
                        name = $"{material.name} (Combined Group)",
                        indexFormat = CalculateIndexFormat(groupedByMaterial[material])
                    };

                    groupedMesh.CombineMeshes(groupedByMaterial[material].ToArray(), true, true, false);
                    materialGroupMeshes.Add(groupedMesh);
                    temporaryMeshes.Add(groupedMesh);
                }

                var combinedMesh = CloneForSession(
                    context,
                    subMeshes[0].Mesh,
                    root,
                    marker.GeneratedObjectName);
                if (combinedMesh == null)
                {
                    return;
                }

                combinedMesh.Clear();
                combinedMesh.indexFormat = CalculateIndexFormat(materialGroupMeshes);

                var finalCombineInstances = materialGroupMeshes
                    .Select(mesh => new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = 0
                    })
                    .ToArray();

                combinedMesh.CombineMeshes(finalCombineInstances, false, false, false);
                combinedMesh.RecalculateBounds();

                var combinedObject = GetOrCreateGeneratedRoot(context, marker.GeneratedObjectName, root);
                combinedObject.layer = root.gameObject.layer;
                combinedObject.isStatic = root.gameObject.isStatic;

                var combinedMeshFilter = EnsureComponent<MeshFilter>(context, combinedObject);
                combinedMeshFilter.sharedMesh = combinedMesh;

                var prototypeRenderer = subMeshes[0].MeshRenderer;
                var combinedMeshRenderer = EnsureComponent<MeshRenderer>(context, combinedObject);
                combinedMeshRenderer.sharedMaterials = materialOrder.ToArray();
                combinedMeshRenderer.shadowCastingMode = prototypeRenderer.shadowCastingMode;
                combinedMeshRenderer.receiveShadows = prototypeRenderer.receiveShadows;
                combinedMeshRenderer.lightProbeUsage = prototypeRenderer.lightProbeUsage;
                combinedMeshRenderer.reflectionProbeUsage = prototypeRenderer.reflectionProbeUsage;
                combinedMeshRenderer.allowOcclusionWhenDynamic = prototypeRenderer.allowOcclusionWhenDynamic;

                if (marker.AddMeshCollider)
                {
                    var meshCollider = EnsureComponent<MeshCollider>(context, combinedObject);
                    meshCollider.sharedMesh = combinedMesh;
                }
                else
                {
                    DestroyComponent(context, combinedObject.GetComponent<MeshCollider>());
                }

                if (marker.DisableSourceRenderers)
                {
                    foreach (var meshRenderer in subMeshes.Select(subMesh => subMesh.MeshRenderer).Distinct())
                    {
                        if (meshRenderer != null)
                        {
                            meshRenderer.enabled = false;
                        }
                    }
                }

                Debug.Log(
                    $"[WNDP Samples] Combined {subMeshes.Select(subMesh => subMesh.MeshFilter).Distinct().Count()} meshes under '{root.name}' for {CurrentSessionKind(context)} session '{CurrentSessionId(context)}'.");
            }
            finally
            {
                foreach (var temporaryMesh in temporaryMeshes)
                {
                    if (temporaryMesh != null)
                    {
                        Object.DestroyImmediate(temporaryMesh);
                    }
                }
            }
        }

        private static List<CombinableSubMesh> CollectCombinableSubMeshes(Transform root, bool includeInactiveChildren)
        {
            var subMeshes = new List<CombinableSubMesh>();
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(includeInactiveChildren);

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    continue;
                }

                var sharedMaterials = meshRenderer.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0)
                {
                    continue;
                }

                var subMeshCount = Mathf.Min(meshFilter.sharedMesh.subMeshCount, sharedMaterials.Length);
                for (var subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                {
                    var material = sharedMaterials[subMeshIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    subMeshes.Add(new CombinableSubMesh(
                        meshFilter,
                        meshRenderer,
                        meshFilter.sharedMesh,
                        material,
                        subMeshIndex));
                }
            }

            return subMeshes;
        }

        private static IndexFormat CalculateIndexFormat(IEnumerable<CombineInstance> combineInstances)
        {
            long vertexCount = 0;
            foreach (var combineInstance in combineInstances)
            {
                if (combineInstance.mesh != null)
                {
                    vertexCount += combineInstance.mesh.vertexCount;
                }
            }

            return vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
        }

        private static IndexFormat CalculateIndexFormat(IEnumerable<Mesh> meshes)
        {
            long vertexCount = 0;
            foreach (var mesh in meshes)
            {
                if (mesh != null)
                {
                    vertexCount += mesh.vertexCount;
                }
            }

            return vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
        }
    }
}