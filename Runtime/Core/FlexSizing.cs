using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal readonly struct FlexAutoAxisContext
    {
        public FlexAutoAxisContext(
            bool hasParentAssignedSize,
            float parentAssignedSize,
            bool hasExternalConstraint,
            float externalConstraintSize,
            float contentSize)
        {
            HasParentAssignedSize = hasParentAssignedSize;
            ParentAssignedSize = parentAssignedSize;
            HasExternalConstraint = hasExternalConstraint;
            ExternalConstraintSize = externalConstraintSize;
            ContentSize = contentSize;
        }

        public bool HasParentAssignedSize { get; }

        public float ParentAssignedSize { get; }

        public bool HasExternalConstraint { get; }

        public float ExternalConstraintSize { get; }

        public float ContentSize { get; }
    }

    internal readonly struct FlexImplicitItemDefaults : IEquatable<FlexImplicitItemDefaults>
    {
        public FlexImplicitItemDefaults(
            float width,
            float height,
            float mainAxisBasis,
            float flexGrow,
            float flexShrink,
            PositionType positionType)
        {
            Width = width;
            Height = height;
            MainAxisBasis = mainAxisBasis;
            FlexGrow = flexGrow;
            FlexShrink = flexShrink;
            PositionType = positionType;
        }

        public float Width { get; }

        public float Height { get; }

        public float MainAxisBasis { get; }

        public float FlexGrow { get; }

        public float FlexShrink { get; }

        public PositionType PositionType { get; }

        public bool Equals(FlexImplicitItemDefaults other)
        {
            return Mathf.Approximately(Width, other.Width)
                && Mathf.Approximately(Height, other.Height)
                && Mathf.Approximately(MainAxisBasis, other.MainAxisBasis)
                && Mathf.Approximately(FlexGrow, other.FlexGrow)
                && Mathf.Approximately(FlexShrink, other.FlexShrink)
                && PositionType == other.PositionType;
        }

        public override bool Equals(object obj)
        {
            return obj is FlexImplicitItemDefaults other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, MainAxisBasis, FlexGrow, FlexShrink, (int)PositionType);
        }
    }

    internal static class FlexSizing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAspectRatio(FlexOptionalFloat aspectRatio)
        {
            return aspectRatio.enabled && aspectRatio.value > 0f;
        }

        public static FlexMeasuredSize ApplyAspectRatioIfNeeded(
            FlexStyle style,
            float width,
            float height)
        {
            if (!HasAspectRatio(style.aspectRatio))
            {
                return new FlexMeasuredSize(width, height);
            }

            var ratio = style.aspectRatio.value;
            var resolvedWidth = width;
            var resolvedHeight = height;

            if (style.width.mode == FlexSizeMode.Auto && style.height.mode != FlexSizeMode.Auto)
            {
                resolvedWidth = ApplyConstraints(resolvedHeight * ratio, style.minWidth, style.maxWidth);
            }
            else if (style.height.mode == FlexSizeMode.Auto && style.width.mode != FlexSizeMode.Auto)
            {
                resolvedHeight = ApplyConstraints(resolvedWidth / ratio, style.minHeight, style.maxHeight);
            }

            return new FlexMeasuredSize(resolvedWidth, resolvedHeight);
        }

        public static Vector2 ApplyAspectRatioIfNeeded(
            FlexNodeStyleData style,
            Vector2 size)
        {
            if (!HasAspectRatio(style.aspectRatio))
            {
                return size;
            }

            var ratio = style.aspectRatio.value;
            var resolved = size;
            if (style.width.mode == FlexSizeMode.Auto && style.height.mode != FlexSizeMode.Auto)
            {
                resolved.x = ApplyConstraints(resolved.y * ratio, style.minWidth, style.maxWidth);
            }
            else if (style.height.mode == FlexSizeMode.Auto && style.width.mode != FlexSizeMode.Auto)
            {
                resolved.y = ApplyConstraints(resolved.x / ratio, style.minHeight, style.maxHeight);
            }

            return resolved;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ResolveCrossSizeFromMainWithAspect(
            FlexStyle style,
            bool isHorizontalMainAxis,
            float mainSize,
            float fallbackCrossSize)
        {
            if (!HasAspectRatio(style.aspectRatio))
            {
                return fallbackCrossSize;
            }

            if (isHorizontalMainAxis)
            {
                if (style.height.mode != FlexSizeMode.Auto)
                {
                    return fallbackCrossSize;
                }

                var cross = mainSize / style.aspectRatio.value;
                return ApplyConstraints(cross, style.minHeight, style.maxHeight);
            }

            if (style.width.mode != FlexSizeMode.Auto)
            {
                return fallbackCrossSize;
            }

            var verticalCross = mainSize * style.aspectRatio.value;
            return ApplyConstraints(verticalCross, style.minWidth, style.maxWidth);
        }

        public static float ResolveAxisSize(FlexValue size, FlexAutoAxisContext context)
        {
            if (size.mode == FlexSizeMode.Points)
            {
                return size.value;
            }

            if (size.mode == FlexSizeMode.Percent)
            {
                if (context.HasParentAssignedSize)
                {
                    return context.ParentAssignedSize * size.value * 0.01f;
                }

                if (context.HasExternalConstraint)
                {
                    return context.ExternalConstraintSize * size.value * 0.01f;
                }

                return context.ContentSize;
            }

            if (context.HasParentAssignedSize)
            {
                return context.ParentAssignedSize;
            }

            if (context.HasExternalConstraint)
            {
                return context.ExternalConstraintSize;
            }

            return context.ContentSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ApplyConstraints(
            float resolvedSize,
            bool useMin,
            float min,
            bool useMax,
            float max)
        {
            var size = Mathf.Max(0f, resolvedSize);

            if (useMin)
            {
                size = Mathf.Max(size, min);
            }

            if (useMax)
            {
                size = Mathf.Min(size, max);
            }

            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ApplyConstraints(
            float resolvedSize,
            FlexOptionalFloat min,
            FlexOptionalFloat max)
        {
            return ApplyConstraints(resolvedSize, min.enabled, min.value, max.enabled, max.value);
        }

        public static float ResolveConstrainedAxisSize(
            FlexValue size,
            FlexAutoAxisContext context,
            bool useMin,
            float min,
            bool useMax,
            float max)
        {
            var resolved = ResolveAxisSize(size, context);
            return ApplyConstraints(resolved, useMin, min, useMax, max);
        }

        public static float ResolveConstrainedAxisSize(
            FlexValue size,
            FlexAutoAxisContext context,
            FlexOptionalFloat min,
            FlexOptionalFloat max)
        {
            var resolved = ResolveAxisSize(size, context);
            return ApplyConstraints(resolved, min, max);
        }

        public static float ResolveFlexBasis(
            FlexValue flexBasis,
            FlexValue mainAxisSize,
            float contentSize,
            bool hasExternalConstraint,
            float externalConstraintSize)
        {
            if (flexBasis.mode == FlexSizeMode.Points)
            {
                return flexBasis.value;
            }

            if (flexBasis.mode == FlexSizeMode.Percent)
            {
                if (hasExternalConstraint)
                {
                    return externalConstraintSize * flexBasis.value * 0.01f;
                }

                return contentSize;
            }

            if (mainAxisSize.mode == FlexSizeMode.Points)
            {
                return mainAxisSize.value;
            }

            if (mainAxisSize.mode == FlexSizeMode.Percent && hasExternalConstraint)
            {
                return externalConstraintSize * mainAxisSize.value * 0.01f;
            }

            if (hasExternalConstraint)
            {
                return externalConstraintSize;
            }

            return contentSize;
        }

        public static FlexImplicitItemDefaults ResolveImplicitItemDefaults(
            Vector2 rectSize,
            FlexDirection parentDirection)
        {
            var isHorizontalMainAxis = parentDirection == FlexDirection.Row || parentDirection == FlexDirection.RowReverse;
            var mainAxisBasis = isHorizontalMainAxis ? rectSize.x : rectSize.y;

            return new FlexImplicitItemDefaults(
                rectSize.x,
                rectSize.y,
                mainAxisBasis,
                0f,
                0f,
                PositionType.Relative);
        }
    }
}
