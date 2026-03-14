using System.Collections.Generic;
using UnityEngine;

namespace WataOfuton.Tool.WNDP.Editor
{
    public interface IWorldValidationSink
    {
        bool HasErrors { get; }

        IReadOnlyList<WorldBuildDiagnostic> Diagnostics { get; }

        void Error(string message, Object context = null);

        void Warning(string message, Object context = null);
    }
}
