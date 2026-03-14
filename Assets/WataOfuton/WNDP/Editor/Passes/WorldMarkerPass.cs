using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Editor
{
    /// <summary>
    /// marker ベースの Pass の共通基底クラス。
    /// marker 列挙、platform 判定、処理後の marker 破棄を共通化する。
    /// </summary>
    /// <typeparam name="TMarker">この Pass が扱う marker 型。</typeparam>
    public abstract class WorldMarkerPass<TMarker> : WorldBuildPassBase where TMarker : WorldPassMarker
    {
        private TMarker[] _snapshot;

        public sealed override bool AppliesTo(WorldBuildContext context)
        {
            _snapshot = EnumerateMarkers(context, context.PassContext).ToArray();
            return _snapshot.Length > 0;
        }

        public sealed override void Validate(WorldBuildContext context, IWorldValidationSink sink)
        {
            foreach (var marker in _snapshot)
            {
                ValidateMarker(context.PassContext, marker, sink);
            }
        }

        public sealed override void Execute(WorldBuildContext context)
        {
            var passContext = context.PassContext;

            foreach (var marker in _snapshot)
            {
                ExecuteMarker(passContext, marker);

                if (ShouldDestroyMarkerAfterExecute(passContext, marker))
                {
                    DestroyMarker(passContext, marker);
                }
            }

            _snapshot = null;
        }

        /// <summary>
        /// この Pass が現在の session で marker を扱うかどうかを判定する。
        /// 既定では null 判定と platform mask 判定を行う。
        /// </summary>
        protected virtual bool AppliesToMarker(WorldPassContext context, TMarker marker)
        {
            return marker != null && marker.AppliesTo(TargetPlatform(context));
        }

        /// <summary>
        /// 実行前に marker を検証する。
        /// </summary>
        protected virtual void ValidateMarker(WorldPassContext context, TMarker marker, IWorldValidationSink sink)
        {
        }

        /// <summary>
        /// 実行後に session Scene から marker を除去するかどうかを判定する。
        /// </summary>
        protected virtual bool ShouldDestroyMarkerAfterExecute(WorldPassContext context, TMarker marker)
        {
            return true;
        }

        /// <summary>
        /// 各 marker に対する Pass 本体を実行する。
        /// </summary>
        protected abstract void ExecuteMarker(WorldPassContext context, TMarker marker);

        /// <summary>
        /// この Pass が扱う marker 一覧を返す。
        /// discovery や ordering を変えたい場合のみ override する。
        /// </summary>
        protected virtual IEnumerable<TMarker> EnumerateMarkers(WorldBuildContext buildContext, WorldPassContext passContext)
        {
            return buildContext.GetMarkers<TMarker>()
                .Where(marker => marker != null)
                .Where(marker => AppliesToMarker(passContext, marker));
        }

        /// <summary>
        /// <see cref="WorldTransientAssets" /> を通して、現在の session 用 transient clone を生成する。
        /// </summary>
        /// <typeparam name="T">clone 対象となる Unity Object 型。</typeparam>
        /// <param name="context">現在の Pass context。</param>
        /// <param name="source">clone する asset 由来の Unity Object。</param>
        /// <param name="owner">
        /// 将来の ownership、diagnostics、lifecycle tracking 用に予約された引数。
        /// <see cref="WorldTransientAssets.CloneForSession{T}(T,UnityEngine.Object,string)" /> へそのまま転送される。
        /// </param>
        /// <param name="nameHint">clone 名に使われる補助ヒント。</param>
        protected T CloneForSession<T>(WorldPassContext context, T source, UnityEngine.Object owner, string nameHint = null)
            where T : UnityEngine.Object
        {
            return context.TransientAssets.CloneForSession(source, owner, nameHint);
        }

        /// <summary>
        /// 現在の session Scene 上に新しい GameObject を生成する。
        /// </summary>
        protected GameObject CreateObject(WorldPassContext context, string name, Transform parent = null)
        {
            return context.TransientObjects.Create(name, parent);
        }

        /// <summary>
        /// 現在の session Scene 上で GameObject を複製する。
        /// </summary>
        protected GameObject CloneObject(
            WorldPassContext context,
            GameObject source,
            Transform parent = null,
            string nameOverride = null)
        {
            return context.TransientObjects.Clone(source, parent, nameOverride);
        }

        /// <summary>
        /// component を保持する GameObject を複製し、複製先の同型 component を返す。
        /// </summary>
        protected T CloneComponentHost<T>(
            WorldPassContext context,
            T sourceComponent,
            Transform parent = null,
            string nameOverride = null)
            where T : Component
        {
            return context.TransientObjects.CloneComponentHost(sourceComponent, parent, nameOverride);
        }

        /// <summary>
        /// prefab asset を現在の session Scene 上へ instance 化する。
        /// </summary>
        protected GameObject InstantiatePrefab(
            WorldPassContext context,
            GameObject prefabAsset,
            Transform parent = null,
            string nameOverride = null)
        {
            return context.TransientObjects.InstantiatePrefab(prefabAsset, parent, nameOverride);
        }

        /// <summary>
        /// 生成物をまとめる generated root を取得し、存在しなければ生成する。
        /// </summary>
        protected GameObject GetOrCreateGeneratedRoot(WorldPassContext context, string name, Transform parent = null)
        {
            return context.TransientObjects.GetOrCreateGeneratedRoot(name, parent);
        }

        /// <summary>
        /// 指定した GameObject に component が無ければ追加し、存在すれば既存の component を返す。
        /// </summary>
        protected T EnsureComponent<T>(WorldPassContext context, GameObject target)
            where T : Component
        {
            return context.TransientObjects.EnsureComponent<T>(target);
        }

        /// <summary>
        /// 指定した component を現在の session Scene 上で破棄する。
        /// </summary>
        protected void DestroyComponent(WorldPassContext context, Component target)
        {
            context.TransientObjects.DestroyComponent(target);
        }

        /// <summary>
        /// 指定した Transform の local 位置・回転・スケールを既定値へ戻す。
        /// </summary>
        protected void ResetLocalTransform(WorldPassContext context, Transform target)
        {
            context.TransientObjects.ResetLocalTransform(target);
        }

        /// <summary>
        /// source の Transform 値を destination へコピーする。
        /// </summary>
        protected void CopyTransform(WorldPassContext context, Transform source, Transform destination)
        {
            context.TransientObjects.CopyTransform(source, destination);
        }

        /// <summary>
        /// session の終了時に実行する cleanup を登録する。
        /// </summary>
        protected void RegisterCleanup(WorldPassContext context, Action cleanupAction)
        {
            context.RegisterCleanup(cleanupAction);
        }

        /// <summary>
        /// 実行後に session Scene から marker component を破棄する。
        /// Source Scene 側は変更しない。
        /// </summary>
        protected void DestroyMarker(WorldPassContext context, TMarker marker)
        {
            context.DestroyMarkerAfterProcessing(marker);
        }

        /// <summary>
        /// 現在の session Scene 上の GameObject を破棄する。
        /// </summary>
        protected void DestroyObject(WorldPassContext context, GameObject target)
        {
            context.TransientObjects.Destroy(target);
        }

        /// <summary>
        /// 現在の session Scene 上の GameObject の active 状態を切り替える。
        /// </summary>
        protected void SetObjectActive(WorldPassContext context, GameObject target, bool active)
        {
            context.TransientObjects.SetActive(target, active);
        }

        /// <summary>
        /// 現在の処理が Build session かどうかを判定する。
        /// </summary>
        protected bool IsBuild(WorldPassContext context)
        {
            return CurrentSessionKind(context) == WorldSessionKind.Build;
        }

        /// <summary>
        /// 現在の処理が Play session かどうかを判定する。
        /// </summary>
        protected bool IsPlay(WorldPassContext context)
        {
            return CurrentSessionKind(context) == WorldSessionKind.Play;
        }

        /// <summary>
        /// 現在の target platform mask に <paramref name="platformMask" /> のいずれかの bit が含まれるかを返す。
        /// これは flags の包含判定であり、完全一致判定ではない。
        /// </summary>
        protected bool IsPlatform(WorldPassContext context, WorldBuildPlatformMask platformMask)
        {
            return (TargetPlatform(context) & platformMask) != 0;
        }

        /// <summary>
        /// 現在の Pass 実行における target platform mask を返す。
        /// </summary>
        protected WorldBuildPlatformMask TargetPlatform(WorldPassContext context)
        {
            return context.TargetPlatformMask;
        }

        /// <summary>
        /// 現在の session が処理中の Scene を返す。
        /// </summary>
        protected Scene SessionScene(WorldPassContext context)
        {
            return context.Scene;
        }

        /// <summary>
        /// 現在の session kind を返す。
        /// </summary>
        protected WorldSessionKind CurrentSessionKind(WorldPassContext context)
        {
            return context.SessionKind;
        }

        /// <summary>
        /// 現在の session identifier を返す。
        /// </summary>
        protected string CurrentSessionId(WorldPassContext context)
        {
            return context.SessionId;
        }
    }
}