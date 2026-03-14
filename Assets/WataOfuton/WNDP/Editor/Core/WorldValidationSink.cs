using System.Collections.Generic;
using UnityEngine;

namespace WataOfuton.Tool.WNDP.Editor
{
    internal sealed class WorldValidationSink : IWorldValidationSink
    {
        private readonly WorldBuildExecutionReport _report;
        private readonly List<WorldBuildDiagnostic> _diagnostics = new List<WorldBuildDiagnostic>();

        public WorldValidationSink(WorldBuildExecutionReport report)
        {
            _report = report;
        }

        public bool HasErrors { get; private set; }

        public IReadOnlyList<WorldBuildDiagnostic> Diagnostics => _diagnostics;

        public void Error(string message, Object context = null)
        {
            HasErrors = true;
            AddDiagnostic("Error", message, context);
            Debug.LogError(FormatMessage(message), context);
        }

        public void Warning(string message, Object context = null)
        {
            AddDiagnostic("Warning", message, context);
            Debug.LogWarning(FormatMessage(message), context);
        }

        private void AddDiagnostic(string severity, string message, Object context)
        {
            var diagnostic = new WorldBuildDiagnostic
            {
                severity = severity,
                message = message,
                context = context != null ? context.name : string.Empty
            };

            _diagnostics.Add(diagnostic);
            _report.AddDiagnostic(diagnostic.severity, diagnostic.message, diagnostic.context);
        }

        private static string FormatMessage(string message)
        {
            return $"[WNDP] {message}";
        }
    }
}
