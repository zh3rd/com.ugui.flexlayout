using Unity.Profiling;

namespace UnityEngine.UI.Flex.Core
{
    internal static class FlexProfiler
    {
        public static readonly ProfilerMarker Rebuild = new("FlexLayout.Rebuild");

        public static readonly ProfilerMarker CollectTree = new("FlexLayout.Collect.Tree");
        public static readonly ProfilerMarker CollectResolveNode = new("FlexLayout.Collect.ResolveNode");
        public static readonly ProfilerMarker CollectBuildMapping = new("FlexLayout.Collect.BuildMapping");

        public static readonly ProfilerMarker MeasureRoot = new("FlexLayout.Measure.Root");
        public static readonly ProfilerMarker MeasureSubtree = new("FlexLayout.Measure.Subtree");
        public static readonly ProfilerMarker MeasureContent = new("FlexLayout.Measure.Content");
        public static readonly ProfilerMarker MeasureText = new("FlexLayout.Measure.Text");
        public static readonly ProfilerMarker MeasurePreparedFlow = new("FlexLayout.Measure.PreparedFlow");
        public static readonly ProfilerMarker MeasureAllocateMainAxis = new("FlexLayout.Measure.AllocateMainAxis");
        public static readonly ProfilerMarker MeasureAllocateCrossAxis = new("FlexLayout.Measure.AllocateCrossAxis");
        public static readonly ProfilerMarker MeasureItemLayouts = new("FlexLayout.Measure.ItemLayouts");

        public static readonly ProfilerMarker WrapBuildLines = new("FlexLayout.Wrap.BuildLines");
        public static readonly ProfilerMarker WrapPrepareLines = new("FlexLayout.Wrap.PrepareLines");
        public static readonly ProfilerMarker WrapResolveLineCross = new("FlexLayout.Wrap.ResolveLineCross");

        public static readonly ProfilerMarker ArrangeFlow = new("FlexLayout.Arrange.Flow");
        public static readonly ProfilerMarker ArrangeSingleLine = new("FlexLayout.Arrange.SingleLine");
        public static readonly ProfilerMarker ArrangeWrap = new("FlexLayout.Arrange.Wrap");

        public static readonly ProfilerMarker ApplySelfSize = new("FlexLayout.Apply.SelfSize");
        public static readonly ProfilerMarker ApplySubtree = new("FlexLayout.Apply.Subtree");
        public static readonly ProfilerMarker ApplyLayoutSubtree = new("FlexLayout.Apply.LayoutSubtree");

        public static readonly ProfilerMarker TrackerRefreshDrivenProperties = new("FlexLayout.Tracker.RefreshDrivenProperties");

        public static readonly ProfilerMarker DirtyRuntimeFlush = new("FlexLayout.Dirty.RuntimeFlush");
        public static readonly ProfilerMarker EditorImplicitChildScan = new("FlexLayout.Editor.ImplicitChildScan");
        public static readonly ProfilerMarker EditorDirtyFlush = new("FlexLayout.Editor.DirtyFlush");
    }
}
