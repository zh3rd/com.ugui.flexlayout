using System.Collections.Generic;

using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexMeasure
    {
        public static IReadOnlyList<FlexMainAxisAllocation> AllocateMainAxisSizes(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableMainAxisSize)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.MeasureAllocateMainAxis.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var parent = store.GetNode(parentId);
                var isHorizontalMainAxis = FlexAxisUtility.IsHorizontalMainAxis(parent.Style.flexDirection);
                var mainAxisPadding = isHorizontalMainAxis
                    ? parent.Style.padding.left + parent.Style.padding.right
                    : parent.Style.padding.top + parent.Style.padding.bottom;
                var preparedItems = BuildPreparedFlowItems(store, parentId, availableMainAxisSize);
                var items = new List<FlexMainAxisAllocation>(preparedItems.Count);
                if (preparedItems.Count == 0)
                {
                    return items;
                }

                var totalBasis = 0f;
                for (var i = 0; i < preparedItems.Count; i++)
                {
                    totalBasis += preparedItems[i].Basis;
                }

                var totalGap = preparedItems.Count > 1 ? parent.Style.mainGap * (preparedItems.Count - 1) : 0f;
                var freeSpace = availableMainAxisSize - mainAxisPadding - totalGap - totalBasis;

                if (freeSpace > 0f)
                {
                    var totalGrow = 0f;
                    for (var i = 0; i < preparedItems.Count; i++)
                    {
                        totalGrow += preparedItems[i].Node.Style.flexGrow;
                    }

                    for (var i = 0; i < preparedItems.Count; i++)
                    {
                        var item = preparedItems[i];
                        var grown = item.Basis;

                        if (totalGrow > 0f && item.Node.Style.flexGrow > 0f)
                        {
                            grown += freeSpace * (item.Node.Style.flexGrow / totalGrow);
                        }

                        items.Add(new FlexMainAxisAllocation(item.NodeId, item.Basis, ApplyMainAxisConstraints(item.Node, isHorizontalMainAxis, grown)));
                    }

                    return items;
                }

                if (freeSpace < 0f)
                {
                    var totalScaledShrink = 0f;
                    for (var i = 0; i < preparedItems.Count; i++)
                    {
                        var item = preparedItems[i];
                        totalScaledShrink += item.Node.Style.flexShrink * item.Basis;
                    }

                    for (var i = 0; i < preparedItems.Count; i++)
                    {
                        var item = preparedItems[i];
                        var shrunk = item.Basis;

                        if (totalScaledShrink > 0f && item.Node.Style.flexShrink > 0f)
                        {
                            var scaledShrink = item.Node.Style.flexShrink * item.Basis;
                            shrunk += freeSpace * (scaledShrink / totalScaledShrink);
                        }

                        items.Add(new FlexMainAxisAllocation(item.NodeId, item.Basis, ApplyMainAxisConstraints(item.Node, isHorizontalMainAxis, shrunk)));
                    }

                    return items;
                }

                for (var i = 0; i < preparedItems.Count; i++)
                {
                    var item = preparedItems[i];
                    items.Add(new FlexMainAxisAllocation(item.NodeId, item.Basis, ApplyMainAxisConstraints(item.Node, isHorizontalMainAxis, item.Basis)));
                }

                return items;
            }
            finally
            {
                FlexRuntimeSampling.AddAllocateMainAxisTicks(samplingStartedAt);
                ExitMeasurePass(ownsPass);
            }
        }

        public static IReadOnlyList<FlexCrossAxisAllocation> AllocateCrossAxisSizes(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableCrossAxisSize,
            IReadOnlyList<FlexMainAxisAllocation> mainAxisAllocations = null)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.MeasureAllocateCrossAxis.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var parent = store.GetNode(parentId);
                var isHorizontalMainAxis = FlexAxisUtility.IsHorizontalMainAxis(parent.Style.flexDirection);
                var crossAxisPadding = isHorizontalMainAxis
                    ? parent.Style.padding.top + parent.Style.padding.bottom
                    : parent.Style.padding.left + parent.Style.padding.right;
                var availableInnerCrossSize = UnityEngine.Mathf.Max(0f, availableCrossAxisSize - crossAxisPadding);
                var children = store.GetChildren(parentId);
                var results = new List<FlexCrossAxisAllocation>();
                var hasMainAxisAllocations = mainAxisAllocations != null && mainAxisAllocations.Count > 0;
                var mainAxisIndex = 0;

                for (var i = 0; i < children.Count; i++)
                {
                    var child = store.GetNode(children[i]);

                    if (child.Style.positionType == PositionType.Absolute)
                    {
                        continue;
                    }

                    var measured = MeasureSubtree(store, child.Id);
                    var measuredCrossSize = isHorizontalMainAxis ? measured.Height : measured.Width;
                    var resolvedAlignSelf = ResolveAlignSelf(parent.Style.alignItems, child.Style.alignSelf);
                    var stretched = resolvedAlignSelf == AlignSelf.Stretch;
                    var finalCrossSize = measuredCrossSize;

                    if (stretched)
                    {
                        finalCrossSize = ApplyCrossAxisConstraints(child, isHorizontalMainAxis, availableInnerCrossSize);
                    }
                    else if (hasMainAxisAllocations && TryGetMainAxisFinalSize(mainAxisAllocations, ref mainAxisIndex, child.Id, out var finalMainSize))
                    {
                        finalCrossSize = FlexSizing.ResolveCrossSizeFromMainWithAspect(
                            child.Style,
                            isHorizontalMainAxis,
                            finalMainSize,
                            measuredCrossSize);
                    }

                    results.Add(new FlexCrossAxisAllocation(child.Id, measuredCrossSize, finalCrossSize, stretched));
                }

                return results;
            }
            finally
            {
                FlexRuntimeSampling.AddAllocateCrossAxisTicks(samplingStartedAt);
                ExitMeasurePass(ownsPass);
            }
        }

        public static IReadOnlyList<FlexItemMeasuredLayout> MeasureItemLayouts(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableMainAxisSize,
            float availableCrossAxisSize)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.MeasureItemLayouts.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var parent = store.GetNode(parentId);
                var isHorizontalMainAxis = FlexAxisUtility.IsHorizontalMainAxis(parent.Style.flexDirection);
                var preparedItems = BuildPreparedFlowItems(store, parentId, availableMainAxisSize);
                var mainAxisAllocations = AllocateMainAxisSizes(store, parentId, availableMainAxisSize);
                var crossAxisPadding = isHorizontalMainAxis
                    ? parent.Style.padding.top + parent.Style.padding.bottom
                    : parent.Style.padding.left + parent.Style.padding.right;
                var availableInnerCrossSize = UnityEngine.Mathf.Max(0f, availableCrossAxisSize - crossAxisPadding);
                var results = new List<FlexItemMeasuredLayout>(mainAxisAllocations.Count);

                for (var i = 0; i < mainAxisAllocations.Count; i++)
                {
                    var main = mainAxisAllocations[i];
                    var prepared = preparedItems[i];
                    var measured = prepared.Measured;
                    var stretched = prepared.ResolvedAlignSelf == AlignSelf.Stretch;
                    var finalCrossSize = stretched
                        ? ApplyCrossAxisConstraints(prepared.Node, isHorizontalMainAxis, availableInnerCrossSize)
                        : FlexSizing.ResolveCrossSizeFromMainWithAspect(
                            prepared.Node.Style,
                            isHorizontalMainAxis,
                            main.FinalSize,
                            prepared.MeasuredCrossSize);

                    results.Add(new FlexItemMeasuredLayout(
                        main.NodeId,
                        measured.Width,
                        measured.Height,
                        main.Basis,
                        main.FinalSize,
                        prepared.MeasuredCrossSize,
                        finalCrossSize,
                        stretched));
                }

                return results;
            }
            finally
            {
                FlexRuntimeSampling.AddItemLayoutsTicks(samplingStartedAt);
                ExitMeasurePass(ownsPass);
            }
        }
    }
}
