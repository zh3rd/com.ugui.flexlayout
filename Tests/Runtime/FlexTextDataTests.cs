using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Flex.Core;

namespace UnityEngine.UI.Flex.Tests.Runtime
{
    public class FlexTextDataTests
    {
        [Test]
        public void Resolve_ImplicitTmpWithoutFlexText_Uses_Rect_Content_By_Default()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var child = new GameObject("Child", typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(root.transform, false);

            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(120f, 36f);

            var tmp = child.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(tmp);
            tmp.enableWordWrapping = true;
            tmp.text = "This is a long text block for resolver input.";

            var implicitRect = FlexBridge.ResolveImplicitSizeForTesting(childRect);
            var resolved = FlexResolvedNodeResolver.Resolve(childRect, implicitRect);
            var legacy = resolved.ToLegacyNode(default);

            Assert.That(legacy.ContentWidth, Is.EqualTo(implicitRect.x).Within(0.01f));
            Assert.That(legacy.ContentHeight, Is.EqualTo(implicitRect.y).Within(0.01f));
            Assert.IsTrue(legacy.AllowImplicitRectPassthrough);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Resolve_FlexText_Uses_TmpPreferred_Content_Size()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var child = new GameObject("Child", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
            child.transform.SetParent(root.transform, false);

            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(80f, 20f);

            var tmp = child.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(tmp);
            tmp.enableWordWrapping = false;
            tmp.text = "This is a long single line text to exceed rect width";

            var implicitRect = FlexBridge.ResolveImplicitSizeForTesting(childRect);
            var resolved = FlexResolvedNodeResolver.Resolve(childRect, implicitRect);
            var legacy = resolved.ToLegacyNode(default);

            Assert.IsFalse(legacy.AllowImplicitRectPassthrough);
            Assert.That(legacy.ContentWidth, Is.GreaterThan(implicitRect.x + 0.1f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Resolve_FlexText_Uses_CurrentRectWidth_For_Wrapped_Text_Height()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var child = new GameObject("Child", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
            child.transform.SetParent(root.transform, false);

            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(90f, 20f);

            var tmp = child.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(tmp);
            tmp.enableWordWrapping = true;
            tmp.text = "This is a multiline text measurement case for constrained width.";
            tmp.ForceMeshUpdate();

            var implicitRect = FlexBridge.ResolveImplicitSizeForTesting(childRect);
            var expectedWithWidthConstraint = tmp.GetPreferredValues(Mathf.Max(0f, implicitRect.x), float.PositiveInfinity);
            var resolved = FlexResolvedNodeResolver.Resolve(childRect, implicitRect).ToLegacyNode(default);
            var expectedWidth = Mathf.Min(Mathf.Max(0f, expectedWithWidthConstraint.x), Mathf.Max(0f, implicitRect.x));

            Assert.IsFalse(resolved.AllowImplicitRectPassthrough);
            Assert.That(resolved.ContentWidth, Is.EqualTo(expectedWidth).Within(0.01f));
            Assert.That(resolved.ContentHeight, Is.EqualTo(expectedWithWidthConstraint.y).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Resolve_FlexText_Wrap_Respects_MaxWidth_Constraint()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;
            rootRect.sizeDelta = new Vector2(600f, 300f);
            var child = new GameObject("Child", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
            child.transform.SetParent(root.transform, false);

            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(300f, 20f);

            var node = child.GetComponent<FlexText>();
            node.style.width = FlexValue.Auto();
            node.style.maxWidth = FlexOptionalFloat.Enabled(120f);

            var tmp = child.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(tmp);
            tmp.enableWordWrapping = true;
            tmp.text = "This text should be measured with max width constraint when wrapping is enabled.";
            tmp.ForceMeshUpdate();

            var expected = tmp.GetPreferredValues(120f, float.PositiveInfinity);
            var implicitRect = FlexBridge.ResolveImplicitSizeForTesting(childRect);
            var resolved = FlexResolvedNodeResolver.Resolve(childRect, implicitRect).ToLegacyNode(default);
            var expectedWidth = Mathf.Min(Mathf.Max(0f, expected.x), 120f);

            Assert.That(resolved.ContentWidth, Is.EqualTo(expectedWidth).Within(0.01f));
            Assert.That(resolved.ContentHeight, Is.EqualTo(expected.y).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Resolve_FlexText_Wrap_Respects_PointsWidth_Constraint()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;
            rootRect.sizeDelta = new Vector2(600f, 300f);
            var child = new GameObject("Child", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
            child.transform.SetParent(root.transform, false);

            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(260f, 20f);

            var node = child.GetComponent<FlexText>();
            node.style.width = FlexValue.Points(110f);
            node.style.maxWidth = FlexOptionalFloat.Disabled();

            var tmp = child.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(tmp);
            tmp.enableWordWrapping = true;
            tmp.text = "This text should use points width as the wrapping measurement bound.";
            tmp.ForceMeshUpdate();

            var expected = tmp.GetPreferredValues(110f, float.PositiveInfinity);
            var implicitRect = FlexBridge.ResolveImplicitSizeForTesting(childRect);
            var resolved = FlexResolvedNodeResolver.Resolve(childRect, implicitRect).ToLegacyNode(default);
            var expectedWidth = Mathf.Min(Mathf.Max(0f, expected.x), 110f);

            Assert.That(resolved.ContentWidth, Is.EqualTo(expectedWidth).Within(0.01f));
            Assert.That(resolved.ContentHeight, Is.EqualTo(expected.y).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Resolve_FlexText_Wrap_AutoWidth_Is_Clamped_By_ActiveParentLayoutInnerWidth()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;
            rootRect.sizeDelta = new Vector2(500f, 300f);

            var rootLayout = root.GetComponent<FlexLayout>();
            rootLayout.style.padding = new FlexEdges
            {
                left = 0f,
                right = 0f,
                top = 0f,
                bottom = 0f,
            };

            var child = new GameObject("Child", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
            child.transform.SetParent(root.transform, false);

            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(620f, 20f);

            var node = child.GetComponent<FlexText>();
            node.style.width = FlexValue.Auto();
            node.style.maxWidth = FlexOptionalFloat.Disabled();

            var tmp = child.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(tmp);
            tmp.enableWordWrapping = true;
            tmp.text = "New TextNew TextNew TextNew Text";
            tmp.ForceMeshUpdate();

            var expected = tmp.GetPreferredValues(500f, float.PositiveInfinity);
            var implicitRect = FlexBridge.ResolveImplicitSizeForTesting(childRect);
            var resolved = FlexResolvedNodeResolver.Resolve(childRect, implicitRect).ToLegacyNode(default);
            var expectedWidth = Mathf.Min(Mathf.Max(0f, expected.x), 500f);

            Assert.That(resolved.ContentWidth, Is.EqualTo(expectedWidth).Within(0.01f));
            Assert.That(resolved.ContentWidth, Is.LessThanOrEqualTo(500f + 0.01f));
            Assert.That(resolved.ContentHeight, Is.EqualTo(expected.y).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarkLayoutDirty_FlexTextOnlyNode_AutoSize_DoesNotCollapseToZero()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNode));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;
            rootRect.sizeDelta = new Vector2(700f, 200f);

            var rootLayout = root.GetComponent<FlexLayout>();
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.style.flexWrap = FlexWrap.NoWrap;
            rootLayout.style.justifyContent = JustifyContent.FlexStart;
            rootLayout.style.alignItems = AlignItems.FlexStart;

            var rootNode = root.GetComponent<FlexNode>();
            rootNode.style.width = FlexValue.Points(700f);
            rootNode.style.height = FlexValue.Points(200f);

            var child = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FlexText));
            child.transform.SetParent(root.transform, false);
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(100f, 50f);

            var childNode = child.GetComponent<FlexText>();
            childNode.style.width = FlexValue.Auto();
            childNode.style.height = FlexValue.Auto();

            var tmp = child.GetComponent<TextMeshProUGUI>();
            EnsureTmpCanMeasure(tmp);
            tmp.enableWordWrapping = false;
            tmp.text = "FlexText auto sizing should use TMP preferred size and never collapse to zero.";

            rootLayout.MarkLayoutDirty();

            Assert.That(childRect.sizeDelta.x, Is.GreaterThan(0.01f));
            Assert.That(childRect.sizeDelta.y, Is.GreaterThan(0.01f));

            Object.DestroyImmediate(root);
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
