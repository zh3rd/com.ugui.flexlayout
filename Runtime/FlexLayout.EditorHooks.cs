#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

using UnityEngine.UI.Flex.Core;

namespace UnityEngine.UI.Flex
{
    public sealed partial class FlexLayout
    {
        private bool m_EditorDirtyQueued;
        private readonly System.Collections.Generic.Dictionary<RectTransform, Vector2> m_EditorImplicitChildSizes = new();
        private readonly System.Collections.Generic.List<RectTransform> m_EditorImplicitChildMissingKeys = new();
        private readonly System.Collections.Generic.HashSet<RectTransform> m_EditorImplicitChildSeenKeys = new();
        internal bool editorDirtyQueued
        {
            get => m_EditorDirtyQueued;
            set => m_EditorDirtyQueued = value;
        }

        private void OnValidate()
        {
            style.mainGap = Mathf.Max(0f, style.mainGap);
            style.crossGap = Mathf.Max(0f, style.crossGap);
            if (implicitItemDefaults.width.mode == FlexSizeMode.Points || implicitItemDefaults.width.mode == FlexSizeMode.Percent)
            {
                implicitItemDefaults.width.value = Mathf.Max(0f, implicitItemDefaults.width.value);
            }

            if (implicitItemDefaults.height.mode == FlexSizeMode.Points || implicitItemDefaults.height.mode == FlexSizeMode.Percent)
            {
                implicitItemDefaults.height.value = Mathf.Max(0f, implicitItemDefaults.height.value);
            }

            implicitItemDefaults.flexGrow = Mathf.Max(0f, implicitItemDefaults.flexGrow);
            implicitItemDefaults.flexShrink = Mathf.Max(0f, implicitItemDefaults.flexShrink);
#if FLEX_LAYOUT_DEBUG_LOGS
            Debug.Log($"[FlexLayout] OnValidate '{name}' dir={style.flexDirection} justify={style.justifyContent} align={style.alignItems}");
#endif

            if (!isActiveAndEnabled)
            {
                ReleaseDrivenProperties();
                return;
            }

            MarkDrivenPropertiesDirty();
            QueueEditorDirty();
        }

        private void QueueEditorDirty()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var target = GetEditorDirtyQueueTarget();
            if (target == null)
            {
                return;
            }

            FlexRebuildPipeline.EnqueueEditor(target);
        }

        private void ApplyEditorDirty()
        {
            FlexRebuildPipeline.ApplyEditorDirty(this);
        }

        private FlexLayout GetEditorDirtyQueueTarget()
        {
            var current = this;
            while (current != null && current.TryGetActiveFlexParent(out var parentLayout))
            {
                current = parentLayout;
            }

            return current;
        }

        private void Update()
        {
            if (Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            using var scope = FlexProfiler.EditorImplicitChildScan.Auto();
            var count = rectTransform.childCount;
            var dirty = false;
            m_EditorImplicitChildMissingKeys.Clear();
            m_EditorImplicitChildSeenKeys.Clear();

            for (var i = 0; i < count; i++)
            {
                if (rectTransform.GetChild(i) is not RectTransform childRect)
                {
                    continue;
                }

                var key = childRect;
                m_EditorImplicitChildSeenKeys.Add(key);

                if (!ShouldWatchImplicitChildSizeInEditor(childRect))
                {
                    m_EditorImplicitChildSizes.Remove(key);
                    continue;
                }

                var currentSize = childRect.sizeDelta;
                if (!m_EditorImplicitChildSizes.TryGetValue(key, out var previousSize) || !Approximately(previousSize, currentSize))
                {
                    m_EditorImplicitChildSizes[key] = currentSize;
                    dirty = true;
                }
            }

            foreach (var kvp in m_EditorImplicitChildSizes)
            {
                if (!m_EditorImplicitChildSeenKeys.Contains(kvp.Key))
                {
                    m_EditorImplicitChildMissingKeys.Add(kvp.Key);
                }
            }

            for (var i = 0; i < m_EditorImplicitChildMissingKeys.Count; i++)
            {
                m_EditorImplicitChildSizes.Remove(m_EditorImplicitChildMissingKeys[i]);
            }

            if (dirty)
            {
                RequestLayoutDirty();
            }
        }

        private static bool ShouldWatchImplicitChildSizeInEditor(RectTransform childRect)
        {
            if (childRect == null || !childRect.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (FlexResolvedNodeResolver.ShouldExcludeDisabledLayoutOnlyChild(childRect))
            {
                return false;
            }

            if (childRect.TryGetComponent<UnityEngine.UI.Flex.FlexNodeBase>(out var node) && node.isActiveAndEnabled)
            {
                return false;
            }

            return !childRect.TryGetComponent<FlexLayout>(out var layout) || !layout.isActiveAndEnabled;
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            const float epsilon = 0.01f;
            return Mathf.Abs(a.x - b.x) <= epsilon && Mathf.Abs(a.y - b.y) <= epsilon;
        }
    }
}
#endif
