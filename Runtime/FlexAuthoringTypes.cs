using System;
using UnityEngine;

namespace UnityEngine.UI.Flex
{
    [Serializable]
    public struct FlexNodeStyleData
    {
        private static readonly FlexNodeStyleData s_Default = new FlexNodeStyleData
        {
            width = FlexValue.Auto(),
            height = FlexValue.Auto(),
            aspectRatio = FlexOptionalFloat.Disabled(),
            minWidth = FlexOptionalFloat.Disabled(),
            maxWidth = FlexOptionalFloat.Disabled(),
            minHeight = FlexOptionalFloat.Disabled(),
            maxHeight = FlexOptionalFloat.Disabled(),
            positionType = PositionType.Relative,
        };

        public FlexValue width;
        public FlexValue height;
        public FlexOptionalFloat aspectRatio;
        public FlexOptionalFloat minWidth;
        public FlexOptionalFloat maxWidth;
        public FlexOptionalFloat minHeight;
        public FlexOptionalFloat maxHeight;
        public PositionType positionType;

        public static FlexNodeStyleData Default => s_Default;
    }

    [Serializable]
    public struct FlexItemStyleData
    {
        private static readonly FlexItemStyleData s_Default = new FlexItemStyleData
        {
            flexGrow = 0f,
            flexShrink = 1f,
            flexBasis = FlexValue.Auto(),
            alignSelf = AlignSelf.Auto,
        };

        [Min(0f)] public float flexGrow;
        [Min(0f)] public float flexShrink;
        public FlexValue flexBasis;
        public AlignSelf alignSelf;

        public static FlexItemStyleData Default => s_Default;
    }

    [Serializable]
    public struct FlexImplicitItemStyleData
    {
        private static readonly FlexImplicitItemStyleData s_Default = new FlexImplicitItemStyleData
        {
            width = FlexValue.Auto(),
            height = FlexValue.Auto(),
            flexGrow = 0f,
            flexShrink = 0f,
            flexBasis = FlexValue.Auto(),
            alignSelf = AlignSelf.Center,
        };

        public FlexValue width;
        public FlexValue height;
        [Min(0f)] public float flexGrow;
        [Min(0f)] public float flexShrink;
        public FlexValue flexBasis;
        public AlignSelf alignSelf;

        public static FlexImplicitItemStyleData Default => s_Default;
    }

}
