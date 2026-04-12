using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FlexItem = UnityEngine.UI.Flex.FlexItem;
using FlexNode = UnityEngine.UI.Flex.FlexNode;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public class FlexLayoutIdempotencyPlayModeTests : PlayModeSceneIsolationFixture
    {
        private GameObject m_RootGo;
        private RectTransform m_RootRect;
        private FlexLayout m_RootLayout;
        private FlexNode m_RootNode;

        [SetUp]
        public void SetUp()
        {
            m_RootGo = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNode));
            m_RootRect = m_RootGo.GetComponent<RectTransform>();
            m_RootRect.anchorMin = Vector2.up;
            m_RootRect.anchorMax = Vector2.up;
            m_RootRect.pivot = Vector2.up;
            m_RootRect.sizeDelta = new Vector2(140f, 220f);

            m_RootLayout = m_RootGo.GetComponent<FlexLayout>();
            m_RootNode = m_RootGo.GetComponent<FlexNode>();
            m_RootNode.style.width = FlexValue.Auto();
            m_RootNode.style.height = FlexValue.Points(220f);

            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.FlexStart;
            m_RootLayout.style.mainGap = 0f;
            m_RootLayout.style.crossGap = 0f;
            m_RootLayout.style.padding = new FlexEdges { left = 10f, right = 10f, top = 10f, bottom = 10f };
            m_RootLayout.implicitItemDefaults.width = FlexValue.Auto();
            m_RootLayout.implicitItemDefaults.height = FlexValue.Auto();
            m_RootLayout.implicitItemDefaults.flexGrow = 1f;
            m_RootLayout.implicitItemDefaults.flexShrink = 1f;
            m_RootLayout.implicitItemDefaults.flexBasis = FlexValue.Auto();
            m_RootLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexStart;
        }

        [TearDown]
        public void TearDown()
        {
            m_RootGo = null;
            m_RootRect = null;
            m_RootLayout = null;
        }

        [UnityTest]
        public IEnumerator SingleParameterToggle_WithoutForcedDirty_RemainsStableAcrossFrames()
        {
            var red = CreateExplicitGrowChild("Red", maxWidth: 10f);
            var green = CreateImplicitGrowChild("Green", 120f, 180f);

            m_RootLayout.MarkLayoutDirty();
            yield return null;

            red.GetComponent<FlexNode>().style.maxWidth = FlexOptionalFloat.Enabled(220f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            var expectedRootWidth = m_RootRect.sizeDelta.x;
            var expectedRedWidth = red.sizeDelta.x;
            var expectedGreenWidth = green.sizeDelta.x;

            for (var i = 0; i < 24; i++)
            {
                yield return null;
                Assert.That(m_RootRect.sizeDelta.x, Is.EqualTo(expectedRootWidth).Within(0.01f));
                Assert.That(red.sizeDelta.x, Is.EqualTo(expectedRedWidth).Within(0.01f));
                Assert.That(green.sizeDelta.x, Is.EqualTo(expectedGreenWidth).Within(0.01f));
            }
        }

        [UnityTest]
        public IEnumerator RepeatedMarkLayoutDirty_WithSameInput_RemainsStableAfterFirstFrame()
        {
            var red = CreateExplicitGrowChild("Red", maxWidth: 10f);
            var green = CreateImplicitGrowChild("Green", 120f, 180f);

            m_RootLayout.MarkLayoutDirty();
            yield return null;

            red.GetComponent<FlexNode>().style.maxWidth = FlexOptionalFloat.Enabled(220f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            var expectedRootWidth = m_RootRect.sizeDelta.x;
            var expectedRedWidth = red.sizeDelta.x;
            var expectedGreenWidth = green.sizeDelta.x;

            for (var i = 0; i < 24; i++)
            {
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.That(m_RootRect.sizeDelta.x, Is.EqualTo(expectedRootWidth).Within(0.01f));
                Assert.That(red.sizeDelta.x, Is.EqualTo(expectedRedWidth).Within(0.01f));
                Assert.That(green.sizeDelta.x, Is.EqualTo(expectedGreenWidth).Within(0.01f));
            }
        }

        private RectTransform CreateExplicitGrowChild(string name, float maxWidth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(FlexNode), typeof(FlexItem));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(m_RootRect, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(0f, 180f);

            var node = go.GetComponent<FlexNode>();
            node.style.width = FlexValue.Points(0f);
            node.style.height = FlexValue.Points(180f);
            node.style.maxWidth = FlexOptionalFloat.Enabled(maxWidth);
            node.style.positionType = PositionType.Relative;

            var item = go.GetComponent<FlexItem>();
            item.style.flexGrow = 1f;
            item.style.flexShrink = 1f;
            item.style.flexBasis = FlexValue.Auto();
            item.style.alignSelf = AlignSelf.FlexStart;

            return rect;
        }

        private RectTransform CreateImplicitGrowChild(string name, float width, float height)
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
    }
}
