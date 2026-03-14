using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WataOfuton.Tool.WNDP.Editor
{
    [Serializable]
    public sealed class WorldBuildExecutionReport
    {
        public string sessionId;
        public string sessionKind;
        public string requestedBuildType;
        public string buildTarget;
        public string sourceScenePath;
        public string buildScenePath;
        public string startedAtUtc;
        public string completedAtUtc;
        public bool succeeded;
        public string failureMessage;
        public List<WorldBuildPassReportEntry> passes = new List<WorldBuildPassReportEntry>();
        public List<WorldBuildDiagnostic> diagnostics = new List<WorldBuildDiagnostic>();
        public List<WorldTransientCloneReportEntry> transientClones = new List<WorldTransientCloneReportEntry>();

        internal static WorldBuildExecutionReport Create(
            WorldBuildSession session,
            string buildScenePath,
            BuildTarget buildTarget)
        {
            return new WorldBuildExecutionReport
            {
                sessionId = session.SessionId,
                sessionKind = session.Kind.ToString(),
                requestedBuildType = session.RequestedBuildType.ToString(),
                buildTarget = buildTarget.ToString(),
                sourceScenePath = session.SourceScenePath,
                buildScenePath = buildScenePath,
                startedAtUtc = DateTime.UtcNow.ToString("O"),
                succeeded = false
            };
        }

        public void AddPass(
            IWorldBuildPass pass,
            bool applied,
            TimeSpan duration,
            string errorMessage = null)
        {
            passes.Add(new WorldBuildPassReportEntry
            {
                passType = pass.GetType().FullName,
                displayName = pass.DisplayName,
                phase = pass.Phase.ToString(),
                order = pass.Order,
                applied = applied,
                durationMilliseconds = duration.TotalMilliseconds,
                errorMessage = errorMessage
            });
        }

        public void AddDiagnostic(string severity, string message, string contextName)
        {
            diagnostics.Add(new WorldBuildDiagnostic
            {
                severity = severity,
                message = message,
                context = contextName
            });
        }

        public void AddTransientClone(UnityEngine.Object source, UnityEngine.Object clone, UnityEngine.Object owner, string nameHint)
        {
            transientClones.Add(new WorldTransientCloneReportEntry
            {
                sourceType = GetObjectTypeName(source),
                sourceName = GetObjectName(source),
                sourceAssetPath = GetAssetPathSafe(source),
                cloneType = GetObjectTypeName(clone),
                cloneName = GetObjectName(clone),
                ownerType = GetObjectTypeName(owner),
                ownerName = GetObjectName(owner),
                ownerAssetPath = GetAssetPathSafe(owner),
                ownerHierarchyPath = GetHierarchyPath(owner),
                nameHint = nameHint ?? string.Empty
            });
        }

        public void MarkSucceeded()
        {
            succeeded = true;
            completedAtUtc = DateTime.UtcNow.ToString("O");
        }

        public void MarkFailed(string message)
        {
            succeeded = false;
            failureMessage = message;
            completedAtUtc = DateTime.UtcNow.ToString("O");
        }

        private static string GetObjectTypeName(UnityEngine.Object value)
        {
            return value != null ? value.GetType().FullName : string.Empty;
        }

        private static string GetObjectName(UnityEngine.Object value)
        {
            return value != null ? value.name : string.Empty;
        }

        private static string GetAssetPathSafe(UnityEngine.Object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return AssetDatabase.GetAssetPath(value) ?? string.Empty;
        }

        private static string GetHierarchyPath(UnityEngine.Object value)
        {
            switch (value)
            {
                case Component component:
                    return BuildHierarchyPath(component.transform);
                case GameObject gameObject:
                    return BuildHierarchyPath(gameObject.transform);
                default:
                    return string.Empty;
            }
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            var names = new Stack<string>();
            var current = transform;

            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return "/" + string.Join("/", names);
        }
    }

    [Serializable]
    public sealed class WorldBuildPassReportEntry
    {
        public string passType;
        public string displayName;
        public string phase;
        public int order;
        public bool applied;
        public double durationMilliseconds;
        public string errorMessage;
    }

    [Serializable]
    public sealed class WorldBuildDiagnostic
    {
        public string severity;
        public string message;
        public string context;
    }

    [Serializable]
    public sealed class WorldTransientCloneReportEntry
    {
        public string sourceType;
        public string sourceName;
        public string sourceAssetPath;
        public string cloneType;
        public string cloneName;
        public string ownerType;
        public string ownerName;
        public string ownerAssetPath;
        public string ownerHierarchyPath;
        public string nameHint;
    }
}
