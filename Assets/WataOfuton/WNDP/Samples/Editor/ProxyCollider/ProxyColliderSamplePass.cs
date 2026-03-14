using UnityEngine;
using WataOfuton.Tool.WNDP;
using WataOfuton.Tool.WNDP.Editor;

namespace WataOfuton.Tool.WNDP.Samples.Editor
{
    [WorldBuildPass(WorldBuildPhase.Generating, 90, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class ProxyColliderSamplePass : WorldMarkerPass<ProxyColliderSampleMarker>
    {
        public override string DisplayName => "Proxy Collider Sample";

        protected override void ValidateMarker(
            WorldPassContext context,
            ProxyColliderSampleMarker marker,
            IWorldValidationSink sink)
        {
            var targetObject = marker.TargetObject;
            if (!TryGetLocalBounds(targetObject, out _))
            {
                sink.Error(
                    "ProxyColliderSampleMarker requires the same GameObject to have a MeshFilter with a mesh or a Renderer with valid bounds.",
                    targetObject);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, ProxyColliderSampleMarker marker)
        {
            var targetObject = marker.TargetObject;
            if (targetObject == null || !TryGetLocalBounds(targetObject, out var localBounds))
            {
                return;
            }

            if (marker.DisableExistingColliders)
            {
                foreach (var existingCollider in targetObject.GetComponents<Collider>())
                {
                    if (existingCollider != null)
                    {
                        existingCollider.enabled = false;
                    }
                }
            }

            var proxyObject = GetOrCreateGeneratedRoot(context, marker.GeneratedChildName, targetObject.transform);
            proxyObject.layer = targetObject.layer;
            proxyObject.isStatic = targetObject.isStatic;

            RemoveObsoleteColliders(context, proxyObject, marker.Shape);

            var proxyCollider = CreateProxyCollider(context, proxyObject, marker, localBounds);
            proxyCollider.isTrigger = marker.IsTrigger;
            proxyCollider.sharedMaterial = marker.SharedMaterial;

            Debug.Log(
                $"[WNDP Samples] Created {marker.Shape} proxy collider for '{targetObject.name}' in {CurrentSessionKind(context)} session '{CurrentSessionId(context)}'.");
        }

        private void RemoveObsoleteColliders(
            WorldPassContext context,
            GameObject proxyObject,
            ProxyColliderSampleMarker.ProxyColliderShape shape)
        {
            switch (shape)
            {
                case ProxyColliderSampleMarker.ProxyColliderShape.Sphere:
                    DestroyComponent(context, proxyObject.GetComponent<BoxCollider>());
                    break;

                case ProxyColliderSampleMarker.ProxyColliderShape.Box:
                default:
                    DestroyComponent(context, proxyObject.GetComponent<SphereCollider>());
                    break;
            }
        }

        private Collider CreateProxyCollider(
            WorldPassContext context,
            GameObject proxyObject,
            ProxyColliderSampleMarker marker,
            Bounds localBounds)
        {
            switch (marker.Shape)
            {
                case ProxyColliderSampleMarker.ProxyColliderShape.Sphere:
                    var sphereCollider = EnsureComponent<SphereCollider>(context, proxyObject);
                    sphereCollider.center = localBounds.center;

                    var expandedExtents = localBounds.extents + marker.Padding;
                    sphereCollider.radius = Mathf.Max(expandedExtents.x, expandedExtents.y, expandedExtents.z);
                    return sphereCollider;

                case ProxyColliderSampleMarker.ProxyColliderShape.Box:
                default:
                    var boxCollider = EnsureComponent<BoxCollider>(context, proxyObject);
                    boxCollider.center = localBounds.center;
                    boxCollider.size = localBounds.size + (marker.Padding * 2.0f);
                    return boxCollider;
            }
        }

        private static bool TryGetLocalBounds(GameObject targetObject, out Bounds localBounds)
        {
            if (targetObject.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh != null)
            {
                localBounds = meshFilter.sharedMesh.bounds;
                return true;
            }

            if (targetObject.TryGetComponent<Renderer>(out var renderer))
            {
                localBounds = ConvertWorldBoundsToLocalBounds(renderer.bounds, targetObject.transform);
                return true;
            }

            localBounds = default;
            return false;
        }

        private static Bounds ConvertWorldBoundsToLocalBounds(Bounds worldBounds, Transform targetTransform)
        {
            var center = worldBounds.center;
            var extents = worldBounds.extents;

            var localCorner0 = targetTransform.InverseTransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z));
            var localBounds = new Bounds(localCorner0, Vector3.zero);

            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        var localCorner = targetTransform.InverseTransformPoint(worldCorner);
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            return localBounds;
        }
    }
}