using TMPro;
using UnityEngine;
using UnityEngine.UI.Flex.Core;

namespace UnityEngine.UI.Flex
{
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Layout/Flex Text")]
    public sealed class FlexText : FlexNodeBase
    {
        protected override bool useRectAsDefaultSizeInput => false;
        internal override bool hasSpecializedContentMeasurement => true;

        private TMP_Text m_Text;

        private TMP_Text textComponent
        {
            get
            {
                if (m_Text == null)
                {
                    m_Text = GetComponent<TMP_Text>();
                }

                return m_Text;
            }
        }

        protected override void OnEnable()
        {
            RegisterDirtyCallbacks();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            UnregisterDirtyCallbacks();
            base.OnDisable();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
        }

        private void HandleTextLayoutDirty()
        {
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }

        private void RegisterDirtyCallbacks()
        {
            var text = textComponent;
            if (text == null)
            {
                return;
            }

            text.RegisterDirtyLayoutCallback(HandleTextLayoutDirty);
            text.RegisterDirtyVerticesCallback(HandleTextLayoutDirty);
        }

        private void UnregisterDirtyCallbacks()
        {
            if (m_Text == null)
            {
                return;
            }

            m_Text.UnregisterDirtyLayoutCallback(HandleTextLayoutDirty);
            m_Text.UnregisterDirtyVerticesCallback(HandleTextLayoutDirty);
        }

        internal override bool TryMeasureContent(Vector2 implicitRectSize, out FlexContentMeasureResult result)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var scope = FlexProfiler.MeasureText.Auto();
            try
            {
                result = default;
                var text = textComponent;
                if (text == null)
                {
                    return false;
                }

                text.ForceMeshUpdate();
                var preferred = text.GetPreferredValues();
                var contentSize = new Vector2(Mathf.Max(0f, preferred.x), Mathf.Max(0f, preferred.y));
                if (text.enableWordWrapping)
                {
                    var widthConstraint = ResolveWrappedMeasureWidthConstraint(implicitRectSize, contentSize.x);

                    var constrained = text.GetPreferredValues(widthConstraint, float.PositiveInfinity);
                    contentSize.x = Mathf.Max(0f, constrained.x);
                    if (!float.IsInfinity(widthConstraint) && widthConstraint > 0f)
                    {
                        contentSize.x = Mathf.Min(contentSize.x, widthConstraint);
                    }
                    contentSize.y = Mathf.Max(0f, constrained.y);
                }

                result = new FlexContentMeasureResult(contentSize, allowImplicitRectPassthrough: false, hasSpecializedSource: true);
                return true;
            }
            finally
            {
                FlexRuntimeSampling.AddMeasureTextTicks(samplingStartedAt);
            }
        }

        private float ResolveWrappedMeasureWidthConstraint(Vector2 implicitRectSize, float fallbackContentWidth)
        {
            var widthConstraint = float.PositiveInfinity;
            var hasParentLayout = FlexAuthoringUtility.TryGetActiveDirectParentLayout(transform, out var parentLayout);

            if (style.width.mode == FlexSizeMode.Points)
            {
                widthConstraint = Mathf.Min(widthConstraint, Mathf.Max(0f, style.width.value));
            }

            if (style.maxWidth.enabled)
            {
                widthConstraint = Mathf.Min(widthConstraint, Mathf.Max(0f, style.maxWidth.value));
            }

            if (style.width.mode == FlexSizeMode.Auto && implicitRectSize.x > 0f && !hasParentLayout)
            {
                widthConstraint = Mathf.Min(widthConstraint, implicitRectSize.x);
            }

            if (hasParentLayout)
            {
                var parentWidth = ResolveAxisInput(parentLayout.rectTransform, RectTransform.Axis.Horizontal);
                var parentInnerWidth = Mathf.Max(0f, parentWidth - parentLayout.style.padding.left - parentLayout.style.padding.right);
                if (style.width.mode == FlexSizeMode.Percent)
                {
                    widthConstraint = Mathf.Min(widthConstraint, parentInnerWidth * Mathf.Max(0f, style.width.value) * 0.01f);
                }

                if (parentInnerWidth > 0f)
                {
                    widthConstraint = Mathf.Min(widthConstraint, parentInnerWidth);
                }
            }

            if (float.IsInfinity(widthConstraint) || widthConstraint <= 0f)
            {
                widthConstraint = Mathf.Max(0f, fallbackContentWidth);
            }

            return widthConstraint;
        }

        private static float ResolveAxisInput(RectTransform target, RectTransform.Axis axis)
        {
            var rectSize = axis == RectTransform.Axis.Horizontal ? target.rect.width : target.rect.height;
            if (rectSize > 0f)
            {
                return rectSize;
            }

            var sizeDelta = axis == RectTransform.Axis.Horizontal ? target.sizeDelta.x : target.sizeDelta.y;
            return Mathf.Abs(sizeDelta);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }
#endif
    }
}
