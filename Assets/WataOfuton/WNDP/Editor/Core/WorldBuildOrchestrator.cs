using System;
using System.Linq;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WataOfuton.Tool.WNDP.Editor
{
    /// <summary>
    /// Build / Play 共通の Pass 実行統括。
    /// どちらの Session でも失敗時は <see cref="BuildFailedException"/> を伝播して中止する。
    /// </summary>
    internal static class WorldBuildOrchestrator
    {
        private static readonly WorldBuildPhase[] PhaseOrder =
        {
            WorldBuildPhase.FirstChance,
            WorldBuildPhase.PlatformInit,
            WorldBuildPhase.Resolving,
            WorldBuildPhase.Generating,
            WorldBuildPhase.Transforming,
            WorldBuildPhase.Optimizing,
            WorldBuildPhase.PlatformFinish
        };

        public static bool RunPreflight(WorldBuildSession session, out string errorMessage)
        {
            if (session == null)
            {
                errorMessage = "Build session is missing.";
                return false;
            }

            if (session.RequestedBuildType != VRC.SDKBase.Editor.BuildPipeline.VRCSDKRequestedBuildType.Scene)
            {
                errorMessage = null;
                return true;
            }

            if (string.IsNullOrWhiteSpace(session.SourceScenePath))
            {
                errorMessage = "The active scene must be saved before running the world pipeline.";
                return false;
            }

            if (!WorldBuildPlatformUtility.IsSupported(session.BuildTarget))
            {
                errorMessage = $"Unsupported build target '{session.BuildTarget}'.";
                return false;
            }

            if (SceneManager.sceneCount != 1)
            {
                errorMessage = "WNDP currently supports single-scene world builds only.";
                return false;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != session.SourceScenePath)
            {
                errorMessage = "The active scene changed during build preflight.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Build / Play 共通の Scene 処理。
        /// 失敗時は <see cref="BuildFailedException"/> を throw し、Build または Play Mode 進入を中止する。
        /// </summary>
        public static void ProcessScene(WorldBuildSession session, Scene scene, BuildReport buildReport)
        {
            var effectiveBuildTarget = buildReport != null && WorldBuildPlatformUtility.IsSupported(buildReport.summary.platform)
                ? buildReport.summary.platform
                : session.BuildTarget;
            var executionReport = WorldBuildExecutionReport.Create(
                session,
                scene.path,
                effectiveBuildTarget);
            WorldBuildContext context = null;
            Exception pendingException = null;

            try
            {
                context = new WorldBuildContext(session, scene, effectiveBuildTarget, executionReport);
                ExecutePhases(context, executionReport);
                StripMarkers(context);
                executionReport.MarkSucceeded();
            }
            catch (BuildFailedException exception)
            {
                executionReport.MarkFailed(exception.Message);
                pendingException = exception;
            }
            catch (Exception exception)
            {
                executionReport.MarkFailed(exception.ToString());
                pendingException = new BuildFailedException($"WNDP failed: {exception.Message}");
            }
            finally
            {
                if (context != null)
                {
                    try
                    {
                        context.RunCleanupActions();
                    }
                    catch (Exception cleanupException)
                    {
                        executionReport.AddDiagnostic("CleanupError", cleanupException.ToString(), string.Empty);

                        if (pendingException == null)
                        {
                            pendingException = new BuildFailedException(
                                $"WNDP cleanup failed: {cleanupException.Message}");
                            executionReport.MarkFailed(pendingException.Message);
                        }
                        else
                        {
                            Debug.LogError($"[WNDP] Cleanup failed: {cleanupException}");
                        }
                    }
                }

                WorldBuildArtifactStore.StoreLastReport(executionReport);
            }

            if (pendingException == null)
            {
                return;
            }

            // Play / Build どちらも失敗時は例外を伝播して中止する。
            // Play 時でも BuildFailedException を throw すれば Unity は Play Mode 進入を中止する。
            throw pendingException;
        }

        private static void ExecutePhases(WorldBuildContext context, WorldBuildExecutionReport executionReport)
        {
            var allPasses = WorldPassRegistry.GetPasses();

            foreach (var phase in PhaseOrder)
            {
                var phasePasses = allPasses
                    .Where(pass => pass.Phase == phase && SupportsSession(pass, context.SessionKind))
                    .ToArray();
                if (phasePasses.Length == 0)
                {
                    continue;
                }

                Debug.Log($"[WNDP] Running phase {phase}.");
                context.RefreshMarkers();

                foreach (var pass in phasePasses)
                {
                    if (!pass.AppliesTo(context))
                    {
                        continue;
                    }

                    var validationSink = new WorldValidationSink(executionReport);
                    pass.Validate(context, validationSink);
                    if (validationSink.HasErrors)
                    {
                        throw new BuildFailedException(BuildValidationSummary(pass, validationSink.Diagnostics));
                    }

                    var startedAt = DateTime.UtcNow;

                    try
                    {
                        Debug.Log($"[WNDP] Executing pass {pass.DisplayName}.");
                        pass.Execute(context);
                        executionReport.AddPass(pass, true, DateTime.UtcNow - startedAt);
                    }
                    catch (Exception exception)
                    {
                        executionReport.AddPass(pass, false, DateTime.UtcNow - startedAt, exception.ToString());
                        throw;
                    }

                    // Pass が Scene hierarchy を変更した可能性があるため、次の Pass 用にマーカーを再取得する
                    context.RefreshMarkers();
                }
            }
        }

        private static string BuildValidationSummary(IWorldBuildPass pass, System.Collections.Generic.IReadOnlyList<WorldBuildDiagnostic> diagnostics)
        {
            var builder = new StringBuilder();
            builder.Append("Validation failed in pass '");
            builder.Append(pass.DisplayName);
            builder.Append("'.");

            foreach (var diagnostic in diagnostics.Where(diagnostic => diagnostic.severity == "Error"))
            {
                builder.AppendLine();
                builder.Append("- ");
                builder.Append(diagnostic.message);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 全 Pass 実行後の最終安全ネット。
        /// Scene 上に残存する全 <see cref="WorldPassMarker"/> を無条件で削除し、
        /// Build / Play 成果物に marker が含まれないことを保証する。
        /// <see cref="WorldPassMarker.DestroyAfterProcessing"/> は Pass 間での marker 保持を制御するフラグであり、
        /// この最終ストリップには影響しない。
        /// </summary>
        private static void StripMarkers(WorldBuildContext context)
        {
            context.RefreshMarkers();

            foreach (var marker in context.Markers.ToArray())
            {
                if (marker != null)
                {
                    UnityEngine.Object.DestroyImmediate(marker);
                }
            }
        }

        private static bool SupportsSession(IWorldBuildPass pass, WorldSessionKind sessionKind)
        {
            switch (sessionKind)
            {
                case WorldSessionKind.Play:
                    return (pass.Targets & WorldBuildPassTargets.Play) != 0;
                case WorldSessionKind.Build:
                default:
                    return (pass.Targets & WorldBuildPassTargets.Build) != 0;
            }
        }
    }
}
