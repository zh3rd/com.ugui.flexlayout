using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexMeasure
    {
        internal readonly struct MeasurePassScope : IDisposable
        {
            private readonly bool m_OwnsPass;

            public MeasurePassScope(bool ownsPass)
            {
                m_OwnsPass = ownsPass;
            }

            public void Dispose()
            {
                ExitMeasurePass(m_OwnsPass);
            }
        }

        private sealed class MeasurePassContext
        {
            public readonly Dictionary<FlexNodeId, FlexMeasuredSize> MeasureCache = new();
            public readonly Dictionary<FlexBasisCacheKey, float> MainAxisBasisCache = new();
            public readonly Dictionary<FlexLineCacheKey, IReadOnlyList<FlexLine>> LineCache = new();
            public readonly Dictionary<FlexLineCacheKey, IReadOnlyList<FlexPreparedFlowItem>> PreparedFlowItemCache = new();
            public readonly Dictionary<FlexLineCacheKey, IReadOnlyList<FlexPreparedWrapLine>> PreparedLineCache = new();
            public readonly List<float> MainAxisBasisScratch = new();
            public readonly List<FlexNodeModel> MainAxisNodesScratch = new();
            public readonly List<FlexNodeId> LineNodeBufferScratch = new();
            public readonly List<FlexLineRange> LineRangesScratch = new();
            public readonly List<float> WrapLineFinalMainSizesScratch = new();
            public readonly List<float> WrapLineCrossSizesScratch = new();
            public readonly List<FlexNodeModel> WrapLineNodesScratch = new();
            public readonly List<float> WrapLineMeasuredCrossScratch = new();
            public readonly List<AlignSelf> WrapLineAlignSelfScratch = new();
            public readonly List<FlexPreparedWrapItem> PreparedWrapItemsScratch = new();
            public readonly List<FlexPreparedWrapLineRange> PreparedWrapLineRangesScratch = new();
            public readonly List<FlexPreparedFlowItem> PreparedFlowItemsScratch = new();
            public int MeasureSubtreeRequests;
            public int MeasureSubtreeHits;
            public int MainAxisBasisRequests;
            public int MainAxisBasisHits;
            public int LineBuildRequests;
            public int LineBuildHits;
            public int PreparedFlowRequests;
            public int PreparedFlowHits;
            public int PreparedWrapLineRequests;
            public int PreparedWrapLineHits;

            public void Reset()
            {
                MeasureCache.Clear();
                MainAxisBasisCache.Clear();
                LineCache.Clear();
                PreparedFlowItemCache.Clear();
                PreparedLineCache.Clear();
                MeasureSubtreeRequests = 0;
                MeasureSubtreeHits = 0;
                MainAxisBasisRequests = 0;
                MainAxisBasisHits = 0;
                LineBuildRequests = 0;
                LineBuildHits = 0;
                PreparedFlowRequests = 0;
                PreparedFlowHits = 0;
                PreparedWrapLineRequests = 0;
                PreparedWrapLineHits = 0;
            }

            public FlexMeasurePassStatistics ToStatistics()
            {
                return new FlexMeasurePassStatistics(
                    MeasureSubtreeRequests,
                    MeasureSubtreeHits,
                    MainAxisBasisRequests,
                    MainAxisBasisHits,
                    LineBuildRequests,
                    LineBuildHits,
                    PreparedFlowRequests,
                    PreparedFlowHits,
                    PreparedWrapLineRequests,
                    PreparedWrapLineHits);
            }
        }

        private readonly struct FlexBasisCacheKey : IEquatable<FlexBasisCacheKey>
        {
            private readonly int m_NodeId;
            private readonly bool m_IsHorizontalMainAxis;
            private readonly int m_AvailableInnerMainAxisBits;

            public FlexBasisCacheKey(FlexNodeId nodeId, bool isHorizontalMainAxis, float availableInnerMainAxisSize)
            {
                m_NodeId = nodeId.Value;
                m_IsHorizontalMainAxis = isHorizontalMainAxis;
                m_AvailableInnerMainAxisBits = BitConverter.SingleToInt32Bits(availableInnerMainAxisSize);
            }

            public bool Equals(FlexBasisCacheKey other)
            {
                return m_NodeId == other.m_NodeId
                    && m_IsHorizontalMainAxis == other.m_IsHorizontalMainAxis
                    && m_AvailableInnerMainAxisBits == other.m_AvailableInnerMainAxisBits;
            }

            public override bool Equals(object obj)
            {
                return obj is FlexBasisCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_NodeId, m_IsHorizontalMainAxis, m_AvailableInnerMainAxisBits);
            }
        }

        private readonly struct FlexLineCacheKey : IEquatable<FlexLineCacheKey>
        {
            private readonly int m_ParentId;
            private readonly int m_AvailableMainAxisBits;

            public FlexLineCacheKey(FlexNodeId parentId, float availableMainAxisSize)
            {
                m_ParentId = parentId.Value;
                m_AvailableMainAxisBits = BitConverter.SingleToInt32Bits(availableMainAxisSize);
            }

            public bool Equals(FlexLineCacheKey other)
            {
                return m_ParentId == other.m_ParentId
                    && m_AvailableMainAxisBits == other.m_AvailableMainAxisBits;
            }

            public override bool Equals(object obj)
            {
                return obj is FlexLineCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_ParentId, m_AvailableMainAxisBits);
            }
        }

        [ThreadStatic]
        private static MeasurePassContext s_MeasurePassContext;

        [ThreadStatic]
        private static int s_MeasurePassDepth;

        [ThreadStatic]
        private static FlexMeasurePassStatistics s_LastCompletedMeasurePassStatistics;

        private static bool EnterMeasurePass()
        {
            var ownsPass = s_MeasurePassDepth == 0;
            if (ownsPass)
            {
                if (s_MeasurePassContext == null)
                {
                    s_MeasurePassContext = new MeasurePassContext();
                }

                s_MeasurePassContext.Reset();
            }

            s_MeasurePassDepth++;
            return ownsPass;
        }

        internal static MeasurePassScope BeginMeasurePass()
        {
            return new MeasurePassScope(EnterMeasurePass());
        }

        internal static FlexMeasurePassStatistics GetLastCompletedMeasurePassStatisticsForTesting()
        {
            return s_LastCompletedMeasurePassStatistics;
        }

        private static MeasurePassContext GetMeasurePassContext()
        {
            if (s_MeasurePassContext == null)
            {
                s_MeasurePassContext = new MeasurePassContext();
            }

            return s_MeasurePassContext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<T> GetScratchList<T>(List<T> scratch, int capacity)
        {
            scratch.Clear();
            if (scratch.Capacity < capacity)
            {
                scratch.Capacity = capacity;
            }

            return scratch;
        }

        private static void ExitMeasurePass(bool ownsPass)
        {
            s_MeasurePassDepth = UnityEngine.Mathf.Max(0, s_MeasurePassDepth - 1);
            if (!ownsPass || s_MeasurePassDepth != 0 || s_MeasurePassContext == null)
            {
                return;
            }

            s_LastCompletedMeasurePassStatistics = s_MeasurePassContext.ToStatistics();
            s_MeasurePassContext.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ResolveMainAxisBasis(
            FlexNodeModel child,
            bool isHorizontalMainAxis,
            float availableInnerMainSize,
            FlexMeasuredSize measured)
        {
            var context = GetMeasurePassContext();
            context.MainAxisBasisRequests++;
            var cacheKey = new FlexBasisCacheKey(child.Id, isHorizontalMainAxis, availableInnerMainSize);
            if (context.MainAxisBasisCache.TryGetValue(cacheKey, out var cached))
            {
                context.MainAxisBasisHits++;
                return cached;
            }

            var contentMain = isHorizontalMainAxis ? measured.Width : measured.Height;
            var mainAxisSize = isHorizontalMainAxis ? child.Style.width : child.Style.height;
            var hasExternalConstraint = isHorizontalMainAxis ? child.HasExternalWidthConstraint : child.HasExternalHeightConstraint;
            var externalConstraint = isHorizontalMainAxis ? child.ExternalWidthConstraint : child.ExternalHeightConstraint;
            var basis = FlexSizing.ResolveFlexBasis(
                child.Style.flexBasis,
                mainAxisSize,
                new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasPercentReferenceSize: true,
                    percentReferenceSize: availableInnerMainSize,
                    hasExternalConstraint: hasExternalConstraint,
                    externalConstraintSize: externalConstraint,
                    contentSize: contentMain));
            context.MainAxisBasisCache[cacheKey] = basis;
            return basis;
        }
    }
}
