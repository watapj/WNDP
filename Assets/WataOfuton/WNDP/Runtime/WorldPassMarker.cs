using UnityEngine;

namespace WataOfuton.Tool.WNDP
{
    public abstract class WorldPassMarker : MonoBehaviour
    {
        [SerializeField]
        private bool _enabled = true;

        [SerializeField]
        private WorldBuildPlatformMask _platformMask = WorldBuildPlatformMask.All;

        [SerializeField]
        private bool _destroyAfterProcessing = true;

        public bool Enabled => _enabled;

        public WorldBuildPlatformMask PlatformMask => _platformMask;

        /// <summary>
        /// Pass 実行後に marker コンポーネントを即時削除するかどうか。
        /// false の場合、後続の Pass からも参照可能な状態で残る。
        /// いずれの場合も全 Pass 実行後の最終ストリップで無条件に削除される。
        /// </summary>
        public bool DestroyAfterProcessing => _destroyAfterProcessing;

        public bool AppliesTo(WorldBuildPlatformMask currentPlatform)
        {
            return _enabled && (_platformMask & currentPlatform) != 0;
        }
    }
}
