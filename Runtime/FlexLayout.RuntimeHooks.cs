using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Flex.Core;

namespace UnityEngine.UI.Flex
{
    public sealed partial class FlexLayout
    {
        private readonly Dictionary<RectTransform, Vector2> m_RuntimeImplicitSizeSnapshots = new();
        private readonly HashSet<RectTransform> m_RuntimeImplicitSizeSnapshotVisited = new();
        private readonly List<RectTransform> m_RuntimeImplicitSizeSnapshotPrune = new();
        private bool m_RuntimeDirtyQueued;
        private bool m_DrivenPropertiesDirty = true;
        internal bool runtimeDirtyQueued
        {
            get => m_RuntimeDirtyQueued;
            set => m_RuntimeDirtyQueued = value;
        }

        public void MarkLayoutDirty()
        {
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            if (TryGetActiveFlexParent(out var parentLayout))
            {
                parentLayout.MarkLayoutDirty();
                return;
            }

#if FLEX_LAYOUT_DEBUG_LOGS
            if (!Application.isPlaying)
            {
                Debug.Log($"[FlexLayout] MarkLayoutDirty '{name}' dir={style.flexDirection} justify={style.justifyContent} align={style.alignItems}");
            }
#endif

            FlexRebuildPipeline.RebuildImmediate(this);
        }

        internal void RequestLayoutDirty()
        {
            RequestLayoutDirty(forceImmediate: false);
        }

        internal void RequestLayoutDirty(bool forceImmediate)
        {
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            if (TryGetActiveFlexParent(out var parentLayout))
            {
                parentLayout.RequestLayoutDirty(forceImmediate);
                return;
            }

            if (forceImmediate)
            {
                MarkLayoutDirty();
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorDirty();
                return;
            }
#endif

            QueueRuntimeDelayedDirty();
        }

        internal void RefreshDrivenProperties()
        {
            using var scope = FlexProfiler.TrackerRefreshDrivenProperties.Auto();
            ReleaseDrivenProperties();
            m_DrivenPropertiesDirty = false;

            if (!isActiveAndEnabled)
            {
                return;
            }

            var selfResolved = FlexResolvedNodeResolver.Resolve(rectTransform, FlexBridge.ResolveImplicitSizeForTesting(rectTransform));
            var childCount = rectTransform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                if (rectTransform.GetChild(i) is not RectTransform childRect)
                {
                    continue;
                }

                if (!childRect.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (FlexResolvedNodeResolver.ShouldExcludeDisabledLayoutOnlyChild(childRect))
                {
                    continue;
                }

                var childResolved = FlexResolvedNodeResolver.Resolve(childRect, FlexBridge.ResolveImplicitSizeForTesting(childRect));
                var childOwnership = FlexOwnershipResolver.ResolveChild(
                    drivesSize: FlexOwnershipResolver.ShouldDriveChildSize(selfResolved, childResolved),
                    childResolved.Node.PositionType);
                var childDriveMask = childOwnership.ToDriveMask();
                if (!childDriveMask.IsNone())
                {
                    FlexDrivenRegistry.SetContribution(this, childRect, childDriveMask);
                }

                if (childRect.TryGetComponent<FlexNodeBase>(out var childNode) && childNode.isActiveAndEnabled)
                {
                    childNode.RefreshForParentContextChange();
                }
            }
        }

        internal void MarkDrivenPropertiesDirty()
        {
            m_DrivenPropertiesDirty = true;
        }

        internal void EnsureDrivenPropertiesUpToDateRecursively()
        {
            EnsureDrivenPropertiesUpToDateRecursivelyInternal();
        }

        private void EnsureDrivenPropertiesUpToDateRecursivelyInternal()
        {
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            if (m_DrivenPropertiesDirty)
            {
                RefreshDrivenProperties();
            }

            var childCount = rectTransform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                if (rectTransform.GetChild(i) is not RectTransform childRect)
                {
                    continue;
                }

                if (!childRect.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!childRect.TryGetComponent<FlexLayout>(out var childLayout) || !childLayout.isActiveAndEnabled)
                {
                    continue;
                }

                childLayout.EnsureDrivenPropertiesUpToDateRecursivelyInternal();
            }
        }

        private void OnEnable()
        {
            ClearRuntimeImplicitSizeSnapshots();
            RefreshDrivenProperties();
            NotifyParentDrivenPropertiesChanged();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorDirty();
                return;
            }
#endif

            RequestLayoutDirty();
        }

        private void Awake()
        {
            if (!isActiveAndEnabled)
            {
                ReleaseDrivenProperties();
            }
        }

        private void OnDisable()
        {
            FlexRebuildPipeline.Remove(this);
            ClearRuntimeImplicitSizeSnapshots();
            ReleaseDrivenProperties();
            m_DrivenPropertiesDirty = true;
            RefreshChildLayoutsAfterDisable();
            NotifyParentLayoutDirty();
        }

        private void ReleaseDrivenProperties()
        {
            FlexDrivenRegistry.ClearOwner(this);
        }

        private void OnTransformChildrenChanged()
        {
            MarkDrivenPropertiesDirty();
            RequestLayoutDirty();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (IsRootLayout())
            {
                RequestLayoutDirty();
            }
        }

        private void OnDidApplyAnimationProperties()
        {
            RequestLayoutDirty();
        }

        private void OnTransformParentChanged()
        {
            MarkDrivenPropertiesDirty();
            NotifyParentLayoutDirty();
            RequestLayoutDirty();
        }

        private void NotifyParentLayoutDirty()
        {
            if (TryGetActiveFlexParent(out var parentLayout))
            {
                parentLayout.RequestLayoutDirty();
            }
        }

        private void NotifyParentDrivenPropertiesChanged()
        {
            if (TryGetActiveFlexParent(out var parentLayout))
            {
                parentLayout.MarkDrivenPropertiesDirty();
                parentLayout.RequestLayoutDirty(forceImmediate: !Application.isPlaying);
            }
        }

        private bool TryGetActiveFlexParent(out FlexLayout parentLayout)
        {
            parentLayout = null;
            if (transform.parent == null)
            {
                return false;
            }

            parentLayout = transform.parent.GetComponent<FlexLayout>();
            return parentLayout != null && parentLayout != this && parentLayout.isActiveAndEnabled;
        }

        private bool IsRootLayout()
        {
            return !TryGetActiveFlexParent(out _);
        }

        private void RefreshChildLayoutsAfterDisable()
        {
            var childCount = transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                if (transform.GetChild(i) is not RectTransform childRect)
                {
                    continue;
                }

                if (!childRect.TryGetComponent<FlexLayout>(out var childLayout))
                {
                    if (childRect.TryGetComponent<FlexNodeBase>(out var childNodeOnly) && childNodeOnly.isActiveAndEnabled)
                    {
                        childNodeOnly.RefreshForParentContextChange();
                    }

                    continue;
                }

                if (!childLayout.isActiveAndEnabled)
                {
                    continue;
                }

                childLayout.MarkDrivenPropertiesDirty();
                childLayout.RequestLayoutDirty();

                if (childRect.TryGetComponent<FlexNodeBase>(out var childNode) && childNode.isActiveAndEnabled)
                {
                    childNode.RefreshForParentContextChange();
                }
            }
        }

        private void QueueRuntimeDelayedDirty()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            FlexRebuildPipeline.EnqueueRuntime(this);
        }

        internal void BeginRuntimeImplicitSizeSnapshotPass()
        {
            m_RuntimeImplicitSizeSnapshotVisited.Clear();
        }

        internal Vector2 ResolveRuntimeImplicitSizeSnapshot(
            RectTransform rectTransform,
            in ResolvedFlexNode resolved,
            bool drivesSizeInParentFlow,
            Vector2 runtimeRectSize)
        {
            if (rectTransform == null)
            {
                return runtimeRectSize;
            }

            if (rectTransform == this.rectTransform)
            {
                return runtimeRectSize;
            }

            if (!resolved.HasNodeSource)
            {
                var key = rectTransform;
                m_RuntimeImplicitSizeSnapshotVisited.Add(key);

                if (!drivesSizeInParentFlow)
                {
                    m_RuntimeImplicitSizeSnapshots[key] = runtimeRectSize;
                    return runtimeRectSize;
                }

                if (m_RuntimeImplicitSizeSnapshots.TryGetValue(key, out var snapshot))
                {
                    return snapshot;
                }

                m_RuntimeImplicitSizeSnapshots[key] = runtimeRectSize;
                return runtimeRectSize;
            }

            return runtimeRectSize;
        }

        internal void EndRuntimeImplicitSizeSnapshotPass()
        {
            m_RuntimeImplicitSizeSnapshotPrune.Clear();
            foreach (var key in m_RuntimeImplicitSizeSnapshots.Keys)
            {
                if (!m_RuntimeImplicitSizeSnapshotVisited.Contains(key))
                {
                    m_RuntimeImplicitSizeSnapshotPrune.Add(key);
                }
            }

            for (var i = 0; i < m_RuntimeImplicitSizeSnapshotPrune.Count; i++)
            {
                m_RuntimeImplicitSizeSnapshots.Remove(m_RuntimeImplicitSizeSnapshotPrune[i]);
            }
        }

        internal void ClearRuntimeImplicitSizeSnapshots()
        {
            m_RuntimeImplicitSizeSnapshots.Clear();
            m_RuntimeImplicitSizeSnapshotVisited.Clear();
            m_RuntimeImplicitSizeSnapshotPrune.Clear();
        }
    }
}
