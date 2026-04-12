using UnityEngine;
using FlexItem = UnityEngine.UI.Flex.FlexItem;

using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal readonly struct ResolvedFlexNodeStyle
    {
        public ResolvedFlexNodeStyle(
            bool hasExplicitAuthoring,
            FlexValue width,
            FlexValue height,
            FlexOptionalFloat aspectRatio,
            FlexOptionalFloat minWidth,
            FlexOptionalFloat maxWidth,
            FlexOptionalFloat minHeight,
            FlexOptionalFloat maxHeight,
            PositionType positionType)
        {
            HasExplicitAuthoring = hasExplicitAuthoring;
            Width = width;
            Height = height;
            AspectRatio = aspectRatio;
            MinWidth = minWidth;
            MaxWidth = maxWidth;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
            PositionType = positionType;
        }

        public bool HasExplicitAuthoring { get; }
        public FlexValue Width { get; }
        public FlexValue Height { get; }
        public FlexOptionalFloat AspectRatio { get; }
        public FlexOptionalFloat MinWidth { get; }
        public FlexOptionalFloat MaxWidth { get; }
        public FlexOptionalFloat MinHeight { get; }
        public FlexOptionalFloat MaxHeight { get; }
        public PositionType PositionType { get; }
    }

    internal readonly struct ResolvedFlexItemStyle
    {
        public ResolvedFlexItemStyle(
            bool hasExplicitAuthoring,
            float flexGrow,
            float flexShrink,
            FlexValue flexBasis,
            AlignSelf alignSelf)
        {
            HasExplicitAuthoring = hasExplicitAuthoring;
            FlexGrow = flexGrow;
            FlexShrink = flexShrink;
            FlexBasis = flexBasis;
            AlignSelf = alignSelf;
        }

        public bool HasExplicitAuthoring { get; }
        public float FlexGrow { get; }
        public float FlexShrink { get; }
        public FlexValue FlexBasis { get; }
        public AlignSelf AlignSelf { get; }
    }

    internal readonly struct ResolvedFlexContainerStyle
    {
        public ResolvedFlexContainerStyle(
            bool isContainer,
            bool hasExplicitAuthoring,
            FlexDirection flexDirection,
            FlexWrap flexWrap,
            JustifyContent justifyContent,
            AlignItems alignItems,
            AlignContent alignContent,
            float mainGap,
            float crossGap,
            FlexEdges padding)
        {
            IsContainer = isContainer;
            HasExplicitAuthoring = hasExplicitAuthoring;
            FlexDirection = flexDirection;
            FlexWrap = flexWrap;
            JustifyContent = justifyContent;
            AlignItems = alignItems;
            AlignContent = alignContent;
            MainGap = mainGap;
            CrossGap = crossGap;
            Padding = padding;
        }

        public bool IsContainer { get; }
        public bool HasExplicitAuthoring { get; }
        public FlexDirection FlexDirection { get; }
        public FlexWrap FlexWrap { get; }
        public JustifyContent JustifyContent { get; }
        public AlignItems AlignItems { get; }
        public AlignContent AlignContent { get; }
        public float MainGap { get; }
        public float CrossGap { get; }
        public FlexEdges Padding { get; }
    }

    internal readonly struct ResolvedFlexNode
    {
        public ResolvedFlexNode(
            ResolvedFlexNodeStyle node,
            ResolvedFlexItemStyle item,
            ResolvedFlexContainerStyle container,
            bool hasNodeSource,
            bool hasItemSource,
            bool hasSpecializedContentSource,
            bool hasExplicitLayoutComponent,
            Vector2 implicitRectSize,
            Vector2 contentSize,
            bool allowImplicitRectPassthrough)
        {
            Node = node;
            Item = item;
            Container = container;
            HasNodeSource = hasNodeSource;
            HasItemSource = hasItemSource;
            HasSpecializedContentSource = hasSpecializedContentSource;
            HasExplicitLayoutComponent = hasExplicitLayoutComponent;
            ImplicitRectSize = implicitRectSize;
            ContentSize = contentSize;
            AllowImplicitRectPassthrough = allowImplicitRectPassthrough;
        }

        public ResolvedFlexNodeStyle Node { get; }
        public ResolvedFlexItemStyle Item { get; }
        public ResolvedFlexContainerStyle Container { get; }
        public bool HasNodeSource { get; }
        public bool HasItemSource { get; }
        public bool HasSpecializedContentSource { get; }
        public bool HasExplicitLayoutComponent { get; }
        public Vector2 ImplicitRectSize { get; }
        public Vector2 ContentSize { get; }
        public bool AllowImplicitRectPassthrough { get; }

        public FlexStyle ToLegacyStyle()
        {
            return new FlexStyle
            {
                width = Node.Width,
                height = Node.Height,
                aspectRatio = Node.AspectRatio,
                minWidth = Node.MinWidth,
                maxWidth = Node.MaxWidth,
                minHeight = Node.MinHeight,
                maxHeight = Node.MaxHeight,
                flexGrow = Item.FlexGrow,
                flexShrink = Item.FlexShrink,
                flexBasis = Item.FlexBasis,
                alignSelf = Item.AlignSelf,
                positionType = Node.PositionType,
                flexDirection = Container.FlexDirection,
                flexWrap = Container.FlexWrap,
                justifyContent = Container.JustifyContent,
                alignItems = Container.AlignItems,
                alignContent = Container.AlignContent,
                mainGap = Container.MainGap,
                crossGap = Container.CrossGap,
                padding = Container.Padding,
            };
        }

        public FlexNodeModel ToLegacyNode(FlexNodeId parentId)
        {
            return new FlexNodeModel
            {
                ParentId = parentId,
                IsExplicit = HasExplicitLayoutComponent,
                HasNodeSource = HasNodeSource,
                HasItemSource = HasItemSource,
                IsContainer = Container.IsContainer,
                HasLayoutComponent = HasExplicitLayoutComponent,
                Style = ToLegacyStyle(),
                ImplicitRectWidth = ImplicitRectSize.x,
                ImplicitRectHeight = ImplicitRectSize.y,
                ContentWidth = (HasNodeSource && !HasSpecializedContentSource) ? 0f : ContentSize.x,
                ContentHeight = (HasNodeSource && !HasSpecializedContentSource) ? 0f : ContentSize.y,
                AllowImplicitRectPassthrough = AllowImplicitRectPassthrough,
            };
        }
    }

    internal static class FlexResolvedNodeResolver
    {
        public static bool ShouldExcludeDisabledLayoutOnlyChild(Component component)
        {
            if (component == null || !component.TryGetComponent<FlexLayout>(out var layout) || layout.isActiveAndEnabled)
            {
                return false;
            }

            if (component.TryGetComponent<FlexNodeBase>(out var node) && node.isActiveAndEnabled)
            {
                return false;
            }

            if (component.TryGetComponent<FlexItem>(out var item) && item.isActiveAndEnabled)
            {
                return false;
            }

            return true;
        }

        public static ResolvedFlexNode Resolve(RectTransform rectTransform, Vector2 implicitRectSize)
        {
            var snapshot = FlexAuthoringSnapshotBuilder.Build(rectTransform, implicitRectSize);
            var parentLayout = ResolveParentLayout(rectTransform);
            return ResolveFromSnapshot(
                snapshot,
                ResolveImplicitItemDefaults(parentLayout),
                ResolveImplicitNodeDefaults(parentLayout));
        }

        public static ResolvedFlexNode Resolve(in FlexAuthoringSnapshot snapshot, FlexLayout parentLayoutForImplicitDefaults)
        {
            return ResolveFromSnapshot(
                snapshot,
                ResolveImplicitItemDefaults(parentLayoutForImplicitDefaults),
                ResolveImplicitNodeDefaults(parentLayoutForImplicitDefaults));
        }

        public static ResolvedFlexNode Resolve(
            in FlexAuthoringSnapshot snapshot,
            in FlexImplicitItemStyleData implicitItemDefaults,
            in FlexImplicitNodeDefaults implicitNodeDefaults)
        {
            return ResolveFromSnapshot(snapshot, implicitItemDefaults, implicitNodeDefaults);
        }

        private static ResolvedFlexNode ResolveFromSnapshot(
            in FlexAuthoringSnapshot snapshot,
            in FlexImplicitItemStyleData implicitItemDefaults,
            in FlexImplicitNodeDefaults implicitNodeDefaults)
        {
            var defaults = FlexStyle.Default;
            var node = snapshot.Node;
            var item = snapshot.Item;
            var layout = snapshot.Layout;
            var hasNodeSource = node != null;
            var hasItemSource = item != null;
            var contentMeasure = FlexContentMeasureAdapterRegistry.Measure(snapshot);
            var useImplicitRectAsDefiniteNodeSize = !hasNodeSource && layout != null;

            var nodeStyle = hasNodeSource
                ? new ResolvedFlexNodeStyle(
                    hasExplicitAuthoring: true,
                    width: node.style.width,
                    height: node.style.height,
                    aspectRatio: node.style.aspectRatio,
                    minWidth: node.style.minWidth,
                    maxWidth: node.style.maxWidth,
                    minHeight: node.style.minHeight,
                    maxHeight: node.style.maxHeight,
                    positionType: node.style.positionType)
                : new ResolvedFlexNodeStyle(
                    hasExplicitAuthoring: false,
                    width: useImplicitRectAsDefiniteNodeSize ? FlexValue.Points(snapshot.ImplicitRectSize.x) : implicitNodeDefaults.Width,
                    height: useImplicitRectAsDefiniteNodeSize ? FlexValue.Points(snapshot.ImplicitRectSize.y) : implicitNodeDefaults.Height,
                    aspectRatio: FlexOptionalFloat.Disabled(),
                    minWidth: FlexOptionalFloat.Disabled(),
                    maxWidth: FlexOptionalFloat.Disabled(),
                    minHeight: FlexOptionalFloat.Disabled(),
                    maxHeight: FlexOptionalFloat.Disabled(),
                    positionType: PositionType.Relative);

            var itemStyle = hasItemSource
                ? new ResolvedFlexItemStyle(
                    hasExplicitAuthoring: true,
                    flexGrow: item.style.flexGrow,
                    flexShrink: item.style.flexShrink,
                    flexBasis: item.style.flexBasis,
                    alignSelf: item.style.alignSelf)
                : new ResolvedFlexItemStyle(
                    hasExplicitAuthoring: false,
                    flexGrow: implicitItemDefaults.flexGrow,
                    flexShrink: implicitItemDefaults.flexShrink,
                    flexBasis: implicitItemDefaults.flexBasis,
                    alignSelf: implicitItemDefaults.alignSelf);

            var containerStyle = layout != null
                ? new ResolvedFlexContainerStyle(
                    isContainer: true,
                    hasExplicitAuthoring: true,
                    flexDirection: layout.style.flexDirection,
                    flexWrap: layout.style.flexWrap,
                    justifyContent: layout.style.justifyContent,
                    alignItems: layout.style.alignItems,
                    alignContent: layout.style.alignContent,
                    mainGap: layout.style.mainGap,
                    crossGap: layout.style.crossGap,
                    padding: layout.style.padding)
                : new ResolvedFlexContainerStyle(
                    isContainer: false,
                    hasExplicitAuthoring: false,
                    flexDirection: defaults.flexDirection,
                    flexWrap: defaults.flexWrap,
                    justifyContent: defaults.justifyContent,
                    alignItems: defaults.alignItems,
                    alignContent: defaults.alignContent,
                    mainGap: defaults.mainGap,
                    crossGap: defaults.crossGap,
                    padding: defaults.padding);

            return new ResolvedFlexNode(
                nodeStyle,
                itemStyle,
                containerStyle,
                hasNodeSource,
                hasItemSource,
                hasSpecializedContentSource: contentMeasure.HasSpecializedSource,
                hasExplicitLayoutComponent: layout != null,
                snapshot.ImplicitRectSize,
                contentSize: contentMeasure.ContentSize,
                allowImplicitRectPassthrough: contentMeasure.AllowImplicitRectPassthrough);
        }

        private static FlexImplicitItemStyleData ResolveImplicitItemDefaults(FlexLayout parentLayout)
        {
            if (parentLayout != null && parentLayout.isActiveAndEnabled)
            {
                return parentLayout.implicitItemDefaults;
            }

            return FlexImplicitItemStyleData.Default;
        }

        private static FlexImplicitNodeDefaults ResolveImplicitNodeDefaults(FlexLayout parentLayout)
        {
            if (parentLayout != null && parentLayout.isActiveAndEnabled)
            {
                return new FlexImplicitNodeDefaults(parentLayout.implicitItemDefaults.width, parentLayout.implicitItemDefaults.height);
            }

            return new FlexImplicitNodeDefaults(FlexValue.Auto(), FlexValue.Auto());
        }

        private static FlexLayout ResolveParentLayout(Component component)
        {
            if (component != null
                && component.transform.parent != null
                && component.transform.parent.TryGetComponent<FlexLayout>(out var parentLayout)
                && parentLayout.isActiveAndEnabled)
            {
                return parentLayout;
            }

            return null;
        }

    }
}
