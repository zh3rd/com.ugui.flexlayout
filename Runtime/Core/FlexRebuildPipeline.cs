using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.UI.Flex.Core
{
    internal static class FlexRebuildPipeline
    {
        private static readonly List<FlexLayout> s_RuntimeDirtyQueue = new();
        private static readonly List<FlexLayout> s_EditorDirtyQueue = new();
        private static readonly List<FlexLayout> s_FlushLayouts = new();
        private static readonly List<FlexBridge.FlexRebuildPlan> s_FlushPlans = new();
        private static bool s_RuntimeDirtyFlushRegistered;

#if UNITY_EDITOR
        private static bool s_EditorDirtyFlushRegistered;
#endif

        public static void EnqueueRuntime(FlexLayout layout)
        {
            if (layout == null || !layout.isActiveAndEnabled || !Application.isPlaying || layout.runtimeDirtyQueued)
            {
                return;
            }

            layout.runtimeDirtyQueued = true;
            s_RuntimeDirtyQueue.Add(layout);
            if (!s_RuntimeDirtyFlushRegistered)
            {
                Canvas.willRenderCanvases += FlushRuntimeDirtyQueue;
                s_RuntimeDirtyFlushRegistered = true;
            }
        }

#if UNITY_EDITOR
        public static void EnqueueEditor(FlexLayout layout)
        {
            if (layout == null || !layout.isActiveAndEnabled || Application.isPlaying || layout.editorDirtyQueued)
            {
                return;
            }

            layout.editorDirtyQueued = true;
            s_EditorDirtyQueue.Add(layout);
            if (!s_EditorDirtyFlushRegistered)
            {
                EditorApplication.delayCall += FlushEditorDirtyQueue;
                s_EditorDirtyFlushRegistered = true;
            }
        }
#endif

        public static void Remove(FlexLayout layout)
        {
            if (layout == null)
            {
                return;
            }

            layout.runtimeDirtyQueued = false;
            s_RuntimeDirtyQueue.Remove(layout);

#if UNITY_EDITOR
            layout.editorDirtyQueued = false;
            s_EditorDirtyQueue.Remove(layout);
#endif
        }

        public static void RebuildImmediate(FlexLayout layout)
        {
            if (layout == null || !layout.isActiveAndEnabled)
            {
                return;
            }

            FlushLayouts(layout);
        }

        public static void ApplyEditorDirty(FlexLayout layout)
        {
            if (layout == null || !layout.isActiveAndEnabled || Application.isPlaying)
            {
                return;
            }

            layout.editorDirtyQueued = false;
            FlushLayouts(layout);
        }

        private static void FlushRuntimeDirtyQueue()
        {
            using var scope = FlexProfiler.DirtyRuntimeFlush.Auto();
            if (s_RuntimeDirtyQueue.Count == 0)
            {
                if (s_RuntimeDirtyFlushRegistered)
                {
                    Canvas.willRenderCanvases -= FlushRuntimeDirtyQueue;
                    s_RuntimeDirtyFlushRegistered = false;
                }

                return;
            }

            CopyAndClearQueue(s_RuntimeDirtyQueue, s_FlushLayouts, isRuntimeQueue: true);
            FlushLayouts(s_FlushLayouts);
            s_FlushLayouts.Clear();
        }

#if UNITY_EDITOR
        private static void FlushEditorDirtyQueue()
        {
            using var scope = FlexProfiler.EditorDirtyFlush.Auto();
            EditorApplication.delayCall -= FlushEditorDirtyQueue;
            s_EditorDirtyFlushRegistered = false;

            if (s_EditorDirtyQueue.Count == 0)
            {
                return;
            }

            CopyAndClearQueue(s_EditorDirtyQueue, s_FlushLayouts, isRuntimeQueue: false);
            FlushLayouts(s_FlushLayouts);
            s_FlushLayouts.Clear();
        }
#endif

        private static void CopyAndClearQueue(List<FlexLayout> source, List<FlexLayout> target, bool isRuntimeQueue)
        {
            for (var i = 0; i < source.Count; i++)
            {
                var layout = source[i];
                source[i] = null;
                if (layout == null)
                {
                    continue;
                }

                if (isRuntimeQueue)
                {
                    layout.runtimeDirtyQueued = false;
                }
                else
                {
                    layout.editorDirtyQueued = false;
                }

                target.Add(layout);
            }

            source.Clear();
        }

        private static void FlushLayouts(FlexLayout layout)
        {
            s_FlushLayouts.Clear();
            s_FlushLayouts.Add(layout);
            FlushLayouts(s_FlushLayouts);
            s_FlushLayouts.Clear();
        }

        private static void FlushLayouts(List<FlexLayout> layouts)
        {
            if (layouts.Count == 0)
            {
                return;
            }

            s_FlushPlans.Clear();
            try
            {
                for (var i = 0; i < layouts.Count; i++)
                {
                    var layout = layouts[i];
                    if (layout == null || !layout.isActiveAndEnabled)
                    {
                        continue;
                    }

                    layout.EnsureDrivenPropertiesUpToDateRecursively();
                    s_FlushPlans.Add(FlexBridge.CollectPlan(layout));
                }

                for (var i = 0; i < s_FlushPlans.Count; i++)
                {
                    FlexBridge.ComputePlan(s_FlushPlans[i]);
                }

                for (var i = 0; i < s_FlushPlans.Count; i++)
                {
                    FlexBridge.ApplyPlan(s_FlushPlans[i]);
                }
            }
            finally
            {
                for (var i = 0; i < s_FlushPlans.Count; i++)
                {
                    FlexBridge.DisposePlan(s_FlushPlans[i]);
                }

                s_FlushPlans.Clear();
            }
        }
    }
}
