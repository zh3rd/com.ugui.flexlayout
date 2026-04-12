using System;
using UnityEngine;

namespace UnityEngine.UI.Flex
{
    public enum FlexDirection
    {
        Row,
        Column,
        RowReverse,
        ColumnReverse,
    }

    public enum FlexWrap
    {
        NoWrap,
        Wrap,
        WrapReverse,
    }

    public enum JustifyContent
    {
        FlexStart,
        Center,
        FlexEnd,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly,
    }

    public enum AlignItems
    {
        Stretch,
        FlexStart,
        Center,
        FlexEnd,
    }

    public enum AlignContent
    {
        Stretch,
        FlexStart,
        Center,
        FlexEnd,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly,
    }

    public enum AlignSelf
    {
        Auto,
        Stretch,
        FlexStart,
        Center,
        FlexEnd,
    }

    public enum PositionType
    {
        Relative,
        Absolute,
    }

    public enum FlexSizeMode
    {
        Auto,
        Points,
        Percent,
    }

    [Serializable]
    public struct FlexValue : IEquatable<FlexValue>
    {
        public FlexSizeMode mode;
        public float value;

        public static FlexValue Auto()
        {
            return new FlexValue
            {
                mode = FlexSizeMode.Auto,
                value = 0f,
            };
        }

        public static FlexValue Points(float points)
        {
            return new FlexValue
            {
                mode = FlexSizeMode.Points,
                value = points,
            };
        }

        public static FlexValue Percent(float percent)
        {
            return new FlexValue
            {
                mode = FlexSizeMode.Percent,
                value = percent,
            };
        }

        public bool Equals(FlexValue other)
        {
            return mode == other.mode && Mathf.Approximately(value, other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is FlexValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)mode, value);
        }
    }

    [Serializable]
    public struct FlexEdges : IEquatable<FlexEdges>
    {
        public float left;
        public float right;
        public float top;
        public float bottom;

        public static FlexEdges Zero => default;

        public bool Equals(FlexEdges other)
        {
            return Mathf.Approximately(left, other.left)
                && Mathf.Approximately(right, other.right)
                && Mathf.Approximately(top, other.top)
                && Mathf.Approximately(bottom, other.bottom);
        }

        public override bool Equals(object obj)
        {
            return obj is FlexEdges other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(left, right, top, bottom);
        }
    }

    [Serializable]
    public struct FlexOptionalFloat : IEquatable<FlexOptionalFloat>
    {
        public bool enabled;
        public float value;

        public static FlexOptionalFloat Disabled()
        {
            return new FlexOptionalFloat
            {
                enabled = false,
                value = 0f,
            };
        }

        public static FlexOptionalFloat Enabled(float value)
        {
            return new FlexOptionalFloat
            {
                enabled = true,
                value = value,
            };
        }

        public bool Equals(FlexOptionalFloat other)
        {
            return enabled == other.enabled && Mathf.Approximately(value, other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is FlexOptionalFloat other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(enabled, value);
        }
    }

    [Serializable]
    public struct FlexStyle
    {
        private static readonly FlexStyle s_Default = new FlexStyle
        {
            width = FlexValue.Auto(),
            height = FlexValue.Auto(),
            aspectRatio = FlexOptionalFloat.Disabled(),
            flexGrow = 0f,
            flexShrink = 1f,
            flexBasis = FlexValue.Auto(),
            alignSelf = AlignSelf.Auto,
            positionType = PositionType.Relative,
            flexDirection = FlexDirection.Row,
            flexWrap = FlexWrap.NoWrap,
            justifyContent = JustifyContent.FlexStart,
            alignItems = AlignItems.Stretch,
            alignContent = AlignContent.FlexStart,
            mainGap = 0f,
            crossGap = 0f,
            padding = FlexEdges.Zero,
        };

        public FlexValue width;
        public FlexValue height;
        public FlexOptionalFloat aspectRatio;
        public FlexOptionalFloat minWidth;
        public FlexOptionalFloat maxWidth;
        public FlexOptionalFloat minHeight;
        public FlexOptionalFloat maxHeight;

        [Min(0f)] public float flexGrow;
        [Min(0f)] public float flexShrink;
        public FlexValue flexBasis;
        public AlignSelf alignSelf;
        public PositionType positionType;

        public FlexDirection flexDirection;
        public FlexWrap flexWrap;
        public JustifyContent justifyContent;
        public AlignItems alignItems;
        public AlignContent alignContent;
        [Min(0f)] public float mainGap;
        [Min(0f)] public float crossGap;
        public FlexEdges padding;

        public static FlexStyle Default => s_Default;
    }
}
