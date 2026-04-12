using System.Runtime.CompilerServices;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static class FlexAxisUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHorizontalMainAxis(FlexDirection direction)
        {
            return direction == FlexDirection.Row || direction == FlexDirection.RowReverse;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMainAxisReversed(FlexDirection direction)
        {
            return direction == FlexDirection.RowReverse || direction == FlexDirection.ColumnReverse;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCrossAxisReversed(FlexWrap wrap)
        {
            return wrap == FlexWrap.WrapReverse;
        }
    }
}
