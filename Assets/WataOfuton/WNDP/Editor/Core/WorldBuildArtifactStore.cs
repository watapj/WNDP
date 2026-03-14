using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace WataOfuton.Tool.WNDP.Editor
{
    /// <summary>
    /// 直近の Build / Play report を Editor session 内に保持し、
    /// 必要になったタイミングで明示 export する。
    /// </summary>
    public static class WorldBuildArtifactStore
    {
        public const string ReportExportFolderRelativePath = "Assets/WNDP Reports";

        private const string LastBuildReportSessionKey = "WataOfuton.Tool.WNDP.LastBuildReport";
        private const string LastPlayReportSessionKey = "WataOfuton.Tool.WNDP.LastPlayReport";

        public static string ReportExportFolderPath { get; } = ToAbsolutePath(ReportExportFolderRelativePath);

        public static string ReportExportFolderAssetPath => ReportExportFolderRelativePath;

        private static string _lastBuildReportJson;
        private static string _lastPlayReportJson;

        public static void StoreLastReport(WorldBuildExecutionReport report)
        {
            if (report == null)
            {
                throw new System.ArgumentNullException(nameof(report));
            }

            var json = JsonUtility.ToJson(report, true);
            var sessionKind = ParseSessionKind(report.sessionKind);

            switch (sessionKind)
            {
                case WorldSessionKind.Play:
                    _lastPlayReportJson = json;
                    SessionState.SetString(LastPlayReportSessionKey, json);
                    break;
                case WorldSessionKind.Build:
                default:
                    _lastBuildReportJson = json;
                    SessionState.SetString(LastBuildReportSessionKey, json);
                    break;
            }
        }

        public static bool HasLastReport(WorldSessionKind sessionKind)
        {
            return !string.IsNullOrEmpty(GetLastReportJson(sessionKind));
        }

        public static bool TryGetLastReport(WorldSessionKind sessionKind, out WorldBuildExecutionReport report)
        {
            var json = GetLastReportJson(sessionKind);
            if (string.IsNullOrWhiteSpace(json))
            {
                report = null;
                return false;
            }

            report = JsonUtility.FromJson<WorldBuildExecutionReport>(json);
            return report != null;
        }

        public static bool ExportLastReport(WorldSessionKind sessionKind, out string assetPath)
        {
            if (!TryGetLastReport(sessionKind, out var report))
            {
                assetPath = null;
                return false;
            }

            Directory.CreateDirectory(ReportExportFolderPath);

            var fileName = BuildReportFileName(report);
            assetPath = BuildUniqueExportAssetPath(fileName);
            var absolutePath = ToAbsolutePath(assetPath);
            var encoding = new UTF8Encoding(false);
            var json = JsonUtility.ToJson(report, true);

            File.WriteAllText(absolutePath, json, encoding);
            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }

            return true;
        }

        private static string ToAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        private static string CombineAssetPath(string left, string right)
        {
            return $"{left.TrimEnd('/', '\\')}/{right.TrimStart('/', '\\')}";
        }

        private static string BuildUniqueExportAssetPath(string fileName)
        {
            var assetPath = CombineAssetPath(ReportExportFolderRelativePath, fileName);
            if (!File.Exists(ToAbsolutePath(assetPath)))
            {
                return assetPath;
            }

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            for (var index = 2; ; index++)
            {
                var candidateFileName = $"{baseName}_{index}{extension}";
                var candidateAssetPath = CombineAssetPath(ReportExportFolderRelativePath, candidateFileName);
                if (!File.Exists(ToAbsolutePath(candidateAssetPath)))
                {
                    return candidateAssetPath;
                }
            }
        }

        private static string GetLastReportJson(WorldSessionKind sessionKind)
        {
            switch (sessionKind)
            {
                case WorldSessionKind.Play:
                    if (!string.IsNullOrEmpty(_lastPlayReportJson))
                    {
                        return _lastPlayReportJson;
                    }

                    _lastPlayReportJson = SessionState.GetString(LastPlayReportSessionKey, string.Empty);
                    return _lastPlayReportJson;

                case WorldSessionKind.Build:
                default:
                    if (!string.IsNullOrEmpty(_lastBuildReportJson))
                    {
                        return _lastBuildReportJson;
                    }

                    _lastBuildReportJson = SessionState.GetString(LastBuildReportSessionKey, string.Empty);
                    return _lastBuildReportJson;
            }
        }

        private static string BuildReportFileName(WorldBuildExecutionReport report)
        {
            var sessionKind = string.IsNullOrWhiteSpace(report.sessionKind) ? "Session" : report.sessionKind;
            var buildTarget = string.IsNullOrWhiteSpace(report.buildTarget) ? "UnknownTarget" : report.buildTarget;
            var sessionId = string.IsNullOrWhiteSpace(report.sessionId) ? "NoSessionId" : report.sessionId;
            var timestamp = TryFormatTimestamp(report.startedAtUtc);
            var result = report.succeeded ? "Succeeded" : "Failed";

            return $"WNDP_{sessionKind}_{buildTarget}_{timestamp}_{sessionId}_{result}.json";
        }

        private static string TryFormatTimestamp(string rawValue)
        {
            if (System.DateTime.TryParse(rawValue, out var dateTime))
            {
                return dateTime.ToLocalTime().ToString("yyyyMMdd_HHmmss");
            }

            return System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        private static WorldSessionKind ParseSessionKind(string rawValue)
        {
            if (System.Enum.TryParse(rawValue, out WorldSessionKind sessionKind))
            {
                return sessionKind;
            }

            Debug.LogWarning(
                $"[WNDP] Unknown sessionKind '{rawValue ?? "<null>"}' in report cache. Falling back to Build.");
            return WorldSessionKind.Build;
        }
    }
}
