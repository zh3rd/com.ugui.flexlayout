using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public class FlexLayoutComplexPlayModeTests : PlayModeSceneIsolationFixture
    {
        private GameObject m_CanvasGo;
        private GameObject m_RootGo;
        private RectTransform m_RootRect;
        private FlexLayout m_RootLayout;
        private FlexNode m_RootNode;

        [SetUp]
        public void SetUp()
        {
            m_CanvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = m_CanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            m_RootGo = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNode));
            m_RootRect = m_RootGo.GetComponent<RectTransform>();
            m_RootRect.SetParent(m_CanvasGo.transform, false);
            m_RootRect.anchorMin = Vector2.up;
            m_RootRect.anchorMax = Vector2.up;
            m_RootRect.pivot = Vector2.up;
            m_RootRect.sizeDelta = new Vector2(500f, 260f);

            m_RootLayout = m_RootGo.GetComponent<FlexLayout>();
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.Stretch;
            m_RootLayout.style.alignContent = AlignContent.Stretch;
            m_RootLayout.style.mainGap = 4f;
            m_RootLayout.style.crossGap = 6f;
            m_RootLayout.style.padding = new FlexEdges
            {
                left = 10f,
                right = 10f,
                top = 10f,
                bottom = 10f,
            };
            m_RootLayout.implicitItemDefaults = new FlexImplicitItemStyleData
            {
                width = FlexValue.Auto(),
                height = FlexValue.Auto(),
                flexGrow = 0f,
                flexShrink = 0f,
                flexBasis = FlexValue.Auto(),
                alignSelf = AlignSelf.Auto,
            };

            m_RootNode = m_RootGo.GetComponent<FlexNode>();
            m_RootNode.style.width = FlexValue.Points(500f);
            m_RootNode.style.height = FlexValue.Points(260f);
        }

        [TearDown]
        public void TearDown()
        {
            m_CanvasGo = null;
            m_RootGo = null;
            m_RootRect = null;
            m_RootLayout = null;
            m_RootNode = null;
        }

        [UnityTest]
        public IEnumerator WrapStretch_NestedLikeLoad_DoesNotOverlap_AndStaysWithinParent()
        {
            var children = new List<RectTransform>();
            for (var i = 0; i < 8; i++)
            {
                children.Add(CreateImplicitChild($"Item_{i}", 180f, 24f));
            }

            yield return null;

            var innerWidth = m_RootRect.rect.width - m_RootLayout.style.padding.left - m_RootLayout.style.padding.right;
            var distinctY = new HashSet<int>();
            for (var i = 0; i < children.Count; i++)
            {
                var rect = children[i];
                distinctY.Add(Mathf.RoundToInt(rect.anchoredPosition.y));
                Assert.That(rect.anchoredPosition.x, Is.GreaterThanOrEqualTo(-0.01f));
                Assert.That(rect.anchoredPosition.x + rect.sizeDelta.x, Is.LessThanOrEqualTo(innerWidth + 0.02f));
            }

            Assert.That(distinctY.Count, Is.GreaterThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator FlexText_UnbreakableWord_Wrap_IsClampedByParentInnerWidth()
        {
            var textRect = CreateTextChild(
                "Text",
                "ThisIsAnUnbreakableVeryLongTokenThisIsAnUnbreakableVeryLongTokenThisIsAnUnbreakableVeryLongToken",
                wrap: true);
            yield return null;

            var innerWidth = m_RootRect.rect.width - m_RootLayout.style.padding.left - m_RootLayout.style.padding.right;
            Assert.That(textRect.sizeDelta.x, Is.LessThanOrEqualTo(innerWidth + 0.02f));
        }

        [UnityTest]
        public IEnumerator ParentResize_Reflow_Stabilizes_WithoutMultiFrameDrift()
        {
            var textRect = CreateTextChild("Text", "Long long long text for reflow stability check.", wrap: true);
            var box = CreateImplicitChild("Box", 120f, 40f);
            yield return null;

            m_RootRect.sizeDelta = new Vector2(320f, 260f);
            m_RootNode.style.width = FlexValue.Points(320f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            var widthFrame1 = textRect.sizeDelta.x;
            var boxXFrame1 = box.anchoredPosition.x;
            yield return null;
            var widthFrame2 = textRect.sizeDelta.x;
            var boxXFrame2 = box.anchoredPosition.x;
            yield return null;
            var widthFrame3 = textRect.sizeDelta.x;
            var boxXFrame3 = box.anchoredPosition.x;

            Assert.That(Mathf.Abs(widthFrame2 - widthFrame1), Is.LessThan(0.02f));
            Assert.That(Mathf.Abs(widthFrame3 - widthFrame2), Is.LessThan(0.02f));
            Assert.That(Mathf.Abs(boxXFrame2 - boxXFrame1), Is.LessThan(0.02f));
            Assert.That(Mathf.Abs(boxXFrame3 - boxXFrame2), Is.LessThan(0.02f));
        }

        [UnityTest]
        public IEnumerator AbsoluteNode_IsRemovedFromFlow_RelativeSiblingsRemainPacked()
        {
            var a = CreateExplicitNodeChild("A", 80f, 30f, PositionType.Relative);
            var absolute = CreateExplicitNodeChild("Absolute", 200f, 40f, PositionType.Absolute);
            absolute.anchoredPosition = new Vector2(160f, -40f);
            var c = CreateExplicitNodeChild("C", 90f, 30f, PositionType.Relative);
            yield return null;

            Assert.That(a.anchoredPosition.x, Is.EqualTo(10f).Within(0.05f)); // left padding
            Assert.That(c.anchoredPosition.x, Is.EqualTo(94f).Within(0.1f)); // left padding + width 80 + mainGap 4
            Assert.That(absolute.anchoredPosition.x, Is.EqualTo(160f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator BatchAddRemove_Stress_ReflowPositionsRemainMonotonic()
        {
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootRect.sizeDelta = new Vector2(2000f, 260f);
            m_RootNode.style.width = FlexValue.Points(2000f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            var items = new List<RectTransform>();
            for (var i = 0; i < 30; i++)
            {
                items.Add(CreateImplicitChild($"B{i}", 40f, 20f));
            }

            yield return null;

            for (var i = 0; i < items.Count; i += 2)
            {
                Object.Destroy(items[i].gameObject);
            }

            yield return null;

            var remaining = new List<RectTransform>();
            foreach (Transform child in m_RootRect)
            {
                var rect = child as RectTransform;
                if (rect != null)
                {
                    remaining.Add(rect);
                }
            }

            var prevX = float.NegativeInfinity;
            for (var i = 0; i < remaining.Count; i++)
            {
                Assert.That(remaining[i].anchoredPosition.x, Is.GreaterThanOrEqualTo(prevX - 0.01f));
                prevX = remaining[i].anchoredPosition.x;
            }
        }

        private RectTransform CreateTextChild(string name, string value, bool wrap)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(m_RootRect, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(120f, 30f);

            var text = go.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(text);
            text.enableWordWrapping = wrap;
            text.text = value;
            return rect;
        }

        private RectTransform CreateImplicitChild(string name, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(m_RootRect, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private RectTransform CreateExplicitNodeChild(string name, float width, float height, PositionType positionType)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(FlexNode));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(m_RootRect, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(width, height);

            var node = go.GetComponent<FlexNode>();
            node.style.width = FlexValue.Points(width);
            node.style.height = FlexValue.Points(height);
            node.style.positionType = positionType;
            return rect;
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
    }
}
