using System;
using UnityEngine;

namespace UnityEngine.UI.Flex.Core
{
    [Flags]
    internal enum FlexDriveMask
    {
        None = 0,
        Anchors = 1 << 0,
        PositionX = 1 << 1,
        PositionY = 1 << 2,
        SizeX = 1 << 3,
        SizeY = 1 << 4,
    }

    internal static class FlexDriveMaskExtensions
    {
        public static bool IsNone(this FlexDriveMask mask)
        {
            return mask == FlexDriveMask.None;
        }

        public static DrivenTransformProperties ToDrivenTransformProperties(this FlexDriveMask mask)
        {
            var driven = DrivenTransformProperties.None;
            if ((mask & FlexDriveMask.Anchors) != 0)
            {
                driven |= DrivenTransformProperties.Anchors;
            }

            if ((mask & FlexDriveMask.PositionX) != 0)
            {
                driven |= DrivenTransformProperties.AnchoredPositionX;
            }

            if ((mask & FlexDriveMask.PositionY) != 0)
            {
                driven |= DrivenTransformProperties.AnchoredPositionY;
            }

            if ((mask & FlexDriveMask.SizeX) != 0)
            {
                driven |= DrivenTransformProperties.SizeDeltaX;
            }

            if ((mask & FlexDriveMask.SizeY) != 0)
            {
                driven |= DrivenTransformProperties.SizeDeltaY;
            }

            return driven;
        }

    }
}
