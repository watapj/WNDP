using UnityEditor;
using UnityEngine;

namespace WataOfuton.Tool.WNDP.Editor
{
    internal static class WorldBuildReportMenu
    {
        private const string ExportLastBuildReportMenuPath =
            "Window/WataOfuton/VRCW Non-Destructive Pipeline/Export Last Build Report";

        private const string ExportLastPlayReportMenuPath =
            "Window/WataOfuton/VRCW Non-Destructive Pipeline/Export Last Play Report";

        [MenuItem(ExportLastBuildReportMenuPath)]
        private static void ExportLastBuildReport()
        {
            ExportLastReport(WorldSessionKind.Build);
        }

        [MenuItem(ExportLastBuildReportMenuPath, true)]
        private static bool ValidateExportLastBuildReport()
        {
            return WorldBuildArtifactStore.HasLastReport(WorldSessionKind.Build);
        }

        [MenuItem(ExportLastPlayReportMenuPath)]
        private static void ExportLastPlayReport()
        {
            ExportLastReport(WorldSessionKind.Play);
        }

        [MenuItem(ExportLastPlayReportMenuPath, true)]
        private static bool ValidateExportLastPlayReport()
        {
            return WorldBuildArtifactStore.HasLastReport(WorldSessionKind.Play);
        }

        private static void ExportLastReport(WorldSessionKind sessionKind)
        {
            if (!WorldBuildArtifactStore.ExportLastReport(sessionKind, out var assetPath))
            {
                Debug.LogWarning($"[WNDP] No cached {sessionKind} report is available to export.");
                return;
            }

            Debug.Log($"[WNDP] Exported {sessionKind} report to '{assetPath}'.");
        }
    }
}
