using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using WataOfuton.Tool.WNDP;
using WataOfuton.Tool.WNDP.Editor;

namespace WataOfuton.Tool.WNDP.Samples.Editor
{
    [WorldBuildPass(WorldBuildPhase.Optimizing, 120, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class HierarchyMeshCombineSamplePass : WorldMarkerPass<HierarchyMeshCombineSampleMarker>
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

        public override string DisplayName => "Hierarchy Mesh Combine Sample";

        protected override void ValidateMarker(
            WorldPassContext context,
            HierarchyMeshCombineSampleMarker marker,
            IWorldValidationSink sink)
        {
            var root = marker.TargetRoot;
            var subMeshes = CollectDescendantSubMeshes(root, marker.IncludeInactiveChildren);
            if (subMeshes.Count == 0)
            {
                sink.Error(
                    "HierarchyMeshCombineSampleMarker requires at least one descendant MeshFilter + MeshRenderer pair with a valid mesh and material.",
                    root);
            }

            if (root.GetComponent<MeshFilter>() != null || root.GetComponent<MeshRenderer>() != null)
            {
                sink.Warning(
                    "HierarchyMeshCombineSampleMarker writes the combined MeshFilter and MeshRenderer onto the marker root. Existing mesh components on the root will be overwritten.",
                    root);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, HierarchyMeshCombineSampleMarker marker)
        {
            var root = marker.TargetRoot;
            var subMeshes = CollectDescendantSubMeshes(root, marker.IncludeInactiveChildren);
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
                        name = $"{material.name} (Hierarchy Combined Group)",
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
                    marker.GeneratedMeshName);
                if (combinedMesh == null)
                {
                    return;
                }

                combinedMesh.Clear();
                combinedMesh.name = marker.GeneratedMeshName;
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

                var rootObject = root.gameObject;
                var combinedMeshFilter = EnsureComponent<MeshFilter>(context, rootObject);
                combinedMeshFilter.sharedMesh = combinedMesh;

                var prototypeRenderer = subMeshes[0].MeshRenderer;
                var combinedMeshRenderer = EnsureComponent<MeshRenderer>(context, rootObject);

                combinedMeshRenderer.enabled = true;
                combinedMeshRenderer.sharedMaterials = materialOrder.ToArray();
                combinedMeshRenderer.shadowCastingMode = prototypeRenderer.shadowCastingMode;
                combinedMeshRenderer.receiveShadows = prototypeRenderer.receiveShadows;
                combinedMeshRenderer.lightProbeUsage = prototypeRenderer.lightProbeUsage;
                combinedMeshRenderer.reflectionProbeUsage = prototypeRenderer.reflectionProbeUsage;
                combinedMeshRenderer.allowOcclusionWhenDynamic = prototypeRenderer.allowOcclusionWhenDynamic;

                if (marker.AddMeshCollider)
                {
                    var meshCollider = EnsureComponent<MeshCollider>(context, rootObject);
                    meshCollider.sharedMesh = combinedMesh;
                }

                if (marker.DestroySourceObjects)
                {
                    DestroySourceObjects(subMeshes);
                }
                else if (marker.DisableSourceRenderers)
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
                    $"[WNDP Samples] Combined {subMeshes.Select(subMesh => subMesh.MeshFilter).Distinct().Count()} descendant meshes onto root '{root.name}' for {CurrentSessionKind(context)} session '{CurrentSessionId(context)}'.");
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

        private static List<CombinableSubMesh> CollectDescendantSubMeshes(Transform root, bool includeInactiveChildren)
        {
            var subMeshes = new List<CombinableSubMesh>();
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(includeInactiveChildren);

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.transform == root || meshFilter.sharedMesh == null)
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

        private static void DestroySourceObjects(IEnumerable<CombinableSubMesh> subMeshes)
        {
            foreach (var sourceObject in subMeshes
                         .Select(subMesh => subMesh.MeshFilter != null ? subMesh.MeshFilter.gameObject : null)
                         .Where(sourceObject => sourceObject != null)
                         .Distinct()
                         .OrderByDescending(GetHierarchyDepth))
            {
                if (sourceObject != null)
                {
                    Object.DestroyImmediate(sourceObject);
                }
            }
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

        private static int GetHierarchyDepth(GameObject gameObject)
        {
            var depth = 0;
            var current = gameObject != null ? gameObject.transform.parent : null;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }
    }
}
