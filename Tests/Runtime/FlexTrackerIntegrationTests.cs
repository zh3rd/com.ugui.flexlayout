using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI.Flex.Core;
using FlexNodeComponent = UnityEngine.UI.Flex.FlexNode;

namespace UnityEngine.UI.Flex.Tests.Runtime
{
    public class FlexTrackerIntegrationTests
    {
        [TearDown]
        public void TearDown()
        {
            FlexDrivenRegistry.ClearAll();
        }

        [Test]
        public void ChildNode_DisableEnable_DoesNot_Leave_Stale_Driven_Bits()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNodeComponent));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var rootLayout = root.GetComponent<FlexLayout>();
            var rootNode = root.GetComponent<FlexNodeComponent>();
            rootNode.style.width = FlexValue.Points(300f);
            rootNode.style.height = FlexValue.Points(120f);

            var child = new GameObject("Child", typeof(RectTransform), typeof(FlexNodeComponent));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.SetParent(root.transform, false);

            var childNode = child.GetComponent<FlexNodeComponent>();
            childNode.style.positionType = PositionType.Absolute;
            childNode.style.width = FlexValue.Points(40f);
            childNode.style.height = FlexValue.Points(20f);

            rootLayout.MarkLayoutDirty();
            var drivenBeforeDisable = GetDrivenProperties(childRect);
            Assert.IsTrue((drivenBeforeDisable & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((drivenBeforeDisable & DrivenTransformProperties.SizeDeltaY) != 0);

            childNode.enabled = false;
            var drivenAfterDisable = GetDrivenProperties(childRect);
            Assert.AreEqual(DrivenTransformProperties.None, drivenAfterDisable);

            childNode.enabled = true;
            rootLayout.MarkLayoutDirty();
            var drivenAfterEnable = GetDrivenProperties(childRect);
            Assert.IsTrue((drivenAfterEnable & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((drivenAfterEnable & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ChildNode_PositionType_RelativeToAbsolute_Swaps_Drive_Set_Cleanly()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNodeComponent));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var rootLayout = root.GetComponent<FlexLayout>();
            var rootNode = root.GetComponent<FlexNodeComponent>();
            rootNode.style.width = FlexValue.Points(300f);
            rootNode.style.height = FlexValue.Points(120f);

            var child = new GameObject("Child", typeof(RectTransform), typeof(FlexNodeComponent));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.SetParent(root.transform, false);

            var childNode = child.GetComponent<FlexNodeComponent>();
            childNode.style.width = FlexValue.Points(50f);
            childNode.style.height = FlexValue.Points(30f);
            childNode.style.positionType = PositionType.Relative;

            rootLayout.MarkLayoutDirty();
            var drivenRelative = GetDrivenProperties(childRect);
            Assert.IsTrue((drivenRelative & DrivenTransformProperties.AnchoredPositionX) != 0);
            Assert.IsTrue((drivenRelative & DrivenTransformProperties.AnchoredPositionY) != 0);
            Assert.IsTrue((drivenRelative & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((drivenRelative & DrivenTransformProperties.SizeDeltaY) != 0);

            childNode.style.positionType = PositionType.Absolute;
            ForceNodeRefresh(childNode);
            ForceLayoutDrivenPropertiesDirty(rootLayout);
            rootLayout.MarkLayoutDirty();
            var drivenAbsolute = GetDrivenProperties(childRect);
            Assert.IsFalse((drivenAbsolute & DrivenTransformProperties.AnchoredPositionX) != 0);
            Assert.IsFalse((drivenAbsolute & DrivenTransformProperties.AnchoredPositionY) != 0);
            Assert.IsTrue((drivenAbsolute & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((drivenAbsolute & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(root);
        }

        private static void ForceNodeRefresh(FlexNodeComponent node)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var method = typeof(FlexNodeComponent).GetMethod("RefreshForParentContextChange", flags);
            Assert.NotNull(method);
            method.Invoke(node, null);
        }

        private static void ForceLayoutDrivenPropertiesDirty(FlexLayout layout)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var method = typeof(FlexLayout).GetMethod("MarkDrivenPropertiesDirty", flags);
            Assert.NotNull(method);
            method.Invoke(layout, null);
        }

        private static DrivenTransformProperties GetDrivenProperties(RectTransform rectTransform)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var property = typeof(RectTransform).GetProperty("drivenProperties", flags);
            if (property != null && property.PropertyType == typeof(DrivenTransformProperties))
            {
                return (DrivenTransformProperties)property.GetValue(rectTransform);
            }

            var field = typeof(RectTransform).GetField("drivenProperties", flags)
                ?? typeof(RectTransform).GetField("m_DrivenProperties", flags);
            if (field != null)
            {
                var value = field.GetValue(rectTransform);
                if (value is DrivenTransformProperties drivenProperties)
                {
                    return drivenProperties;
                }

                if (value is int raw)
                {
                    return (DrivenTransformProperties)raw;
                }
            }

            return DrivenTransformProperties.None;
        }
    }
}
