using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public class FlexTextPlayModeTests : PlayModeSceneIsolationFixture
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
            m_RootRect.sizeDelta = new Vector2(700f, 120f);

            m_RootLayout = m_RootGo.GetComponent<FlexLayout>();
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.FlexStart;
            m_RootLayout.style.padding = FlexEdges.Zero;
            m_RootLayout.style.mainGap = 0f;
            m_RootLayout.implicitItemDefaults = FlexImplicitItemStyleData.Default;

            m_RootNode = m_RootGo.GetComponent<FlexNode>();
            m_RootNode.style.width = FlexValue.Points(700f);
            m_RootNode.style.height = FlexValue.Points(120f);
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
        public IEnumerator TextChange_WithFlexTextTmpPreferred_Reflows_Sibling_Without_ManualDirty()
        {
            var textRect = CreateTextChild("Text", "short");
            var siblingRect = CreateImplicitChild("Box", 80f, 30f);
            yield return null;

            var before = siblingRect.anchoredPosition.x;
            var text = textRect.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(text);
            text.text = "This text became significantly longer and should push the next sibling.";
            yield return null;

            var after = siblingRect.anchoredPosition.x;
            Assert.That(after, Is.GreaterThan(before + 0.1f));
        }

        [UnityTest]
        public IEnumerator ImplicitTmp_DefaultRectSource_DoesNotUsePreferredWidth()
        {
            var textRect = CreateTextChildWithoutFlexText("Text", "A very very very long text content to test rect source.");
            var siblingRect = CreateImplicitChild("Box", 50f, 30f);
            textRect.sizeDelta = new Vector2(120f, 30f);
            yield return null;

            Assert.That(siblingRect.anchoredPosition.x, Is.EqualTo(120f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator FlexText_Pushes_Sibling_By_Preferred_Text_Width()
        {
            var textRect = CreateTextChild("Text", "A long text content to force preferred size wider than initial rect.");
            var siblingRect = CreateImplicitChild("Box", 50f, 30f);
            yield return null;

            var text = textRect.GetComponent<TextMeshProUGUI>();
            var constrainedPreferred = text.GetPreferredValues(m_RootRect.rect.width, float.PositiveInfinity).x;
            var expectedWidth = Mathf.Min(Mathf.Max(0f, constrainedPreferred), m_RootRect.rect.width);
            Assert.That(textRect.sizeDelta.x, Is.EqualTo(expectedWidth).Within(0.5f));
            Assert.That(siblingRect.anchoredPosition.x, Is.EqualTo(expectedWidth).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator Wrap_With_FlexText_Aspect_Does_Not_Overlap_Next_Line_Item()
        {
            m_RootNode.style.width = FlexValue.Points(500f);
            m_RootNode.style.height = FlexValue.Points(300f);
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.Center;

            var textRect = CreateTextChild("Text", "New TextNew TextNew TextNew Text");
            var textNode = textRect.GetComponent<FlexText>();
            textNode.style.width = FlexValue.Auto();
            textNode.style.height = FlexValue.Auto();
            textNode.style.aspectRatio = FlexOptionalFloat.Enabled(2f);

            var imageRect = CreateImplicitChild("Image", 100f, 100f);
            yield return null;

            var textBottom = textRect.anchoredPosition.y - textRect.sizeDelta.y;
            var imageTop = imageRect.anchoredPosition.y;

            Assert.That(imageTop, Is.LessThanOrEqualTo(textBottom + 0.5f));
        }

        private RectTransform CreateTextChild(string name, string value)
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
            text.enableWordWrapping = true;
            text.text = value;

            return rect;
        }

        private RectTransform CreateTextChildWithoutFlexText(string name, string value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(m_RootRect, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;

            var text = go.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(text);
            text.enableWordWrapping = true;
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
