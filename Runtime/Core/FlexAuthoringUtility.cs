using UnityEngine;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static class FlexAuthoringUtility
    {
        public static bool TryGetActiveDirectParentLayout(Transform transform, out FlexLayout layout)
        {
            layout = null;
            if (transform == null || transform.parent == null)
            {
                return false;
            }

            layout = transform.parent.GetComponent<FlexLayout>();
            return layout != null && layout.isActiveAndEnabled;
        }

        public static void NotifyAuthoringChanged(Component owner, bool forceImmediate = false)
        {
            if (owner == null)
            {
                return;
            }

            if (owner.TryGetComponent<FlexLayout>(out var selfLayout)
                && selfLayout.isActiveAndEnabled)
            {
                selfLayout.MarkDrivenPropertiesDirty();
                selfLayout.RequestLayoutDirty(forceImmediate);
            }

            if (TryGetActiveDirectParentLayout(owner.transform, out var parentLayout))
            {
                parentLayout.MarkDrivenPropertiesDirty();
                parentLayout.RequestLayoutDirty(forceImmediate);
            }
        }
    }
}
