using System;
using System.Collections.Generic;

using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal readonly struct FlexNodeId : IEquatable<FlexNodeId>
    {
        public FlexNodeId(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public bool IsValid => Value > 0;

        public bool Equals(FlexNodeId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is FlexNodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal readonly struct FlexMeasuredSize : IEquatable<FlexMeasuredSize>
    {
        public FlexMeasuredSize(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; }

        public float Height { get; }

        public bool Equals(FlexMeasuredSize other)
        {
            return UnityEngine.Mathf.Approximately(Width, other.Width)
                && UnityEngine.Mathf.Approximately(Height, other.Height);
        }

        public override bool Equals(object obj)
        {
            return obj is FlexMeasuredSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }
    }

    internal readonly struct FlexMainAxisAllocation : IEquatable<FlexMainAxisAllocation>
    {
        public FlexMainAxisAllocation(FlexNodeId nodeId, float basis, float finalSize)
        {
            NodeId = nodeId;
            Basis = basis;
            FinalSize = finalSize;
        }

        public FlexNodeId NodeId { get; }

        public float Basis { get; }

        public float FinalSize { get; }

        public bool Equals(FlexMainAxisAllocation other)
        {
            return NodeId.Equals(other.NodeId)
                && UnityEngine.Mathf.Approximately(Basis, other.Basis)
                && UnityEngine.Mathf.Approximately(FinalSize, other.FinalSize);
        }

        public override bool Equals(object obj)
        {
            return obj is FlexMainAxisAllocation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeId, Basis, FinalSize);
        }
    }

    internal readonly struct FlexCrossAxisAllocation : IEquatable<FlexCrossAxisAllocation>
    {
        public FlexCrossAxisAllocation(FlexNodeId nodeId, float measuredCrossSize, float finalCrossSize, bool stretched)
        {
            NodeId = nodeId;
            MeasuredCrossSize = measuredCrossSize;
            FinalCrossSize = finalCrossSize;
            Stretched = stretched;
        }

        public FlexNodeId NodeId { get; }

        public float MeasuredCrossSize { get; }

        public float FinalCrossSize { get; }

        public bool Stretched { get; }

        public bool Equals(FlexCrossAxisAllocation other)
        {
            return NodeId.Equals(other.NodeId)
                && UnityEngine.Mathf.Approximately(MeasuredCrossSize, other.MeasuredCrossSize)
                && UnityEngine.Mathf.Approximately(FinalCrossSize, other.FinalCrossSize)
                && Stretched == other.Stretched;
        }

        public override bool Equals(object obj)
        {
            return obj is FlexCrossAxisAllocation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeId, MeasuredCrossSize, FinalCrossSize, Stretched);
        }
    }

    internal readonly struct FlexItemMeasuredLayout : IEquatable<FlexItemMeasuredLayout>
    {
        public FlexItemMeasuredLayout(
            FlexNodeId nodeId,
            float measuredWidth,
            float measuredHeight,
            float mainAxisBasis,
            float finalMainAxisSize,
            float measuredCrossAxisSize,
            float finalCrossAxisSize,
            bool stretchedOnCrossAxis)
        {
            NodeId = nodeId;
            MeasuredWidth = measuredWidth;
            MeasuredHeight = measuredHeight;
            MainAxisBasis = mainAxisBasis;
            FinalMainAxisSize = finalMainAxisSize;
            MeasuredCrossAxisSize = measuredCrossAxisSize;
            FinalCrossAxisSize = finalCrossAxisSize;
            StretchedOnCrossAxis = stretchedOnCrossAxis;
        }

        public FlexNodeId NodeId { get; }

        public float MeasuredWidth { get; }

        public float MeasuredHeight { get; }

        public float MainAxisBasis { get; }

        public float FinalMainAxisSize { get; }

        public float MeasuredCrossAxisSize { get; }

        public float FinalCrossAxisSize { get; }

        public bool StretchedOnCrossAxis { get; }

        public bool Equals(FlexItemMeasuredLayout other)
        {
            return NodeId.Equals(other.NodeId)
                && UnityEngine.Mathf.Approximately(MeasuredWidth, other.MeasuredWidth)
                && UnityEngine.Mathf.Approximately(MeasuredHeight, other.MeasuredHeight)
                && UnityEngine.Mathf.Approximately(MainAxisBasis, other.MainAxisBasis)
                && UnityEngine.Mathf.Approximately(FinalMainAxisSize, other.FinalMainAxisSize)
                && UnityEngine.Mathf.Approximately(MeasuredCrossAxisSize, other.MeasuredCrossAxisSize)
                && UnityEngine.Mathf.Approximately(FinalCrossAxisSize, other.FinalCrossAxisSize)
                && StretchedOnCrossAxis == other.StretchedOnCrossAxis;
        }

        public override bool Equals(object obj)
        {
            return obj is FlexItemMeasuredLayout other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                NodeId,
                MeasuredWidth,
                MeasuredHeight,
                MainAxisBasis,
                FinalMainAxisSize,
                MeasuredCrossAxisSize,
                FinalCrossAxisSize,
                StretchedOnCrossAxis);
        }
    }

    internal readonly struct FlexItemLayoutResult : IEquatable<FlexItemLayoutResult>
    {
        public FlexItemLayoutResult(FlexNodeId nodeId, float x, float y, float width, float height)
        {
            NodeId = nodeId;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public FlexNodeId NodeId { get; }

        public float X { get; }

        public float Y { get; }

        public float Width { get; }

        public float Height { get; }

        public bool Equals(FlexItemLayoutResult other)
        {
            return NodeId.Equals(other.NodeId)
                && UnityEngine.Mathf.Approximately(X, other.X)
                && UnityEngine.Mathf.Approximately(Y, other.Y)
                && UnityEngine.Mathf.Approximately(Width, other.Width)
                && UnityEngine.Mathf.Approximately(Height, other.Height);
        }

        public override bool Equals(object obj)
        {
            return obj is FlexItemLayoutResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeId, X, Y, Width, Height);
        }
    }

    internal readonly struct FlexLine : IEquatable<FlexLine>
    {
        public FlexLine(IReadOnlyList<FlexNodeId> nodeIds, float totalMainSize, float maxCrossSize)
        {
            NodeIds = nodeIds;
            TotalMainSize = totalMainSize;
            MaxCrossSize = maxCrossSize;
        }

        public IReadOnlyList<FlexNodeId> NodeIds { get; }

        public float TotalMainSize { get; }

        public float MaxCrossSize { get; }

        public bool Equals(FlexLine other)
        {
            if (!UnityEngine.Mathf.Approximately(TotalMainSize, other.TotalMainSize)
                || !UnityEngine.Mathf.Approximately(MaxCrossSize, other.MaxCrossSize))
            {
                return false;
            }

            if (NodeIds == null || other.NodeIds == null)
            {
                return NodeIds == other.NodeIds;
            }

            if (NodeIds.Count != other.NodeIds.Count)
            {
                return false;
            }

            for (var i = 0; i < NodeIds.Count; i++)
            {
                if (!NodeIds[i].Equals(other.NodeIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is FlexLine other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(TotalMainSize, MaxCrossSize);
            if (NodeIds == null)
            {
                return hash;
            }

            for (var i = 0; i < NodeIds.Count; i++)
            {
                hash = HashCode.Combine(hash, NodeIds[i]);
            }

            return hash;
        }
    }

    internal readonly struct FlexNodeIdSlice : IReadOnlyList<FlexNodeId>
    {
        private readonly FlexNodeId[] m_Buffer;
        private readonly int m_Start;
        private readonly int m_Count;

        public FlexNodeIdSlice(FlexNodeId[] buffer, int start, int count)
        {
            m_Buffer = buffer;
            m_Start = start;
            m_Count = count;
        }

        public int Count => m_Count;

        public FlexNodeId this[int index] => m_Buffer[m_Start + index];

        public IEnumerator<FlexNodeId> GetEnumerator()
        {
            for (var i = 0; i < m_Count; i++)
            {
                yield return m_Buffer[m_Start + i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal readonly struct FlexLineRange
    {
        public FlexLineRange(int start, int count, float totalMainSize, float maxCrossSize)
        {
            Start = start;
            Count = count;
            TotalMainSize = totalMainSize;
            MaxCrossSize = maxCrossSize;
        }

        public int Start { get; }

        public int Count { get; }

        public float TotalMainSize { get; }

        public float MaxCrossSize { get; }
    }

    internal readonly struct FlexPreparedWrapItem
    {
        public FlexPreparedWrapItem(
            FlexNodeId nodeId,
            FlexNodeModel node,
            float basis,
            float measuredCrossSize,
            AlignSelf resolvedAlignSelf)
        {
            NodeId = nodeId;
            Node = node;
            Basis = basis;
            MeasuredCrossSize = measuredCrossSize;
            ResolvedAlignSelf = resolvedAlignSelf;
        }

        public FlexNodeId NodeId { get; }

        public FlexNodeModel Node { get; }

        public float Basis { get; }

        public float MeasuredCrossSize { get; }

        public AlignSelf ResolvedAlignSelf { get; }
    }

    internal readonly struct FlexPreparedFlowItem
    {
        public FlexPreparedFlowItem(
            FlexNodeId nodeId,
            FlexNodeModel node,
            FlexMeasuredSize measured,
            float basis,
            float measuredCrossSize,
            AlignSelf resolvedAlignSelf)
        {
            NodeId = nodeId;
            Node = node;
            Measured = measured;
            Basis = basis;
            MeasuredCrossSize = measuredCrossSize;
            ResolvedAlignSelf = resolvedAlignSelf;
        }

        public FlexNodeId NodeId { get; }

        public FlexNodeModel Node { get; }

        public FlexMeasuredSize Measured { get; }

        public float Basis { get; }

        public float MeasuredCrossSize { get; }

        public AlignSelf ResolvedAlignSelf { get; }
    }

    internal readonly struct FlexPreparedWrapItemSlice : IReadOnlyList<FlexPreparedWrapItem>
    {
        private readonly FlexPreparedWrapItem[] m_Buffer;
        private readonly int m_Start;
        private readonly int m_Count;

        public FlexPreparedWrapItemSlice(FlexPreparedWrapItem[] buffer, int start, int count)
        {
            m_Buffer = buffer;
            m_Start = start;
            m_Count = count;
        }

        public int Count => m_Count;

        public FlexPreparedWrapItem this[int index] => m_Buffer[m_Start + index];

        public IEnumerator<FlexPreparedWrapItem> GetEnumerator()
        {
            for (var i = 0; i < m_Count; i++)
            {
                yield return m_Buffer[m_Start + i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal readonly struct FlexPreparedWrapLine
    {
        public FlexPreparedWrapLine(
            IReadOnlyList<FlexPreparedWrapItem> items,
            float totalMainSize,
            float totalBasis,
            float maxCrossSize)
        {
            Items = items;
            TotalMainSize = totalMainSize;
            TotalBasis = totalBasis;
            MaxCrossSize = maxCrossSize;
        }

        public IReadOnlyList<FlexPreparedWrapItem> Items { get; }

        public float TotalMainSize { get; }

        public float TotalBasis { get; }

        public float MaxCrossSize { get; }
    }

    internal readonly struct FlexPreparedWrapLineRange
    {
        public FlexPreparedWrapLineRange(int start, int count, float totalMainSize, float totalBasis, float maxCrossSize)
        {
            Start = start;
            Count = count;
            TotalMainSize = totalMainSize;
            TotalBasis = totalBasis;
            MaxCrossSize = maxCrossSize;
        }

        public int Start { get; }

        public int Count { get; }

        public float TotalMainSize { get; }

        public float TotalBasis { get; }

        public float MaxCrossSize { get; }
    }

    internal readonly struct FlexMeasurePassStatistics : IEquatable<FlexMeasurePassStatistics>
    {
        public FlexMeasurePassStatistics(
            int measureSubtreeRequests,
            int measureSubtreeHits,
            int mainAxisBasisRequests,
            int mainAxisBasisHits,
            int lineBuildRequests,
            int lineBuildHits,
            int preparedFlowRequests,
            int preparedFlowHits,
            int preparedWrapLineRequests,
            int preparedWrapLineHits)
        {
            MeasureSubtreeRequests = measureSubtreeRequests;
            MeasureSubtreeHits = measureSubtreeHits;
            MainAxisBasisRequests = mainAxisBasisRequests;
            MainAxisBasisHits = mainAxisBasisHits;
            LineBuildRequests = lineBuildRequests;
            LineBuildHits = lineBuildHits;
            PreparedFlowRequests = preparedFlowRequests;
            PreparedFlowHits = preparedFlowHits;
            PreparedWrapLineRequests = preparedWrapLineRequests;
            PreparedWrapLineHits = preparedWrapLineHits;
        }

        public int MeasureSubtreeRequests { get; }

        public int MeasureSubtreeHits { get; }

        public int MainAxisBasisRequests { get; }

        public int MainAxisBasisHits { get; }

        public int LineBuildRequests { get; }

        public int LineBuildHits { get; }

        public int PreparedFlowRequests { get; }

        public int PreparedFlowHits { get; }

        public int PreparedWrapLineRequests { get; }

        public int PreparedWrapLineHits { get; }

        public bool Equals(FlexMeasurePassStatistics other)
        {
            return MeasureSubtreeRequests == other.MeasureSubtreeRequests
                && MeasureSubtreeHits == other.MeasureSubtreeHits
                && MainAxisBasisRequests == other.MainAxisBasisRequests
                && MainAxisBasisHits == other.MainAxisBasisHits
                && LineBuildRequests == other.LineBuildRequests
                && LineBuildHits == other.LineBuildHits
                && PreparedFlowRequests == other.PreparedFlowRequests
                && PreparedFlowHits == other.PreparedFlowHits
                && PreparedWrapLineRequests == other.PreparedWrapLineRequests
                && PreparedWrapLineHits == other.PreparedWrapLineHits;
        }

        public override bool Equals(object obj)
        {
            return obj is FlexMeasurePassStatistics other && Equals(other);
        }

        public override int GetHashCode()
        {
            var first = HashCode.Combine(
                MeasureSubtreeRequests,
                MeasureSubtreeHits,
                MainAxisBasisRequests,
                MainAxisBasisHits,
                LineBuildRequests);
            var second = HashCode.Combine(
                LineBuildHits,
                PreparedFlowRequests,
                PreparedFlowHits,
                PreparedWrapLineRequests,
                PreparedWrapLineHits);
            return HashCode.Combine(first, second);
        }
    }

    internal readonly struct FlexPercentReferenceOverrides
    {
        public FlexPercentReferenceOverrides(
            bool hasWidthReference,
            float widthReference,
            bool hasHeightReference,
            float heightReference)
        {
            HasWidthReference = hasWidthReference;
            WidthReference = widthReference;
            HasHeightReference = hasHeightReference;
            HeightReference = heightReference;
        }

        public bool HasWidthReference { get; }

        public float WidthReference { get; }

        public bool HasHeightReference { get; }

        public float HeightReference { get; }

        public static FlexPercentReferenceOverrides None => default;
    }

    internal struct FlexNodeModel
    {
        public FlexNodeId Id;
        public FlexNodeId ParentId;
        public FlexStyle Style;
        public bool IsExplicit;
        public bool HasNodeSource;
        public bool HasItemSource;
        public bool IsContainer;
        public bool HasLayoutComponent;
        public bool HasExternalWidthConstraint;
        public float ExternalWidthConstraint;
        public bool HasExternalHeightConstraint;
        public float ExternalHeightConstraint;
        public float ContentWidth;
        public float ContentHeight;
        public float ImplicitRectWidth;
        public float ImplicitRectHeight;
        public bool AllowImplicitRectPassthrough;

        public bool UsesNodeSource => HasNodeSource || IsExplicit;
        public bool UsesItemSource => HasItemSource || IsExplicit;
        public bool IsContainerNode => IsContainer || IsExplicit;
    }

    internal sealed class FlexNodeStore
    {
        private readonly List<FlexNodeModel> m_Nodes = new List<FlexNodeModel> { default };
        private readonly List<List<FlexNodeId>> m_Children = new List<List<FlexNodeId>> { null };
        private int m_NextId = 1;

        public FlexNodeId CreateNode(FlexNodeModel node)
        {
            var id = new FlexNodeId(m_NextId++);
            node.Id = id;
            m_Nodes.Add(node);
            m_Children.Add(new List<FlexNodeId>());

            if (node.ParentId.IsValid)
            {
                GetOrCreateChildren(node.ParentId).Add(id);
            }

            return id;
        }

        public FlexNodeModel GetNode(FlexNodeId id)
        {
            return m_Nodes[id.Value];
        }

        public void SetNode(FlexNodeModel node)
        {
            m_Nodes[node.Id.Value] = node;
        }

        public IReadOnlyList<FlexNodeId> GetChildren(FlexNodeId parentId)
        {
            return GetOrCreateChildren(parentId);
        }

        private List<FlexNodeId> GetOrCreateChildren(FlexNodeId id)
        {
            var rawId = id.Value;
            if (rawId <= 0 || rawId >= m_Children.Count)
            {
                return s_EmptyChildren;
            }

            var children = m_Children[rawId];
            if (children == null)
            {
                children = new List<FlexNodeId>();
                m_Children[rawId] = children;
            }

            return children;
        }

        private static readonly List<FlexNodeId> s_EmptyChildren = new List<FlexNodeId>(0);
    }
}
