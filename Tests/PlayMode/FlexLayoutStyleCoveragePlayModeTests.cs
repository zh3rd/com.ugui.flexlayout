using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FlexItem = UnityEngine.UI.Flex.FlexItem;
using FlexNode = UnityEngine.UI.Flex.FlexNode;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public class FlexLayoutStyleCoveragePlayModeTests : PlayModeSceneIsolationFixture
    {
        private GameObject m_RootGo;
        private RectTransform m_RootRect;
        private FlexLayout m_RootLayout;
        private FlexNode m_RootNode;

        [SetUp]
        public void SetUp()
        {
            m_RootGo = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            m_RootRect = m_RootGo.GetComponent<RectTransform>();
            m_RootRect.anchorMin = Vector2.up;
            m_RootRect.anchorMax = Vector2.up;
            m_RootRect.pivot = Vector2.up;
            m_RootRect.sizeDelta = new Vector2(300f, 100f);

            m_RootLayout = m_RootGo.GetComponent<FlexLayout>();
            m_RootNode = m_RootGo.AddComponent<FlexNode>();
            m_RootNode.style.width = FlexValue.Points(300f);
            m_RootNode.style.height = FlexValue.Points(100f);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.FlexStart;
            m_RootLayout.style.mainGap = 0f;
            m_RootLayout.style.crossGap = 0f;
            m_RootLayout.style.padding = FlexEdges.Zero;
            m_RootLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexStart;
            m_RootLayout.MarkLayoutDirty();
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
        public IEnumerator Size_WidthPoints_DrivesRootWidth()
        {
            m_RootNode.style.width = FlexValue.Points(420f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            Assert.That(m_RootRect.sizeDelta.x, Is.EqualTo(420f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_HeightPoints_DrivesRootHeight()
        {
            m_RootNode.style.height = FlexValue.Points(220f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            Assert.That(m_RootRect.sizeDelta.y, Is.EqualTo(220f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_WidthAuto_UsesContent()
        {
            CreateImplicitChild("A", 60f, 20f);
            CreateImplicitChild("B", 80f, 20f);
            m_RootLayout.style.mainGap = 10f;
            m_RootLayout.style.crossGap = 10f;
            m_RootNode.style.width = FlexValue.Auto();
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(m_RootRect.sizeDelta.x, Is.EqualTo(150f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_HeightAuto_UsesContent()
        {
            CreateImplicitChild("A", 60f, 25f);
            CreateImplicitChild("B", 80f, 40f);
            m_RootNode.style.height = FlexValue.Auto();
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(m_RootRect.sizeDelta.y, Is.EqualTo(40f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_MinWidth_Clamp_Works()
        {
            m_RootNode.style.width = FlexValue.Points(120f);
            m_RootNode.style.minWidth = FlexOptionalFloat.Enabled(180f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(m_RootRect.sizeDelta.x, Is.EqualTo(180f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_MaxWidth_Clamp_Works()
        {
            m_RootNode.style.width = FlexValue.Points(260f);
            m_RootNode.style.maxWidth = FlexOptionalFloat.Enabled(190f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(m_RootRect.sizeDelta.x, Is.EqualTo(190f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_MinHeight_Clamp_Works()
        {
            m_RootNode.style.height = FlexValue.Points(70f);
            m_RootNode.style.minHeight = FlexOptionalFloat.Enabled(90f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(m_RootRect.sizeDelta.y, Is.EqualTo(90f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_MaxHeight_Clamp_Works()
        {
            m_RootNode.style.height = FlexValue.Points(170f);
            m_RootNode.style.maxHeight = FlexOptionalFloat.Enabled(120f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(m_RootRect.sizeDelta.y, Is.EqualTo(120f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_ChildWidthPoints_DrivesChildWidth()
        {
            var child = CreateExplicitChild("A", 50f, 20f, PositionType.Relative);
            var childNode = child.GetComponent<FlexNode>();
            childNode.style.width = FlexValue.Points(140f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.sizeDelta.x, Is.EqualTo(140f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_ChildHeightPoints_DrivesChildHeight()
        {
            var child = CreateExplicitChild("A", 50f, 20f, PositionType.Relative);
            var childNode = child.GetComponent<FlexNode>();
            childNode.style.height = FlexValue.Points(66f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.sizeDelta.y, Is.EqualTo(66f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_ChildMinMax_Clamp_Works()
        {
            var child = CreateExplicitChild("A", 120f, 90f, PositionType.Relative);
            var childNode = child.GetComponent<FlexNode>();
            childNode.style.minWidth = FlexOptionalFloat.Enabled(130f);
            childNode.style.maxWidth = FlexOptionalFloat.Enabled(140f);
            childNode.style.minHeight = FlexOptionalFloat.Enabled(95f);
            childNode.style.maxHeight = FlexOptionalFloat.Enabled(100f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.sizeDelta.x, Is.EqualTo(130f).Within(0.02f));
            Assert.That(child.sizeDelta.y, Is.EqualTo(95f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_FlexBasis_AutoAndPoints_BothAffectMainAxis()
        {
            var a = CreateExplicitChild("A", 60f, 20f, PositionType.Relative);
            var b = CreateExplicitChild("B", 60f, 20f, PositionType.Relative);
            var aItem = a.GetComponent<FlexItem>();
            var bItem = b.GetComponent<FlexItem>();

            aItem.style.flexBasis = FlexValue.Points(120f);
            bItem.style.flexBasis = FlexValue.Auto();
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            Assert.That(a.sizeDelta.x, Is.EqualTo(120f).Within(0.02f));

            aItem.style.flexBasis = FlexValue.Auto();
            bItem.style.flexBasis = FlexValue.Points(140f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            Assert.That(b.sizeDelta.x, Is.EqualTo(140f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Size_FlexGrow_Values_ChangeAllocatedWidth()
        {
            var a = CreateExplicitChild("A", 60f, 20f, PositionType.Relative);
            var b = CreateExplicitChild("B", 60f, 20f, PositionType.Relative);
            var aItem = a.GetComponent<FlexItem>();
            var bItem = b.GetComponent<FlexItem>();

            m_RootNode.style.width = FlexValue.Points(300f);
            aItem.style.flexGrow = 0f;
            bItem.style.flexGrow = 0f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var baseA = a.sizeDelta.x;
            var baseB = b.sizeDelta.x;

            aItem.style.flexGrow = 1f;
            bItem.style.flexGrow = 2f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(a.sizeDelta.x, Is.GreaterThan(baseA));
            Assert.That(b.sizeDelta.x, Is.GreaterThan(baseB));
            Assert.That(b.sizeDelta.x, Is.GreaterThan(a.sizeDelta.x));
        }

        [UnityTest]
        public IEnumerator Size_FlexShrink_Values_ChangeAllocatedWidth()
        {
            var a = CreateExplicitChild("A", 120f, 20f, PositionType.Relative);
            var b = CreateExplicitChild("B", 120f, 20f, PositionType.Relative);
            var aItem = a.GetComponent<FlexItem>();
            var bItem = b.GetComponent<FlexItem>();

            m_RootNode.style.width = FlexValue.Points(180f);
            aItem.style.flexShrink = 0f;
            bItem.style.flexShrink = 1f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var firstA = a.sizeDelta.x;
            var firstB = b.sizeDelta.x;

            aItem.style.flexShrink = 1f;
            bItem.style.flexShrink = 0f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(a.sizeDelta.x, Is.LessThan(firstA));
            Assert.That(b.sizeDelta.x, Is.GreaterThan(firstB));
        }

        [UnityTest]
        public IEnumerator Coordinate_PositionType_Absolute_StopsDrivenPosition()
        {
            var child = CreateExplicitChild("A", 80f, 30f, PositionType.Relative);
            var childNode = child.GetComponent<FlexNode>();
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            childNode.style.positionType = PositionType.Absolute;
            child.anchoredPosition = new Vector2(99f, -55f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.anchoredPosition.x, Is.EqualTo(99f).Within(0.02f));
            Assert.That(child.anchoredPosition.y, Is.EqualTo(-55f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Coordinate_Gap_ChangesSiblingOffset()
        {
            var a = CreateImplicitChild("A", 50f, 20f);
            var b = CreateImplicitChild("B", 50f, 20f);
            m_RootLayout.style.mainGap = 0f;
            m_RootLayout.style.crossGap = 0f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var noGap = b.anchoredPosition.x - a.anchoredPosition.x;

            m_RootLayout.style.mainGap = 18f;
            m_RootLayout.style.crossGap = 18f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var withGap = b.anchoredPosition.x - a.anchoredPosition.x;

            Assert.That(withGap, Is.EqualTo(noGap + 18f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Coordinate_Padding_ChangesStartPosition()
        {
            var a = CreateImplicitChild("A", 50f, 20f);
            m_RootLayout.style.padding = FlexEdges.Zero;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var x0 = a.anchoredPosition.x;
            var y0 = a.anchoredPosition.y;

            m_RootLayout.style.padding = new FlexEdges { left = 14f, right = 0f, top = 8f, bottom = 0f };
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(a.anchoredPosition.x, Is.EqualTo(x0 + 14f).Within(0.02f));
            Assert.That(a.anchoredPosition.y, Is.EqualTo(y0 - 8f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Coordinate_FlexDirection_AllEnums_ProduceExpectedOrdering()
        {
            var a = CreateImplicitChild("A", 50f, 20f);
            var b = CreateImplicitChild("B", 50f, 20f);
            m_RootNode.style.width = FlexValue.Points(300f);
            m_RootNode.style.height = FlexValue.Points(100f);

            foreach (FlexDirection dir in System.Enum.GetValues(typeof(FlexDirection)))
            {
                m_RootLayout.style.flexDirection = dir;
                m_RootLayout.MarkLayoutDirty();
                yield return null;

                if (dir == FlexDirection.Row)
                {
                    Assert.That(a.anchoredPosition.x, Is.EqualTo(0f).Within(0.02f));
                    Assert.That(b.anchoredPosition.x, Is.EqualTo(50f).Within(0.02f));
                }
                else if (dir == FlexDirection.RowReverse)
                {
                    Assert.That(a.anchoredPosition.x, Is.EqualTo(250f).Within(0.02f));
                    Assert.That(b.anchoredPosition.x, Is.EqualTo(200f).Within(0.02f));
                }
                else if (dir == FlexDirection.Column)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(0f).Within(0.02f));
                    Assert.That(b.anchoredPosition.y, Is.EqualTo(-20f).Within(0.02f));
                }
                else if (dir == FlexDirection.ColumnReverse)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(-80f).Within(0.02f));
                    Assert.That(b.anchoredPosition.y, Is.EqualTo(-60f).Within(0.02f));
                }
            }
        }

        [UnityTest]
        public IEnumerator Coordinate_JustifyContent_AllEnums_ProduceExpectedFirstItemX()
        {
            var a = CreateImplicitChild("A", 50f, 20f);
            CreateImplicitChild("B", 50f, 20f);
            CreateImplicitChild("C", 50f, 20f);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootNode.style.width = FlexValue.Points(300f);
            m_RootLayout.style.mainGap = 0f;
            m_RootLayout.style.crossGap = 0f;

            var expected = new Dictionary<JustifyContent, float>
            {
                { JustifyContent.FlexStart, 0f },
                { JustifyContent.Center, 75f },
                { JustifyContent.FlexEnd, 150f },
                { JustifyContent.SpaceBetween, 0f },
                { JustifyContent.SpaceAround, 25f },
                { JustifyContent.SpaceEvenly, 37.5f },
            };

            foreach (JustifyContent value in System.Enum.GetValues(typeof(JustifyContent)))
            {
                m_RootLayout.style.justifyContent = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.That(a.anchoredPosition.x, Is.EqualTo(expected[value]).Within(0.02f));
            }
        }

        [UnityTest]
        public IEnumerator Coordinate_AlignItems_AllEnums_ProduceExpectedCrossAxis()
        {
            var a = CreateExplicitChild("A", 50f, 20f, PositionType.Relative);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootNode.style.height = FlexValue.Points(100f);

            foreach (AlignItems value in System.Enum.GetValues(typeof(AlignItems)))
            {
                m_RootLayout.style.alignItems = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;

                if (value == AlignItems.FlexStart)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(0f).Within(0.02f));
                    Assert.That(a.sizeDelta.y, Is.EqualTo(20f).Within(0.02f));
                }
                else if (value == AlignItems.Center)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(-40f).Within(0.02f));
                    Assert.That(a.sizeDelta.y, Is.EqualTo(20f).Within(0.02f));
                }
                else if (value == AlignItems.FlexEnd)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(-80f).Within(0.02f));
                    Assert.That(a.sizeDelta.y, Is.EqualTo(20f).Within(0.02f));
                }
                else if (value == AlignItems.Stretch)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(0f).Within(0.02f));
                    Assert.That(a.sizeDelta.y, Is.EqualTo(100f).Within(0.02f));
                }
            }
        }

        [UnityTest]
        public IEnumerator Coordinate_AlignSelf_AllEnums_OverrideParentAlignItems()
        {
            var a = CreateExplicitChild("A", 50f, 20f, PositionType.Relative);
            var aItem = a.GetComponent<FlexItem>();
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootNode.style.height = FlexValue.Points(100f);
            m_RootLayout.style.alignItems = AlignItems.Center;

            foreach (AlignSelf value in System.Enum.GetValues(typeof(AlignSelf)))
            {
                aItem.style.alignSelf = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;

                if (value == AlignSelf.FlexStart)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(0f).Within(0.02f));
                }
                else if (value == AlignSelf.Center || value == AlignSelf.Auto)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(-40f).Within(0.02f));
                }
                else if (value == AlignSelf.FlexEnd)
                {
                    Assert.That(a.anchoredPosition.y, Is.EqualTo(-80f).Within(0.02f));
                }
                else if (value == AlignSelf.Stretch)
                {
                    Assert.That(a.sizeDelta.y, Is.EqualTo(100f).Within(0.02f));
                }
            }
        }

        [UnityTest]
        public IEnumerator Coordinate_FlexWrap_AllEnums_StayFinite_AndApplied()
        {
            var children = new List<RectTransform>();
            for (var i = 0; i < 4; i++)
            {
                children.Add(CreateImplicitChild("C" + i, 90f, 20f));
            }

            foreach (FlexWrap value in System.Enum.GetValues(typeof(FlexWrap)))
            {
                m_RootLayout.style.flexWrap = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.AreEqual(value, m_RootLayout.style.flexWrap);
                AssertFinite(children);
            }
        }

        [UnityTest]
        public IEnumerator Coordinate_AlignContent_AllEnums_ProduceExpectedWrappedLineOffsets()
        {
            var a = CreateImplicitChild("A", 60f, 20f);
            var b = CreateImplicitChild("B", 60f, 30f);
            var c = CreateImplicitChild("C", 40f, 10f);
            m_RootNode.style.width = FlexValue.Points(130f);
            m_RootNode.style.height = FlexValue.Points(100f);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.alignContent = AlignContent.FlexStart;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.FlexStart;
            m_RootLayout.style.mainGap = 5f;
            m_RootLayout.style.crossGap = 5f;

            var expected = new Dictionary<AlignContent, (float firstY, float secondY)>
            {
                { AlignContent.Stretch, (0f, -62.5f) },
                { AlignContent.FlexStart, (0f, -35f) },
                { AlignContent.Center, (-27.5f, -62.5f) },
                { AlignContent.FlexEnd, (-55f, -90f) },
                { AlignContent.SpaceBetween, (0f, -90f) },
                { AlignContent.SpaceAround, (-13.75f, -76.25f) },
                { AlignContent.SpaceEvenly, (-18.33333f, -71.66667f) },
            };

            foreach (AlignContent value in System.Enum.GetValues(typeof(AlignContent)))
            {
                m_RootLayout.style.alignContent = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;

                Assert.That(a.anchoredPosition.y, Is.EqualTo(expected[value].firstY).Within(0.02f));
                Assert.That(b.anchoredPosition.y, Is.EqualTo(expected[value].firstY).Within(0.02f));
                Assert.That(c.anchoredPosition.y, Is.EqualTo(expected[value].secondY).Within(0.02f));
            }
        }

        [UnityTest]
        public IEnumerator Wrap_Row_Reflows_Third_Item_To_Second_Line()
        {
            var a = CreateImplicitChild("A", 50f, 20f);
            var b = CreateImplicitChild("B", 50f, 20f);
            var c = CreateImplicitChild("C", 50f, 20f);
            m_RootNode.style.width = FlexValue.Points(100f);
            m_RootNode.style.height = FlexValue.Points(100f);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.alignContent = AlignContent.FlexStart;
            m_RootLayout.style.mainGap = 0f;
            m_RootLayout.style.crossGap = 0f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(a.anchoredPosition.x, Is.EqualTo(0f).Within(0.02f));
            Assert.That(b.anchoredPosition.x, Is.EqualTo(50f).Within(0.02f));
            Assert.That(c.anchoredPosition.x, Is.EqualTo(0f).Within(0.02f));
            Assert.That(c.anchoredPosition.y, Is.EqualTo(-20f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Wrap_Reverse_Places_First_Line_From_Cross_End()
        {
            var a = CreateImplicitChild("A", 60f, 20f);
            var b = CreateImplicitChild("B", 60f, 30f);
            var c = CreateImplicitChild("C", 40f, 10f);
            m_RootNode.style.width = FlexValue.Points(130f);
            m_RootNode.style.height = FlexValue.Points(100f);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.WrapReverse;
            m_RootLayout.style.alignContent = AlignContent.FlexStart;
            m_RootLayout.style.mainGap = 5f;
            m_RootLayout.style.crossGap = 5f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(a.anchoredPosition.y, Is.EqualTo(-70f).Within(0.02f));
            Assert.That(b.anchoredPosition.y, Is.EqualTo(-70f).Within(0.02f));
            Assert.That(c.anchoredPosition.y, Is.EqualTo(-55f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Wrap_RootAutoHeight_Uses_Wrapped_Content()
        {
            CreateImplicitChild("A", 60f, 20f);
            CreateImplicitChild("B", 60f, 30f);
            m_RootNode.style.width = FlexValue.Points(100f);
            m_RootNode.style.height = FlexValue.Auto();
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.mainGap = 5f;
            m_RootLayout.style.crossGap = 5f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(m_RootRect.sizeDelta.y, Is.EqualTo(55f).Within(0.02f));
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
            m_RootLayout.MarkLayoutDirty();
            return rect;
        }

        private RectTransform CreateExplicitChild(string name, float width, float height, PositionType positionType)
        {
            var rect = CreateImplicitChild(name, width, height);
            rect.gameObject.AddComponent<FlexLayout>();
            var node = rect.gameObject.AddComponent<FlexNode>();
            var item = rect.gameObject.AddComponent<FlexItem>();
            node.style.width = FlexValue.Points(width);
            node.style.height = FlexValue.Points(height);
            node.style.positionType = positionType;
            item.style.flexBasis = FlexValue.Auto();
            item.style.flexGrow = 0f;
            item.style.flexShrink = 1f;
            m_RootLayout.MarkLayoutDirty();
            return rect;
        }

        private static void AssertFinite(IReadOnlyList<RectTransform> children)
        {
            for (var i = 0; i < children.Count; i++)
            {
                var c = children[i];
                Assert.That(float.IsNaN(c.anchoredPosition.x) || float.IsInfinity(c.anchoredPosition.x), Is.False);
                Assert.That(float.IsNaN(c.anchoredPosition.y) || float.IsInfinity(c.anchoredPosition.y), Is.False);
                Assert.That(float.IsNaN(c.sizeDelta.x) || float.IsInfinity(c.sizeDelta.x), Is.False);
                Assert.That(float.IsNaN(c.sizeDelta.y) || float.IsInfinity(c.sizeDelta.y), Is.False);
            }
        }
    }
}
