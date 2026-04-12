using UnityEngine;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static class FlexResolvedNodeAssembler
    {
        public static ResolvedFlexNodeStyle Build(
            IFlexNodeStyleSource source,
            in FlexImplicitNodeDefaults defaults)
        {
            var hasNodeSource = source.HasNodeSource;
            var useImplicitRectAsDefiniteNodeSize = !hasNodeSource && source.HasLayoutSource;
            if (hasNodeSource)
            {
                return new ResolvedFlexNodeStyle(
                    hasExplicitAuthoring: true,
                    width: source.NodeStyle.width,
                    height: source.NodeStyle.height,
                    aspectRatio: source.NodeStyle.aspectRatio,
                    minWidth: source.NodeStyle.minWidth,
                    maxWidth: source.NodeStyle.maxWidth,
                    minHeight: source.NodeStyle.minHeight,
                    maxHeight: source.NodeStyle.maxHeight,
                    positionType: source.NodeStyle.positionType);
            }

            return new ResolvedFlexNodeStyle(
                hasExplicitAuthoring: false,
                width: useImplicitRectAsDefiniteNodeSize ? FlexValue.Points(source.ImplicitRectSize.x) : defaults.Width,
                height: useImplicitRectAsDefiniteNodeSize ? FlexValue.Points(source.ImplicitRectSize.y) : defaults.Height,
                aspectRatio: FlexOptionalFloat.Disabled(),
                minWidth: FlexOptionalFloat.Disabled(),
                maxWidth: FlexOptionalFloat.Disabled(),
                minHeight: FlexOptionalFloat.Disabled(),
                maxHeight: FlexOptionalFloat.Disabled(),
                positionType: PositionType.Relative);
        }
    }

    internal static class FlexResolvedItemAssembler
    {
        public static ResolvedFlexItemStyle Build(
            IFlexItemStyleSource source,
            in FlexImplicitItemStyleData defaults)
        {
            if (source.HasItemSource)
            {
                return new ResolvedFlexItemStyle(
                    hasExplicitAuthoring: true,
                    flexGrow: source.ItemStyle.flexGrow,
                    flexShrink: source.ItemStyle.flexShrink,
                    flexBasis: source.ItemStyle.flexBasis,
                    alignSelf: source.ItemStyle.alignSelf);
            }

            return new ResolvedFlexItemStyle(
                hasExplicitAuthoring: false,
                flexGrow: defaults.flexGrow,
                flexShrink: defaults.flexShrink,
                flexBasis: defaults.flexBasis,
                alignSelf: defaults.alignSelf);
        }
    }

    internal static class FlexResolvedContainerAssembler
    {
        public static ResolvedFlexContainerStyle Build(IFlexContainerStyleSource source, in FlexStyle defaults)
        {
            if (source.HasContainerSource)
            {
                return new ResolvedFlexContainerStyle(
                    isContainer: true,
                    hasExplicitAuthoring: true,
                    flexDirection: source.ContainerStyle.flexDirection,
                    flexWrap: source.ContainerStyle.flexWrap,
                    justifyContent: source.ContainerStyle.justifyContent,
                    alignItems: source.ContainerStyle.alignItems,
                    alignContent: source.ContainerStyle.alignContent,
                    mainGap: source.ContainerStyle.mainGap,
                    crossGap: source.ContainerStyle.crossGap,
                    padding: source.ContainerStyle.padding);
            }

            return new ResolvedFlexContainerStyle(
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
        }
    }

    internal readonly struct FlexImplicitNodeDefaults
    {
        public FlexImplicitNodeDefaults(FlexValue width, FlexValue height)
        {
            Width = width;
            Height = height;
        }

        public FlexValue Width { get; }
        public FlexValue Height { get; }
    }
}
