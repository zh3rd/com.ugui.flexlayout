using UnityEngine;

namespace UnityEngine.UI.Flex
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Layout/Flex Layout")]
    public sealed partial class FlexLayout : MonoBehaviour
    {
        public FlexStyle style = FlexStyle.Default;
        public FlexImplicitItemStyleData implicitItemDefaults = FlexImplicitItemStyleData.Default;

        private RectTransform m_RectTransform;

        public RectTransform rectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    m_RectTransform = GetComponent<RectTransform>();
                }

                return m_RectTransform;
            }
        }

        private void Reset()
        {
            style.flexDirection = FlexDirection.Row;
            style.flexWrap = FlexWrap.NoWrap;
            style.justifyContent = JustifyContent.FlexStart;
            style.alignItems = AlignItems.Stretch;
            style.alignContent = AlignContent.FlexStart;
            style.mainGap = 0f;
            style.crossGap = 0f;
            style.padding = FlexEdges.Zero;
            implicitItemDefaults = FlexImplicitItemStyleData.Default;
        }
    }
}
