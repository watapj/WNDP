using System;
using UnityEngine;

namespace WataOfuton.Tool.WNDP.Editor
{
    /// <summary>
    /// asset 的な Unity Object に対して、session 専用の transient clone を生成する。
    /// 現在の Build / Play では <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/> によるインメモリ clone を用いる。
    /// </summary>
    public sealed class WorldTransientAssets
    {
        private readonly WorldBuildExecutionReport _executionReport;

        internal WorldTransientAssets(WorldBuildExecutionReport executionReport)
        {
            _executionReport = executionReport ?? throw new ArgumentNullException(nameof(executionReport));
        }

        /// <summary>
        /// <paramref name="source"/> の session 専用インメモリ clone を返す。
        /// 返された clone は、Build / Play 中に処理対象 Scene へ再代入されることを前提とする。
        /// </summary>
        /// <typeparam name="T">clone 対象となる Unity Object 型。</typeparam>
        /// <param name="source">現在の session 用に clone する asset 的な Unity Object。</param>
        /// <param name="owner">
        /// 将来の ownership、diagnostics、lifecycle tracking 用に予約されている引数。
        /// 現在の実装では、この値は clone の寿命、命名、到達性の判定には使われない。
        /// </param>
        /// <param name="nameHint">clone 名にのみ使われる任意の接尾ヒント。</param>
        /// <returns>
        /// <paramref name="source"/> が <see langword="null"/> の場合は <see langword="null"/>。
        /// それ以外の場合は session 専用 clone。
        /// </returns>
        /// <remarks>
        /// <see cref="GameObject"/> と <see cref="Component"/> の clone は意図的に対象外とする。
        /// Scene object / prefab の複製は別 API で扱う。
        /// </remarks>
        public T CloneForSession<T>(T source, UnityEngine.Object owner, string nameHint = null)
            where T : UnityEngine.Object
        {
            if (source == null)
            {
                return null;
            }

            if (source is GameObject || source is Component)
            {
                throw new NotSupportedException(
                    $"Transient asset cloning does not support scene object types like '{source.GetType().FullName}'.");
            }

            var clone = UnityEngine.Object.Instantiate(source);
            if (clone == null)
            {
                return null;
            }

            clone.name = BuildCloneName(source, nameHint);
            _executionReport.AddTransientClone(source, clone, owner, nameHint);
            return clone;
        }

        private static string BuildCloneName(UnityEngine.Object source, string nameHint)
        {
            if (!string.IsNullOrWhiteSpace(nameHint))
            {
                return $"{source.name} ({nameHint})";
            }

            return $"{source.name} (Transient Clone)";
        }
    }
}
