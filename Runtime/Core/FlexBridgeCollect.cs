using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexBridge
    {
        private static FlexNodeId CollectTree(FlexLayout rootLayout, FlexNodeStore store, List<BridgeNodeMapping> mappings)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            rootLayout.BeginRuntimeImplicitSizeSnapshotPass();
            try
            {
                using (FlexProfiler.CollectTree.Auto())
                {
                    return BuildNodeRecursive(
                        rootLayout.rectTransform,
                        rootLayout,
                        store,
                        mappings,
                        default,
                        null,
                        FlexImplicitItemStyleData.Default,
                        new FlexImplicitNodeDefaults(FlexValue.Auto(), FlexValue.Auto()));
                }
            }
            finally
            {
                rootLayout.EndRuntimeImplicitSizeSnapshotPass();
                FlexRuntimeSampling.AddCollectTreeTicks(samplingStartedAt);
            }
        }

        private static BridgeNodeMappingIndex BuildMappingIndex(List<BridgeNodeMapping> mappings)
        {
            var samplingStartedAt = FlexRuntimeSampling.BeginSample();
            using var scope = FlexProfiler.CollectBuildMapping.Auto();
            try
            {
                var mappingsById = new BridgeNodeMapping[mappings.Count + 1];
                for (var i = 0; i < mappings.Count; i++)
                {
                    mappingsById[mappings[i].NodeId.Value] = mappings[i];
                }

                return new BridgeNodeMappingIndex(mappingsById);
            }
            finally
            {
                FlexRuntimeSampling.AddBuildMappingTicks(samplingStartedAt);
            }
        }

        private static FlexNodeId BuildNodeRecursive(
            RectTransform rectTransform,
            FlexLayout rootLayout,
            FlexNodeStore store,
            List<BridgeNodeMapping> mappings,
            FlexNodeId parentId,
            ResolvedFlexNode? parentResolved,
            in FlexImplicitItemStyleData parentImplicitItemDefaults,
            in FlexImplicitNodeDefaults parentImplicitNodeDefaults)
        {
            using var scope = FlexProfiler.CollectResolveNode.Auto();
            var runtimeImplicitSize = ResolveImplicitSize(rectTransform);
            var snapshot = FlexAuthoringSnapshotBuilder.Build(rectTransform, runtimeImplicitSize);
            var layoutComponent = snapshot.Layout;
            var nodeComponent = snapshot.Node;
            var itemComponent = snapshot.Item;
            var resolved = FlexResolvedNodeResolver.Resolve(snapshot, parentImplicitItemDefaults, parentImplicitNodeDefaults);
            var parentDrivesChildSize = true;
            if (rootLayout != null && parentResolved.HasValue)
            {
                parentDrivesChildSize = FlexOwnershipResolver.ShouldDriveChildSize(parentResolved.Value, resolved);
                var snapshotSize = rootLayout.ResolveRuntimeImplicitSizeSnapshot(
                    rectTransform,
                    resolved,
                    parentDrivesChildSize,
                    runtimeImplicitSize);
                if (!Approximately(runtimeImplicitSize, snapshotSize))
                {
                    snapshot = FlexAuthoringSnapshotBuilder.Build(
                        rectTransform,
                        layoutComponent,
                        nodeComponent,
                        itemComponent,
                        snapshotSize);
                    resolved = FlexResolvedNodeResolver.Resolve(snapshot, parentImplicitItemDefaults, parentImplicitNodeDefaults);
                }
            }

            var node = resolved.ToLegacyNode(parentId);
            var drivesSizeInParentFlow = parentDrivesChildSize;

            var nodeId = store.CreateNode(node);
            mappings.Add(new BridgeNodeMapping(
                nodeId,
                rectTransform,
                rectTransform.pivot,
                resolved.Container.IsContainer,
                resolved.Node.PositionType == PositionType.Absolute,
                drivesSizeInParentFlow));

            var childImplicitItemDefaults = layoutComponent != null
                ? layoutComponent.implicitItemDefaults
                : FlexImplicitItemStyleData.Default;
            var childImplicitNodeDefaults = layoutComponent != null
                ? new FlexImplicitNodeDefaults(layoutComponent.implicitItemDefaults.width, layoutComponent.implicitItemDefaults.height)
                : new FlexImplicitNodeDefaults(FlexValue.Auto(), FlexValue.Auto());

            for (var i = 0; i < rectTransform.childCount; i++)
            {
                if (rectTransform.GetChild(i) is not RectTransform childRect)
                {
                    continue;
                }

                if (!childRect.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (FlexResolvedNodeResolver.ShouldExcludeDisabledLayoutOnlyChild(childRect))
                {
                    continue;
                }

                BuildNodeRecursive(
                    childRect,
                    rootLayout,
                    store,
                    mappings,
                    nodeId,
                    resolved,
                    childImplicitItemDefaults,
                    childImplicitNodeDefaults);
            }

            return nodeId;
        }

        private static Vector2 ResolveImplicitSize(RectTransform rectTransform)
        {
            return new Vector2(
                ResolveImplicitAxisSize(rectTransform, RectTransform.Axis.Horizontal),
                ResolveImplicitAxisSize(rectTransform, RectTransform.Axis.Vertical));
        }

        internal static Vector2 ResolveImplicitSizeForTesting(RectTransform rectTransform)
        {
            return ResolveImplicitSize(rectTransform);
        }

        private static float ResolveImplicitAxisSize(RectTransform rectTransform, RectTransform.Axis axis)
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
    }
}
