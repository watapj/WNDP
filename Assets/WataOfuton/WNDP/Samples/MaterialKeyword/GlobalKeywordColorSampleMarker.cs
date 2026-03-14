using UnityEngine;
using WataOfuton.Tool.WNDP;

namespace WataOfuton.Tool.WNDP.Samples
{
    [DisallowMultipleComponent]
    public sealed class GlobalKeywordColorSampleMarker : WorldPassMarker
    {
        [SerializeField]
        private WorldBuildPlatformMask _keywordEnabledPlatforms = WorldBuildPlatformMask.StandaloneWindows;

        [SerializeField]
        private string _keywordName = "WATA_WNDP_SAMPLE_COLOR_SWAP";

        [SerializeField]
        private string _expectedShaderName = "WataOfuton/WNDP/Samples/GlobalKeywordColor";

        public Renderer TargetRenderer => GetComponent<Renderer>();

        public WorldBuildPlatformMask KeywordEnabledPlatforms => _keywordEnabledPlatforms;

        public string KeywordName => _keywordName;

        public string ExpectedShaderName => _expectedShaderName;

        public bool ShouldEnableKeyword(WorldBuildPlatformMask currentPlatform)
        {
            return (_keywordEnabledPlatforms & currentPlatform) != 0;
        }
    }
}
