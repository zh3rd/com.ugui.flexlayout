using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public class FlexAspectPlayModeTests : PlayModeSceneIsolationFixture
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
            m_RootRect.sizeDelta = new Vector2(800f, 300f);

            m_RootLayout = m_RootGo.GetComponent<FlexLayout>();
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.FlexStart;
            m_RootLayout.style.padding = FlexEdges.Zero;
            m_RootLayout.style.mainGap = 0f;

            m_RootNode = m_RootGo.GetComponent<FlexNode>();
            m_RootNode.style.width = FlexValue.Points(800f);
            m_RootNode.style.height = FlexValue.Points(300f);
        }

        [TearDown]
        public void TearDown()
        {
            m_RootGo = null;
            m_RootRect = null;
            m_RootLayout = null;
            m_RootNode = null;
        }

        [UnityTest]
        public IEnumerator Aspect_WidthPoints_HeightAuto_ResolvesHeightFromRatio()
        {
            var rect = CreateChild("A");
            var node = rect.GetComponent<FlexNode>();
            node.style.width = FlexValue.Points(240f);
            node.style.height = FlexValue.Auto();
            node.style.aspectRatio = FlexOptionalFloat.Enabled(2f);
            yield return null;

            Assert.That(rect.sizeDelta.x, Is.EqualTo(240f).Within(0.05f));
            Assert.That(rect.sizeDelta.y, Is.EqualTo(120f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator Aspect_WidthAuto_HeightPoints_ResolvesWidthFromRatio()
        {
            m_RootLayout.style.flexDirection = FlexDirection.Column;
            m_RootLayout.MarkLayoutDirty();

            var rect = CreateChild("B");
            var node = rect.GetComponent<FlexNode>();
            node.style.width = FlexValue.Auto();
            node.style.height = FlexValue.Points(150f);
            node.style.aspectRatio = FlexOptionalFloat.Enabled(1.5f);
            yield return null;

            Assert.That(rect.sizeDelta.y, Is.EqualTo(150f).Within(0.05f));
            Assert.That(rect.sizeDelta.x, Is.EqualTo(225f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator Aspect_NonStretchCrossSize_FollowsFinalMainSizeAfterGrow()
        {
            var a = CreateChild("A");
            var aNode = a.GetComponent<FlexNode>();
            var aItem = a.GetComponent<FlexItem>();
            aNode.style.width = FlexValue.Auto();
            aNode.style.height = FlexValue.Auto();
            aNode.style.aspectRatio = FlexOptionalFloat.Enabled(2f);
            aItem.style.flexBasis = FlexValue.Points(100f);
            aItem.style.flexGrow = 1f;
            aItem.style.flexShrink = 1f;

            var b = CreateChild("B");
            var bNode = b.GetComponent<FlexNode>();
            var bItem = b.GetComponent<FlexItem>();
            bNode.style.width = FlexValue.Points(100f);
            bNode.style.height = FlexValue.Points(40f);
            bItem.style.flexBasis = FlexValue.Points(100f);
            bItem.style.flexGrow = 1f;
            bItem.style.flexShrink = 1f;

            yield return null;

            Assert.That(a.sizeDelta.x, Is.EqualTo(400f).Within(0.1f));
            Assert.That(a.sizeDelta.y, Is.EqualTo(200f).Within(0.1f));
            Assert.That(b.sizeDelta.x, Is.EqualTo(400f).Within(0.1f));
        }

        private RectTransform CreateChild(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(FlexNode), typeof(FlexItem));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(m_RootRect, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(100f, 40f);
            return rect;
        }
    }
}
