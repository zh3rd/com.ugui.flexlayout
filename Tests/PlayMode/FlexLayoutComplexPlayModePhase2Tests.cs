using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public class FlexLayoutComplexPlayModePhase2Tests : PlayModeSceneIsolationFixture
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
            m_CanvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            m_RootGo = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNode));
            m_RootRect = m_RootGo.GetComponent<RectTransform>();
            m_RootRect.SetParent(m_CanvasGo.transform, false);
            m_RootRect.anchorMin = Vector2.up;
            m_RootRect.anchorMax = Vector2.up;
            m_RootRect.pivot = Vector2.up;
            m_RootRect.sizeDelta = new Vector2(780f, 480f);

            m_RootLayout = m_RootGo.GetComponent<FlexLayout>();
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.Stretch;
            m_RootLayout.style.alignContent = AlignContent.Stretch;
            m_RootLayout.style.mainGap = 8f;
            m_RootLayout.style.crossGap = 8f;
            m_RootLayout.style.padding = new FlexEdges
            {
                left = 12f,
                right = 12f,
                top = 12f,
                bottom = 12f,
            };

            m_RootNode = m_RootGo.GetComponent<FlexNode>();
            m_RootNode.style.width = FlexValue.Points(780f);
            m_RootNode.style.height = FlexValue.Points(480f);
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
        public IEnumerator NestedContainers_TextMix_AllChildrenStayWithinImmediateParent()
        {
            var colA = CreateContainer("ColA", 320f, 180f, FlexDirection.Column, FlexWrap.NoWrap);
            var colB = CreateContainer("ColB", 320f, 180f, FlexDirection.Column, FlexWrap.NoWrap);
            yield return null;

            var aText = CreateTextChild(colA, "A_Text", "A long text block should wrap inside A container.", true);
            var aBox = CreateImplicitChild(colA, "A_Box", 120f, 32f);
            var bText = CreateTextChild(colB, "B_Text", "Another long text block should wrap inside B container.", true);
            var bBox = CreateImplicitChild(colB, "B_Box", 140f, 32f);
            yield return null;

            AssertRectWithinParent(aText, colA);
            AssertRectWithinParent(aBox, colA);
            AssertRectWithinParent(bText, colB);
            AssertRectWithinParent(bBox, colB);
        }

        [UnityTest]
        public IEnumerator NestedParentDirectionSwitch_ReflowsContainerCoordinates()
        {
            var first = CreateContainer("First", 280f, 140f, FlexDirection.Column, FlexWrap.NoWrap);
            var second = CreateContainer("Second", 280f, 140f, FlexDirection.Column, FlexWrap.NoWrap);
            yield return null;

            var beforeFirst = first.anchoredPosition;
            var beforeSecond = second.anchoredPosition;

            m_RootLayout.style.flexDirection = FlexDirection.Column;
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            var afterFirst = first.anchoredPosition;
            var afterSecond = second.anchoredPosition;

            Assert.That(afterFirst, Is.Not.EqualTo(beforeFirst));
            Assert.That(afterSecond, Is.Not.EqualTo(beforeSecond));
            Assert.That(afterSecond.y, Is.LessThan(afterFirst.y - 0.01f));
        }

        [UnityTest]
        public IEnumerator ToggleChildLayout_EnableDisable_ReflowsImmediateChildren()
        {
            var container = CreateContainer("ChildContainer", 360f, 160f, FlexDirection.Row, FlexWrap.NoWrap);
            var c0 = CreateImplicitChild(container, "C0", 80f, 30f);
            var c1 = CreateImplicitChild(container, "C1", 80f, 30f);
            yield return null;

            var layout = container.GetComponent<FlexLayout>();
            Assert.That(c1.anchoredPosition.x, Is.GreaterThan(c0.anchoredPosition.x + 0.1f));

            layout.enabled = false;
            yield return null;
            c1.anchoredPosition = new Vector2(222f, -11f);
            yield return null;

            layout.enabled = true;
            layout.MarkLayoutDirty();
            yield return null;

            Assert.That(c1.anchoredPosition.x, Is.GreaterThan(c0.anchoredPosition.x + 0.1f));
            Assert.That(c1.anchoredPosition.x, Is.Not.EqualTo(222f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ToggleNodeAndText_EnableDisable_ReflowRecoversAfterReenable()
        {
            m_RootRect.sizeDelta = new Vector2(1600f, 480f);
            m_RootNode.style.width = FlexValue.Points(1600f);
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            var textRect = CreateTextChild(m_RootRect, "Text", "short", false);
            var sibling = CreateImplicitChild(m_RootRect, "Sibling", 60f, 30f);
            yield return null;

            var textNode = textRect.GetComponent<FlexText>();
            var text = textRect.GetComponent<TextMeshProUGUI>();
            var before = sibling.anchoredPosition.x;

            textNode.enabled = false;
            text.text = "This text became much much longer and should not push sibling while FlexText is disabled.";
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var disabledX = sibling.anchoredPosition.x;

            textNode.enabled = true;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var reenabledX = sibling.anchoredPosition.x;

            Assert.That(disabledX, Is.EqualTo(before).Within(0.5f));
            Assert.That(reenabledX, Is.GreaterThan(disabledX + 0.5f));
        }

        private RectTransform CreateContainer(string name, float width, float height, FlexDirection direction, FlexWrap wrap)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(FlexLayout), typeof(FlexNode));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(m_RootRect, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(width, height);

            var node = go.GetComponent<FlexNode>();
            node.style.width = FlexValue.Points(width);
            node.style.height = FlexValue.Points(height);

            var layout = go.GetComponent<FlexLayout>();
            layout.style.flexDirection = direction;
            layout.style.flexWrap = wrap;
            layout.style.justifyContent = JustifyContent.FlexStart;
            layout.style.alignItems = AlignItems.Stretch;
            layout.style.alignContent = AlignContent.Stretch;
            layout.style.mainGap = 4f;
            layout.style.crossGap = 4f;
            layout.style.padding = new FlexEdges
            {
                left = 6f,
                right = 6f,
                top = 6f,
                bottom = 6f,
            };
            return rect;
        }

        private RectTransform CreateImplicitChild(RectTransform parent, string name, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private RectTransform CreateTextChild(RectTransform parent, string name, string value, bool wrap)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
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

        private static void AssertRectWithinParent(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            var rect = parent.rect;
            for (var i = 0; i < corners.Length; i++)
            {
                var local = parent.InverseTransformPoint(corners[i]);
                Assert.That(local.x, Is.GreaterThanOrEqualTo(rect.xMin - 0.1f));
                Assert.That(local.x, Is.LessThanOrEqualTo(rect.xMax + 0.1f));
                Assert.That(local.y, Is.GreaterThanOrEqualTo(rect.yMin - 0.1f));
                Assert.That(local.y, Is.LessThanOrEqualTo(rect.yMax + 0.1f));
            }
        }
    }
}
