using System.Linq;
using UnityEngine;
using WataOfuton.Tool.WNDP;
using WataOfuton.Tool.WNDP.Editor;

namespace WataOfuton.Tool.WNDP.Samples.Editor
{
    [WorldBuildPass(WorldBuildPhase.PlatformInit, 50, Targets = WorldBuildPassTargets.BuildAndPlay)]
    public sealed class GlobalKeywordColorSamplePass : WorldMarkerPass<GlobalKeywordColorSampleMarker>
    {
        public override string DisplayName => "Global Keyword Color Sample";

        protected override void ValidateMarker(
            WorldPassContext context,
            GlobalKeywordColorSampleMarker marker,
            IWorldValidationSink sink)
        {
            if (string.IsNullOrWhiteSpace(marker.KeywordName))
            {
                sink.Error("GlobalKeywordColorSampleMarker requires a keyword name.", marker);
                return;
            }

            var renderer = marker.TargetRenderer;
            if (renderer == null)
            {
                sink.Error("GlobalKeywordColorSampleMarker requires a Renderer on the same GameObject as the marker.", marker);
                return;
            }

            var materials = renderer.sharedMaterials.Where(material => material != null).ToArray();
            if (materials.Length == 0)
            {
                sink.Error("GlobalKeywordColorSampleMarker target renderer must have at least one material.", renderer);
                return;
            }

            if (!string.IsNullOrWhiteSpace(marker.ExpectedShaderName)
                && !materials.Any(material => material.shader != null && material.shader.name == marker.ExpectedShaderName))
            {
                sink.Error(
                    $"GlobalKeywordColorSampleMarker expected shader '{marker.ExpectedShaderName}' was not found on the target renderer.",
                    renderer);
            }
        }

        protected override void ExecuteMarker(WorldPassContext context, GlobalKeywordColorSampleMarker marker)
        {
            ApplyMaterialKeywordToRenderer(context, marker);
        }

        private void ApplyMaterialKeywordToRenderer(
            WorldPassContext context,
            GlobalKeywordColorSampleMarker marker)
        {
            var renderer = marker.TargetRenderer;
            if (renderer == null)
            {
                return;
            }

            var sourceMaterials = renderer.sharedMaterials;
            var resolvedMaterials = new Material[sourceMaterials.Length];
            var shouldEnableKeyword = marker.ShouldEnableKeyword(TargetPlatform(context));
            var modifiedCount = 0;

            for (var index = 0; index < sourceMaterials.Length; index++)
            {
                var sourceMaterial = sourceMaterials[index];
                if (sourceMaterial == null)
                {
                    resolvedMaterials[index] = null;
                    continue;
                }

                var targetsExpectedShader = string.IsNullOrWhiteSpace(marker.ExpectedShaderName)
                    || (sourceMaterial.shader != null && sourceMaterial.shader.name == marker.ExpectedShaderName);
                if (!targetsExpectedShader)
                {
                    resolvedMaterials[index] = sourceMaterial;
                    continue;
                }

                var clonedMaterial = CloneForSession(
                    context,
                    sourceMaterial,
                    renderer,
                    $"{index}_{marker.KeywordName}");
                if (clonedMaterial == null)
                {
                    resolvedMaterials[index] = null;
                    continue;
                }

                if (shouldEnableKeyword)
                {
                    clonedMaterial.EnableKeyword(marker.KeywordName);
                }
                else
                {
                    clonedMaterial.DisableKeyword(marker.KeywordName);
                }

                resolvedMaterials[index] = clonedMaterial;
                modifiedCount++;
            }

            if (modifiedCount == 0)
            {
                return;
            }

            renderer.sharedMaterials = resolvedMaterials;

            Debug.Log(
                $"[WNDP Samples] Applied local keyword '{marker.KeywordName}' to {modifiedCount} material(s) on '{renderer.name}' for {CurrentSessionKind(context)} session '{CurrentSessionId(context)}'.");
        }
    }
}
