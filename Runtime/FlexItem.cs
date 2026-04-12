using UnityEngine;

using UnityEngine.UI.Flex.Core;

namespace UnityEngine.UI.Flex
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Layout/Flex Item")]
    public sealed class FlexItem : MonoBehaviour
    {
        public FlexItemStyleData style = FlexItemStyleData.Default;

        private void Reset()
        {
            style = FlexItemStyleData.Default;
        }

        private void OnEnable()
        {
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }

        private void OnDisable()
        {
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }

        private void OnTransformParentChanged()
        {
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            style.flexGrow = Mathf.Max(0f, style.flexGrow);
            style.flexShrink = Mathf.Max(0f, style.flexShrink);
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }
#endif
    }
}
