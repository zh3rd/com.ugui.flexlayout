using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexMeasure
    {
        public static FlexMeasuredSize MeasureSubtree(FlexNodeStore store, FlexNodeId nodeId)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.MeasureSubtree.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var context = GetMeasurePassContext();
                context.MeasureSubtreeRequests++;
                if (context.MeasureCache.TryGetValue(nodeId, out var cached))
                {
                    context.MeasureSubtreeHits++;
                    return cached;
                }

                var node = store.GetNode(nodeId);

                var useImplicitRectPassthrough = !node.UsesNodeSource
                    && !node.IsContainerNode
                    && (node.AllowImplicitRectPassthrough
                        || (UnityEngine.Mathf.Approximately(node.ContentWidth, 0f)
                            && UnityEngine.Mathf.Approximately(node.ContentHeight, 0f)))
                    && node.Style.width.mode == FlexSizeMode.Auto
                    && node.Style.height.mode == FlexSizeMode.Auto
                    && !node.Style.minWidth.enabled
                    && !node.Style.maxWidth.enabled
                    && !node.Style.minHeight.enabled
                    && !node.Style.maxHeight.enabled;

                if (useImplicitRectPassthrough)
                {
                    var implicitSize = new FlexMeasuredSize(node.ImplicitRectWidth, node.ImplicitRectHeight);
                    context.MeasureCache[nodeId] = implicitSize;
                    return implicitSize;
                }

                var childContent = MeasureChildContent(store, node);

                var widthContext = new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasExternalConstraint: node.HasExternalWidthConstraint,
                    externalConstraintSize: node.ExternalWidthConstraint,
                    contentSize: childContent.Width);

                var heightContext = new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasExternalConstraint: node.HasExternalHeightConstraint,
                    externalConstraintSize: node.ExternalHeightConstraint,
                    contentSize: childContent.Height);

                var measuredWidth = FlexSizing.ResolveConstrainedAxisSize(
                    node.Style.width,
                    widthContext,
                    node.Style.minWidth,
                    node.Style.maxWidth);

                var measuredHeight = FlexSizing.ResolveConstrainedAxisSize(
                    node.Style.height,
                    heightContext,
                    node.Style.minHeight,
                    node.Style.maxHeight);

                var aspectAdjusted = FlexSizing.ApplyAspectRatioIfNeeded(node.Style, measuredWidth, measuredHeight);
                measuredWidth = aspectAdjusted.Width;
                measuredHeight = aspectAdjusted.Height;

                var result = new FlexMeasuredSize(measuredWidth, measuredHeight);
                context.MeasureCache[nodeId] = result;

                return result;
            }
            finally
            {
                FlexRuntimeSampling.AddMeasureSubtreeTicks(samplingStartedAt);
                ExitMeasurePass(ownsPass);
            }
        }

        public static FlexMeasuredSize MeasureChildContent(FlexNodeStore store, FlexNodeModel node)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var profilerScope = FlexProfiler.MeasureContent.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var isHorizontalMainAxis = FlexAxisUtility.IsHorizontalMainAxis(node.Style.flexDirection);
                var horizontalPadding = node.Style.padding.left + node.Style.padding.right;
                var verticalPadding = node.Style.padding.top + node.Style.padding.bottom;
                var children = store.GetChildren(node.Id);

                if (node.Style.flexWrap != FlexWrap.NoWrap && TryResolveDefiniteMainAxisSize(node, isHorizontalMainAxis, out var definiteMainAxisSize))
                {
                    var preparedLines = BuildPreparedWrapLines(store, node.Id, definiteMainAxisSize);
                    if (preparedLines.Count == 0)
                    {
                        return new FlexMeasuredSize(horizontalPadding, verticalPadding);
                    }

                    var maxLineMain = 0f;
                    var totalLineCross = 0f;
                    for (var i = 0; i < preparedLines.Count; i++)
                    {
                        maxLineMain = UnityEngine.Mathf.Max(maxLineMain, preparedLines[i].TotalMainSize);
                        totalLineCross += preparedLines[i].MaxCrossSize;
                    }

                    if (preparedLines.Count > 1)
                    {
                        totalLineCross += node.Style.crossGap * (preparedLines.Count - 1);
                    }

                    if (isHorizontalMainAxis)
                    {
                        return new FlexMeasuredSize(horizontalPadding + maxLineMain, verticalPadding + totalLineCross);
                    }

                    return new FlexMeasuredSize(horizontalPadding + totalLineCross, verticalPadding + maxLineMain);
                }

                if (children.Count == 0)
                {
                    return new FlexMeasuredSize(
                        node.ContentWidth + horizontalPadding,
                        node.ContentHeight + verticalPadding);
                }

                var totalMain = 0f;
                var maxCross = 0f;
                var inFlowCount = 0;

                for (var i = 0; i < children.Count; i++)
                {
                    var child = store.GetNode(children[i]);
                    if (child.Style.positionType == PositionType.Absolute)
                    {
                        continue;
                    }

                    var childMeasured = MeasureSubtree(store, child.Id);
                    var childMain = isHorizontalMainAxis ? childMeasured.Width : childMeasured.Height;
                    var childCross = isHorizontalMainAxis ? childMeasured.Height : childMeasured.Width;

                    totalMain += childMain;
                    maxCross = UnityEngine.Mathf.Max(maxCross, childCross);
                    inFlowCount++;
                }

                if (inFlowCount > 1)
                {
                    totalMain += node.Style.mainGap * (inFlowCount - 1);
                }

                if (isHorizontalMainAxis)
                {
                    return new FlexMeasuredSize(horizontalPadding + totalMain, verticalPadding + maxCross);
                }

                return new FlexMeasuredSize(horizontalPadding + maxCross, verticalPadding + totalMain);
            }
            finally
            {
                FlexRuntimeSampling.AddMeasureContentTicks(samplingStartedAt);
                ExitMeasurePass(ownsPass);
            }
        }
    }
}
