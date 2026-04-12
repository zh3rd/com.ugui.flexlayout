using UnityEngine;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal readonly struct FlexAuthoringSnapshot
    {
        public FlexAuthoringSnapshot(
            RectTransform rectTransform,
            FlexLayout layout,
            FlexNodeBase node,
            FlexItem item,
            Vector2 implicitRectSize)
        {
            RectTransform = rectTransform;
            Layout = layout;
            Node = node;
            Item = item;
            ImplicitRectSize = implicitRectSize;
        }

        public RectTransform RectTransform { get; }
        public FlexLayout Layout { get; }
        public FlexNodeBase Node { get; }
        public FlexItem Item { get; }
        public Vector2 ImplicitRectSize { get; }
    }

    internal interface IFlexNodeStyleSource
    {
        bool HasNodeSource { get; }
        FlexNodeStyleData NodeStyle { get; }
        bool HasLayoutSource { get; }
        Vector2 ImplicitRectSize { get; }
    }

    internal interface IFlexItemStyleSource
    {
        bool HasItemSource { get; }
        FlexItemStyleData ItemStyle { get; }
    }

    internal interface IFlexContainerStyleSource
    {
        bool HasContainerSource { get; }
        FlexStyle ContainerStyle { get; }
    }

    internal readonly struct FlexSnapshotStyleSource : IFlexNodeStyleSource, IFlexItemStyleSource, IFlexContainerStyleSource
    {
        private readonly FlexAuthoringSnapshot m_Snapshot;

        public FlexSnapshotStyleSource(in FlexAuthoringSnapshot snapshot)
        {
            m_Snapshot = snapshot;
        }

        public bool HasNodeSource => m_Snapshot.Node != null;
        public FlexNodeStyleData NodeStyle => m_Snapshot.Node != null ? m_Snapshot.Node.style : FlexNodeStyleData.Default;
        public bool HasLayoutSource => m_Snapshot.Layout != null;
        public Vector2 ImplicitRectSize => m_Snapshot.ImplicitRectSize;
        public bool HasItemSource => m_Snapshot.Item != null;
        public FlexItemStyleData ItemStyle => m_Snapshot.Item != null ? m_Snapshot.Item.style : FlexItemStyleData.Default;
        public bool HasContainerSource => m_Snapshot.Layout != null;
        public FlexStyle ContainerStyle => m_Snapshot.Layout != null ? m_Snapshot.Layout.style : FlexStyle.Default;
    }

    internal static class FlexAuthoringSnapshotBuilder
    {
        public static FlexAuthoringSnapshot Build(RectTransform rectTransform, Vector2 implicitRectSize)
        {
            var layout = GetActiveLayout(rectTransform);
            var node = GetActiveNode(rectTransform);
            var item = GetActiveItem(rectTransform);
            return new FlexAuthoringSnapshot(rectTransform, layout, node, item, implicitRectSize);
        }

        public static FlexAuthoringSnapshot Build(
            RectTransform rectTransform,
            FlexLayout layout,
            FlexNodeBase node,
            FlexItem item,
            Vector2 implicitRectSize)
        {
            return new FlexAuthoringSnapshot(rectTransform, layout, node, item, implicitRectSize);
        }

        private static FlexLayout GetActiveLayout(Component component)
        {
            if (component == null || !component.TryGetComponent<FlexLayout>(out var layout))
            {
                return null;
            }

            return layout.isActiveAndEnabled ? layout : null;
        }

        private static FlexNodeBase GetActiveNode(Component component)
        {
            if (component == null || !component.TryGetComponent<FlexNodeBase>(out var node))
            {
                return null;
            }

            return node.isActiveAndEnabled ? node : null;
        }

        private static FlexItem GetActiveItem(Component component)
        {
            if (component == null || !component.TryGetComponent<FlexItem>(out var itemComponent))
            {
                return null;
            }

            return itemComponent.isActiveAndEnabled ? itemComponent : null;
        }
    }

    internal readonly struct FlexContentMeasureResult
    {
        public FlexContentMeasureResult(Vector2 contentSize, bool allowImplicitRectPassthrough, bool hasSpecializedSource)
        {
            ContentSize = contentSize;
            AllowImplicitRectPassthrough = allowImplicitRectPassthrough;
            HasSpecializedSource = hasSpecializedSource;
        }

        public Vector2 ContentSize { get; }
        public bool AllowImplicitRectPassthrough { get; }
        public bool HasSpecializedSource { get; }
    }

    internal static class FlexContentMeasureAdapterRegistry
    {
        public static FlexContentMeasureResult Measure(in FlexAuthoringSnapshot snapshot)
        {
            if (snapshot.Node != null
                && snapshot.Node.hasSpecializedContentMeasurement
                && snapshot.Node.TryMeasureContent(snapshot.ImplicitRectSize, out var measured))
            {
                return measured;
            }

            return new FlexContentMeasureResult(snapshot.ImplicitRectSize, allowImplicitRectPassthrough: true, hasSpecializedSource: false);
        }
    }
}
