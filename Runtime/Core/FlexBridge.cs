using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal static partial class FlexBridge
    {
        private const float ApplyEpsilon = 0.01f;
        internal sealed class FlexRebuildPlan
        {
            internal FlexRebuildPlan(FlexLayout rootLayout, FlexNodeStore store, BridgeNodeMappingIndex mappingIndex, FlexNodeId rootId)
            {
                m_RootLayout = rootLayout;
                m_Store = store;
                m_MappingIndex = mappingIndex;
                m_RootId = rootId;
            }

            private readonly FlexLayout m_RootLayout;
            private readonly FlexNodeStore m_Store;
            private readonly BridgeNodeMappingIndex m_MappingIndex;
            private readonly FlexNodeId m_RootId;
            public FlexMeasuredSize RootMeasured { get; set; }

            public FlexLayout RootLayout => m_RootLayout;
            public FlexNodeStore Store => m_Store;
            public FlexNodeId RootId => m_RootId;

            internal void Apply()
            {
                ApplyRootSize(m_RootLayout.rectTransform, RootMeasured);
                ApplySubtreeLayout(m_Store, m_RootId, RootMeasured, m_MappingIndex);
            }
        }

        internal readonly struct BridgeNodeMapping
        {
            public BridgeNodeMapping(
                FlexNodeId nodeId,
                RectTransform rectTransform,
                Vector2 pivot,
                bool isContainerNode,
                bool isAbsolutePositioned,
                bool drivesSizeInParentFlow)
            {
                NodeId = nodeId;
                RectTransform = rectTransform;
                Pivot = pivot;
                IsContainerNode = isContainerNode;
                IsAbsolutePositioned = isAbsolutePositioned;
                DrivesSizeInParentFlow = drivesSizeInParentFlow;
            }

            public FlexNodeId NodeId { get; }

            public RectTransform RectTransform { get; }

            public Vector2 Pivot { get; }

            public bool IsContainerNode { get; }

            public bool IsAbsolutePositioned { get; }

            public bool DrivesSizeInParentFlow { get; }
        }

        internal readonly struct BridgeNodeMappingIndex
        {
            internal BridgeNodeMappingIndex(BridgeNodeMapping[] mappingsById)
            {
                MappingsById = mappingsById;
            }

            internal BridgeNodeMapping[] MappingsById { get; }
        }

        public static void Rebuild(FlexLayout rootLayout)
        {
            using var rebuildScope = FlexProfiler.Rebuild.Auto();
            if (rootLayout == null)
            {
                return;
            }

            FlexRuntimeSampling.BeginRebuild();
            try
            {
                rootLayout.EnsureDrivenPropertiesUpToDateRecursively();
                var plan = CollectPlan(rootLayout);
                try
                {
                    ComputePlan(plan);
                    ApplyPlan(plan);
                }
                finally
                {
                    DisposePlan(plan);
                }
            }
            finally
            {
                FlexRuntimeSampling.EndRebuild();
            }
        }

        internal static FlexRebuildPlan CollectPlan(FlexLayout rootLayout)
        {
#if UNITY_EDITOR && FLEX_LAYOUT_DEBUG_LOGS
            if (!Application.isPlaying)
            {
                Debug.Log($"[FlexLayout] Rebuild '{rootLayout.name}' dir={rootLayout.style.flexDirection} justify={rootLayout.style.justifyContent} align={rootLayout.style.alignItems}");
            }
#endif

            var store = new FlexNodeStore();
            var mappings = new List<BridgeNodeMapping>();
            var rootId = CollectTree(rootLayout, store, mappings);
            var mappingIndex = BuildMappingIndex(mappings);
            return CreatePlan(rootLayout, store, mappingIndex, rootId);
        }

        internal static void ComputePlan(FlexRebuildPlan plan)
        {
            if (plan == null)
            {
                return;
            }

            using var measurePass = FlexMeasure.BeginMeasurePass();
            plan.RootMeasured = MeasureRoot(plan.Store, plan.RootId);
        }

        internal static void ApplyPlan(FlexRebuildPlan plan)
        {
            if (plan == null)
            {
                return;
            }

            plan.Apply();
        }

        internal static void DisposePlan(FlexRebuildPlan plan)
        {
        }

        private static FlexRebuildPlan CreatePlan(
            FlexLayout rootLayout,
            FlexNodeStore store,
            BridgeNodeMappingIndex mappingIndex,
            FlexNodeId rootId)
        {
            return new FlexRebuildPlan(rootLayout, store, mappingIndex, rootId);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) <= ApplyEpsilon
                && Mathf.Abs(a.y - b.y) <= ApplyEpsilon;
        }
    }
}
