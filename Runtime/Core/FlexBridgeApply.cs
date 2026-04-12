using UnityEngine;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexBridge
    {
        private static FlexMeasuredSize MeasureRoot(FlexNodeStore store, FlexNodeId rootId)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var scope = FlexProfiler.MeasureRoot.Auto();
            try
            {
                var rootNode = store.GetNode(rootId);
                var rootMeasured = FlexMeasure.MeasureSubtree(store, rootId);

                rootNode.HasExternalWidthConstraint = true;
                rootNode.ExternalWidthConstraint = rootMeasured.Width;
                rootNode.HasExternalHeightConstraint = true;
                rootNode.ExternalHeightConstraint = rootMeasured.Height;
                store.SetNode(rootNode);

                return rootMeasured;
            }
            finally
            {
                FlexRuntimeSampling.AddMeasureRootTicks(samplingStartedAt);
            }
        }

        private static void ApplyRootSize(RectTransform rectTransform, FlexMeasuredSize rootMeasured)
        {
            using var scope = FlexProfiler.ApplySelfSize.Auto();
            ApplySelfSize(rectTransform, rootMeasured);
        }

        private static void ApplySubtreeLayout(
            FlexNodeStore store,
            FlexNodeId rootId,
            FlexMeasuredSize rootMeasured,
            BridgeNodeMappingIndex mappingIndex)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var scope = FlexProfiler.ApplySubtree.Auto();
            try
            {
                ApplyLayoutSubtree(store, rootId, rootMeasured.Width, rootMeasured.Height, mappingIndex);
            }
            finally
            {
                FlexRuntimeSampling.AddApplySubtreeTicks(samplingStartedAt);
            }
        }

        private static void ApplySelfSize(RectTransform rectTransform, FlexMeasuredSize size)
        {
            var current = rectTransform.sizeDelta;
            var target = new Vector2(size.Width, size.Height);

            if (!Approximately(current, target))
            {
                rectTransform.sizeDelta = target;
            }
        }

        private static void ApplyRelativeLayout(RectTransform rectTransform, Vector2 pivot, FlexItemLayoutResult result, bool applySize)
        {
            if (rectTransform.anchorMin != Vector2.up)
            {
                rectTransform.anchorMin = Vector2.up;
            }

            if (rectTransform.anchorMax != Vector2.up)
            {
                rectTransform.anchorMax = Vector2.up;
            }

            if (applySize)
            {
                var size = new Vector2(result.Width, result.Height);
                if (!Approximately(rectTransform.sizeDelta, size))
                {
                    rectTransform.sizeDelta = size;
                }
            }

            var position = new Vector2(
                result.X + result.Width * pivot.x,
                -result.Y - result.Height * (1f - pivot.y));
            if (!Approximately(rectTransform.anchoredPosition, position))
            {
                rectTransform.anchoredPosition = position;
            }
        }

        private static void ApplyLayoutSubtree(
            FlexNodeStore store,
            FlexNodeId layoutNodeId,
            float availableWidth,
            float availableHeight,
            BridgeNodeMappingIndex mappingIndex)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var scope = FlexProfiler.ApplyLayoutSubtree.Auto();
            try
            {
                var parent = store.GetNode(layoutNodeId);
                if (!parent.IsContainerNode)
                {
                    return;
                }

                var parentIsHorizontalMainAxis = FlexAxisUtility.IsHorizontalMainAxis(parent.Style.flexDirection);
                var availableMainAxisSize = parentIsHorizontalMainAxis ? availableWidth : availableHeight;
                var availableCrossAxisSize = parentIsHorizontalMainAxis ? availableHeight : availableWidth;
                var arranged = FlexMeasure.Arrange(store, layoutNodeId, availableMainAxisSize, availableCrossAxisSize);
                var childIds = store.GetChildren(layoutNodeId);
                var mappingsById = mappingIndex.MappingsById;
                var arrangedIndex = 0;
                for (var i = 0; i < childIds.Count; i++)
                {
                    var childId = childIds[i];
                    var rawChildId = childId.Value;
                    if (rawChildId <= 0 || rawChildId >= mappingsById.Length)
                    {
                        continue;
                    }

                    var mapping = mappingsById[rawChildId];
                    if (mapping.RectTransform == null)
                    {
                        continue;
                    }

                    var hasRelativeResult = !mapping.IsAbsolutePositioned;
                    FlexItemLayoutResult childResult = default;
                    if (hasRelativeResult)
                    {
                        if (arrangedIndex >= arranged.Count)
                        {
                            continue;
                        }

                        childResult = arranged[arrangedIndex++];
                        ApplyRelativeLayout(mapping.RectTransform, mapping.Pivot, childResult, mapping.DrivesSizeInParentFlow);
                    }

                    if (!mapping.IsContainerNode)
                    {
                        continue;
                    }

                    float childWidth;
                    float childHeight;
                    if (mapping.IsAbsolutePositioned)
                    {
                        var measured = FlexMeasure.MeasureSubtree(store, childId);
                        ApplySelfSize(mapping.RectTransform, measured);
                        childWidth = measured.Width;
                        childHeight = measured.Height;
                    }
                    else
                    {
                        childWidth = childResult.Width;
                        childHeight = childResult.Height;
                    }

                    ApplyLayoutSubtree(store, childId, childWidth, childHeight, mappingIndex);
                }
            }
            finally
            {
                FlexRuntimeSampling.AddApplyLayoutSubtreeTicks(samplingStartedAt);
            }
        }
    }
}
