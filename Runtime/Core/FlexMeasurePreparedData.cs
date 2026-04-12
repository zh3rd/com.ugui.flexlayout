using System.Collections.Generic;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexMeasure
    {
        private static IReadOnlyList<FlexPreparedFlowItem> BuildPreparedFlowItems(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableMainAxisSize)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.MeasurePreparedFlow.Auto();
            try
            {
                var context = GetMeasurePassContext();
                context.PreparedFlowRequests++;
                var cacheKey = new FlexLineCacheKey(parentId, availableMainAxisSize);
                if (context.PreparedFlowItemCache.TryGetValue(cacheKey, out var cachedItems))
                {
                    context.PreparedFlowHits++;
                    return cachedItems;
                }

                var parent = store.GetNode(parentId);
                var isHorizontalMainAxis = parent.Style.flexDirection == FlexDirection.Row || parent.Style.flexDirection == FlexDirection.RowReverse;
                var mainAxisPadding = isHorizontalMainAxis
                    ? parent.Style.padding.left + parent.Style.padding.right
                    : parent.Style.padding.top + parent.Style.padding.bottom;
                var availableInnerMainSize = UnityEngine.Mathf.Max(0f, availableMainAxisSize - mainAxisPadding);
                var childIds = store.GetChildren(parentId);
                var preparedItems = GetScratchList(context.PreparedFlowItemsScratch, childIds.Count);

                for (var i = 0; i < childIds.Count; i++)
                {
                    var child = store.GetNode(childIds[i]);
                    if (child.Style.positionType == PositionType.Absolute)
                    {
                        continue;
                    }

                    var measured = MeasureSubtree(store, child.Id);
                    preparedItems.Add(new FlexPreparedFlowItem(
                        child.Id,
                        child,
                        measured,
                        ResolveMainAxisBasis(child, isHorizontalMainAxis, availableInnerMainSize, measured),
                        isHorizontalMainAxis ? measured.Height : measured.Width,
                        ResolveAlignSelf(parent.Style.alignItems, child.Style.alignSelf)));
                }

                var result = preparedItems.ToArray();
                context.PreparedFlowItemCache[cacheKey] = result;
                return result;
            }
            finally
            {
                FlexRuntimeSampling.AddPreparedFlowTicks(samplingStartedAt);
            }
        }

        public static IReadOnlyList<FlexLine> BuildLines(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableMainAxisSize)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.WrapBuildLines.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var context = GetMeasurePassContext();
                context.LineBuildRequests++;
                var cacheKey = new FlexLineCacheKey(parentId, availableMainAxisSize);
                if (context.LineCache.TryGetValue(cacheKey, out var cachedLines))
                {
                    context.LineBuildHits++;
                    return cachedLines;
                }
                var preparedLines = BuildPreparedWrapLines(store, parentId, availableMainAxisSize);
                var lines = new List<FlexLine>(preparedLines.Count);
                for (var i = 0; i < preparedLines.Count; i++)
                {
                    var preparedLine = preparedLines[i];
                    var nodeIds = new FlexNodeId[preparedLine.Items.Count];
                    for (var itemIndex = 0; itemIndex < preparedLine.Items.Count; itemIndex++)
                    {
                        nodeIds[itemIndex] = preparedLine.Items[itemIndex].NodeId;
                    }

                    lines.Add(new FlexLine(nodeIds, preparedLine.TotalMainSize, preparedLine.MaxCrossSize));
                }

                context.LineCache[cacheKey] = lines;
                return lines;
            }
            finally
            {
                FlexRuntimeSampling.AddWrapBuildLinesTicks(samplingStartedAt);
                ExitMeasurePass(ownsPass);
            }
        }

        private static IReadOnlyList<FlexPreparedWrapLine> BuildPreparedWrapLines(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableMainAxisSize)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.WrapPrepareLines.Auto();
            try
            {
                var context = GetMeasurePassContext();
                context.PreparedWrapLineRequests++;
                var cacheKey = new FlexLineCacheKey(parentId, availableMainAxisSize);
                if (context.PreparedLineCache.TryGetValue(cacheKey, out var cachedPreparedLines))
                {
                    context.PreparedWrapLineHits++;
                    return cachedPreparedLines;
                }

                var parent = store.GetNode(parentId);
                var wrap = parent.Style.flexWrap;
                var isHorizontalMainAxis = FlexAxisUtility.IsHorizontalMainAxis(parent.Style.flexDirection);
                var mainAxisPadding = isHorizontalMainAxis
                    ? parent.Style.padding.left + parent.Style.padding.right
                    : parent.Style.padding.top + parent.Style.padding.bottom;
                var innerMainSize = UnityEngine.Mathf.Max(0f, availableMainAxisSize - mainAxisPadding);
                var childIds = store.GetChildren(parentId);
                var preparedItems = GetScratchList(context.PreparedWrapItemsScratch, childIds.Count);
                var lineRanges = GetScratchList(context.PreparedWrapLineRangesScratch, childIds.Count);
                var currentMain = 0f;
                var currentBasis = 0f;
                var currentCross = 0f;
                var currentLineStart = 0;
                var currentLineCount = 0;

                for (var i = 0; i < childIds.Count; i++)
                {
                    var child = store.GetNode(childIds[i]);
                    if (child.Style.positionType == PositionType.Absolute)
                    {
                        continue;
                    }

                    var measured = MeasureSubtree(store, child.Id);
                    var measuredCross = isHorizontalMainAxis ? measured.Height : measured.Width;
                    var basis = ResolveMainAxisBasis(child, isHorizontalMainAxis, innerMainSize, measured);

                    if (wrap != FlexWrap.NoWrap && currentLineCount > 0)
                    {
                        var nextMain = currentMain + parent.Style.mainGap + basis;
                        if (nextMain > innerMainSize)
                        {
                            lineRanges.Add(new FlexPreparedWrapLineRange(currentLineStart, currentLineCount, currentMain, currentBasis, currentCross));
                            currentMain = 0f;
                            currentBasis = 0f;
                            currentCross = 0f;
                            currentLineStart = preparedItems.Count;
                            currentLineCount = 0;
                        }
                    }

                    if (currentLineCount > 0)
                    {
                        currentMain += parent.Style.mainGap;
                    }

                    preparedItems.Add(new FlexPreparedWrapItem(
                        child.Id,
                        child,
                        basis,
                        measuredCross,
                        ResolveAlignSelf(parent.Style.alignItems, child.Style.alignSelf)));

                    currentLineCount++;
                    currentMain += basis;
                    currentBasis += basis;
                    var childCrossForLine = FlexSizing.ResolveCrossSizeFromMainWithAspect(
                        child.Style,
                        isHorizontalMainAxis,
                        basis,
                        measuredCross);
                    currentCross = UnityEngine.Mathf.Max(currentCross, childCrossForLine);
                }

                if (currentLineCount > 0)
                {
                    lineRanges.Add(new FlexPreparedWrapLineRange(currentLineStart, currentLineCount, currentMain, currentBasis, currentCross));
                }

                var itemBuffer = preparedItems.ToArray();
                var preparedLines = new List<FlexPreparedWrapLine>(lineRanges.Count);
                for (var i = 0; i < lineRanges.Count; i++)
                {
                    var range = lineRanges[i];
                    preparedLines.Add(new FlexPreparedWrapLine(
                        new FlexPreparedWrapItemSlice(itemBuffer, range.Start, range.Count),
                        range.TotalMainSize,
                        range.TotalBasis,
                        range.MaxCrossSize));
                }

                context.PreparedLineCache[cacheKey] = preparedLines;
                return preparedLines;
            }
            finally
            {
                FlexRuntimeSampling.AddWrapPrepareLinesTicks(samplingStartedAt);
            }
        }
    }
}
