using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI.Flex.Core;

namespace UnityEngine.UI.Flex.Tests.Runtime
{
    public class FlexDrivenRegistryTests
    {
        [TearDown]
        public void TearDown()
        {
            FlexDrivenRegistry.ClearAll();
        }

        [Test]
        public void SetContribution_Aggregates_Masks_Per_Target()
        {
            var ownerA = new GameObject("OwnerA");
            var ownerB = new GameObject("OwnerB");
            var targetGo = new GameObject("Target", typeof(RectTransform));
            var target = targetGo.GetComponent<RectTransform>();

            FlexDrivenRegistry.SetContribution(ownerA, target, FlexDriveMask.SizeX);
            FlexDrivenRegistry.SetContribution(ownerB, target, FlexDriveMask.SizeY);

            var driven = GetDrivenProperties(target);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(ownerA);
            Object.DestroyImmediate(ownerB);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void ClearOwner_Removes_Only_Owner_Contribution()
        {
            var ownerA = new GameObject("OwnerA");
            var ownerB = new GameObject("OwnerB");
            var targetGo = new GameObject("Target", typeof(RectTransform));
            var target = targetGo.GetComponent<RectTransform>();

            FlexDrivenRegistry.SetContribution(ownerA, target, FlexDriveMask.SizeX);
            FlexDrivenRegistry.SetContribution(ownerB, target, FlexDriveMask.SizeY);
            FlexDrivenRegistry.ClearOwner(ownerA);

            var driven = GetDrivenProperties(target);
            Assert.IsFalse((driven & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(ownerA);
            Object.DestroyImmediate(ownerB);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void ClearAll_Removes_All_Driven_Properties()
        {
            var ownerA = new GameObject("OwnerA");
            var targetGo = new GameObject("Target", typeof(RectTransform));
            var target = targetGo.GetComponent<RectTransform>();

            FlexDrivenRegistry.SetContribution(ownerA, target, FlexDriveMask.Anchors | FlexDriveMask.PositionX | FlexDriveMask.PositionY);
            Assert.AreNotEqual(DrivenTransformProperties.None, GetDrivenProperties(target));

            FlexDrivenRegistry.ClearAll();
            Assert.AreEqual(DrivenTransformProperties.None, GetDrivenProperties(target));

            Object.DestroyImmediate(ownerA);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void SetContribution_SameOwner_Updates_Mask_Without_Stale_Bits()
        {
            var owner = new GameObject("Owner");
            var targetGo = new GameObject("Target", typeof(RectTransform));
            var target = targetGo.GetComponent<RectTransform>();

            FlexDrivenRegistry.SetContribution(owner, target, FlexDriveMask.SizeX | FlexDriveMask.SizeY);
            FlexDrivenRegistry.SetContribution(owner, target, FlexDriveMask.SizeX);

            var driven = GetDrivenProperties(target);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsFalse((driven & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void ClearOwner_Does_Not_Affect_Other_Targets()
        {
            var ownerA = new GameObject("OwnerA");
            var ownerB = new GameObject("OwnerB");
            var targetA = new GameObject("TargetA", typeof(RectTransform)).GetComponent<RectTransform>();
            var targetB = new GameObject("TargetB", typeof(RectTransform)).GetComponent<RectTransform>();

            FlexDrivenRegistry.SetContribution(ownerA, targetA, FlexDriveMask.SizeX);
            FlexDrivenRegistry.SetContribution(ownerB, targetB, FlexDriveMask.SizeY);

            FlexDrivenRegistry.ClearOwner(ownerA);

            Assert.AreEqual(DrivenTransformProperties.None, GetDrivenProperties(targetA));
            Assert.IsTrue((GetDrivenProperties(targetB) & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(ownerA);
            Object.DestroyImmediate(ownerB);
            Object.DestroyImmediate(targetA.gameObject);
            Object.DestroyImmediate(targetB.gameObject);
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
