using System.Collections.Generic;

using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexMeasure
    {
        public static IReadOnlyList<FlexItemLayoutResult> Arrange(
            FlexNodeStore store,
            FlexNodeId parentId,
            float availableMainAxisSize,
            float availableCrossAxisSize)
        {
            using var profilerScope = FlexProfiler.ArrangeFlow.Auto();
            var ownsPass = EnterMeasurePass();
            try
            {
                var parent = store.GetNode(parentId);
                if (parent.Style.flexWrap == FlexWrap.NoWrap)
                {
                    return ArrangeSingleLine(store, parentId, availableMainAxisSize, availableCrossAxisSize);
                }

                return ArrangeWrapped(store, parentId, availableMainAxisSize, availableCrossAxisSize);
            }
            finally
            {
                ExitMeasurePass(ownsPass);
            }
        }
    }
}
