using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexMeasure
    {
        public static IReadOnlyList<FlexItemLayoutResult> ArrangeSingleLine(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableMainAxisSize,
            float availableCrossAxisSize)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.ArrangeSingleLine.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var parent = store.GetNode(parentId);
                var isHorizontalMainAxis = FlexAxisUtility.IsHorizontalMainAxis(parent.Style.flexDirection);
                var isMainAxisReversed = FlexAxisUtility.IsMainAxisReversed(parent.Style.flexDirection);
                var preparedItems = BuildPreparedFlowItems(store, parentId, availableMainAxisSize);
                var mainAxisAllocations = AllocateMainAxisSizes(store, parentId, availableMainAxisSize);
                var results = new List<FlexItemLayoutResult>(mainAxisAllocations.Count);
                var totalItemMain = 0f;
                var crossAxisPadding = isHorizontalMainAxis
                    ? parent.Style.padding.top + parent.Style.padding.bottom
                    : parent.Style.padding.left + parent.Style.padding.right;
                var availableInnerCrossSize = UnityEngine.Mathf.Max(0f, availableCrossAxisSize - crossAxisPadding);

                for (var i = 0; i < mainAxisAllocations.Count; i++)
                {
                    totalItemMain += mainAxisAllocations[i].FinalSize;
                }

                var itemCount = mainAxisAllocations.Count;
                var baseGap = itemCount > 1 ? parent.Style.mainGap * (itemCount - 1) : 0f;
                var occupiedMain = totalItemMain + baseGap;
                var mainStartPadding = GetMainAxisStartPadding(parent.Style, isHorizontalMainAxis, isMainAxisReversed);
                var mainEndPadding = GetMainAxisEndPadding(parent.Style, isHorizontalMainAxis, isMainAxisReversed);
                var innerMainSize = UnityEngine.Mathf.Max(0f, availableMainAxisSize - mainStartPadding - mainEndPadding);
                ResolveJustifySpacing(
                    parent.Style.justifyContent,
                    innerMainSize,
                    occupiedMain,
                    itemCount,
                    parent.Style.mainGap,
                    out var leadingMainOffset,
                    out var betweenMainSpacing);

                var cursorMain = isMainAxisReversed
                    ? availableMainAxisSize - mainStartPadding - leadingMainOffset
                    : mainStartPadding + leadingMainOffset;

                for (var i = 0; i < mainAxisAllocations.Count; i++)
                {
                    var main = mainAxisAllocations[i];
                    var prepared = preparedItems[i];
                    var itemMainSize = main.FinalSize;
                    var itemCrossSize = prepared.ResolvedAlignSelf == AlignSelf.Stretch
                        ? ApplyCrossAxisConstraints(prepared.Node, isHorizontalMainAxis, availableInnerCrossSize)
                        : FlexSizing.ResolveCrossSizeFromMainWithAspect(
                            prepared.Node.Style,
                            isHorizontalMainAxis,
                            itemMainSize,
                            prepared.MeasuredCrossSize);
                    var crossOffset = ResolveCrossOffset(
                        prepared.ResolvedAlignSelf,
                        availableCrossAxisSize,
                        itemCrossSize,
                        parent.Style,
                        isHorizontalMainAxis);
                    float mainPosition;

                    if (isMainAxisReversed)
                    {
                        mainPosition = cursorMain - itemMainSize;
                        cursorMain = mainPosition - betweenMainSpacing;
                    }
                    else
                    {
                        mainPosition = cursorMain;
                        cursorMain = mainPosition + itemMainSize + betweenMainSpacing;
                    }

                    if (isHorizontalMainAxis)
                    {
                        results.Add(new FlexItemLayoutResult(
                            main.NodeId,
                            mainPosition,
                            crossOffset,
                            itemMainSize,
                            itemCrossSize));
                    }
                    else
                    {
                        results.Add(new FlexItemLayoutResult(
                            main.NodeId,
                            crossOffset,
                            mainPosition,
                            itemCrossSize,
                            itemMainSize));
                    }
                }

                return results;
            }
            finally
            {
                FlexRuntimeSampling.AddArrangeSingleLineTicks(samplingStartedAt);
                ExitMeasurePass(ownsPass);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ApplyMainAxisConstraints(FlexNodeModel node, bool isHorizontalMainAxis, float size)
        {
            if (isHorizontalMainAxis)
            {
                return FlexSizing.ApplyConstraints(size, node.Style.minWidth, node.Style.maxWidth);
            }

            return FlexSizing.ApplyConstraints(size, node.Style.minHeight, node.Style.maxHeight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ApplyCrossAxisConstraints(FlexNodeModel node, bool isHorizontalMainAxis, float size)
        {
            if (isHorizontalMainAxis)
            {
                return FlexSizing.ApplyConstraints(size, node.Style.minHeight, node.Style.maxHeight);
            }

            return FlexSizing.ApplyConstraints(size, node.Style.minWidth, node.Style.maxWidth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AlignSelf ResolveAlignSelf(AlignItems parentAlignItems, AlignSelf childAlignSelf)
        {
            if (childAlignSelf != AlignSelf.Auto)
            {
                return childAlignSelf;
            }

            return parentAlignItems switch
            {
                AlignItems.Stretch => AlignSelf.Stretch,
                AlignItems.FlexStart => AlignSelf.FlexStart,
                AlignItems.Center => AlignSelf.Center,
                AlignItems.FlexEnd => AlignSelf.FlexEnd,
                _ => AlignSelf.Stretch,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetMainAxisStartPadding(FlexStyle style, bool isHorizontalMainAxis, bool isMainAxisReversed)
        {
            if (isHorizontalMainAxis)
            {
                return isMainAxisReversed ? style.padding.right : style.padding.left;
            }

            return isMainAxisReversed ? style.padding.bottom : style.padding.top;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetMainAxisEndPadding(FlexStyle style, bool isHorizontalMainAxis, bool isMainAxisReversed)
        {
            if (isHorizontalMainAxis)
            {
                return isMainAxisReversed ? style.padding.left : style.padding.right;
            }

            return isMainAxisReversed ? style.padding.top : style.padding.bottom;
        }

        private static void ResolveJustifySpacing(
            JustifyContent justifyContent,
            float innerMainSize,
            float occupiedMainSize,
            int itemCount,
            float gap,
            out float leadingOffset,
            out float betweenSpacing)
        {
            var signedFreeSpace = innerMainSize - occupiedMainSize;
            var distributableFreeSpace = UnityEngine.Mathf.Max(0f, signedFreeSpace);
            leadingOffset = 0f;
            betweenSpacing = gap;

            switch (justifyContent)
            {
                case JustifyContent.Center:
                    leadingOffset = signedFreeSpace * 0.5f;
                    return;
                case JustifyContent.FlexEnd:
                    leadingOffset = signedFreeSpace;
                    return;
                case JustifyContent.SpaceBetween:
                    if (itemCount > 1)
                    {
                        betweenSpacing = gap + (distributableFreeSpace / (itemCount - 1));
                    }
                    return;
                case JustifyContent.SpaceAround:
                    if (itemCount > 0)
                    {
                        var slot = distributableFreeSpace / itemCount;
                        leadingOffset = slot * 0.5f;
                        betweenSpacing = gap + slot;
                    }
                    return;
                case JustifyContent.SpaceEvenly:
                    if (itemCount > 0)
                    {
                        var slot = distributableFreeSpace / (itemCount + 1);
                        leadingOffset = slot;
                        betweenSpacing = gap + slot;
                    }
                    return;
                default:
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ResolveCrossOffset(
            AlignSelf alignSelf,
            float availableCrossAxisSize,
            float itemCrossSize,
            FlexStyle parentStyle,
            bool isHorizontalMainAxis)
        {
            var paddingStart = isHorizontalMainAxis ? parentStyle.padding.top : parentStyle.padding.left;
            var paddingEnd = isHorizontalMainAxis ? parentStyle.padding.bottom : parentStyle.padding.right;
            var innerCrossSize = UnityEngine.Mathf.Max(0f, availableCrossAxisSize - paddingStart - paddingEnd);

            if (alignSelf == AlignSelf.Stretch)
            {
                return paddingStart;
            }

            return alignSelf switch
            {
                AlignSelf.Center => paddingStart + UnityEngine.Mathf.Max(0f, (innerCrossSize - itemCrossSize) * 0.5f),
                AlignSelf.FlexEnd => paddingStart + UnityEngine.Mathf.Max(0f, innerCrossSize - itemCrossSize),
                _ => paddingStart,
            };
        }

        private static bool TryGetMainAxisFinalSize(
            IReadOnlyList<FlexMainAxisAllocation> mainAxisAllocations,
            ref int index,
            FlexNodeId nodeId,
            out float finalSize)
        {
            if (index < mainAxisAllocations.Count && mainAxisAllocations[index].NodeId.Equals(nodeId))
            {
                finalSize = mainAxisAllocations[index].FinalSize;
                index++;
                return true;
            }

            for (var i = 0; i < mainAxisAllocations.Count; i++)
            {
                if (!mainAxisAllocations[i].NodeId.Equals(nodeId))
                {
                    continue;
                }

                finalSize = mainAxisAllocations[i].FinalSize;
                return true;
            }

            finalSize = 0f;
            return false;
        }

        private static FlexCrossAxisAllocation GetCrossAxisAllocationAt(
            IReadOnlyList<FlexCrossAxisAllocation> crossAxisAllocations,
            int index,
            FlexNodeId nodeId)
        {
            if (index < crossAxisAllocations.Count && crossAxisAllocations[index].NodeId.Equals(nodeId))
            {
                return crossAxisAllocations[index];
            }

            for (var i = 0; i < crossAxisAllocations.Count; i++)
            {
                if (crossAxisAllocations[i].NodeId.Equals(nodeId))
                {
                    return crossAxisAllocations[i];
                }
            }

            throw new InvalidOperationException($"Missing cross-axis allocation for node '{nodeId}'.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryResolveDefiniteMainAxisSize(FlexNodeModel node, bool isHorizontalMainAxis, out float size)
        {
            size = 0f;
            var mainAxisValue = isHorizontalMainAxis ? node.Style.width : node.Style.height;
            if (mainAxisValue.mode == FlexSizeMode.Points)
            {
                size = ApplyMainAxisConstraints(node, isHorizontalMainAxis, mainAxisValue.value);
                return true;
            }

            var hasExternalConstraint = isHorizontalMainAxis ? node.HasExternalWidthConstraint : node.HasExternalHeightConstraint;
            if (mainAxisValue.mode == FlexSizeMode.Percent && hasExternalConstraint)
            {
                var externalConstraint = isHorizontalMainAxis ? node.ExternalWidthConstraint : node.ExternalHeightConstraint;
                size = ApplyMainAxisConstraints(node, isHorizontalMainAxis, externalConstraint * mainAxisValue.value * 0.01f);
                return true;
            }

            if (hasExternalConstraint)
            {
                var externalConstraint = isHorizontalMainAxis ? node.ExternalWidthConstraint : node.ExternalHeightConstraint;
                size = ApplyMainAxisConstraints(node, isHorizontalMainAxis, externalConstraint);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ResolveCrossOffsetInLine(AlignSelf alignSelf, float lineCrossSize, float itemCrossSize)
        {
            if (alignSelf == AlignSelf.Stretch || alignSelf == AlignSelf.FlexStart || alignSelf == AlignSelf.Auto)
            {
                return 0f;
            }

            return alignSelf switch
            {
                AlignSelf.Center => UnityEngine.Mathf.Max(0f, (lineCrossSize - itemCrossSize) * 0.5f),
                AlignSelf.FlexEnd => UnityEngine.Mathf.Max(0f, lineCrossSize - itemCrossSize),
                _ => 0f,
            };
        }

        private static void ResolveAlignContentSpacing(
            AlignContent alignContent,
            float innerCrossSize,
            float occupiedCrossSize,
            int lineCount,
            float gap,
            IList<float> lineCrossSizes,
            out float leadingOffset,
            out float betweenSpacing)
        {
            var signedFreeSpace = innerCrossSize - occupiedCrossSize;
            var distributableFreeSpace = UnityEngine.Mathf.Max(0f, signedFreeSpace);
            leadingOffset = 0f;
            betweenSpacing = gap;

            switch (alignContent)
            {
                case AlignContent.Stretch:
                    if (lineCount > 0 && signedFreeSpace > 0f)
                    {
                        var extraPerLine = signedFreeSpace / lineCount;
                        for (var i = 0; i < lineCount; i++)
                        {
                            lineCrossSizes[i] += extraPerLine;
                        }
                    }

                    return;
                case AlignContent.Center:
                    leadingOffset = signedFreeSpace * 0.5f;
                    return;
                case AlignContent.FlexEnd:
                    leadingOffset = signedFreeSpace;
                    return;
                case AlignContent.SpaceBetween:
                    if (signedFreeSpace <= 0f)
                    {
                        return;
                    }

                    if (lineCount > 1)
                    {
                        betweenSpacing = gap + (distributableFreeSpace / (lineCount - 1));
                    }

                    return;
                case AlignContent.SpaceAround:
                    if (signedFreeSpace <= 0f)
                    {
                        return;
                    }

                    if (lineCount > 0)
                    {
                        var slot = distributableFreeSpace / lineCount;
                        leadingOffset = slot * 0.5f;
                        betweenSpacing = gap + slot;
                    }

                    return;
                case AlignContent.SpaceEvenly:
                    if (signedFreeSpace <= 0f)
                    {
                        return;
                    }

                    if (lineCount > 0)
                    {
                        var slot = distributableFreeSpace / (lineCount + 1);
                        leadingOffset = slot;
                        betweenSpacing = gap + slot;
                    }

                    return;
                default:
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetCrossAxisStartPadding(FlexStyle style, bool isHorizontalMainAxis, bool isCrossAxisReversed)
        {
            if (isHorizontalMainAxis)
            {
                return isCrossAxisReversed ? style.padding.bottom : style.padding.top;
            }

            return isCrossAxisReversed ? style.padding.right : style.padding.left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetCrossAxisEndPadding(FlexStyle style, bool isHorizontalMainAxis, bool isCrossAxisReversed)
        {
            if (isHorizontalMainAxis)
            {
                return isCrossAxisReversed ? style.padding.top : style.padding.bottom;
            }

            return isCrossAxisReversed ? style.padding.left : style.padding.right;
        }
    }
}
