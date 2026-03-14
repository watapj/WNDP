using UnityEngine;
using Object = UnityEngine.Object;

namespace WataOfuton.Tool.WNDP
{
    public sealed class ReferenceSwapMarker : WorldPassMarker
    {
        [SerializeField]
        private Component _targetComponent;

        [SerializeField]
        private string _propertyPath;

        [SerializeField]
        private Object _standaloneObject;

        [SerializeField]
        private Object _androidObject;

        public Component TargetComponent => _targetComponent;

        public string PropertyPath => _propertyPath;

        public Object StandaloneObject => _standaloneObject;

        public Object AndroidObject => _androidObject;

        public Object GetReplacement(WorldBuildPlatformMask currentPlatform)
        {
            if ((currentPlatform & WorldBuildPlatformMask.Android) != 0)
            {
                return _androidObject;
            }

            if ((currentPlatform & WorldBuildPlatformMask.StandaloneWindows) != 0)
            {
                return _standaloneObject;
            }

            return null;
        }
    }
}
