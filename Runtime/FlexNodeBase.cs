using UnityEngine;
using UnityEngine.UI.Flex.Core;

namespace UnityEngine.UI.Flex
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public abstract class FlexNodeBase : MonoBehaviour
    {
        public FlexNodeStyleData style = FlexNodeStyleData.Default;
        protected virtual bool useRectAsDefaultSizeInput => true;
        internal virtual bool hasSpecializedContentMeasurement => false;

        private RectTransform m_RectTransform;
#if UNITY_EDITOR
        private bool m_EditorApplyQueued;
#endif

        protected RectTransform rectTransform
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

        protected virtual void Reset()
        {
            style = FlexNodeStyleData.Default;
            if (!useRectAsDefaultSizeInput)
            {
                return;
            }

            style.width = FlexValue.Points(ResolveInitialAxisSizeFromRect(rectTransform, RectTransform.Axis.Horizontal));
            style.height = FlexValue.Points(ResolveInitialAxisSizeFromRect(rectTransform, RectTransform.Axis.Vertical));
        }

        protected virtual void Awake()
        {
            InitializeStyleValuesFromRectIfDefault();
            if (!isActiveAndEnabled)
            {
                ReleaseDrivenProperties();
            }
        }

        protected virtual void OnEnable()
        {
            InitializeStyleValuesFromRectIfDefault();
            InitializeAspectRatioValueFromRectIfUnset();
            RefreshDrivenProperties();
            ApplyStandaloneSelfSizing();
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }

        protected virtual void OnDisable()
        {
            ReleaseDrivenProperties();
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }

        protected virtual void OnTransformParentChanged()
        {
            RefreshDrivenProperties();
            ApplyStandaloneSelfSizing();
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }

        protected virtual void OnDidApplyAnimationProperties()
        {
            FlexAuthoringUtility.NotifyAuthoringChanged(this);
        }

        protected void RefreshDrivenProperties()
        {
            ReleaseDrivenProperties();

            if (!isActiveAndEnabled)
            {
                return;
            }

            var hasFlexParent = FlexAuthoringUtility.TryGetActiveDirectParentLayout(transform, out _);
            var ownership = FlexOwnershipResolver.ResolveSelf(hasFlexParent, style.positionType);
            var driveMask = ownership.ToDriveMask();
            if (!driveMask.IsNone())
            {
                FlexDrivenRegistry.SetContribution(this, rectTransform, driveMask);
            }
        }

        internal void RefreshForParentContextChange()
        {
            RefreshDrivenProperties();
            ApplyStandaloneSelfSizing();
        }

        protected void ApplyStandaloneSelfSizing()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (TryGetComponent<FlexLayout>(out var selfLayout) && selfLayout.isActiveAndEnabled)
            {
                return;
            }

            var hasFlexParent = FlexAuthoringUtility.TryGetActiveDirectParentLayout(transform, out _);
            var ownership = FlexOwnershipResolver.ResolveSelf(hasFlexParent, style.positionType);
            if (!ownership.DriveSizeX && !ownership.DriveSizeY)
            {
                return;
            }

            var target = rectTransform.sizeDelta;
            var implicitRectSize = rectTransform.rect.size;
            var measuredContent = default(FlexContentMeasureResult);
            var hasMeasuredContent = hasSpecializedContentMeasurement
                && TryMeasureContent(implicitRectSize, out measuredContent)
                && measuredContent.HasSpecializedSource;

            if (ownership.DriveSizeX)
            {
                var autoContent = hasMeasuredContent
                    ? measuredContent.ContentSize.x
                    : ResolveCurrentAxisInput(RectTransform.Axis.Horizontal);
                var parentConstraintX = 0f;
                var hasParentConstraintX = style.width.mode == FlexSizeMode.Percent
                    && TryResolveParentAxisInput(RectTransform.Axis.Horizontal, out parentConstraintX);
                target.x = FlexSizing.ResolveConstrainedAxisSize(
                    style.width,
                    new FlexAutoAxisContext(
                        hasParentAssignedSize: false,
                        parentAssignedSize: 0f,
                        hasPercentReferenceSize: hasParentConstraintX,
                        percentReferenceSize: parentConstraintX,
                        hasExternalConstraint: false,
                        externalConstraintSize: 0f,
                        contentSize: autoContent),
                    style.minWidth,
                    style.maxWidth);
            }

            if (ownership.DriveSizeY)
            {
                var autoContent = hasMeasuredContent
                    ? measuredContent.ContentSize.y
                    : ResolveCurrentAxisInput(RectTransform.Axis.Vertical);
                var parentConstraintY = 0f;
                var hasParentConstraintY = style.height.mode == FlexSizeMode.Percent
                    && TryResolveParentAxisInput(RectTransform.Axis.Vertical, out parentConstraintY);
                target.y = FlexSizing.ResolveConstrainedAxisSize(
                    style.height,
                    new FlexAutoAxisContext(
                        hasParentAssignedSize: false,
                        parentAssignedSize: 0f,
                        hasPercentReferenceSize: hasParentConstraintY,
                        percentReferenceSize: parentConstraintY,
                        hasExternalConstraint: false,
                        externalConstraintSize: 0f,
                        contentSize: autoContent),
                    style.minHeight,
                    style.maxHeight);
            }

            target = FlexSizing.ApplyAspectRatioIfNeeded(style, target);

            if (rectTransform.sizeDelta != target)
            {
                rectTransform.sizeDelta = target;
            }
        }

        protected float ResolveCurrentAxisInput(RectTransform.Axis axis)
        {
            var rectSize = axis == RectTransform.Axis.Horizontal ? rectTransform.rect.width : rectTransform.rect.height;
            if (rectSize > 0f)
            {
                return rectSize;
            }

            var sizeDelta = axis == RectTransform.Axis.Horizontal ? rectTransform.sizeDelta.x : rectTransform.sizeDelta.y;
            return Mathf.Abs(sizeDelta);
        }

        protected bool TryResolveParentAxisInput(RectTransform.Axis axis, out float value)
        {
            value = 0f;
            if (!(rectTransform.parent is RectTransform parentRect))
            {
                return false;
            }

            var rectSize = axis == RectTransform.Axis.Horizontal ? parentRect.rect.width : parentRect.rect.height;
            if (rectSize > 0f)
            {
                value = rectSize;
                return true;
            }

            var sizeDelta = axis == RectTransform.Axis.Horizontal ? parentRect.sizeDelta.x : parentRect.sizeDelta.y;
            value = Mathf.Abs(sizeDelta);
            return value > 0f;
        }

        protected void ReleaseDrivenProperties()
        {
            FlexDrivenRegistry.ClearOwner(this);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            InitializeStyleValuesFromRectIfDefault();
            if (style.width.mode == FlexSizeMode.Points || style.width.mode == FlexSizeMode.Percent)
            {
                style.width.value = Mathf.Max(0f, style.width.value);
            }

            if (style.height.mode == FlexSizeMode.Points || style.height.mode == FlexSizeMode.Percent)
            {
                style.height.value = Mathf.Max(0f, style.height.value);
            }

            if (style.aspectRatio.enabled)
            {
                style.aspectRatio.value = Mathf.Max(0f, style.aspectRatio.value);
            }

            if (!isActiveAndEnabled)
            {
                ReleaseDrivenProperties();
                return;
            }

            RefreshDrivenProperties();
            FlexAuthoringUtility.NotifyAuthoringChanged(this);

            if (Application.isPlaying)
            {
                return;
            }

            QueueEditorStandaloneSizingApply();
        }

        protected void QueueEditorStandaloneSizingApply()
        {
            if (m_EditorApplyQueued)
            {
                return;
            }

            m_EditorApplyQueued = true;
            UnityEditor.EditorApplication.delayCall += ApplyStandaloneSizingOnEditorDelayCall;
        }

        protected void ApplyStandaloneSizingOnEditorDelayCall()
        {
            m_EditorApplyQueued = false;
            UnityEditor.EditorApplication.delayCall -= ApplyStandaloneSizingOnEditorDelayCall;
            if (this == null || !isActiveAndEnabled || Application.isPlaying)
            {
                return;
            }

            ApplyStandaloneSelfSizing();
        }
#endif

        protected void InitializeStyleValuesFromRectIfDefault()
        {
            if (!useRectAsDefaultSizeInput)
            {
                return;
            }

            if (style.width.mode != FlexSizeMode.Auto
                || style.height.mode != FlexSizeMode.Auto
                || !Mathf.Approximately(style.width.value, 0f)
                || !Mathf.Approximately(style.height.value, 0f))
            {
                return;
            }

            if (style.minWidth.enabled
                || style.maxWidth.enabled
                || style.minHeight.enabled
                || style.maxHeight.enabled
                || style.positionType != PositionType.Relative)
            {
                return;
            }

            style.width = FlexValue.Points(ResolveInitialAxisSizeFromRect(rectTransform, RectTransform.Axis.Horizontal));
            style.height = FlexValue.Points(ResolveInitialAxisSizeFromRect(rectTransform, RectTransform.Axis.Vertical));
        }

        protected void InitializeAspectRatioValueFromRectIfUnset()
        {
            if (!Mathf.Approximately(style.aspectRatio.value, 0f))
            {
                return;
            }

            var ratio = ResolveInitialAspectRatioFromRect(rectTransform);
            if (ratio > 0f)
            {
                style.aspectRatio.value = ratio;
            }
        }

        protected static float ResolveInitialAxisSizeFromRect(RectTransform rectTransform, RectTransform.Axis axis)
        {
            var rectSize = axis == RectTransform.Axis.Horizontal
                ? rectTransform.rect.width
                : rectTransform.rect.height;
            if (rectSize > 0f)
            {
                return rectSize;
            }

            var sizeDelta = axis == RectTransform.Axis.Horizontal
                ? rectTransform.sizeDelta.x
                : rectTransform.sizeDelta.y;
            if (!Mathf.Approximately(sizeDelta, 0f))
            {
                return Mathf.Abs(sizeDelta);
            }

            return 0f;
        }

        protected static float ResolveInitialAspectRatioFromRect(RectTransform rectTransform)
        {
            var width = ResolveInitialAxisSizeFromRect(rectTransform, RectTransform.Axis.Horizontal);
            var height = ResolveInitialAxisSizeFromRect(rectTransform, RectTransform.Axis.Vertical);
            if (width <= 0f || height <= 0f)
            {
                return 0f;
            }

            return width / height;
        }

        internal virtual bool TryMeasureContent(Vector2 implicitRectSize, out FlexContentMeasureResult result)
        {
            result = new FlexContentMeasureResult(
                implicitRectSize,
                allowImplicitRectPassthrough: true,
                hasSpecializedSource: false);
            return true;
        }
    }
}
