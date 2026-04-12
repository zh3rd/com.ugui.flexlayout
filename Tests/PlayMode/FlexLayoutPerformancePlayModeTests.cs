using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI.Flex.Core;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    [Category("Performance")]
    public class FlexLayoutPerformancePlayModeTests : PlayModeSceneIsolationFixture
    {
        private const float Epsilon = 0.01f;

        private GameObject m_RootGo;
        private RectTransform m_RootRect;
        private FlexLayout m_RootLayout;
        private FlexNode m_RootNode;

        [SetUp]
        public void SetUp()
        {
            m_RootGo = new GameObject("PerfRoot", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNode));
            m_RootRect = m_RootGo.GetComponent<RectTransform>();
            m_RootRect.anchorMin = Vector2.up;
            m_RootRect.anchorMax = Vector2.up;
            m_RootRect.pivot = new Vector2(0.5f, 0.5f);
            m_RootRect.sizeDelta = new Vector2(1920f, 1080f);

            m_RootLayout = m_RootGo.GetComponent<FlexLayout>();
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.Stretch;
            m_RootLayout.style.alignContent = AlignContent.Stretch;
            m_RootLayout.style.mainGap = 2f;
            m_RootLayout.style.crossGap = 2f;
            m_RootLayout.style.padding = FlexEdges.Zero;
            m_RootLayout.implicitItemDefaults = FlexImplicitItemStyleData.Default;

            m_RootNode = m_RootGo.GetComponent<FlexNode>();
            m_RootNode.style.width = FlexValue.Points(1920f);
            m_RootNode.style.height = FlexValue.Points(1080f);
        }

        [TearDown]
        public void TearDown()
        {
            m_RootGo = null;
            m_RootRect = null;
            m_RootLayout = null;
            m_RootNode = null;
        }

        [UnityTest, Explicit("Manual performance baseline run.")]
        public IEnumerator Perf_Implicit200_SteadyRebuild_Baseline()
        {
            CreateImplicitChildren(200, 80f, 32f);
            yield return null;

            var metrics = MeasureRebuild(iterations: 80, mutateBeforeRebuild: null);
            UnityEngine.Debug.Log(
                $"[FlexPerf] Implicit200 steady avgMs={metrics.AverageMs:F4} allocBytesPerRebuild={metrics.ManagedAllocBytesPerRebuild:F1}");
            LogMeasureCacheStats("Implicit200 steady");

            Assert.That(metrics.AverageMs, Is.LessThan(50f));
            Assert.That(metrics.ManagedAllocBytesPerRebuild, Is.LessThan(8_000_000f));
        }

        [UnityTest, Explicit("Manual performance baseline run.")]
        public IEnumerator Perf_Implicit200_FlexGrowToggle_Baseline()
        {
            var childItems = CreateExplicitItemChildren(200, 80f, 32f);
            yield return null;

            var toggleIndex = 0;
            var metrics = MeasureRebuild(
                iterations: 80,
                mutateBeforeRebuild: () =>
                {
                    var item = childItems[toggleIndex % childItems.Length];
                    item.style.flexGrow = item.style.flexGrow < Epsilon ? 1f : 0f;
                    toggleIndex++;
                });

            UnityEngine.Debug.Log(
                $"[FlexPerf] Implicit200 grow-toggle avgMs={metrics.AverageMs:F4} allocBytesPerRebuild={metrics.ManagedAllocBytesPerRebuild:F1}");
            LogMeasureCacheStats("Implicit200 grow-toggle");

            Assert.That(metrics.AverageMs, Is.LessThan(60f));
            Assert.That(metrics.ManagedAllocBytesPerRebuild, Is.LessThan(10_000_000f));
        }

        [UnityTest, Explicit("Manual performance baseline run.")]
        public IEnumerator Perf_Wrap1000_ComplexTree_Baseline()
        {
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.alignContent = AlignContent.Stretch;
            m_RootLayout.style.alignItems = AlignItems.Stretch;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.mainGap = 4f;
            m_RootLayout.style.crossGap = 4f;
            CreateNestedTree(totalLeafCount: 1000, containerCount: 20);
            yield return null;

            var metrics = MeasureRebuild(iterations: 40, mutateBeforeRebuild: null);
            UnityEngine.Debug.Log(
                $"[FlexPerf] Wrap1000 complex avgMs={metrics.AverageMs:F4} allocBytesPerRebuild={metrics.ManagedAllocBytesPerRebuild:F1}");
            LogMeasureCacheStats("Wrap1000 complex");

            Assert.That(metrics.AverageMs, Is.LessThan(120f));
            Assert.That(metrics.ManagedAllocBytesPerRebuild, Is.LessThan(20_000_000f));
        }

        [UnityTest, Explicit("Manual performance baseline run.")]
        public IEnumerator Perf_Text200_WrapPreferred_Baseline()
        {
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.alignContent = AlignContent.Stretch;
            m_RootLayout.style.alignItems = AlignItems.FlexStart;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.mainGap = 6f;
            m_RootLayout.style.crossGap = 6f;
            CreateTextChildren(200);
            yield return null;

            var toggle = false;
            var metrics = MeasureRebuild(
                iterations: 40,
                mutateBeforeRebuild: () =>
                {
                    toggle = !toggle;
                    m_RootLayout.style.mainGap = toggle ? 6f : 8f;
                });

            UnityEngine.Debug.Log(
                $"[FlexPerf] Text200 wrap-preferred avgMs={metrics.AverageMs:F4} allocBytesPerRebuild={metrics.ManagedAllocBytesPerRebuild:F1}");
            LogMeasureCacheStats("Text200 wrap-preferred");

            Assert.That(metrics.AverageMs, Is.LessThan(120f));
            Assert.That(metrics.ManagedAllocBytesPerRebuild, Is.LessThan(20_000_000f));
        }

        [UnityTest, Explicit("Manual performance baseline run.")]
        public IEnumerator Perf_DeepNested80_ChainRebuild_Baseline()
        {
            m_RootLayout.style.flexDirection = FlexDirection.Column;
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.style.alignItems = AlignItems.Stretch;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            CreateDeepNestedChain(depth: 80, siblingLeafCount: 2);
            yield return null;

            var toggle = false;
            var metrics = MeasureRebuild(
                iterations: 60,
                mutateBeforeRebuild: () =>
                {
                    toggle = !toggle;
                    m_RootLayout.style.crossGap = toggle ? 2f : 4f;
                });

            UnityEngine.Debug.Log(
                $"[FlexPerf] DeepNested80 chain avgMs={metrics.AverageMs:F4} allocBytesPerRebuild={metrics.ManagedAllocBytesPerRebuild:F1}");
            LogMeasureCacheStats("DeepNested80 chain");

            Assert.That(metrics.AverageMs, Is.LessThan(120f));
            Assert.That(metrics.ManagedAllocBytesPerRebuild, Is.LessThan(20_000_000f));
        }

        [UnityTest, Explicit("Manual performance baseline run.")]
        public IEnumerator Perf_WrapImplicitExplicit600_MixedFlow_Baseline()
        {
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.alignContent = AlignContent.Stretch;
            m_RootLayout.style.alignItems = AlignItems.Center;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.mainGap = 4f;
            m_RootLayout.style.crossGap = 4f;
            var explicitItems = CreateMixedWrapChildren(count: 600);
            yield return null;

            var toggleIndex = 0;
            var metrics = MeasureRebuild(
                iterations: 40,
                mutateBeforeRebuild: () =>
                {
                    var item = explicitItems[toggleIndex % explicitItems.Length];
                    item.style.alignSelf = item.style.alignSelf == AlignSelf.Auto
                        ? AlignSelf.FlexStart
                        : AlignSelf.Auto;
                    toggleIndex++;
                });

            UnityEngine.Debug.Log(
                $"[FlexPerf] WrapImplicitExplicit600 mixed avgMs={metrics.AverageMs:F4} allocBytesPerRebuild={metrics.ManagedAllocBytesPerRebuild:F1}");
            LogMeasureCacheStats("WrapImplicitExplicit600 mixed");

            Assert.That(metrics.AverageMs, Is.LessThan(120f));
            Assert.That(metrics.ManagedAllocBytesPerRebuild, Is.LessThan(20_000_000f));
        }

        private PerfMetrics MeasureRebuild(int iterations, Action mutateBeforeRebuild)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var startAlloc = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                mutateBeforeRebuild?.Invoke();
                m_RootLayout.MarkLayoutDirty();
            }

            stopwatch.Stop();
            var endAlloc = GC.GetAllocatedBytesForCurrentThread();

            var totalAlloc = Math.Max(0L, endAlloc - startAlloc);
            return new PerfMetrics(
                averageMs: (float)(stopwatch.Elapsed.TotalMilliseconds / iterations),
                managedAllocBytesPerRebuild: (float)totalAlloc / iterations);
        }

        private void CreateImplicitChildren(int count, float width, float height)
        {
            for (var i = 0; i < count; i++)
            {
                var childGo = new GameObject($"Implicit_{i}", typeof(RectTransform));
                var childRect = childGo.GetComponent<RectTransform>();
                childRect.SetParent(m_RootRect, false);
                childRect.anchorMin = Vector2.up;
                childRect.anchorMax = Vector2.up;
                childRect.pivot = new Vector2(0.5f, 0.5f);
                childRect.sizeDelta = new Vector2(width, height);
            }
        }

        private FlexItem[] CreateExplicitItemChildren(int count, float width, float height)
        {
            var items = new FlexItem[count];
            for (var i = 0; i < count; i++)
            {
                var childGo = new GameObject($"Item_{i}", typeof(RectTransform), typeof(FlexItem));
                var childRect = childGo.GetComponent<RectTransform>();
                childRect.SetParent(m_RootRect, false);
                childRect.anchorMin = Vector2.up;
                childRect.anchorMax = Vector2.up;
                childRect.pivot = new Vector2(0.5f, 0.5f);
                childRect.sizeDelta = new Vector2(width, height);

                var item = childGo.GetComponent<FlexItem>();
                item.style.flexBasis = FlexValue.Auto();
                item.style.flexGrow = 0f;
                item.style.flexShrink = 1f;
                item.style.alignSelf = AlignSelf.Auto;
                items[i] = item;
            }

            return items;
        }

        private void CreateNestedTree(int totalLeafCount, int containerCount)
        {
            var leavesPerContainer = Mathf.Max(1, totalLeafCount / Mathf.Max(1, containerCount));
            for (var i = 0; i < containerCount; i++)
            {
                var containerGo = new GameObject(
                    $"Container_{i}",
                    typeof(RectTransform),
                    typeof(FlexLayout),
                    typeof(FlexNode));
                var containerRect = containerGo.GetComponent<RectTransform>();
                containerRect.SetParent(m_RootRect, false);
                containerRect.anchorMin = Vector2.up;
                containerRect.anchorMax = Vector2.up;
                containerRect.pivot = new Vector2(0.5f, 0.5f);
                containerRect.sizeDelta = new Vector2(320f, 240f);

                var containerLayout = containerGo.GetComponent<FlexLayout>();
                containerLayout.style.flexDirection = FlexDirection.Row;
                containerLayout.style.flexWrap = FlexWrap.Wrap;
                containerLayout.style.justifyContent = JustifyContent.FlexStart;
                containerLayout.style.alignItems = AlignItems.Stretch;
                containerLayout.style.alignContent = AlignContent.Stretch;
                containerLayout.style.mainGap = 2f;
                containerLayout.style.crossGap = 2f;
                containerLayout.style.padding = FlexEdges.Zero;

                var containerNode = containerGo.GetComponent<FlexNode>();
                containerNode.style.width = FlexValue.Auto();
                containerNode.style.height = FlexValue.Auto();

                for (var j = 0; j < leavesPerContainer; j++)
                {
                    var leafGo = new GameObject($"Leaf_{i}_{j}", typeof(RectTransform), typeof(FlexItem));
                    var leafRect = leafGo.GetComponent<RectTransform>();
                    leafRect.SetParent(containerRect, false);
                    leafRect.anchorMin = Vector2.up;
                    leafRect.anchorMax = Vector2.up;
                    leafRect.pivot = new Vector2(0.5f, 0.5f);
                    leafRect.sizeDelta = new Vector2(40f + (j % 5), 24f + (j % 3));

                    var leafItem = leafGo.GetComponent<FlexItem>();
                    leafItem.style.flexBasis = FlexValue.Auto();
                    leafItem.style.flexGrow = j % 3 == 0 ? 1f : 0f;
                    leafItem.style.flexShrink = 1f;
                    leafItem.style.alignSelf = AlignSelf.Auto;
                }
            }
        }

        private void CreateTextChildren(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var childGo = new GameObject($"Text_{i}", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
                var childRect = childGo.GetComponent<RectTransform>();
                childRect.SetParent(m_RootRect, false);
                childRect.anchorMin = Vector2.up;
                childRect.anchorMax = Vector2.up;
                childRect.pivot = new Vector2(0.5f, 0.5f);
                childRect.sizeDelta = new Vector2(140f, 48f);

                var text = childGo.GetComponent<TextMeshProUGUI>();
                EnsureTmpCanMeasure(text);
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Overflow;
                text.text = $"Flex text perf sample {i} with wrap and preferred measurement.";

                var textNode = childGo.GetComponent<FlexText>();
                textNode.style.width = FlexValue.Auto();
                textNode.style.height = FlexValue.Auto();
            }
        }

        private void CreateDeepNestedChain(int depth, int siblingLeafCount)
        {
            var parentRect = m_RootRect;

            for (var depthIndex = 0; depthIndex < depth; depthIndex++)
            {
                var containerGo = new GameObject(
                    $"DeepContainer_{depthIndex}",
                    typeof(RectTransform),
                    typeof(FlexLayout),
                    typeof(FlexNode));
                var containerRect = containerGo.GetComponent<RectTransform>();
                containerRect.SetParent(parentRect, false);
                containerRect.anchorMin = Vector2.up;
                containerRect.anchorMax = Vector2.up;
                containerRect.pivot = new Vector2(0.5f, 0.5f);
                containerRect.sizeDelta = new Vector2(160f + (depthIndex % 4) * 8f, 96f + (depthIndex % 3) * 6f);

                var containerLayout = containerGo.GetComponent<FlexLayout>();
                containerLayout.style.flexDirection = depthIndex % 2 == 0 ? FlexDirection.Column : FlexDirection.Row;
                containerLayout.style.flexWrap = FlexWrap.NoWrap;
                containerLayout.style.justifyContent = JustifyContent.FlexStart;
                containerLayout.style.alignItems = AlignItems.Stretch;
                containerLayout.style.alignContent = AlignContent.Stretch;
                containerLayout.style.mainGap = 2f;
                containerLayout.style.crossGap = 1f;
                containerLayout.style.padding = FlexEdges.Zero;

                var containerNode = containerGo.GetComponent<FlexNode>();
                containerNode.style.width = FlexValue.Auto();
                containerNode.style.height = FlexValue.Auto();

                for (var siblingIndex = 0; siblingIndex < siblingLeafCount; siblingIndex++)
                {
                    if ((depthIndex + siblingIndex) % 2 == 0)
                    {
                        var implicitLeaf = new GameObject($"DeepImplicit_{depthIndex}_{siblingIndex}", typeof(RectTransform));
                        var implicitRect = implicitLeaf.GetComponent<RectTransform>();
                        implicitRect.SetParent(containerRect, false);
                        implicitRect.anchorMin = Vector2.up;
                        implicitRect.anchorMax = Vector2.up;
                        implicitRect.pivot = new Vector2(0.5f, 0.5f);
                        implicitRect.sizeDelta = new Vector2(28f + siblingIndex * 4f, 18f + (depthIndex % 2) * 4f);
                    }
                    else
                    {
                        var explicitLeaf = new GameObject($"DeepItem_{depthIndex}_{siblingIndex}", typeof(RectTransform), typeof(FlexItem));
                        var explicitRect = explicitLeaf.GetComponent<RectTransform>();
                        explicitRect.SetParent(containerRect, false);
                        explicitRect.anchorMin = Vector2.up;
                        explicitRect.anchorMax = Vector2.up;
                        explicitRect.pivot = new Vector2(0.5f, 0.5f);
                        explicitRect.sizeDelta = new Vector2(30f + siblingIndex * 3f, 20f + (depthIndex % 3) * 3f);

                        var explicitItem = explicitLeaf.GetComponent<FlexItem>();
                        explicitItem.style.flexBasis = FlexValue.Auto();
                        explicitItem.style.flexGrow = siblingIndex == siblingLeafCount - 1 ? 1f : 0f;
                        explicitItem.style.flexShrink = 1f;
                        explicitItem.style.alignSelf = AlignSelf.Auto;
                    }
                }

                parentRect = containerRect;
            }
        }

        private FlexItem[] CreateMixedWrapChildren(int count)
        {
            var explicitItems = new FlexItem[count / 2];
            var explicitIndex = 0;

            for (var i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                {
                    var implicitGo = new GameObject($"MixedImplicit_{i}", typeof(RectTransform));
                    var implicitRect = implicitGo.GetComponent<RectTransform>();
                    implicitRect.SetParent(m_RootRect, false);
                    implicitRect.anchorMin = Vector2.up;
                    implicitRect.anchorMax = Vector2.up;
                    implicitRect.pivot = new Vector2(0.5f, 0.5f);
                    implicitRect.sizeDelta = new Vector2(48f + (i % 5) * 6f, 24f + (i % 3) * 4f);
                    continue;
                }

                var explicitGo = new GameObject($"MixedItem_{i}", typeof(RectTransform), typeof(FlexItem));
                var explicitRect = explicitGo.GetComponent<RectTransform>();
                explicitRect.SetParent(m_RootRect, false);
                explicitRect.anchorMin = Vector2.up;
                explicitRect.anchorMax = Vector2.up;
                explicitRect.pivot = new Vector2(0.5f, 0.5f);
                explicitRect.sizeDelta = new Vector2(52f + (i % 4) * 5f, 26f + (i % 2) * 6f);

                var explicitItem = explicitGo.GetComponent<FlexItem>();
                explicitItem.style.flexBasis = FlexValue.Auto();
                explicitItem.style.flexGrow = i % 6 == 1 ? 1f : 0f;
                explicitItem.style.flexShrink = 1f;
                explicitItem.style.alignSelf = AlignSelf.Auto;
                explicitItems[explicitIndex++] = explicitItem;
            }

            return explicitItems;
        }

        private static void EnsureTmpCanMeasure(TextMeshProUGUI text)
        {
            if (text.font == null && TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            if (text.font == null)
            {
                Assert.Ignore("TMP default font asset is not available in current test environment.");
            }
        }

        private static void LogMeasureCacheStats(string scenario)
        {
            var stats = FlexMeasure.GetLastCompletedMeasurePassStatisticsForTesting();
            UnityEngine.Debug.Log(
                $"[FlexPerfCache] {scenario} " +
                $"measure={stats.MeasureSubtreeHits}/{stats.MeasureSubtreeRequests} " +
                $"basis={stats.MainAxisBasisHits}/{stats.MainAxisBasisRequests} " +
                $"preparedFlow={stats.PreparedFlowHits}/{stats.PreparedFlowRequests} " +
                $"preparedWrap={stats.PreparedWrapLineHits}/{stats.PreparedWrapLineRequests} " +
                $"lines={stats.LineBuildHits}/{stats.LineBuildRequests}");

            var timing = FlexRuntimeSampling.GetLastCompletedTimingForTesting();
            UnityEngine.Debug.Log(
                $"[FlexPerfTiming] {scenario} " +
                $"rebuild={timing.RebuildMs:F3}ms " +
                $"collect={timing.CollectTreeMs:F3}ms " +
                $"map={timing.BuildMappingMs:F3}ms " +
                $"measureRoot={timing.MeasureRootMs:F3}ms " +
                $"measureSubtree={timing.MeasureSubtreeMs:F3}ms " +
                $"measureContent={timing.MeasureContentMs:F3}ms " +
                $"measureText={timing.MeasureTextMs:F3}ms " +
                $"preparedFlow={timing.PreparedFlowMs:F3}ms " +
                $"allocMain={timing.AllocateMainAxisMs:F3}ms " +
                $"allocCross={timing.AllocateCrossAxisMs:F3}ms " +
                $"itemLayouts={timing.ItemLayoutsMs:F3}ms " +
                $"wrapPrepare={timing.WrapPrepareLinesMs:F3}ms " +
                $"wrapBuild={timing.WrapBuildLinesMs:F3}ms " +
                $"wrapCross={timing.WrapResolveLineCrossMs:F3}ms " +
                $"arrangeSingle={timing.ArrangeSingleLineMs:F3}ms " +
                $"arrangeWrap={timing.ArrangeWrapMs:F3}ms " +
                $"applySubtree={timing.ApplySubtreeMs:F3}ms " +
                $"applyLayout={timing.ApplyLayoutSubtreeMs:F3}ms");
        }

        private readonly struct PerfMetrics
        {
            public PerfMetrics(float averageMs, float managedAllocBytesPerRebuild)
            {
                AverageMs = averageMs;
                ManagedAllocBytesPerRebuild = managedAllocBytesPerRebuild;
            }

            public float AverageMs { get; }

            public float ManagedAllocBytesPerRebuild { get; }
        }
    }
}
