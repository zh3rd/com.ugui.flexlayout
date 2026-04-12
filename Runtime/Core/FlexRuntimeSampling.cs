using System;
using System.Diagnostics;

namespace UnityEngine.UI.Flex.Core
{
#if FLEX_LAYOUT_PERF_SAMPLING
    internal readonly struct FlexRuntimeTimingSnapshot
    {
        public FlexRuntimeTimingSnapshot(
            double rebuildMs,
            double collectTreeMs,
            double buildMappingMs,
            double measureRootMs,
            double measureSubtreeMs,
            double measureContentMs,
            double measureTextMs,
            double preparedFlowMs,
            double allocateMainAxisMs,
            double allocateCrossAxisMs,
            double itemLayoutsMs,
            double wrapPrepareLinesMs,
            double wrapBuildLinesMs,
            double wrapResolveLineCrossMs,
            double arrangeSingleLineMs,
            double arrangeWrapMs,
            double applySubtreeMs,
            double applyLayoutSubtreeMs)
        {
            RebuildMs = rebuildMs;
            CollectTreeMs = collectTreeMs;
            BuildMappingMs = buildMappingMs;
            MeasureRootMs = measureRootMs;
            MeasureSubtreeMs = measureSubtreeMs;
            MeasureContentMs = measureContentMs;
            MeasureTextMs = measureTextMs;
            PreparedFlowMs = preparedFlowMs;
            AllocateMainAxisMs = allocateMainAxisMs;
            AllocateCrossAxisMs = allocateCrossAxisMs;
            ItemLayoutsMs = itemLayoutsMs;
            WrapPrepareLinesMs = wrapPrepareLinesMs;
            WrapBuildLinesMs = wrapBuildLinesMs;
            WrapResolveLineCrossMs = wrapResolveLineCrossMs;
            ArrangeSingleLineMs = arrangeSingleLineMs;
            ArrangeWrapMs = arrangeWrapMs;
            ApplySubtreeMs = applySubtreeMs;
            ApplyLayoutSubtreeMs = applyLayoutSubtreeMs;
        }

        public double RebuildMs { get; }
        public double CollectTreeMs { get; }
        public double BuildMappingMs { get; }
        public double MeasureRootMs { get; }
        public double MeasureSubtreeMs { get; }
        public double MeasureContentMs { get; }
        public double MeasureTextMs { get; }
        public double PreparedFlowMs { get; }
        public double AllocateMainAxisMs { get; }
        public double AllocateCrossAxisMs { get; }
        public double ItemLayoutsMs { get; }
        public double WrapPrepareLinesMs { get; }
        public double WrapBuildLinesMs { get; }
        public double WrapResolveLineCrossMs { get; }
        public double ArrangeSingleLineMs { get; }
        public double ArrangeWrapMs { get; }
        public double ApplySubtreeMs { get; }
        public double ApplyLayoutSubtreeMs { get; }
    }

    internal static class FlexRuntimeSampling
    {
        private sealed class RuntimeTimingAccumulator
        {
            public long RebuildStartTimestamp;
            public long CollectTreeTicks;
            public long BuildMappingTicks;
            public long MeasureRootTicks;
            public long MeasureSubtreeTicks;
            public long MeasureContentTicks;
            public long MeasureTextTicks;
            public long PreparedFlowTicks;
            public long AllocateMainAxisTicks;
            public long AllocateCrossAxisTicks;
            public long ItemLayoutsTicks;
            public long WrapPrepareLinesTicks;
            public long WrapBuildLinesTicks;
            public long WrapResolveLineCrossTicks;
            public long ArrangeSingleLineTicks;
            public long ArrangeWrapTicks;
            public long ApplySubtreeTicks;
            public long ApplyLayoutSubtreeTicks;

            public void Reset()
            {
                RebuildStartTimestamp = 0L;
                CollectTreeTicks = 0L;
                BuildMappingTicks = 0L;
                MeasureRootTicks = 0L;
                MeasureSubtreeTicks = 0L;
                MeasureContentTicks = 0L;
                MeasureTextTicks = 0L;
                PreparedFlowTicks = 0L;
                AllocateMainAxisTicks = 0L;
                AllocateCrossAxisTicks = 0L;
                ItemLayoutsTicks = 0L;
                WrapPrepareLinesTicks = 0L;
                WrapBuildLinesTicks = 0L;
                WrapResolveLineCrossTicks = 0L;
                ArrangeSingleLineTicks = 0L;
                ArrangeWrapTicks = 0L;
                ApplySubtreeTicks = 0L;
                ApplyLayoutSubtreeTicks = 0L;
            }
        }

        [ThreadStatic]
        private static RuntimeTimingAccumulator s_Current;

        [ThreadStatic]
        private static FlexRuntimeTimingSnapshot s_LastCompleted;

        internal static void BeginRebuild()
        {
            if (s_Current == null)
            {
                s_Current = new RuntimeTimingAccumulator();
            }

            s_Current.Reset();
            s_Current.RebuildStartTimestamp = Stopwatch.GetTimestamp();
        }

        internal static void EndRebuild()
        {
            if (s_Current == null || s_Current.RebuildStartTimestamp == 0L)
            {
                return;
            }

            var rebuildTicks = Stopwatch.GetTimestamp() - s_Current.RebuildStartTimestamp;
            s_LastCompleted = new FlexRuntimeTimingSnapshot(
                TicksToMs(rebuildTicks),
                TicksToMs(s_Current.CollectTreeTicks),
                TicksToMs(s_Current.BuildMappingTicks),
                TicksToMs(s_Current.MeasureRootTicks),
                TicksToMs(s_Current.MeasureSubtreeTicks),
                TicksToMs(s_Current.MeasureContentTicks),
                TicksToMs(s_Current.MeasureTextTicks),
                TicksToMs(s_Current.PreparedFlowTicks),
                TicksToMs(s_Current.AllocateMainAxisTicks),
                TicksToMs(s_Current.AllocateCrossAxisTicks),
                TicksToMs(s_Current.ItemLayoutsTicks),
                TicksToMs(s_Current.WrapPrepareLinesTicks),
                TicksToMs(s_Current.WrapBuildLinesTicks),
                TicksToMs(s_Current.WrapResolveLineCrossTicks),
                TicksToMs(s_Current.ArrangeSingleLineTicks),
                TicksToMs(s_Current.ArrangeWrapTicks),
                TicksToMs(s_Current.ApplySubtreeTicks),
                TicksToMs(s_Current.ApplyLayoutSubtreeTicks));
        }

        internal static FlexRuntimeTimingSnapshot GetLastCompletedTimingForTesting()
        {
            return s_LastCompleted;
        }

        internal static long BeginSample()
        {
            return Stopwatch.GetTimestamp();
        }

        internal static void AddCollectTreeTicks(long startedAt) => EnsureCurrent().CollectTreeTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddBuildMappingTicks(long startedAt) => EnsureCurrent().BuildMappingTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddMeasureRootTicks(long startedAt) => EnsureCurrent().MeasureRootTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddMeasureSubtreeTicks(long startedAt) => EnsureCurrent().MeasureSubtreeTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddMeasureContentTicks(long startedAt) => EnsureCurrent().MeasureContentTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddMeasureTextTicks(long startedAt) => EnsureCurrent().MeasureTextTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddPreparedFlowTicks(long startedAt) => EnsureCurrent().PreparedFlowTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddAllocateMainAxisTicks(long startedAt) => EnsureCurrent().AllocateMainAxisTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddAllocateCrossAxisTicks(long startedAt) => EnsureCurrent().AllocateCrossAxisTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddItemLayoutsTicks(long startedAt) => EnsureCurrent().ItemLayoutsTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddWrapPrepareLinesTicks(long startedAt) => EnsureCurrent().WrapPrepareLinesTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddWrapBuildLinesTicks(long startedAt) => EnsureCurrent().WrapBuildLinesTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddWrapResolveLineCrossTicks(long startedAt) => EnsureCurrent().WrapResolveLineCrossTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddArrangeSingleLineTicks(long startedAt) => EnsureCurrent().ArrangeSingleLineTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddArrangeWrapTicks(long startedAt) => EnsureCurrent().ArrangeWrapTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddApplySubtreeTicks(long startedAt) => EnsureCurrent().ApplySubtreeTicks += Stopwatch.GetTimestamp() - startedAt;
        internal static void AddApplyLayoutSubtreeTicks(long startedAt) => EnsureCurrent().ApplyLayoutSubtreeTicks += Stopwatch.GetTimestamp() - startedAt;

        private static RuntimeTimingAccumulator EnsureCurrent()
        {
            if (s_Current == null)
            {
                s_Current = new RuntimeTimingAccumulator();
            }

            return s_Current;
        }

        private static double TicksToMs(long ticks)
        {
            return ticks * 1000.0 / Stopwatch.Frequency;
        }
    }
#else
    internal readonly struct FlexRuntimeTimingSnapshot
    {
        public double RebuildMs => 0d;
        public double CollectTreeMs => 0d;
        public double BuildMappingMs => 0d;
        public double MeasureRootMs => 0d;
        public double MeasureSubtreeMs => 0d;
        public double MeasureContentMs => 0d;
        public double MeasureTextMs => 0d;
        public double PreparedFlowMs => 0d;
        public double AllocateMainAxisMs => 0d;
        public double AllocateCrossAxisMs => 0d;
        public double ItemLayoutsMs => 0d;
        public double WrapPrepareLinesMs => 0d;
        public double WrapBuildLinesMs => 0d;
        public double WrapResolveLineCrossMs => 0d;
        public double ArrangeSingleLineMs => 0d;
        public double ArrangeWrapMs => 0d;
        public double ApplySubtreeMs => 0d;
        public double ApplyLayoutSubtreeMs => 0d;
    }

    internal static class FlexRuntimeSampling
    {
        internal static void BeginRebuild() { }
        internal static void EndRebuild() { }
        internal static FlexRuntimeTimingSnapshot GetLastCompletedTimingForTesting() => default;
        internal static long BeginSample() => 0L;
        internal static void AddCollectTreeTicks(long startedAt) { }
        internal static void AddBuildMappingTicks(long startedAt) { }
        internal static void AddMeasureRootTicks(long startedAt) { }
        internal static void AddMeasureSubtreeTicks(long startedAt) { }
        internal static void AddMeasureContentTicks(long startedAt) { }
        internal static void AddMeasureTextTicks(long startedAt) { }
        internal static void AddPreparedFlowTicks(long startedAt) { }
        internal static void AddAllocateMainAxisTicks(long startedAt) { }
        internal static void AddAllocateCrossAxisTicks(long startedAt) { }
        internal static void AddItemLayoutsTicks(long startedAt) { }
        internal static void AddWrapPrepareLinesTicks(long startedAt) { }
        internal static void AddWrapBuildLinesTicks(long startedAt) { }
        internal static void AddWrapResolveLineCrossTicks(long startedAt) { }
        internal static void AddArrangeSingleLineTicks(long startedAt) { }
        internal static void AddArrangeWrapTicks(long startedAt) { }
        internal static void AddApplySubtreeTicks(long startedAt) { }
        internal static void AddApplyLayoutSubtreeTicks(long startedAt) { }
    }
#endif
}
