using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexMeasure
    {
        private static IReadOnlyList<FlexItemLayoutResult> ArrangeWrapped(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableMainAxisSize,
            float availableCrossAxisSize)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.ArrangeWrap.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var context = GetMeasurePassContext();
                var parent = store.GetNode(parentId);
                var isHorizontalMainAxis = FlexAxisUtility.IsHorizontalMainAxis(parent.Style.flexDirection);
                var isMainAxisReversed = FlexAxisUtility.IsMainAxisReversed(parent.Style.flexDirection);
                var isCrossAxisReversed = FlexAxisUtility.IsCrossAxisReversed(parent.Style.flexWrap);
                var preparedLines = BuildPreparedWrapLines(store, parentId, availableMainAxisSize);
                var results = new List<FlexItemLayoutResult>();

                var lineCrossSizes = BuildResolvedLineCrossSizes(context, preparedLines, parent, availableCrossAxisSize, isHorizontalMainAxis, isCrossAxisReversed, out var crossCursor, out var betweenCrossSpacing);
                var mainStartPadding = GetMainAxisStartPadding(parent.Style, isHorizontalMainAxis, isMainAxisReversed);
                var mainEndPadding = GetMainAxisEndPadding(parent.Style, isHorizontalMainAxis, isMainAxisReversed);
                var innerMainSize = UnityEngine.Mathf.Max(0f, availableMainAxisSize - mainStartPadding - mainEndPadding);

                for (var lineIndex = 0; lineIndex < preparedLines.Count; lineIndex++)
                {
                    var line = preparedLines[lineIndex];
                    var lineCross = lineCrossSizes[lineIndex];
                    var lineCrossStart = isCrossAxisReversed
                        ? crossCursor - lineCross
                        : crossCursor;

                    AppendPreparedWrapLineResults(
                        context,
                        results,
                        line,
                        parent,
                        availableMainAxisSize,
                        innerMainSize,
                        lineCross,
                        lineCrossStart,
                        isHorizontalMainAxis,
                        isMainAxisReversed);

                    if (isCrossAxisReversed)
                    {
                        crossCursor = lineCrossStart - betweenCrossSpacing;
                    }
                    else
                    {
                        crossCursor = lineCrossStart + lineCross + betweenCrossSpacing;
                    }
                }

                return results;
            }
            finally
            {
                FlexRuntimeSampling.AddArrangeWrapTicks(samplingStartedAt);
                ExitMeasurePass(ownsPass);
            }
        }

        private static List<float> BuildResolvedLineCrossSizes(
            MeasurePassContext context,
            IReadOnlyList<FlexPreparedWrapLine> preparedLines,
            FlexNodeModel parent,
            float availableCrossAxisSize,
            bool isHorizontalMainAxis,
            bool isCrossAxisReversed,
            out float crossCursor,
            out float betweenCrossSpacing)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.WrapResolveLineCross.Auto();
            try
            {
                var crossStartPadding = GetCrossAxisStartPadding(parent.Style, isHorizontalMainAxis, isCrossAxisReversed);
                var crossEndPadding = GetCrossAxisEndPadding(parent.Style, isHorizontalMainAxis, isCrossAxisReversed);
                var innerCrossSize = UnityEngine.Mathf.Max(0f, availableCrossAxisSize - crossStartPadding - crossEndPadding);

                var totalCross = 0f;
                for (var i = 0; i < preparedLines.Count; i++)
                {
                    totalCross += preparedLines[i].MaxCrossSize;
                }

                if (preparedLines.Count > 1)
                {
                    totalCross += parent.Style.crossGap * (preparedLines.Count - 1);
                }

                var lineCrossSizes = GetScratchList(context.WrapLineCrossSizesScratch, preparedLines.Count);
                for (var i = 0; i < preparedLines.Count; i++)
                {
                    lineCrossSizes.Add(preparedLines[i].MaxCrossSize);
                }

                ResolveAlignContentSpacing(
                    parent.Style.alignContent,
                    innerCrossSize,
                    totalCross,
                    preparedLines.Count,
                    parent.Style.crossGap,
                    lineCrossSizes,
                    out var leadingCrossOffset,
                    out betweenCrossSpacing);

                crossCursor = isCrossAxisReversed
                    ? availableCrossAxisSize - crossStartPadding - leadingCrossOffset
                    : crossStartPadding + leadingCrossOffset;
                return lineCrossSizes;
            }
            finally
            {
                FlexRuntimeSampling.AddWrapResolveLineCrossTicks(samplingStartedAt);
            }
        }

        private static void AppendPreparedWrapLineResults(
            MeasurePassContext context,
            List<FlexItemLayoutResult> results,
            FlexPreparedWrapLine line,
            FlexNodeModel parent,
            float availableMainAxisSize,
            float innerMainSize,
            float lineCross,
            float lineCrossStart,
            bool isHorizontalMainAxis,
            bool isMainAxisReversed)
        {
            var finalMainSizes = GetScratchList(context.WrapLineFinalMainSizesScratch, line.Items.Count);
            FillPreparedWrapLineFinalMainSizes(
                line,
                parent,
                availableMainAxisSize,
                isHorizontalMainAxis,
                finalMainSizes,
                out var totalItemMain);

            var lineOccupiedMain = totalItemMain + (finalMainSizes.Count > 1 ? parent.Style.mainGap * (finalMainSizes.Count - 1) : 0f);
            ResolveJustifySpacing(
                parent.Style.justifyContent,
                innerMainSize,
                lineOccupiedMain,
                finalMainSizes.Count,
                parent.Style.mainGap,
                out var leadingMainOffset,
                out var betweenMainSpacing);

            var mainStartPadding = GetMainAxisStartPadding(parent.Style, isHorizontalMainAxis, isMainAxisReversed);
            var mainCursor = isMainAxisReversed
                ? availableMainAxisSize - mainStartPadding - leadingMainOffset
                : mainStartPadding + leadingMainOffset;

            for (var i = 0; i < finalMainSizes.Count; i++)
            {
                var preparedItem = line.Items[i];
                var itemMainSize = finalMainSizes[i];
                var itemCrossSize = ResolvePreparedWrapItemCrossSize(preparedItem, itemMainSize, isHorizontalMainAxis, lineCross);
                var lineLocalCrossOffset = ResolveCrossOffsetInLine(preparedItem.ResolvedAlignSelf, lineCross, itemCrossSize);

                float mainPosition;
                if (isMainAxisReversed)
                {
                    mainPosition = mainCursor - itemMainSize;
                    mainCursor = mainPosition - betweenMainSpacing;
                }
                else
                {
                    mainPosition = mainCursor;
                    mainCursor = mainPosition + itemMainSize + betweenMainSpacing;
                }

                if (isHorizontalMainAxis)
                {
                    results.Add(new FlexItemLayoutResult(
                        preparedItem.NodeId,
                        mainPosition,
                        lineCrossStart + lineLocalCrossOffset,
                        itemMainSize,
                        itemCrossSize));
                }
                else
                {
                    results.Add(new FlexItemLayoutResult(
                        preparedItem.NodeId,
                        lineCrossStart + lineLocalCrossOffset,
                        mainPosition,
                        itemCrossSize,
                        itemMainSize));
                }
            }
        }

        private static float ResolvePreparedWrapItemCrossSize(
            FlexPreparedWrapItem preparedItem,
            float finalMainSize,
            bool isHorizontalMainAxis,
            float lineCross)
        {
            if (preparedItem.ResolvedAlignSelf == AlignSelf.Stretch)
            {
                return ApplyCrossAxisConstraints(preparedItem.Node, isHorizontalMainAxis, lineCross);
            }

            return FlexSizing.ResolveCrossSizeFromMainWithAspect(
                preparedItem.Node.Style,
                isHorizontalMainAxis,
                finalMainSize,
                preparedItem.MeasuredCrossSize);
        }

        private static void FillPreparedWrapLineFinalMainSizes(
            FlexPreparedWrapLine line,
            FlexNodeModel parent,
            float availableMainAxisSize,
            bool isHorizontalMainAxis,
            List<float> finalMainSizes,
            out float totalItemMain)
        {
            totalItemMain = 0f;
            var mainAxisPadding = isHorizontalMainAxis
                ? parent.Style.padding.left + parent.Style.padding.right
                : parent.Style.padding.top + parent.Style.padding.bottom;
            var totalGap = line.Items.Count > 1 ? parent.Style.mainGap * (line.Items.Count - 1) : 0f;
            var freeSpace = availableMainAxisSize - mainAxisPadding - totalGap - line.TotalBasis;

            if (freeSpace > 0f)
            {
                var totalGrow = 0f;
                for (var i = 0; i < line.Items.Count; i++)
                {
                    totalGrow += line.Items[i].Node.Style.flexGrow;
                }

                for (var i = 0; i < line.Items.Count; i++)
                {
                    var item = line.Items[i];
                    var grown = item.Basis;
                    if (totalGrow > 0f && item.Node.Style.flexGrow > 0f)
                    {
                        grown += freeSpace * (item.Node.Style.flexGrow / totalGrow);
                    }

                    var finalSize = ApplyMainAxisConstraints(item.Node, isHorizontalMainAxis, grown);
                    finalMainSizes.Add(finalSize);
                    totalItemMain += finalSize;
                }

                return;
            }

            if (freeSpace < 0f)
            {
                var totalScaledShrink = 0f;
                for (var i = 0; i < line.Items.Count; i++)
                {
                    var item = line.Items[i];
                    totalScaledShrink += item.Node.Style.flexShrink * item.Basis;
                }

                for (var i = 0; i < line.Items.Count; i++)
                {
                    var item = line.Items[i];
                    var shrunk = item.Basis;
                    if (totalScaledShrink > 0f && item.Node.Style.flexShrink > 0f)
                    {
                        var scaledShrink = item.Node.Style.flexShrink * item.Basis;
                        shrunk += freeSpace * (scaledShrink / totalScaledShrink);
                    }

                    var finalSize = ApplyMainAxisConstraints(item.Node, isHorizontalMainAxis, shrunk);
                    finalMainSizes.Add(finalSize);
                    totalItemMain += finalSize;
                }

                return;
            }

            for (var i = 0; i < line.Items.Count; i++)
            {
                var item = line.Items[i];
                var finalSize = ApplyMainAxisConstraints(item.Node, isHorizontalMainAxis, item.Basis);
                finalMainSizes.Add(finalSize);
                totalItemMain += finalSize;
            }
        }
    }
}
