using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FlexItem = UnityEngine.UI.Flex.FlexItem;
using FlexNode = UnityEngine.UI.Flex.FlexNode;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public class FlexLayoutDynamicPlayModeTests : PlayModeSceneIsolationFixture
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
        public IEnumerator Dynamic_Add_Implicit_Child_Repositions_From_Origin()
        {
            var child = CreateImplicitChild("A", 100f, 30f);
            yield return null;

            Assert.That(child.anchoredPosition.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(child.anchoredPosition.y, Is.LessThanOrEqualTo(0f));
            Assert.That(child.sizeDelta.x, Is.EqualTo(100f).Within(0.01f));
            Assert.That(child.sizeDelta.y, Is.EqualTo(30f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Dynamic_Remove_Child_Reflows_Siblings()
        {
            var a = CreateImplicitChild("A", 100f, 30f);
            var b = CreateImplicitChild("B", 100f, 30f);
            yield return null;

            var before = b.anchoredPosition.x;
            Object.Destroy(a.gameObject);
            yield return null;

            Assert.That(before, Is.EqualTo(100f).Within(0.01f));
            Assert.That(b.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Dynamic_Disable_Child_Reflows_Siblings()
        {
            var a = CreateImplicitChild("A", 100f, 30f);
            var b = CreateImplicitChild("B", 100f, 30f);
            yield return null;

            Assert.That(b.anchoredPosition.x, Is.EqualTo(100f).Within(0.01f));
            a.gameObject.SetActive(false);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(b.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
        }

        public IEnumerator Dynamic_Add_FlexLayout_To_Implicit_Child_Makes_It_Explicit_Driven()
        {
            var child = CreateImplicitChild("A", 80f, 30f);
            yield return null;

            var node = EnsureNode(child);
            node.style.width = FlexValue.Points(150f);
            node.style.height = FlexValue.Points(40f);
            node.style.positionType = PositionType.Relative;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.sizeDelta.x, Is.EqualTo(150f).Within(0.01f));
            Assert.That(child.sizeDelta.y, Is.EqualTo(40f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Dynamic_PositionType_Relative_To_Absolute_Stops_Position_Driving()
        {
            var child = CreateExplicitChild("A", 80f, 30f, PositionType.Relative);
            yield return null;

            Assert.That(child.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            var childNode = child.GetComponent<FlexNode>();
            childNode.style.positionType = PositionType.Absolute;
            child.anchoredPosition = new Vector2(77f, -33f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.anchoredPosition.x, Is.EqualTo(77f).Within(0.01f));
            Assert.That(child.anchoredPosition.y, Is.EqualTo(-33f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Dynamic_JustifyContent_Enum_Switch_Works()
        {
            var a = CreateImplicitChild("A", 50f, 20f);
            CreateImplicitChild("B", 50f, 20f);
            CreateImplicitChild("C", 50f, 20f);
            var cases = new (JustifyContent justify, float expectedFirstX)[]
            {
                (JustifyContent.FlexStart, 0f),
                (JustifyContent.Center, 75f),
                (JustifyContent.FlexEnd, 150f),
                (JustifyContent.SpaceBetween, 0f),
                (JustifyContent.SpaceAround, 25f),
                (JustifyContent.SpaceEvenly, 37.5f),
            };

            for (var i = 0; i < cases.Length; i++)
            {
                m_RootLayout.style.justifyContent = cases[i].justify;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.That(a.anchoredPosition.x, Is.EqualTo(cases[i].expectedFirstX).Within(0.02f));
            }
        }

        [UnityTest]
        public IEnumerator Dynamic_FlexDirection_Enum_Switch_Works()
        {
            var a = CreateImplicitChild("A", 50f, 20f);
            var b = CreateImplicitChild("B", 50f, 20f);
            var cases = new (FlexDirection direction, float expectedAX, float expectedBX, float expectedBY)[]
            {
                (FlexDirection.Row, 0f, 50f, 0f),
                (FlexDirection.RowReverse, 250f, 200f, 0f),
                (FlexDirection.Column, 0f, 0f, -20f),
                (FlexDirection.ColumnReverse, 0f, 0f, -60f),
            };

            for (var i = 0; i < cases.Length; i++)
            {
                m_RootLayout.style.flexDirection = cases[i].direction;
                m_RootLayout.MarkLayoutDirty();
                yield return null;

                Assert.That(a.anchoredPosition.x, Is.EqualTo(cases[i].expectedAX).Within(0.02f));
                Assert.That(b.anchoredPosition.x, Is.EqualTo(cases[i].expectedBX).Within(0.02f));
                if (cases[i].direction == FlexDirection.Column || cases[i].direction == FlexDirection.ColumnReverse)
                {
                    Assert.That(b.anchoredPosition.y, Is.EqualTo(cases[i].expectedBY).Within(0.02f));
                }
            }
        }

        [UnityTest]
        public IEnumerator Dynamic_AlignItems_Enum_Switch_Works()
        {
            var child = CreateExplicitChild("A", 50f, 20f, PositionType.Relative);
            var cases = new (AlignItems alignItems, float expectedY, float expectedHeight)[]
            {
                (AlignItems.FlexStart, 0f, 20f),
                (AlignItems.Center, 40f, 20f),
                (AlignItems.FlexEnd, 80f, 20f),
                (AlignItems.Stretch, 0f, 100f),
            };

            for (var i = 0; i < cases.Length; i++)
            {
                m_RootLayout.style.alignItems = cases[i].alignItems;
                m_RootLayout.MarkLayoutDirty();
                yield return null;

                Assert.That(child.anchoredPosition.y, Is.EqualTo(-cases[i].expectedY).Within(0.02f));
                Assert.That(child.sizeDelta.y, Is.EqualTo(cases[i].expectedHeight).Within(0.02f));
            }
        }

        [UnityTest]
        public IEnumerator Dynamic_ContainerEnums_Refresh_Then_ReadChildCoordinates_ChangesLayout()
        {
            var a = CreateExplicitChild("A", 60f, 20f, PositionType.Relative);
            var b = CreateExplicitChild("B", 80f, 30f, PositionType.Relative);
            var c = CreateExplicitChild("C", 40f, 10f, PositionType.Relative);
            var children = new List<RectTransform> { a, b, c };

            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.justifyContent = JustifyContent.FlexStart;
            m_RootLayout.style.alignItems = AlignItems.FlexStart;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var baseline = Snapshot(children);

            m_RootLayout.style.flexDirection = FlexDirection.Column;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var afterDirection = Snapshot(children);
            Assert.That(afterDirection, Is.Not.EqualTo(baseline));

            m_RootLayout.style.justifyContent = JustifyContent.SpaceAround;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var afterJustify = Snapshot(children);
            Assert.That(afterJustify, Is.Not.EqualTo(afterDirection));

            m_RootLayout.style.alignItems = AlignItems.FlexEnd;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            var afterAlign = Snapshot(children);
            Assert.That(afterAlign, Is.Not.EqualTo(afterJustify));
        }

        [UnityTest]
        public IEnumerator SceneLike_ColumnCenterFlexEnd_ProducesExpectedCoordinates()
        {
            m_RootNode.style.width = FlexValue.Points(700f);
            m_RootNode.style.height = FlexValue.Points(140f);
            m_RootLayout.style.flexDirection = FlexDirection.Column;
            m_RootLayout.style.justifyContent = JustifyContent.Center;
            m_RootLayout.style.alignItems = AlignItems.FlexEnd;
            m_RootLayout.implicitItemDefaults.alignSelf = AlignSelf.Auto;
            m_RootLayout.style.mainGap = 16f;
            m_RootLayout.style.crossGap = 16f;
            m_RootLayout.style.padding = new FlexEdges { left = 16f, right = 16f, top = 12f, bottom = 12f };

            var redImplicit = CreateImplicitChild("RedImplicit", 0f, 0f);
            var greenExplicit = CreateExplicitChild("GreenExplicit", 180f, 70f, PositionType.Relative);
            var greenItem = greenExplicit.GetComponent<FlexItem>();
            greenItem.style.alignSelf = AlignSelf.FlexStart;

            var blueGrow = CreateExplicitChild("BlueGrow", 80f, 40f, PositionType.Relative);
            var blueItem = blueGrow.GetComponent<FlexItem>();
            blueItem.style.flexGrow = 1f;
            blueItem.style.flexShrink = 1f;
            blueItem.style.alignSelf = AlignSelf.Auto;

            var imageImplicit = CreateImplicitChild("Image", 0f, 0f);

            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(redImplicit.anchoredPosition.x, Is.EqualTo(684f).Within(0.03f));
            Assert.That(redImplicit.anchoredPosition.y, Is.EqualTo(-12f).Within(0.03f));
            Assert.That(redImplicit.sizeDelta.x, Is.EqualTo(0f).Within(0.03f));
            Assert.That(redImplicit.sizeDelta.y, Is.EqualTo(0f).Within(0.03f));

            Assert.That(greenExplicit.anchoredPosition.x, Is.EqualTo(16f).Within(0.03f));
            Assert.That(greenExplicit.anchoredPosition.y, Is.EqualTo(-28f).Within(0.03f));
            Assert.That(greenExplicit.sizeDelta.x, Is.EqualTo(180f).Within(0.03f));
            Assert.That(greenExplicit.sizeDelta.y, Is.EqualTo(43.2727f).Within(0.05f));

            Assert.That(blueGrow.anchoredPosition.x, Is.EqualTo(604f).Within(0.03f));
            Assert.That(blueGrow.anchoredPosition.y, Is.EqualTo(-87.2727f).Within(0.05f));
            Assert.That(blueGrow.sizeDelta.x, Is.EqualTo(80f).Within(0.03f));
            Assert.That(blueGrow.sizeDelta.y, Is.EqualTo(24.7273f).Within(0.05f));

            Assert.That(imageImplicit.anchoredPosition.x, Is.EqualTo(684f).Within(0.03f));
            Assert.That(imageImplicit.anchoredPosition.y, Is.EqualTo(-128f).Within(0.05f));
            Assert.That(imageImplicit.sizeDelta.x, Is.EqualTo(0f).Within(0.03f));
            Assert.That(imageImplicit.sizeDelta.y, Is.EqualTo(0f).Within(0.03f));
        }

        [UnityTest]
        public IEnumerator SceneLike_RowReverseSpaceEvenlyCenter_ProducesExpectedCoordinates()
        {
            m_RootNode.style.width = FlexValue.Points(700f);
            m_RootNode.style.height = FlexValue.Points(140f);
            m_RootLayout.style.flexDirection = FlexDirection.RowReverse;
            m_RootLayout.style.justifyContent = JustifyContent.SpaceEvenly;
            m_RootLayout.style.alignItems = AlignItems.Center;
            m_RootLayout.implicitItemDefaults.alignSelf = AlignSelf.Auto;
            m_RootLayout.style.mainGap = 16f;
            m_RootLayout.style.crossGap = 16f;
            m_RootLayout.style.padding = new FlexEdges { left = 16f, right = 16f, top = 12f, bottom = 12f };

            var redImplicit = CreateImplicitChild("RedImplicit", 0f, 0f);
            var greenExplicit = CreateExplicitChild("GreenExplicit", 180f, 70f, PositionType.Relative);
            var greenItem = greenExplicit.GetComponent<FlexItem>();
            greenItem.style.alignSelf = AlignSelf.FlexStart;

            var blueGrow = CreateExplicitChild("BlueGrow", 80f, 40f, PositionType.Relative);
            var blueItem = blueGrow.GetComponent<FlexItem>();
            blueItem.style.flexGrow = 1f;
            blueItem.style.flexShrink = 1f;
            blueItem.style.alignSelf = AlignSelf.Auto;

            var imageImplicit = CreateImplicitChild("Image", 0f, 0f);

            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(redImplicit.anchoredPosition.x, Is.EqualTo(684f).Within(0.03f));
            Assert.That(redImplicit.anchoredPosition.y, Is.EqualTo(-70f).Within(0.03f));
            Assert.That(redImplicit.sizeDelta.x, Is.EqualTo(0f).Within(0.03f));
            Assert.That(redImplicit.sizeDelta.y, Is.EqualTo(0f).Within(0.03f));

            Assert.That(greenExplicit.anchoredPosition.x, Is.EqualTo(488f).Within(0.03f));
            Assert.That(greenExplicit.anchoredPosition.y, Is.EqualTo(-12f).Within(0.03f));
            Assert.That(greenExplicit.sizeDelta.x, Is.EqualTo(180f).Within(0.03f));
            Assert.That(greenExplicit.sizeDelta.y, Is.EqualTo(70f).Within(0.03f));

            Assert.That(blueGrow.anchoredPosition.x, Is.EqualTo(32f).Within(0.03f));
            Assert.That(blueGrow.anchoredPosition.y, Is.EqualTo(-50f).Within(0.03f));
            Assert.That(blueGrow.sizeDelta.x, Is.EqualTo(440f).Within(0.03f));
            Assert.That(blueGrow.sizeDelta.y, Is.EqualTo(40f).Within(0.03f));

            Assert.That(imageImplicit.anchoredPosition.x, Is.EqualTo(16f).Within(0.03f));
            Assert.That(imageImplicit.anchoredPosition.y, Is.EqualTo(-70f).Within(0.03f));
            Assert.That(imageImplicit.sizeDelta.x, Is.EqualTo(0f).Within(0.03f));
            Assert.That(imageImplicit.sizeDelta.y, Is.EqualTo(0f).Within(0.03f));
        }

        [UnityTest]
        public IEnumerator Dynamic_Enum_Coverage_All_Values_Are_Accepted()
        {
            var child = CreateExplicitChild("A", 50f, 20f, PositionType.Relative);
            CreateImplicitChild("B", 50f, 20f);
            yield return null;

            foreach (FlexDirection value in System.Enum.GetValues(typeof(FlexDirection)))
            {
                m_RootLayout.style.flexDirection = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.AreEqual(value, m_RootLayout.style.flexDirection);
            }

            foreach (FlexWrap value in System.Enum.GetValues(typeof(FlexWrap)))
            {
                m_RootLayout.style.flexWrap = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.AreEqual(value, m_RootLayout.style.flexWrap);
            }

            foreach (JustifyContent value in System.Enum.GetValues(typeof(JustifyContent)))
            {
                m_RootLayout.style.justifyContent = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.AreEqual(value, m_RootLayout.style.justifyContent);
            }

            foreach (AlignItems value in System.Enum.GetValues(typeof(AlignItems)))
            {
                m_RootLayout.style.alignItems = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.AreEqual(value, m_RootLayout.style.alignItems);
            }

            var childItem = child.GetComponent<FlexItem>();
            var childNode = child.GetComponent<FlexNode>();
            foreach (AlignSelf value in System.Enum.GetValues(typeof(AlignSelf)))
            {
                childItem.style.alignSelf = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.AreEqual(value, childItem.style.alignSelf);
            }

            foreach (PositionType value in System.Enum.GetValues(typeof(PositionType)))
            {
                childNode.style.positionType = value;
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.AreEqual(value, childNode.style.positionType);
            }

            foreach (FlexSizeMode value in System.Enum.GetValues(typeof(FlexSizeMode)))
            {
                m_RootNode.style.width = CreateValueForMode(value, 300f, 100f);
                m_RootNode.style.height = CreateValueForMode(value, 100f, 100f);
                childItem.style.flexBasis = CreateValueForMode(value, 50f, 25f);
                m_RootLayout.MarkLayoutDirty();
                yield return null;
                Assert.AreEqual(value, m_RootNode.style.width.mode);
                Assert.AreEqual(value, m_RootNode.style.height.mode);
                Assert.AreEqual(value, childItem.style.flexBasis.mode);
            }
        }

        [UnityTest]
        public IEnumerator Dynamic_Field_Coverage_All_Style_Fields_Affect_Runtime()
        {
            var a = CreateExplicitChild("A", 50f, 20f, PositionType.Relative);
            var b = CreateExplicitChild("B", 50f, 20f, PositionType.Relative);
            var aNode = a.GetComponent<FlexNode>();
            var aItem = a.GetComponent<FlexItem>();
            var bNode = b.GetComponent<FlexNode>();
            var bItem = b.GetComponent<FlexItem>();

            m_RootNode.style.width = FlexValue.Points(400f);
            m_RootNode.style.height = FlexValue.Points(200f);
            m_RootNode.style.minWidth = FlexOptionalFloat.Enabled(350f);
            m_RootNode.style.maxWidth = FlexOptionalFloat.Enabled(450f);
            m_RootNode.style.minHeight = FlexOptionalFloat.Enabled(150f);
            m_RootNode.style.maxHeight = FlexOptionalFloat.Enabled(250f);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.WrapReverse;
            m_RootLayout.style.justifyContent = JustifyContent.SpaceEvenly;
            m_RootLayout.style.alignItems = AlignItems.Center;
            m_RootLayout.style.alignContent = AlignContent.FlexStart;
            m_RootLayout.style.mainGap = 10f;
            m_RootLayout.style.crossGap = 10f;
            m_RootLayout.style.padding = new FlexEdges { left = 20f, right = 30f, top = 15f, bottom = 25f };

            aNode.style.width = FlexValue.Points(60f);
            aNode.style.height = FlexValue.Points(30f);
            aNode.style.minWidth = FlexOptionalFloat.Enabled(55f);
            aNode.style.maxWidth = FlexOptionalFloat.Enabled(80f);
            aNode.style.minHeight = FlexOptionalFloat.Enabled(25f);
            aNode.style.maxHeight = FlexOptionalFloat.Enabled(40f);
            aNode.style.positionType = PositionType.Relative;
            aItem.style.flexGrow = 1f;
            aItem.style.flexShrink = 1f;
            aItem.style.flexBasis = FlexValue.Points(70f);
            aItem.style.alignSelf = AlignSelf.FlexStart;

            bNode.style.width = FlexValue.Points(40f);
            bNode.style.height = FlexValue.Points(20f);
            bNode.style.positionType = PositionType.Relative;
            bItem.style.flexGrow = 2f;
            bItem.style.flexShrink = 1f;
            bItem.style.flexBasis = FlexValue.Auto();
            bItem.style.alignSelf = AlignSelf.FlexEnd;

            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(m_RootRect.sizeDelta.x, Is.EqualTo(400f).Within(0.02f));
            Assert.That(m_RootRect.sizeDelta.y, Is.EqualTo(200f).Within(0.02f));
            Assert.That(a.sizeDelta.x, Is.GreaterThanOrEqualTo(55f));
            Assert.That(a.sizeDelta.x, Is.LessThanOrEqualTo(80f));
            Assert.That(a.sizeDelta.y, Is.GreaterThanOrEqualTo(25f));
            Assert.That(a.sizeDelta.y, Is.LessThanOrEqualTo(40f));
            Assert.That(a.anchoredPosition.y, Is.EqualTo(-145f).Within(0.02f));
            Assert.That(b.anchoredPosition.y, Is.LessThan(a.anchoredPosition.y));

            aNode.style.positionType = PositionType.Absolute;
            var before = a.anchoredPosition;
            a.anchoredPosition = new Vector2(111f, -77f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(a.anchoredPosition.x, Is.EqualTo(111f).Within(0.02f));
            Assert.That(a.anchoredPosition.y, Is.EqualTo(-77f).Within(0.02f));
            Assert.That(a.anchoredPosition, Is.Not.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator Dynamic_Percent_NodeSize_Resolves_Against_ParentInnerSize()
        {
            var child = CreateExplicitChild("PercentNode", 20f, 20f, PositionType.Relative);
            var childNode = child.GetComponent<FlexNode>();
            var childItem = child.GetComponent<FlexItem>();
            childItem.style.flexGrow = 0f;
            childItem.style.flexShrink = 0f;
            childItem.style.flexBasis = FlexValue.Auto();
            childNode.style.width = FlexValue.Percent(50f);
            childNode.style.height = FlexValue.Points(20f);

            m_RootNode.style.width = FlexValue.Points(300f);
            m_RootNode.style.height = FlexValue.Points(100f);
            m_RootLayout.style.padding = FlexEdges.Zero;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.sizeDelta.x, Is.EqualTo(150f).Within(0.02f));
            Assert.That(child.sizeDelta.y, Is.EqualTo(20f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Dynamic_Percent_FlexBasis_Resolves_Against_ParentMainAxis()
        {
            var a = CreateExplicitChild("A", 20f, 20f, PositionType.Relative);
            var b = CreateExplicitChild("B", 20f, 20f, PositionType.Relative);
            var aItem = a.GetComponent<FlexItem>();
            var bItem = b.GetComponent<FlexItem>();
            var aNode = a.GetComponent<FlexNode>();
            var bNode = b.GetComponent<FlexNode>();

            aNode.style.width = FlexValue.Auto();
            bNode.style.width = FlexValue.Auto();
            aItem.style.flexGrow = 0f;
            bItem.style.flexGrow = 0f;
            aItem.style.flexShrink = 0f;
            bItem.style.flexShrink = 0f;
            aItem.style.flexBasis = FlexValue.Percent(25f);
            bItem.style.flexBasis = FlexValue.Percent(50f);

            m_RootNode.style.width = FlexValue.Points(300f);
            m_RootLayout.style.padding = FlexEdges.Zero;
            m_RootLayout.style.mainGap = 0f;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(a.sizeDelta.x, Is.EqualTo(75f).Within(0.02f));
            Assert.That(b.sizeDelta.x, Is.EqualTo(150f).Within(0.02f));
            Assert.That(b.anchoredPosition.x, Is.EqualTo(75f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Dynamic_FlexWrap_Field_Updates_Without_Runtime_Errors()
        {
            CreateImplicitChild("A", 180f, 20f);
            CreateImplicitChild("B", 180f, 20f);
            yield return null;

            m_RootLayout.style.flexWrap = FlexWrap.NoWrap;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            Assert.AreEqual(FlexWrap.NoWrap, m_RootLayout.style.flexWrap);

            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            Assert.AreEqual(FlexWrap.Wrap, m_RootLayout.style.flexWrap);

            m_RootLayout.style.flexWrap = FlexWrap.WrapReverse;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            Assert.AreEqual(FlexWrap.WrapReverse, m_RootLayout.style.flexWrap);
        }

        [UnityTest]
        public IEnumerator Dynamic_Wrap_Reflows_When_Container_Size_Changes()
        {
            var a = CreateImplicitChild("A", 60f, 20f);
            var b = CreateImplicitChild("B", 60f, 20f);
            var c = CreateImplicitChild("C", 60f, 20f);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootLayout.style.alignContent = AlignContent.FlexStart;
            m_RootNode.style.width = FlexValue.Points(200f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(c.anchoredPosition.y, Is.EqualTo(0f).Within(0.02f));

            m_RootNode.style.width = FlexValue.Points(120f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(b.anchoredPosition.y, Is.EqualTo(0f).Within(0.02f));
            Assert.That(c.anchoredPosition.y, Is.EqualTo(-20f).Within(0.02f));
            Assert.That(a.anchoredPosition.x, Is.EqualTo(0f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Dynamic_Wrap_Absolute_Child_Does_Not_Consume_Line_Space()
        {
            var a = CreateExplicitChild("A", 60f, 20f, PositionType.Relative);
            var abs = CreateExplicitChild("Abs", 100f, 20f, PositionType.Absolute);
            var b = CreateExplicitChild("B", 60f, 20f, PositionType.Relative);
            m_RootLayout.style.flexDirection = FlexDirection.Row;
            m_RootLayout.style.flexWrap = FlexWrap.Wrap;
            m_RootNode.style.width = FlexValue.Points(130f);
            abs.anchoredPosition = new Vector2(77f, -9f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(a.anchoredPosition.y, Is.EqualTo(0f).Within(0.02f));
            Assert.That(b.anchoredPosition.y, Is.EqualTo(0f).Within(0.02f));
            Assert.That(b.anchoredPosition.x, Is.EqualTo(60f).Within(0.02f));
            Assert.That(abs.anchoredPosition.x, Is.EqualTo(77f).Within(0.02f));
            Assert.That(abs.anchoredPosition.y, Is.EqualTo(-9f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Dynamic_Remove_And_ReAdd_FlexLayout_Component_Updates_Behavior()
        {
            var child = CreateExplicitChild("A", 120f, 30f, PositionType.Relative);
            yield return null;

            Assert.That(child.sizeDelta.x, Is.EqualTo(120f).Within(0.02f));
            var layout = child.GetComponent<FlexLayout>();
            Object.Destroy(layout);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.sizeDelta.x, Is.EqualTo(120f).Within(0.02f));

            var readded = child.gameObject.AddComponent<FlexLayout>();
            var readdedNode = EnsureNode(child);
            readdedNode.style.width = FlexValue.Points(70f);
            readdedNode.style.height = FlexValue.Points(25f);
            readdedNode.style.positionType = PositionType.Relative;
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.sizeDelta.x, Is.EqualTo(70f).Within(0.02f));
            Assert.That(child.sizeDelta.y, Is.EqualTo(25f).Within(0.02f));
        }

        public IEnumerator Dynamic_MinMax_Constraint_Update_Is_Applied()
        {
            var child = CreateExplicitChild("A", 50f, 20f, PositionType.Relative);
            var childNode = child.GetComponent<FlexNode>();
            childNode.style.maxWidth = FlexOptionalFloat.Enabled(40f);
            childNode.style.minHeight = FlexOptionalFloat.Enabled(60f);
            m_RootLayout.MarkLayoutDirty();
            yield return null;

            Assert.That(child.sizeDelta.x, Is.EqualTo(40f).Within(0.02f));
            Assert.That(child.sizeDelta.y, Is.EqualTo(60f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Dynamic_Enable_Disable_RootLayout_Reapplies_When_Reenabled()
        {
            var child = CreateImplicitChild("A", 100f, 20f);
            yield return null;
            child.anchoredPosition = new Vector2(25f, -10f);

            m_RootLayout.enabled = false;
            yield return null;
            Assert.That(child.anchoredPosition.x, Is.EqualTo(25f).Within(0.01f));

            m_RootLayout.enabled = true;
            m_RootLayout.MarkLayoutDirty();
            yield return null;
            Assert.That(child.anchoredPosition.x, Is.EqualTo(0f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Matrix_All_Enum_Combinations_Produce_Stable_Finite_Layout()
        {
            var children = new List<RectTransform>
            {
                CreateExplicitChild("A", 40f, 20f, PositionType.Relative),
                CreateExplicitChild("B", 60f, 25f, PositionType.Relative),
                CreateExplicitChild("C", 30f, 15f, PositionType.Relative),
            };

            var items = new List<FlexItem>();
            var nodes = new List<FlexNode>();
            for (var i = 0; i < children.Count; i++)
            {
                nodes.Add(children[i].GetComponent<FlexNode>());
                items.Add(children[i].GetComponent<FlexItem>());
            }

            var scenarioCount = 0;
            foreach (FlexDirection direction in System.Enum.GetValues(typeof(FlexDirection)))
            {
                foreach (JustifyContent justify in System.Enum.GetValues(typeof(JustifyContent)))
                {
                    foreach (AlignItems alignItems in System.Enum.GetValues(typeof(AlignItems)))
                    {
                        foreach (FlexWrap wrap in System.Enum.GetValues(typeof(FlexWrap)))
                        {
                            foreach (AlignSelf alignSelf in System.Enum.GetValues(typeof(AlignSelf)))
                            {
                                m_RootLayout.style.flexDirection = direction;
                                m_RootLayout.style.justifyContent = justify;
                                m_RootLayout.style.alignItems = alignItems;
                                m_RootLayout.style.flexWrap = wrap;
                                m_RootLayout.style.mainGap = 5f;
                                m_RootLayout.style.crossGap = 5f;
                                m_RootLayout.style.padding = new FlexEdges { left = 3f, right = 7f, top = 11f, bottom = 13f };

                                for (var i = 0; i < items.Count; i++)
                                {
                                    items[i].style.alignSelf = alignSelf;
                                    nodes[i].style.positionType = PositionType.Relative;
                                }

                                m_RootLayout.MarkLayoutDirty();
                                AssertChildrenFinite(children);
                                var snapshot1 = Snapshot(children);

                                m_RootLayout.MarkLayoutDirty();
                                AssertChildrenFinite(children);
                                var snapshot2 = Snapshot(children);

                                AssertSnapshotsEqual(snapshot1, snapshot2, 0.01f);
                                scenarioCount++;
                                if (scenarioCount % 120 == 0)
                                {
                                    yield return null;
                                }
                            }
                        }
                    }
                }
            }

            Assert.That(scenarioCount, Is.GreaterThan(1000));
        }

        [UnityTest]
        public IEnumerator Fuzz_All_Style_Fields_With_Dynamic_Mutations_Remains_Sane()
        {
            var random = new System.Random(12345);
            var children = new List<RectTransform>();

            for (var i = 0; i < 4; i++)
            {
                children.Add(CreateExplicitChild("F" + i, 20f + i * 10f, 15f + i * 5f, PositionType.Relative));
            }

            for (var step = 0; step < 320; step++)
            {
                m_RootNode.style.width = FlexValue.Points(120f + random.Next(0, 480));
                m_RootNode.style.height = FlexValue.Points(80f + random.Next(0, 260));
                m_RootNode.style.minWidth = random.NextDouble() > 0.5 ? FlexOptionalFloat.Enabled(60f + random.Next(0, 120)) : FlexOptionalFloat.Disabled();
                m_RootNode.style.maxWidth = random.NextDouble() > 0.5 ? FlexOptionalFloat.Enabled(220f + random.Next(0, 520)) : FlexOptionalFloat.Disabled();
                m_RootNode.style.minHeight = random.NextDouble() > 0.5 ? FlexOptionalFloat.Enabled(40f + random.Next(0, 80)) : FlexOptionalFloat.Disabled();
                m_RootNode.style.maxHeight = random.NextDouble() > 0.5 ? FlexOptionalFloat.Enabled(120f + random.Next(0, 300)) : FlexOptionalFloat.Disabled();
                m_RootLayout.style.flexDirection = (FlexDirection)random.Next(0, System.Enum.GetValues(typeof(FlexDirection)).Length);
                m_RootLayout.style.flexWrap = (FlexWrap)random.Next(0, System.Enum.GetValues(typeof(FlexWrap)).Length);
                m_RootLayout.style.justifyContent = (JustifyContent)random.Next(0, System.Enum.GetValues(typeof(JustifyContent)).Length);
                m_RootLayout.style.alignItems = (AlignItems)random.Next(0, System.Enum.GetValues(typeof(AlignItems)).Length);
                var gapValue = random.Next(0, 30);
                m_RootLayout.style.mainGap = gapValue;
                m_RootLayout.style.crossGap = gapValue;
                m_RootLayout.style.padding = new FlexEdges
                {
                    left = random.Next(0, 20),
                    right = random.Next(0, 20),
                    top = random.Next(0, 20),
                    bottom = random.Next(0, 20),
                };

                if (random.NextDouble() < 0.2)
                {
                    var added = CreateImplicitChild("Dyn_" + step, 20f + random.Next(0, 60), 20f + random.Next(0, 50));
                    children.Add(added);
                }

                if (children.Count > 2 && random.NextDouble() < 0.15)
                {
                    var removeIndex = random.Next(0, children.Count);
                    var toRemove = children[removeIndex];
                    children.RemoveAt(removeIndex);
                    if (toRemove != null)
                    {
                        Object.Destroy(toRemove.gameObject);
                    }
                }

                for (var i = 0; i < children.Count; i++)
                {
                    var rect = children[i];
                    if (rect == null)
                    {
                        continue;
                    }

                    if (random.NextDouble() < 0.08)
                    {
                        rect.gameObject.SetActive(!rect.gameObject.activeSelf);
                    }

                    var childLayout = rect.GetComponent<FlexLayout>();
                    if (childLayout == null && random.NextDouble() < 0.35)
                    {
                        childLayout = rect.gameObject.AddComponent<FlexLayout>();
                    }

                    var childNode = rect.GetComponent<FlexNode>();
                    if (childNode == null && random.NextDouble() < 0.35)
                    {
                        childNode = rect.gameObject.AddComponent<FlexNode>();
                    }

                    var childItem = rect.GetComponent<FlexItem>();
                    if (childItem == null && random.NextDouble() < 0.35)
                    {
                        childItem = rect.gameObject.AddComponent<FlexItem>();
                    }

                    if (childNode != null)
                    {
                        childNode.style.width = CreateRandomFlexValue(random, 10f, 130f, 10f, 90f);
                        childNode.style.height = CreateRandomFlexValue(random, 10f, 100f, 10f, 90f);
                        childNode.style.minWidth = random.NextDouble() < 0.5 ? FlexOptionalFloat.Enabled(5f + random.Next(0, 40)) : FlexOptionalFloat.Disabled();
                        childNode.style.maxWidth = random.NextDouble() < 0.5 ? FlexOptionalFloat.Enabled(40f + random.Next(0, 120)) : FlexOptionalFloat.Disabled();
                        childNode.style.minHeight = random.NextDouble() < 0.5 ? FlexOptionalFloat.Enabled(5f + random.Next(0, 40)) : FlexOptionalFloat.Disabled();
                        childNode.style.maxHeight = random.NextDouble() < 0.5 ? FlexOptionalFloat.Enabled(40f + random.Next(0, 120)) : FlexOptionalFloat.Disabled();
                        childNode.style.positionType = (PositionType)random.Next(0, System.Enum.GetValues(typeof(PositionType)).Length);
                    }

                    if (childItem != null)
                    {
                        childItem.style.flexGrow = random.Next(0, 4);
                        childItem.style.flexShrink = random.Next(0, 4);
                        childItem.style.flexBasis = CreateRandomFlexValue(random, 10f, 110f, 10f, 90f);
                        childItem.style.alignSelf = (AlignSelf)random.Next(0, System.Enum.GetValues(typeof(AlignSelf)).Length);
                    }

                    if (childLayout != null && random.NextDouble() < 0.05)
                    {
                        childLayout.enabled = !childLayout.enabled;
                    }

                }

                m_RootLayout.MarkLayoutDirty();
                AssertRectFinite(m_RootRect);
                for (var i = 0; i < children.Count; i++)
                {
                    if (children[i] != null)
                    {
                        AssertRectFinite(children[i]);
                    }
                }

                if (step % 20 == 0)
                {
                    yield return null;
                }
            }
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

        private static FlexNode EnsureNode(RectTransform rect)
        {
            return rect.TryGetComponent<FlexNode>(out var node)
                ? node
                : rect.gameObject.AddComponent<FlexNode>();
        }

        private static FlexItem EnsureItem(RectTransform rect)
        {
            return rect.TryGetComponent<FlexItem>(out var item)
                ? item
                : rect.gameObject.AddComponent<FlexItem>();
        }

        private static void AssertRectFinite(RectTransform rect)
        {
            var pos = rect.anchoredPosition;
            var size = rect.sizeDelta;
            Assert.That(float.IsNaN(pos.x) || float.IsInfinity(pos.x), Is.False);
            Assert.That(float.IsNaN(pos.y) || float.IsInfinity(pos.y), Is.False);
            Assert.That(float.IsNaN(size.x) || float.IsInfinity(size.x), Is.False);
            Assert.That(float.IsNaN(size.y) || float.IsInfinity(size.y), Is.False);
        }

        private static void AssertChildrenFinite(IReadOnlyList<RectTransform> children)
        {
            for (var i = 0; i < children.Count; i++)
            {
                AssertRectFinite(children[i]);
            }
        }

        private static Vector4[] Snapshot(IReadOnlyList<RectTransform> children)
        {
            var snapshot = new Vector4[children.Count];
            for (var i = 0; i < children.Count; i++)
            {
                var rect = children[i];
                snapshot[i] = new Vector4(rect.anchoredPosition.x, rect.anchoredPosition.y, rect.sizeDelta.x, rect.sizeDelta.y);
            }

            return snapshot;
        }

        private static void AssertSnapshotsEqual(IReadOnlyList<Vector4> lhs, IReadOnlyList<Vector4> rhs, float tolerance)
        {
            Assert.AreEqual(lhs.Count, rhs.Count);
            for (var i = 0; i < lhs.Count; i++)
            {
                Assert.That(lhs[i].x, Is.EqualTo(rhs[i].x).Within(tolerance));
                Assert.That(lhs[i].y, Is.EqualTo(rhs[i].y).Within(tolerance));
                Assert.That(lhs[i].z, Is.EqualTo(rhs[i].z).Within(tolerance));
                Assert.That(lhs[i].w, Is.EqualTo(rhs[i].w).Within(tolerance));
            }
        }

        private static FlexValue CreateValueForMode(FlexSizeMode mode, float pointsValue, float percentValue)
        {
            switch (mode)
            {
                case FlexSizeMode.Auto:
                    return FlexValue.Auto();
                case FlexSizeMode.Percent:
                    return FlexValue.Percent(percentValue);
                default:
                    return FlexValue.Points(pointsValue);
            }
        }

        private static FlexValue CreateRandomFlexValue(System.Random random, float minPoints, float maxPoints, float minPercent, float maxPercent)
        {
            var modeRoll = random.NextDouble();
            if (modeRoll < 0.33)
            {
                return FlexValue.Auto();
            }

            if (modeRoll < 0.66)
            {
                return FlexValue.Points(Mathf.Lerp(minPoints, maxPoints, (float)random.NextDouble()));
            }

            return FlexValue.Percent(Mathf.Lerp(minPercent, maxPercent, (float)random.NextDouble()));
        }
    }
}
