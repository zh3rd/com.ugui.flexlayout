using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FlexNode = UnityEngine.UI.Flex.FlexNode;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public class FlexRebuildPipelinePlayModeTests : PlayModeSceneIsolationFixture
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private GameObject m_RootGo;
        private RectTransform m_RootRect;
        private FlexLayout m_RootLayout;
        private FlexNode m_RootNode;
        private GameObject m_ChildGo;
        private RectTransform m_ChildRect;
        private FlexLayout m_ChildLayout;
        private FlexNode m_ChildNode;

        [SetUp]
        public void SetUp()
        {
            m_RootGo = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNode));
            m_RootRect = m_RootGo.GetComponent<RectTransform>();
            m_RootRect.anchorMin = Vector2.up;
            m_RootRect.anchorMax = Vector2.up;
            m_RootRect.pivot = Vector2.up;
            m_RootRect.sizeDelta = new Vector2(300f, 120f);

            m_RootLayout = m_RootGo.GetComponent<FlexLayout>();
            m_RootNode = m_RootGo.GetComponent<FlexNode>();
            m_RootNode.style.width = FlexValue.Points(300f);
            m_RootNode.style.height = FlexValue.Points(120f);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.FlexStart;
            m_RootLayout.style.mainGap = 0f;
            m_RootLayout.style.crossGap = 0f;

            m_ChildGo = new GameObject("Child", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNode));
            m_ChildRect = m_ChildGo.GetComponent<RectTransform>();
            m_ChildRect.anchorMin = Vector2.up;
            m_ChildRect.anchorMax = Vector2.up;
            m_ChildRect.pivot = Vector2.up;
            m_ChildRect.SetParent(m_RootRect, false);

            m_ChildLayout = m_ChildGo.GetComponent<FlexLayout>();
            m_ChildNode = m_ChildGo.GetComponent<FlexNode>();
            m_ChildNode.style.width = FlexValue.Points(80f);
            m_ChildNode.style.height = FlexValue.Points(30f);
            m_ChildNode.style.positionType = PositionType.Relative;
        }

        [TearDown]
        public void TearDown()
        {
            m_RootGo = null;
            m_RootRect = null;
            m_RootLayout = null;
            m_RootNode = null;
            m_ChildGo = null;
            m_ChildRect = null;
            m_ChildLayout = null;
            m_ChildNode = null;
        }

        [UnityTest]
        public IEnumerator RequestLayoutDirty_PlayMode_Defers_Rebuild_Until_Next_Frame()
        {
            var requestMethod = typeof(FlexLayout).GetMethod("RequestLayoutDirty", InstanceFlags, null, System.Type.EmptyTypes, null);
            Assert.NotNull(requestMethod);

            m_RootLayout.MarkLayoutDirty();
            yield return null;

            var expectedPosition = m_ChildRect.anchoredPosition;

            m_ChildRect.anchoredPosition = new Vector2(53f, -9f);
            requestMethod.Invoke(m_ChildLayout, null);

            Assert.That(m_ChildRect.anchoredPosition.x, Is.EqualTo(53f).Within(0.01f));
            Assert.That(m_ChildRect.anchoredPosition.y, Is.EqualTo(-9f).Within(0.01f));

            yield return null;

            Assert.That(m_ChildRect.anchoredPosition.x, Is.EqualTo(expectedPosition.x).Within(0.01f));
            Assert.That(m_ChildRect.anchoredPosition.y, Is.EqualTo(expectedPosition.y).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator MarkLayoutDirty_PlayMode_Applies_Immediately()
        {
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            var expectedPosition = m_ChildRect.anchoredPosition;

            m_ChildRect.anchoredPosition = new Vector2(41f, -13f);
            m_ChildLayout.MarkLayoutDirty();

            Assert.That(m_ChildRect.anchoredPosition.x, Is.EqualTo(expectedPosition.x).Within(0.01f));
            Assert.That(m_ChildRect.anchoredPosition.y, Is.EqualTo(expectedPosition.y).Within(0.01f));

            yield return null;

            Assert.That(m_ChildRect.anchoredPosition.x, Is.EqualTo(expectedPosition.x).Within(0.01f));
            Assert.That(m_ChildRect.anchoredPosition.y, Is.EqualTo(expectedPosition.y).Within(0.01f));
        }
    }
}
