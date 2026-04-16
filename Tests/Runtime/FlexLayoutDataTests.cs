using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityEngine.UI.Flex.Core;
using UnityEngine.UIElements;
using FlexItem = UnityEngine.UI.Flex.FlexItem;
using FlexNodeComponent = UnityEngine.UI.Flex.FlexNode;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace UnityEngine.UI.Flex.Tests
{
    public class FlexLayoutDataTests
    {
        [Test]
        public void Default_Style_Uses_Discussed_Flex_Baseline()
        {
            var style = FlexStyle.Default;

            Assert.AreEqual(FlexSizeMode.Auto, style.width.mode);
            Assert.AreEqual(FlexSizeMode.Auto, style.height.mode);
            Assert.AreEqual(0f, style.flexGrow);
            Assert.AreEqual(1f, style.flexShrink);
            Assert.AreEqual(FlexSizeMode.Auto, style.flexBasis.mode);
            Assert.AreEqual(AlignSelf.Auto, style.alignSelf);
            Assert.AreEqual(PositionType.Relative, style.positionType);
            Assert.AreEqual(FlexDirection.Row, style.flexDirection);
            Assert.AreEqual(FlexWrap.NoWrap, style.flexWrap);
            Assert.AreEqual(JustifyContent.FlexStart, style.justifyContent);
            Assert.AreEqual(AlignItems.Stretch, style.alignItems);
            Assert.AreEqual(0f, style.mainGap);
            Assert.AreEqual(0f, style.crossGap);
        }

        [Test]
        public void FlexValue_Factory_Methods_Create_Expected_Modes()
        {
            var auto = FlexValue.Auto();
            var points = FlexValue.Points(64f);
            var percent = FlexValue.Percent(50f);

            Assert.AreEqual(FlexSizeMode.Auto, auto.mode);
            Assert.AreEqual(0f, auto.value);
            Assert.AreEqual(FlexSizeMode.Points, points.mode);
            Assert.AreEqual(64f, points.value);
            Assert.AreEqual(FlexSizeMode.Percent, percent.mode);
            Assert.AreEqual(50f, percent.value);
        }

        [Test]
        public void MeasurePass_SingleLine_Reuses_PreparedFlow_And_Measure_Cache()
        {
            var store = new FlexNodeStore();
            var rootId = CreateTestContainer(store, FlexDirection.Row, FlexWrap.NoWrap);
            var childId = CreateTestLeaf(store, rootId, 40f, 20f);
            CreateTestLeaf(store, rootId, 60f, 24f);
            CreateTestLeaf(store, rootId, 50f, 18f);

            using (FlexMeasure.BeginMeasurePass())
            {
                FlexMeasure.MeasureSubtree(store, childId);
                FlexMeasure.MeasureSubtree(store, childId);
                FlexMeasure.AllocateMainAxisSizes(store, rootId, 300f);
                FlexMeasure.MeasureItemLayouts(store, rootId, 300f, 120f);
            }

            var stats = FlexMeasure.GetLastCompletedMeasurePassStatisticsForTesting();
            Assert.That(stats.PreparedFlowRequests, Is.EqualTo(3));
            Assert.That(stats.PreparedFlowHits, Is.EqualTo(2));
            Assert.That(stats.MeasureSubtreeHits, Is.GreaterThan(0));
        }

        [Test]
        public void MeasurePass_Basis_Cache_Is_Reused_Across_Flow_And_Wrap_Preparation()
        {
            var store = new FlexNodeStore();
            var rootId = CreateTestContainer(store, FlexDirection.Row, FlexWrap.Wrap);
            CreateTestLeaf(store, rootId, 80f, 20f);
            CreateTestLeaf(store, rootId, 90f, 24f);
            CreateTestLeaf(store, rootId, 70f, 18f);

            using (FlexMeasure.BeginMeasurePass())
            {
                FlexMeasure.AllocateMainAxisSizes(store, rootId, 180f);
                FlexMeasure.BuildLines(store, rootId, 180f);
            }

            var stats = FlexMeasure.GetLastCompletedMeasurePassStatisticsForTesting();
            Assert.That(stats.MainAxisBasisRequests, Is.GreaterThan(0));
            Assert.That(stats.MainAxisBasisHits, Is.GreaterThan(0));
        }

        [Test]
        public void MeasurePass_PreparedFlow_Cache_Key_Differs_By_Available_Main_Size()
        {
            var store = new FlexNodeStore();
            var rootId = CreateTestContainer(store, FlexDirection.Row, FlexWrap.NoWrap);
            CreateTestLeaf(store, rootId, 40f, 20f);
            CreateTestLeaf(store, rootId, 60f, 24f);
            CreateTestLeaf(store, rootId, 50f, 18f);

            using (FlexMeasure.BeginMeasurePass())
            {
                FlexMeasure.AllocateMainAxisSizes(store, rootId, 300f);
                FlexMeasure.AllocateMainAxisSizes(store, rootId, 280f);
            }

            var stats = FlexMeasure.GetLastCompletedMeasurePassStatisticsForTesting();
            Assert.That(stats.PreparedFlowRequests, Is.EqualTo(2));
            Assert.That(stats.PreparedFlowHits, Is.EqualTo(0));
            Assert.That(stats.MeasureSubtreeHits, Is.GreaterThan(0));
            Assert.That(stats.MainAxisBasisHits, Is.EqualTo(0));
        }

        [Test]
        public void MeasurePass_Wrap_Reuses_Line_And_Prepared_Line_Cache()
        {
            var store = new FlexNodeStore();
            var rootId = CreateTestContainer(store, FlexDirection.Row, FlexWrap.Wrap);
            CreateTestLeaf(store, rootId, 80f, 20f);
            CreateTestLeaf(store, rootId, 90f, 24f);
            CreateTestLeaf(store, rootId, 70f, 18f);

            using (FlexMeasure.BeginMeasurePass())
            {
                FlexMeasure.BuildLines(store, rootId, 180f);
                FlexMeasure.BuildLines(store, rootId, 180f);
                FlexMeasure.Arrange(store, rootId, 180f, 120f);
            }

            var stats = FlexMeasure.GetLastCompletedMeasurePassStatisticsForTesting();
            Assert.That(stats.LineBuildRequests, Is.EqualTo(2));
            Assert.That(stats.LineBuildHits, Is.EqualTo(1));
            Assert.That(stats.PreparedWrapLineRequests, Is.EqualTo(2));
            Assert.That(stats.PreparedWrapLineHits, Is.EqualTo(1));
        }

        [Test]
        public void MeasurePass_Statistics_Reset_Between_Passes()
        {
            var store = new FlexNodeStore();
            var rootId = CreateTestContainer(store, FlexDirection.Row, FlexWrap.NoWrap);
            var childId = CreateTestLeaf(store, rootId, 40f, 20f);

            using (FlexMeasure.BeginMeasurePass())
            {
                FlexMeasure.MeasureSubtree(store, childId);
                FlexMeasure.MeasureSubtree(store, childId);
            }

            var firstPass = FlexMeasure.GetLastCompletedMeasurePassStatisticsForTesting();
            Assert.That(firstPass.MeasureSubtreeRequests, Is.EqualTo(2));
            Assert.That(firstPass.MeasureSubtreeHits, Is.EqualTo(1));

            using (FlexMeasure.BeginMeasurePass())
            {
                FlexMeasure.MeasureSubtree(store, childId);
            }

            var secondPass = FlexMeasure.GetLastCompletedMeasurePassStatisticsForTesting();
            Assert.That(secondPass.MeasureSubtreeRequests, Is.EqualTo(1));
            Assert.That(secondPass.MeasureSubtreeHits, Is.EqualTo(0));
        }

        [Test]
        public void FlexLayout_Default_ImplicitItemDefaults_Are_ZeroZeroAutoCenter()
        {
            var go = new GameObject("Layout", typeof(RectTransform), typeof(FlexLayout));
            var layout = go.GetComponent<FlexLayout>();

            Assert.AreEqual(FlexSizeMode.Auto, layout.implicitItemDefaults.width.mode);
            Assert.AreEqual(FlexSizeMode.Auto, layout.implicitItemDefaults.height.mode);
            Assert.AreEqual(0f, layout.implicitItemDefaults.flexGrow);
            Assert.AreEqual(0f, layout.implicitItemDefaults.flexShrink);
            Assert.AreEqual(FlexSizeMode.Auto, layout.implicitItemDefaults.flexBasis.mode);
            Assert.AreEqual(AlignSelf.Center, layout.implicitItemDefaults.alignSelf);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FlexNode_Reset_Initializes_Points_From_RectTransform()
        {
            var go = new GameObject("FlexNode", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(320f, 120f);

            var node = go.AddComponent<FlexNodeComponent>();
            var reset = typeof(FlexNodeComponent).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);

            rect.sizeDelta = new Vector2(320f, 120f);
            reset.Invoke(node, null);

            Assert.AreEqual(FlexSizeMode.Points, node.style.width.mode);
            Assert.AreEqual(FlexSizeMode.Points, node.style.height.mode);
            Assert.AreEqual(320f, node.style.width.value);
            Assert.AreEqual(120f, node.style.height.value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FlexNode_Awake_Initializes_Default_WidthHeight_Points_From_RectTransform()
        {
            var go = new GameObject("FlexNode", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 140f);

            var node = go.AddComponent<FlexNodeComponent>();
            node.style = UnityEngine.UI.Flex.FlexNodeStyleData.Default;
            rect.sizeDelta = new Vector2(360f, 140f);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var awake = typeof(FlexNodeComponent).GetMethod("Awake", flags);
            Assert.NotNull(awake);
            awake.Invoke(node, null);

            Assert.AreEqual(FlexSizeMode.Points, node.style.width.mode);
            Assert.AreEqual(FlexSizeMode.Points, node.style.height.mode);
            Assert.AreEqual(360f, node.style.width.value);
            Assert.AreEqual(140f, node.style.height.value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FlexNode_Awake_Initializes_Default_WidthHeight_From_SizeDelta_When_Rect_Is_Zero()
        {
            var go = new GameObject("FlexNode", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.sizeDelta = new Vector2(100f, 100f);

            var node = go.AddComponent<FlexNodeComponent>();
            node.style = UnityEngine.UI.Flex.FlexNodeStyleData.Default;
            rect.sizeDelta = new Vector2(100f, 100f);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var awake = typeof(FlexNodeComponent).GetMethod("Awake", flags);
            Assert.NotNull(awake);
            awake.Invoke(node, null);

            Assert.AreEqual(FlexSizeMode.Points, node.style.width.mode);
            Assert.AreEqual(FlexSizeMode.Points, node.style.height.mode);
            Assert.AreEqual(100f, node.style.width.value);
            Assert.AreEqual(100f, node.style.height.value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FlexNode_OnEnable_Initializes_AspectRatio_Value_From_Rect_When_Zero()
        {
            var go = new GameObject("FlexNode", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 120f);

            var node = go.AddComponent<FlexNodeComponent>();
            node.style.aspectRatio = FlexOptionalFloat.Disabled();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var onEnable = typeof(FlexNodeComponent).GetMethod("OnEnable", flags);
            Assert.NotNull(onEnable);
            onEnable.Invoke(node, null);

            Assert.That(node.style.aspectRatio.value, Is.EqualTo(2.5f).Within(0.0001f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolvedNode_Implicit_Uses_Contextual_Node_And_Item_Defaults()
        {
            var go = new GameObject("Implicit", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 80f);

            var resolved = FlexResolvedNodeResolver.Resolve(rect, rect.sizeDelta);

            Assert.IsFalse(resolved.HasExplicitLayoutComponent);
            Assert.IsFalse(resolved.Node.HasExplicitAuthoring);
            Assert.IsFalse(resolved.Item.HasExplicitAuthoring);
            Assert.IsFalse(resolved.Container.IsContainer);
            Assert.AreEqual(FlexSizeMode.Auto, resolved.Node.Width.mode);
            Assert.AreEqual(FlexSizeMode.Auto, resolved.Node.Height.mode);
            Assert.AreEqual(PositionType.Relative, resolved.Node.PositionType);
            Assert.AreEqual(0f, resolved.Item.FlexGrow);
            Assert.AreEqual(0f, resolved.Item.FlexShrink);
            Assert.AreEqual(FlexSizeMode.Auto, resolved.Item.FlexBasis.mode);
            Assert.AreEqual(AlignSelf.Center, resolved.Item.AlignSelf);
            Assert.AreEqual(new Vector2(120f, 80f), resolved.ImplicitRectSize);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolvedNode_ExplicitNode_Item_And_Layout_Split_Into_Three_Slices()
        {
            var go = new GameObject("Explicit", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 100f);

            var layout = go.AddComponent<FlexLayout>();
            layout.style.flexDirection = FlexDirection.Column;
            layout.style.flexWrap = FlexWrap.WrapReverse;
            layout.style.justifyContent = JustifyContent.SpaceAround;
            layout.style.alignItems = AlignItems.FlexEnd;
            layout.style.alignContent = AlignContent.SpaceEvenly;
            layout.style.mainGap = 12f;
            layout.style.crossGap = 18f;
            layout.style.padding = new FlexEdges { left = 1f, right = 2f, top = 3f, bottom = 4f };
            var node = go.AddComponent<FlexNodeComponent>();
            node.style.width = FlexValue.Points(320f);
            node.style.height = FlexValue.Auto();
            node.style.minWidth = FlexOptionalFloat.Enabled(64f);
            node.style.maxHeight = FlexOptionalFloat.Enabled(240f);
            node.style.positionType = PositionType.Absolute;
            var item = go.AddComponent<FlexItem>();
            item.style.flexGrow = 2f;
            item.style.flexShrink = 3f;
            item.style.flexBasis = FlexValue.Points(90f);
            item.style.alignSelf = AlignSelf.Center;

            var resolved = FlexResolvedNodeResolver.Resolve(rect, rect.sizeDelta);

            Assert.IsTrue(resolved.HasExplicitLayoutComponent);
            Assert.IsTrue(resolved.Node.HasExplicitAuthoring);
            Assert.IsTrue(resolved.Item.HasExplicitAuthoring);
            Assert.IsTrue(resolved.Container.IsContainer);
            Assert.IsTrue(resolved.Container.HasExplicitAuthoring);
            Assert.AreEqual(320f, resolved.Node.Width.value);
            Assert.AreEqual(FlexSizeMode.Auto, resolved.Node.Height.mode);
            Assert.IsTrue(resolved.Node.MinWidth.enabled);
            Assert.AreEqual(64f, resolved.Node.MinWidth.value);
            Assert.IsTrue(resolved.Node.MaxHeight.enabled);
            Assert.AreEqual(240f, resolved.Node.MaxHeight.value);
            Assert.AreEqual(PositionType.Absolute, resolved.Node.PositionType);
            Assert.AreEqual(2f, resolved.Item.FlexGrow);
            Assert.AreEqual(3f, resolved.Item.FlexShrink);
            Assert.AreEqual(90f, resolved.Item.FlexBasis.value);
            Assert.AreEqual(AlignSelf.Center, resolved.Item.AlignSelf);
            Assert.AreEqual(FlexDirection.Column, resolved.Container.FlexDirection);
            Assert.AreEqual(FlexWrap.WrapReverse, resolved.Container.FlexWrap);
            Assert.AreEqual(JustifyContent.SpaceAround, resolved.Container.JustifyContent);
            Assert.AreEqual(AlignItems.FlexEnd, resolved.Container.AlignItems);
            Assert.AreEqual(AlignContent.SpaceEvenly, resolved.Container.AlignContent);
            Assert.AreEqual(12f, resolved.Container.MainGap);
            Assert.AreEqual(18f, resolved.Container.CrossGap);
            Assert.AreEqual(1f, resolved.Container.Padding.left);
            Assert.AreEqual(2f, resolved.Container.Padding.right);
            Assert.AreEqual(3f, resolved.Container.Padding.top);
            Assert.AreEqual(4f, resolved.Container.Padding.bottom);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolvedNode_ExplicitNode_And_Item_Override_Layout_Self_Defaults()
        {
            var go = new GameObject("Override", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 100f);

            var layout = go.AddComponent<FlexLayout>();
            layout.style.flexDirection = FlexDirection.Column;

            var node = go.AddComponent<FlexNodeComponent>();
            node.style.width = FlexValue.Points(150f);
            node.style.height = FlexValue.Points(90f);
            node.style.positionType = PositionType.Absolute;

            var item = go.AddComponent<FlexItem>();
            item.style.flexGrow = 5f;
            item.style.flexShrink = 0f;
            item.style.flexBasis = FlexValue.Points(55f);
            item.style.alignSelf = AlignSelf.FlexEnd;

            var resolved = FlexResolvedNodeResolver.Resolve(rect, rect.sizeDelta);

            Assert.IsTrue(resolved.HasExplicitLayoutComponent);
            Assert.IsTrue(resolved.HasNodeSource);
            Assert.IsTrue(resolved.HasItemSource);
            Assert.IsTrue(resolved.Node.HasExplicitAuthoring);
            Assert.IsTrue(resolved.Item.HasExplicitAuthoring);
            Assert.AreEqual(150f, resolved.Node.Width.value);
            Assert.AreEqual(90f, resolved.Node.Height.value);
            Assert.AreEqual(PositionType.Absolute, resolved.Node.PositionType);
            Assert.AreEqual(5f, resolved.Item.FlexGrow);
            Assert.AreEqual(0f, resolved.Item.FlexShrink);
            Assert.AreEqual(55f, resolved.Item.FlexBasis.value);
            Assert.AreEqual(AlignSelf.FlexEnd, resolved.Item.AlignSelf);
            Assert.AreEqual(FlexDirection.Column, resolved.Container.FlexDirection);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolvedNode_Container_Without_Node_Or_Item_Falls_Back_To_Implicit_Node_And_Item()
        {
            var go = new GameObject("ContainerImplicit", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 70f);

            var layout = go.AddComponent<FlexLayout>();
            layout.style.flexDirection = FlexDirection.Column;

            var resolved = FlexResolvedNodeResolver.Resolve(rect, rect.sizeDelta);

            Assert.IsFalse(resolved.HasNodeSource);
            Assert.IsFalse(resolved.HasItemSource);
            Assert.IsTrue(resolved.Container.IsContainer);
            Assert.AreEqual(FlexSizeMode.Points, resolved.Node.Width.mode);
            Assert.AreEqual(FlexSizeMode.Points, resolved.Node.Height.mode);
            Assert.AreEqual(180f, resolved.Node.Width.value);
            Assert.AreEqual(70f, resolved.Node.Height.value);
            Assert.AreEqual(PositionType.Relative, resolved.Node.PositionType);
            Assert.AreEqual(0f, resolved.Item.FlexGrow);
            Assert.AreEqual(0f, resolved.Item.FlexShrink);
            Assert.AreEqual(FlexSizeMode.Auto, resolved.Item.FlexBasis.mode);
            Assert.AreEqual(AlignSelf.Center, resolved.Item.AlignSelf);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolvedNode_ImplicitItem_Uses_ParentLayout_ImplicitDefaults()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootLayout = root.GetComponent<FlexLayout>();
            rootLayout.implicitItemDefaults.width = FlexValue.Points(101f);
            rootLayout.implicitItemDefaults.height = FlexValue.Points(37f);
            rootLayout.implicitItemDefaults.flexGrow = 2f;
            rootLayout.implicitItemDefaults.flexShrink = 3f;
            rootLayout.implicitItemDefaults.flexBasis = FlexValue.Points(55f);
            rootLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexEnd;

            var child = new GameObject("Child", typeof(RectTransform));
            var childRect = child.GetComponent<RectTransform>();
            childRect.SetParent(root.transform, false);
            childRect.sizeDelta = new Vector2(120f, 80f);

            var resolved = FlexResolvedNodeResolver.Resolve(childRect, childRect.sizeDelta);

            Assert.IsFalse(resolved.HasNodeSource);
            Assert.AreEqual(FlexSizeMode.Points, resolved.Node.Width.mode);
            Assert.AreEqual(FlexSizeMode.Points, resolved.Node.Height.mode);
            Assert.AreEqual(101f, resolved.Node.Width.value);
            Assert.AreEqual(37f, resolved.Node.Height.value);
            Assert.IsFalse(resolved.HasItemSource);
            Assert.AreEqual(2f, resolved.Item.FlexGrow);
            Assert.AreEqual(3f, resolved.Item.FlexShrink);
            Assert.AreEqual(FlexSizeMode.Points, resolved.Item.FlexBasis.mode);
            Assert.AreEqual(55f, resolved.Item.FlexBasis.value);
            Assert.AreEqual(AlignSelf.FlexEnd, resolved.Item.AlignSelf);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarkLayoutDirty_Implicit_DefaultWidthHeight_Points_Drive_ChildSize()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNodeComponent));
            var rootLayout = root.GetComponent<FlexLayout>();
            var rootNode = root.GetComponent<FlexNodeComponent>();
            rootNode.style.width = FlexValue.Points(300f);
            rootNode.style.height = FlexValue.Points(120f);
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(120f);
            rootLayout.style.alignItems = AlignItems.FlexStart;
            rootLayout.implicitItemDefaults.width = FlexValue.Points(64f);
            rootLayout.implicitItemDefaults.height = FlexValue.Points(22f);
            rootLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexStart;

            var child = new GameObject("ImplicitChild", typeof(RectTransform));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(10f, 10f);
            childRect.SetParent(root.transform, false);

            rootLayout.MarkLayoutDirty();

            Assert.That(childRect.sizeDelta.x, Is.EqualTo(64f).Within(0.01f));
            Assert.That(childRect.sizeDelta.y, Is.EqualTo(22f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void FlexNode_Standalone_Applies_Self_Size_When_No_Flex_Parent()
        {
            var go = new GameObject("StandaloneNode", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(10f, 20f);

            var node = go.AddComponent<FlexNodeComponent>();
            node.style.width = FlexValue.Points(120f);
            node.style.height = FlexValue.Points(70f);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var applyStandaloneSelfSizing = typeof(FlexNodeComponent).GetMethod("ApplyStandaloneSelfSizing", flags);
            Assert.NotNull(applyStandaloneSelfSizing);
            applyStandaloneSelfSizing.Invoke(node, null);

            Assert.AreEqual(new Vector2(120f, 70f), rect.sizeDelta);
            Assert.That(GetDrivenProperties(rect), Is.EqualTo(DrivenTransformProperties.SizeDeltaX | DrivenTransformProperties.SizeDeltaY));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FlexNode_Standalone_Percent_Uses_ParentRectTransform_Size_As_Constraint()
        {
            var parent = new GameObject("Parent", typeof(RectTransform));
            var parentRect = parent.GetComponent<RectTransform>();
            parentRect.anchorMin = new Vector2(0.5f, 0.5f);
            parentRect.anchorMax = new Vector2(0.5f, 0.5f);
            parentRect.sizeDelta = new Vector2(400f, 200f);

            var go = new GameObject("StandaloneNode", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parentRect, false);
            rect.sizeDelta = new Vector2(10f, 20f);

            var node = go.AddComponent<FlexNodeComponent>();
            node.style.width = FlexValue.Percent(50f);
            node.style.height = FlexValue.Percent(25f);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var applyStandaloneSelfSizing = typeof(FlexNodeComponent).GetMethod("ApplyStandaloneSelfSizing", flags);
            Assert.NotNull(applyStandaloneSelfSizing);
            applyStandaloneSelfSizing.Invoke(node, null);

            Assert.AreEqual(new Vector2(200f, 50f), rect.sizeDelta);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void FlexNode_Standalone_Auto_With_ParentRect_Uses_Self_Rect_Input_Not_Parent_Size()
        {
            var parent = new GameObject("Parent", typeof(RectTransform));
            var parentRect = parent.GetComponent<RectTransform>();
            parentRect.anchorMin = new Vector2(0.5f, 0.5f);
            parentRect.anchorMax = new Vector2(0.5f, 0.5f);
            parentRect.sizeDelta = new Vector2(400f, 200f);

            var go = new GameObject("StandaloneNode", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parentRect, false);
            rect.sizeDelta = new Vector2(120f, 70f);

            var node = go.AddComponent<FlexNodeComponent>();
            node.style.width = FlexValue.Auto();
            node.style.height = FlexValue.Auto();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var applyStandaloneSelfSizing = typeof(FlexNodeComponent).GetMethod("ApplyStandaloneSelfSizing", flags);
            Assert.NotNull(applyStandaloneSelfSizing);
            applyStandaloneSelfSizing.Invoke(node, null);

            Assert.AreEqual(new Vector2(120f, 70f), rect.sizeDelta);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void FlexNode_Standalone_Percent_Without_ParentRect_Falls_Back_To_Auto_Input()
        {
            var go = new GameObject("StandaloneNode", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 70f);

            var node = go.AddComponent<FlexNodeComponent>();
            node.style.width = FlexValue.Percent(50f);
            node.style.height = FlexValue.Percent(50f);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var applyStandaloneSelfSizing = typeof(FlexNodeComponent).GetMethod("ApplyStandaloneSelfSizing", flags);
            Assert.NotNull(applyStandaloneSelfSizing);
            applyStandaloneSelfSizing.Invoke(node, null);

            // No parent RectTransform means percent has no external basis and resolves as auto-input fallback.
            Assert.AreEqual(new Vector2(120f, 70f), rect.sizeDelta);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FlexLayout_With_FlexNode_Root_Percent_Uses_Direct_Parent_Rect_Not_Content()
        {
            var parent = new GameObject("Parent", typeof(RectTransform));
            var parentRect = parent.GetComponent<RectTransform>();
            parentRect.anchorMin = Vector2.up;
            parentRect.anchorMax = Vector2.up;
            parentRect.pivot = Vector2.up;
            parentRect.sizeDelta = new Vector2(400f, 300f);

            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNodeComponent));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;
            rootRect.SetParent(parentRect, false);
            rootRect.sizeDelta = new Vector2(50f, 50f);

            var rootLayout = root.GetComponent<FlexLayout>();
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.style.flexWrap = FlexWrap.NoWrap;
            rootLayout.style.alignItems = AlignItems.FlexStart;
            rootLayout.style.justifyContent = JustifyContent.FlexStart;

            var rootNode = root.GetComponent<FlexNodeComponent>();
            rootNode.style.width = FlexValue.Percent(50f);
            rootNode.style.height = FlexValue.Percent(50f);

            var child = new GameObject("Child", typeof(RectTransform));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(500f, 40f);
            childRect.SetParent(root.transform, false);

            rootLayout.MarkLayoutDirty();

            Assert.That(rootRect.sizeDelta.x, Is.EqualTo(200f).Within(0.01f));
            Assert.That(rootRect.sizeDelta.y, Is.EqualTo(150f).Within(0.01f));

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void MarkLayoutDirty_Node_And_Item_Children_Use_New_Authoring_Components()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);

            var rootLayout = root.AddComponent<FlexLayout>();
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.style.alignItems = AlignItems.FlexStart;
            rootLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexStart;
            var rootNode = root.AddComponent<FlexNodeComponent>();
            rootNode.style.width = FlexValue.Points(100f);
            rootNode.style.height = FlexValue.Points(30f);

            var a = new GameObject("NodeChild", typeof(RectTransform));
            var aRect = a.GetComponent<RectTransform>();
            aRect.SetParent(root.transform, false);
            aRect.anchorMin = new Vector2(0f, 1f);
            aRect.anchorMax = new Vector2(0f, 1f);
            aRect.pivot = new Vector2(0f, 1f);
            aRect.sizeDelta = new Vector2(10f, 10f);
            var aNode = a.AddComponent<FlexNodeComponent>();
            aNode.style.width = FlexValue.Points(30f);
            aNode.style.height = FlexValue.Points(10f);

            var b = new GameObject("ItemChild", typeof(RectTransform));
            var bRect = b.GetComponent<RectTransform>();
            bRect.SetParent(root.transform, false);
            bRect.anchorMin = new Vector2(0f, 1f);
            bRect.anchorMax = new Vector2(0f, 1f);
            bRect.pivot = new Vector2(0f, 1f);
            bRect.sizeDelta = new Vector2(20f, 10f);
            var bItem = b.AddComponent<FlexItem>();
            bItem.style.flexGrow = 1f;
            bItem.style.flexShrink = 1f;
            bItem.style.flexBasis = FlexValue.Auto();
            bItem.style.alignSelf = AlignSelf.Auto;

            rootLayout.MarkLayoutDirty();

            Assert.AreEqual(new Vector2(30f, 10f), aRect.sizeDelta);
            Assert.AreEqual(0f, aRect.anchoredPosition.x, 0.01f);
            Assert.AreEqual(0f, aRect.anchoredPosition.y, 0.01f);
            Assert.AreEqual(70f, bRect.sizeDelta.x, 0.01f);
            Assert.AreEqual(10f, bRect.sizeDelta.y, 0.01f);
            Assert.AreEqual(30f, bRect.anchoredPosition.x, 0.01f);
            Assert.AreEqual(0f, bRect.anchoredPosition.y, 0.01f);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ResolvedNode_ToLegacyNode_Preserves_Current_Runtime_Compatibility()
        {
            var go = new GameObject("Compatibility", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 90f);

            var layout = go.AddComponent<FlexLayout>();
            layout.style.flexDirection = FlexDirection.ColumnReverse;
            layout.style.flexWrap = FlexWrap.Wrap;
            layout.style.justifyContent = JustifyContent.Center;
            layout.style.alignItems = AlignItems.Center;
            layout.style.alignContent = AlignContent.FlexEnd;
            layout.style.mainGap = 7f;
            layout.style.crossGap = 9f;
            var nodeComponent = go.AddComponent<FlexNodeComponent>();
            nodeComponent.style.width = FlexValue.Points(260f);
            nodeComponent.style.height = FlexValue.Points(110f);
            nodeComponent.style.positionType = PositionType.Relative;
            var item = go.AddComponent<FlexItem>();
            item.style.flexGrow = 1f;
            item.style.flexShrink = 0f;
            item.style.flexBasis = FlexValue.Points(130f);
            item.style.alignSelf = AlignSelf.FlexEnd;

            var resolved = FlexResolvedNodeResolver.Resolve(rect, rect.sizeDelta);
            var legacyNode = resolved.ToLegacyNode(default);

            Assert.IsTrue(legacyNode.IsExplicit);
            Assert.AreEqual(260f, legacyNode.Style.width.value);
            Assert.AreEqual(110f, legacyNode.Style.height.value);
            Assert.AreEqual(1f, legacyNode.Style.flexGrow);
            Assert.AreEqual(0f, legacyNode.Style.flexShrink);
            Assert.AreEqual(130f, legacyNode.Style.flexBasis.value);
            Assert.AreEqual(AlignSelf.FlexEnd, legacyNode.Style.alignSelf);
            Assert.AreEqual(PositionType.Relative, legacyNode.Style.positionType);
            Assert.AreEqual(FlexDirection.ColumnReverse, legacyNode.Style.flexDirection);
            Assert.AreEqual(FlexWrap.Wrap, legacyNode.Style.flexWrap);
            Assert.AreEqual(JustifyContent.Center, legacyNode.Style.justifyContent);
            Assert.AreEqual(AlignItems.Center, legacyNode.Style.alignItems);
            Assert.AreEqual(AlignContent.FlexEnd, legacyNode.Style.alignContent);
            Assert.AreEqual(7f, legacyNode.Style.mainGap);
            Assert.AreEqual(9f, legacyNode.Style.crossGap);
            Assert.AreEqual(140f, legacyNode.ImplicitRectWidth);
            Assert.AreEqual(90f, legacyNode.ImplicitRectHeight);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolveAxisSize_Points_Ignores_All_Auto_Context()
        {
            var result = FlexSizing.ResolveAxisSize(
                FlexValue.Points(88f),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: true,
                    parentAssignedSize: 200f,
                    hasExternalConstraint: true,
                    externalConstraintSize: 150f,
                    contentSize: 120f));

            Assert.AreEqual(88f, result);
        }

        [Test]
        public void ResolveAxisSize_Auto_Prefers_Parent_Assigned_Size()
        {
            var result = FlexSizing.ResolveAxisSize(
                FlexValue.Auto(),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: true,
                    parentAssignedSize: 200f,
                    hasExternalConstraint: true,
                    externalConstraintSize: 150f,
                    contentSize: 120f));

            Assert.AreEqual(200f, result);
        }

        [Test]
        public void ResolveAxisSize_Auto_Uses_External_Constraint_When_No_Parent_Size()
        {
            var result = FlexSizing.ResolveAxisSize(
                FlexValue.Auto(),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasExternalConstraint: true,
                    externalConstraintSize: 150f,
                    contentSize: 120f));

            Assert.AreEqual(150f, result);
        }

        [Test]
        public void ResolveAxisSize_Auto_Falls_Back_To_Content_When_Unconstrained()
        {
            var result = FlexSizing.ResolveAxisSize(
                FlexValue.Auto(),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasExternalConstraint: false,
                    externalConstraintSize: 0f,
                    contentSize: 120f));

            Assert.AreEqual(120f, result);
        }

        [Test]
        public void ResolveAxisSize_Percent_Uses_Percent_Reference_When_Present()
        {
            var result = FlexSizing.ResolveAxisSize(
                FlexValue.Percent(25f),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasPercentReferenceSize: true,
                    percentReferenceSize: 320f,
                    hasExternalConstraint: false,
                    externalConstraintSize: 0f,
                    contentSize: 120f));

            Assert.AreEqual(80f, result);
        }

        [Test]
        public void ResolveConstrainedAxisSize_Applies_Min_Constraint()
        {
            var result = FlexSizing.ResolveConstrainedAxisSize(
                FlexValue.Auto(),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasExternalConstraint: false,
                    externalConstraintSize: 0f,
                    contentSize: 40f),
                useMin: true,
                min: 80f,
                useMax: false,
                max: 0f);

            Assert.AreEqual(80f, result);
        }

        [Test]
        public void ResolveConstrainedAxisSize_Applies_Max_Constraint()
        {
            var result = FlexSizing.ResolveConstrainedAxisSize(
                FlexValue.Auto(),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: true,
                    parentAssignedSize: 240f,
                    hasExternalConstraint: false,
                    externalConstraintSize: 0f,
                    contentSize: 40f),
                useMin: false,
                min: 0f,
                useMax: true,
                max: 100f);

            Assert.AreEqual(100f, result);
        }

        [Test]
        public void ResolveConstrainedAxisSize_Applies_Min_Then_Max()
        {
            var result = FlexSizing.ResolveConstrainedAxisSize(
                FlexValue.Points(150f),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasExternalConstraint: false,
                    externalConstraintSize: 0f,
                    contentSize: 0f),
                useMin: true,
                min: 120f,
                useMax: true,
                max: 130f);

            Assert.AreEqual(130f, result);
        }

        [Test]
        public void ResolveFlexBasis_Points_Wins_Over_Main_Axis_And_Content()
        {
            var result = FlexSizing.ResolveFlexBasis(
                FlexValue.Points(60f),
                FlexValue.Points(100f),
                contentSize: 120f,
                hasExternalConstraint: true,
                externalConstraintSize: 140f);

            Assert.AreEqual(60f, result);
        }

        [Test]
        public void ResolveFlexBasis_Auto_Uses_Main_Axis_Points_Size()
        {
            var result = FlexSizing.ResolveFlexBasis(
                FlexValue.Auto(),
                FlexValue.Points(100f),
                contentSize: 120f,
                hasExternalConstraint: true,
                externalConstraintSize: 140f);

            Assert.AreEqual(100f, result);
        }

        [Test]
        public void ResolveFlexBasis_Auto_Uses_External_Constraint_When_Main_Axis_Is_Auto()
        {
            var result = FlexSizing.ResolveFlexBasis(
                FlexValue.Auto(),
                FlexValue.Auto(),
                contentSize: 120f,
                hasExternalConstraint: true,
                externalConstraintSize: 140f);

            Assert.AreEqual(140f, result);
        }

        [Test]
        public void ResolveFlexBasis_Auto_Falls_Back_To_Content_When_Unresolved()
        {
            var result = FlexSizing.ResolveFlexBasis(
                FlexValue.Auto(),
                FlexValue.Auto(),
                contentSize: 120f,
                hasExternalConstraint: false,
                externalConstraintSize: 0f);

            Assert.AreEqual(120f, result);
        }

        [Test]
        public void ResolveFlexBasis_Auto_Uses_Main_Axis_Percent_Reference()
        {
            var result = FlexSizing.ResolveFlexBasis(
                FlexValue.Auto(),
                FlexValue.Percent(50f),
                new FlexAutoAxisContext(
                    hasParentAssignedSize: false,
                    parentAssignedSize: 0f,
                    hasPercentReferenceSize: true,
                    percentReferenceSize: 300f,
                    hasExternalConstraint: false,
                    externalConstraintSize: 0f,
                    contentSize: 120f));

            Assert.AreEqual(150f, result);
        }

        [Test]
        public void Implicit_Item_Defaults_Use_Row_Main_Axis_Width()
        {
            var defaults = FlexSizing.ResolveImplicitItemDefaults(
                new Vector2(200f, 80f),
                FlexDirection.Row);

            Assert.AreEqual(200f, defaults.Width);
            Assert.AreEqual(80f, defaults.Height);
            Assert.AreEqual(200f, defaults.MainAxisBasis);
            Assert.AreEqual(0f, defaults.FlexGrow);
            Assert.AreEqual(0f, defaults.FlexShrink);
            Assert.AreEqual(PositionType.Relative, defaults.PositionType);
        }

        [Test]
        public void Implicit_Item_Defaults_Use_Column_Main_Axis_Height()
        {
            var defaults = FlexSizing.ResolveImplicitItemDefaults(
                new Vector2(200f, 80f),
                FlexDirection.Column);

            Assert.AreEqual(80f, defaults.MainAxisBasis);
        }

        [Test]
        public void MeasureSubtree_Implicit_Node_Uses_Implicit_Rect_Size()
        {
            var store = new FlexNodeStore();
            var nodeId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = false,
                ImplicitRectWidth = 120f,
                ImplicitRectHeight = 45f,
            });

            var measured = FlexMeasure.MeasureSubtree(store, nodeId);

            Assert.AreEqual(new FlexMeasuredSize(120f, 45f), measured);
        }

        [Test]
        public void MeasureSubtree_Explicit_Node_With_Points_Size_Ignores_Content()
        {
            var store = new FlexNodeStore();
            var nodeId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(300f),
                    height = FlexValue.Points(200f),
                },
                ContentWidth = 50f,
                ContentHeight = 40f,
            });

            var measured = FlexMeasure.MeasureSubtree(store, nodeId);

            Assert.AreEqual(new FlexMeasuredSize(300f, 200f), measured);
        }

        [Test]
        public void MeasureSubtree_Root_Auto_Uses_Content_When_Unconstrained()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = FlexStyle.Default,
                ContentWidth = 90f,
                ContentHeight = 70f,
            });

            var measured = FlexMeasure.MeasureSubtree(store, rootId);

            Assert.AreEqual(new FlexMeasuredSize(90f, 70f), measured);
        }

        [Test]
        public void MeasureSubtree_Auto_Uses_External_Constraint_When_Present()
        {
            var store = new FlexNodeStore();
            var nodeId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = FlexStyle.Default,
                HasExternalWidthConstraint = true,
                ExternalWidthConstraint = 210f,
                ContentWidth = 90f,
                ContentHeight = 70f,
            });

            var measured = FlexMeasure.MeasureSubtree(store, nodeId);

            Assert.AreEqual(new FlexMeasuredSize(210f, 70f), measured);
        }

        [Test]
        public void MeasureSubtree_Percent_Uses_Provided_Percent_Reference()
        {
            var store = new FlexNodeStore();
            var nodeId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Percent(50f),
                    height = FlexValue.Percent(25f),
                },
                ContentWidth = 500f,
                ContentHeight = 300f,
            });

            var measured = FlexMeasure.MeasureSubtree(
                store,
                nodeId,
                new FlexPercentReferenceOverrides(
                    hasWidthReference: true,
                    widthReference: 400f,
                    hasHeightReference: true,
                    heightReference: 200f));

            Assert.AreEqual(new FlexMeasuredSize(200f, 50f), measured);
        }

        [Test]
        public void MeasureChildContent_Row_Sums_Children_And_Gap()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    mainGap = 10f,
                    crossGap = 10f,
                    padding = new FlexEdges { left = 5f, right = 7f, top = 2f, bottom = 3f },
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 100f,
                ImplicitRectHeight = 20f,
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 80f,
                ImplicitRectHeight = 40f,
            });

            var content = FlexMeasure.MeasureChildContent(store, store.GetNode(rootId));

            Assert.AreEqual(new FlexMeasuredSize(202f, 45f), content);
        }

        [Test]
        public void MeasureChildContent_Column_Uses_Max_Width_And_Sums_Height()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Column,
                    mainGap = 5f,
                    crossGap = 5f,
                    padding = new FlexEdges { left = 3f, right = 7f, top = 11f, bottom = 13f },
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 100f,
                ImplicitRectHeight = 20f,
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 80f,
                ImplicitRectHeight = 40f,
            });

            var content = FlexMeasure.MeasureChildContent(store, store.GetNode(rootId));

            Assert.AreEqual(new FlexMeasuredSize(110f, 89f), content);
        }

        [Test]
        public void MeasureChildContent_Ignores_Absolute_Explicit_Child()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 50f,
                ImplicitRectHeight = 30f,
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(999f),
                    height = FlexValue.Points(999f),
                    positionType = PositionType.Absolute,
                },
            });

            var content = FlexMeasure.MeasureChildContent(store, store.GetNode(rootId));

            Assert.AreEqual(new FlexMeasuredSize(50f, 30f), content);
        }

        [Test]
        public void MeasureSubtree_Auto_Child_Uses_Child_Content_Then_Min_Max()
        {
            var store = new FlexNodeStore();
            var childId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Auto(),
                    height = FlexValue.Auto(),
                    minWidth = FlexOptionalFloat.Enabled(120f),
                    maxHeight = FlexOptionalFloat.Enabled(35f),
                    flexDirection = FlexDirection.Row,
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = childId,
                IsExplicit = false,
                ImplicitRectWidth = 50f,
                ImplicitRectHeight = 20f,
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = childId,
                IsExplicit = false,
                ImplicitRectWidth = 30f,
                ImplicitRectHeight = 40f,
            });

            var measured = FlexMeasure.MeasureSubtree(store, childId);

            Assert.AreEqual(new FlexMeasuredSize(120f, 35f), measured);
        }

        [Test]
        public void AllocateMainAxisSizes_Uses_Basis_When_No_Free_Space()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(100f),
                    height = FlexValue.Points(20f),
                    flexBasis = FlexValue.Auto(),
                    flexGrow = 0f,
                    flexShrink = 1f,
                },
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(50f),
                    height = FlexValue.Points(20f),
                    flexBasis = FlexValue.Auto(),
                    flexGrow = 0f,
                    flexShrink = 1f,
                },
            });

            var allocations = FlexMeasure.AllocateMainAxisSizes(store, rootId, 150f);

            Assert.AreEqual(2, allocations.Count);
            Assert.AreEqual(new FlexMainAxisAllocation(a, 100f, 100f), allocations[0]);
            Assert.AreEqual(new FlexMainAxisAllocation(b, 50f, 50f), allocations[1]);
        }

        [Test]
        public void AllocateMainAxisSizes_Distributes_Free_Space_By_FlexGrow()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(100f),
                    height = FlexValue.Points(20f),
                    flexGrow = 1f,
                    flexShrink = 1f,
                    flexBasis = FlexValue.Auto(),
                },
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(50f),
                    height = FlexValue.Points(20f),
                    flexGrow = 3f,
                    flexShrink = 1f,
                    flexBasis = FlexValue.Auto(),
                },
            });

            var allocations = FlexMeasure.AllocateMainAxisSizes(store, rootId, 250f);

            Assert.AreEqual(125f, allocations[0].FinalSize);
            Assert.AreEqual(125f, allocations[1].FinalSize);
        }

        [Test]
        public void AllocateMainAxisSizes_Distributes_Overflow_By_Scaled_FlexShrink()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(100f),
                    height = FlexValue.Points(20f),
                    flexGrow = 0f,
                    flexShrink = 1f,
                    flexBasis = FlexValue.Auto(),
                },
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(50f),
                    height = FlexValue.Points(20f),
                    flexGrow = 0f,
                    flexShrink = 1f,
                    flexBasis = FlexValue.Auto(),
                },
            });

            var allocations = FlexMeasure.AllocateMainAxisSizes(store, rootId, 120f);

            Assert.AreEqual(new FlexMainAxisAllocation(a, 100f, 80f), allocations[0]);
            Assert.AreEqual(new FlexMainAxisAllocation(b, 50f, 40f), allocations[1]);
        }

        [Test]
        public void AllocateMainAxisSizes_Implicit_Children_Do_Not_Grow()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 60f,
                ImplicitRectHeight = 20f,
            });

            var allocations = FlexMeasure.AllocateMainAxisSizes(store, rootId, 200f);

            Assert.AreEqual(1, allocations.Count);
            Assert.AreEqual(new FlexMainAxisAllocation(childId, 60f, 60f), allocations[0]);
        }

        [Test]
        public void AllocateMainAxisSizes_Uses_Gap_And_Padding_In_Free_Space_Calculation()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    mainGap = 10f,
                    crossGap = 10f,
                    padding = new FlexEdges { left = 5f, right = 5f },
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(50f),
                    flexGrow = 1f,
                    flexBasis = FlexValue.Auto(),
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(50f),
                    flexGrow = 1f,
                    flexBasis = FlexValue.Auto(),
                },
            });

            var allocations = FlexMeasure.AllocateMainAxisSizes(store, rootId, 170f);

            Assert.AreEqual(75f, allocations[0].FinalSize);
            Assert.AreEqual(75f, allocations[1].FinalSize);
        }

        [Test]
        public void AllocateMainAxisSizes_Applies_Main_Axis_Min_Max_After_Distribution()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(50f),
                    flexGrow = 1f,
                    flexBasis = FlexValue.Auto(),
                    maxWidth = FlexOptionalFloat.Enabled(60f),
                },
            });

            var allocations = FlexMeasure.AllocateMainAxisSizes(store, rootId, 200f);

            Assert.AreEqual(new FlexMainAxisAllocation(childId, 50f, 60f), allocations[0]);
        }

        [Test]
        public void AllocateMainAxisSizes_Shrink_Does_Not_Produce_Negative_Size()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            var fixedId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(180f),
                    flexShrink = 0f,
                    flexBasis = FlexValue.Auto(),
                },
            });

            var shrinkId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(80f),
                    flexShrink = 1f,
                    flexBasis = FlexValue.Auto(),
                },
            });

            var allocations = FlexMeasure.AllocateMainAxisSizes(store, rootId, 100f);

            Assert.AreEqual(new FlexMainAxisAllocation(fixedId, 180f, 180f), allocations[0]);
            Assert.AreEqual(new FlexMainAxisAllocation(shrinkId, 80f, 0f), allocations[1]);
        }

        [Test]
        public void AllocateCrossAxisSizes_Stretch_Uses_Inner_Cross_Size()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = AlignItems.Stretch,
                    padding = new FlexEdges { top = 10f, bottom = 20f },
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(40f),
                    height = FlexValue.Points(20f),
                    alignSelf = AlignSelf.Auto,
                },
            });

            var allocations = FlexMeasure.AllocateCrossAxisSizes(store, rootId, 100f);

            Assert.AreEqual(1, allocations.Count);
            Assert.AreEqual(new FlexCrossAxisAllocation(childId, 20f, 70f, true), allocations[0]);
        }

        [Test]
        public void AllocateCrossAxisSizes_NonStretch_Keeps_Measured_Cross_Size()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = AlignItems.FlexStart,
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(40f),
                    height = FlexValue.Points(20f),
                },
            });

            var allocations = FlexMeasure.AllocateCrossAxisSizes(store, rootId, 100f);

            Assert.AreEqual(new FlexCrossAxisAllocation(childId, 20f, 20f, false), allocations[0]);
        }

        [Test]
        public void AllocateCrossAxisSizes_AlignSelf_Overrides_Parent_Stretch()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = AlignItems.Stretch,
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(40f),
                    height = FlexValue.Points(20f),
                    alignSelf = AlignSelf.FlexStart,
                },
            });

            var allocations = FlexMeasure.AllocateCrossAxisSizes(store, rootId, 100f);

            Assert.AreEqual(new FlexCrossAxisAllocation(childId, 20f, 20f, false), allocations[0]);
        }

        [Test]
        public void AllocateCrossAxisSizes_Applies_Cross_Axis_Min_Max_When_Stretched()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = AlignItems.Stretch,
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(40f),
                    height = FlexValue.Points(20f),
                    minHeight = FlexOptionalFloat.Enabled(50f),
                    maxHeight = FlexOptionalFloat.Enabled(60f),
                },
            });

            var allocations = FlexMeasure.AllocateCrossAxisSizes(store, rootId, 100f);

            Assert.AreEqual(childId, allocations[0].NodeId);
            Assert.AreEqual(50f, allocations[0].MeasuredCrossSize);
            Assert.AreEqual(60f, allocations[0].FinalCrossSize);
            Assert.IsTrue(allocations[0].Stretched);
        }

        [Test]
        public void AllocateCrossAxisSizes_Column_Stretch_Uses_Width_As_Cross_Axis()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = AlignItems.Stretch,
                    padding = new FlexEdges { left = 5f, right = 15f },
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(30f),
                    height = FlexValue.Points(20f),
                },
            });

            var allocations = FlexMeasure.AllocateCrossAxisSizes(store, rootId, 100f);

            Assert.AreEqual(new FlexCrossAxisAllocation(childId, 30f, 80f, true), allocations[0]);
        }

        [Test]
        public void AllocateCrossAxisSizes_Ignores_Absolute_Children()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = AlignItems.Stretch,
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(30f),
                    height = FlexValue.Points(10f),
                    positionType = PositionType.Absolute,
                },
            });

            var allocations = FlexMeasure.AllocateCrossAxisSizes(store, rootId, 100f);

            Assert.AreEqual(0, allocations.Count);
        }

        [Test]
        public void MeasureItemLayouts_Row_Combines_Main_And_Cross_Axis_Results()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = AlignItems.Stretch,
                    padding = new FlexEdges { top = 10f, bottom = 10f },
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(40f),
                    height = FlexValue.Points(20f),
                    flexBasis = FlexValue.Auto(),
                },
            });

            var results = FlexMeasure.MeasureItemLayouts(store, rootId, 100f, 80f);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(childId, results[0].NodeId);
            Assert.AreEqual(40f, results[0].MeasuredWidth);
            Assert.AreEqual(20f, results[0].MeasuredHeight);
            Assert.AreEqual(40f, results[0].MainAxisBasis);
            Assert.AreEqual(40f, results[0].FinalMainAxisSize);
            Assert.AreEqual(20f, results[0].MeasuredCrossAxisSize);
            Assert.AreEqual(60f, results[0].FinalCrossAxisSize);
            Assert.IsTrue(results[0].StretchedOnCrossAxis);
        }

        [Test]
        public void MeasureItemLayouts_Column_Combines_Main_And_Cross_Axis_Results()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = AlignItems.FlexStart,
                },
            });

            var childId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(30f),
                    height = FlexValue.Points(50f),
                    flexBasis = FlexValue.Auto(),
                },
            });

            var results = FlexMeasure.MeasureItemLayouts(store, rootId, 200f, 100f);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(childId, results[0].NodeId);
            Assert.AreEqual(30f, results[0].MeasuredWidth);
            Assert.AreEqual(50f, results[0].MeasuredHeight);
            Assert.AreEqual(50f, results[0].MainAxisBasis);
            Assert.AreEqual(50f, results[0].FinalMainAxisSize);
            Assert.AreEqual(30f, results[0].MeasuredCrossAxisSize);
            Assert.AreEqual(30f, results[0].FinalCrossAxisSize);
            Assert.IsFalse(results[0].StretchedOnCrossAxis);
        }

        [Test]
        public void MeasureItemLayouts_Excludes_Children_Outside_Normal_Flow()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(20f),
                    height = FlexValue.Points(20f),
                    positionType = PositionType.Absolute,
                },
            });

            var results = FlexMeasure.MeasureItemLayouts(store, rootId, 100f, 100f);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void BuildLines_RowWrap_Breaks_Overflow_Into_Second_Line()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    mainGap = 10f,
                    crossGap = 10f,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 60f,
                ImplicitRectHeight = 20f,
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 60f,
                ImplicitRectHeight = 30f,
            });

            var c = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 40f,
                ImplicitRectHeight = 10f,
            });

            var lines = FlexMeasure.BuildLines(store, rootId, 140f);

            Assert.AreEqual(2, lines.Count);
            CollectionAssert.AreEqual(new[] { a, b }, lines[0].NodeIds);
            CollectionAssert.AreEqual(new[] { c }, lines[1].NodeIds);
            Assert.AreEqual(130f, lines[0].TotalMainSize);
            Assert.AreEqual(30f, lines[0].MaxCrossSize);
            Assert.AreEqual(40f, lines[1].TotalMainSize);
            Assert.AreEqual(10f, lines[1].MaxCrossSize);
        }

        [Test]
        public void BuildLines_ColumnWrap_Breaks_Overflow_Into_Second_Line()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Column,
                    flexWrap = FlexWrap.Wrap,
                    mainGap = 5f,
                    crossGap = 5f,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 30f,
                ImplicitRectHeight = 40f,
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 20f,
                ImplicitRectHeight = 40f,
            });

            var c = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 50f,
                ImplicitRectHeight = 20f,
            });

            var lines = FlexMeasure.BuildLines(store, rootId, 90f);

            Assert.AreEqual(2, lines.Count);
            CollectionAssert.AreEqual(new[] { a, b }, lines[0].NodeIds);
            CollectionAssert.AreEqual(new[] { c }, lines[1].NodeIds);
            Assert.AreEqual(85f, lines[0].TotalMainSize);
            Assert.AreEqual(30f, lines[0].MaxCrossSize);
            Assert.AreEqual(20f, lines[1].TotalMainSize);
            Assert.AreEqual(50f, lines[1].MaxCrossSize);
        }

        [Test]
        public void BuildLines_Does_Not_Wrap_When_Items_Exactly_Fit()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    mainGap = 10f,
                    crossGap = 10f,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 50f,
                ImplicitRectHeight = 20f,
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 40f,
                ImplicitRectHeight = 30f,
            });

            var lines = FlexMeasure.BuildLines(store, rootId, 100f);

            Assert.AreEqual(1, lines.Count);
            CollectionAssert.AreEqual(new[] { a, b }, lines[0].NodeIds);
            Assert.AreEqual(100f, lines[0].TotalMainSize);
            Assert.AreEqual(30f, lines[0].MaxCrossSize);
        }

        [Test]
        public void BuildLines_Single_Oversized_Item_Forms_Its_Own_Line()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    mainGap = 10f,
                    crossGap = 10f,
                },
            });

            var oversized = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 200f,
                ImplicitRectHeight = 40f,
            });

            var other = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 20f,
                ImplicitRectHeight = 10f,
            });

            var lines = FlexMeasure.BuildLines(store, rootId, 100f);

            Assert.AreEqual(2, lines.Count);
            CollectionAssert.AreEqual(new[] { oversized }, lines[0].NodeIds);
            CollectionAssert.AreEqual(new[] { other }, lines[1].NodeIds);
            Assert.AreEqual(200f, lines[0].TotalMainSize);
            Assert.AreEqual(40f, lines[0].MaxCrossSize);
        }

        [Test]
        public void BuildLines_Excludes_Absolute_Children_From_Line_Space()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    mainGap = 10f,
                    crossGap = 10f,
                },
            });

            var inFlow = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 60f,
                ImplicitRectHeight = 20f,
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(100f),
                    height = FlexValue.Points(30f),
                    positionType = PositionType.Absolute,
                },
            });

            var other = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 60f,
                ImplicitRectHeight = 15f,
            });

            var lines = FlexMeasure.BuildLines(store, rootId, 140f);

            Assert.AreEqual(1, lines.Count);
            CollectionAssert.AreEqual(new[] { inFlow, other }, lines[0].NodeIds);
        }

        [Test]
        public void BuildLines_Implicit_Child_Participates_In_Line_Build()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                },
            });

            var implicitChild = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 55f,
                ImplicitRectHeight = 25f,
            });

            var lines = FlexMeasure.BuildLines(store, rootId, 100f);

            Assert.AreEqual(1, lines.Count);
            CollectionAssert.AreEqual(new[] { implicitChild }, lines[0].NodeIds);
            Assert.AreEqual(55f, lines[0].TotalMainSize);
            Assert.AreEqual(25f, lines[0].MaxCrossSize);
        }

        [Test]
        public void Arrange_Wrap_JustifyContent_Is_Applied_Per_Line()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    justifyContent = JustifyContent.Center,
                    alignItems = AlignItems.FlexStart,
                    alignContent = AlignContent.FlexStart,
                },
            });

            var a = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 40f, ImplicitRectHeight = 10f });
            var b = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 40f, ImplicitRectHeight = 10f });
            var c = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 40f, ImplicitRectHeight = 10f });

            var arranged = FlexMeasure.Arrange(store, rootId, 100f, 100f);

            Assert.AreEqual(new FlexItemLayoutResult(a, 10f, 0f, 40f, 10f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 50f, 0f, 40f, 10f), arranged[1]);
            Assert.AreEqual(new FlexItemLayoutResult(c, 30f, 10f, 40f, 10f), arranged[2]);
        }

        [Test]
        public void Arrange_Wrap_FlexGrow_Is_Resolved_Per_Line()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    justifyContent = JustifyContent.FlexStart,
                    alignItems = AlignItems.FlexStart,
                    alignContent = AlignContent.FlexStart,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(40f), height = FlexValue.Points(10f), flexGrow = 1f, flexShrink = 1f, flexBasis = FlexValue.Auto() },
            });
            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(40f), height = FlexValue.Points(10f), flexGrow = 1f, flexShrink = 1f, flexBasis = FlexValue.Auto() },
            });
            var c = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(40f), height = FlexValue.Points(10f), flexGrow = 1f, flexShrink = 1f, flexBasis = FlexValue.Auto() },
            });

            var arranged = FlexMeasure.Arrange(store, rootId, 100f, 100f);

            Assert.AreEqual(new FlexItemLayoutResult(a, 0f, 0f, 50f, 10f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 50f, 0f, 50f, 10f), arranged[1]);
            Assert.AreEqual(new FlexItemLayoutResult(c, 0f, 10f, 100f, 10f), arranged[2]);
        }

        [Test]
        public void Arrange_Wrap_LineCrossSize_Uses_Max_Item_Cross()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    justifyContent = JustifyContent.FlexStart,
                    alignItems = AlignItems.FlexStart,
                    alignContent = AlignContent.FlexStart,
                    mainGap = 5f,
                    crossGap = 5f,
                },
            });

            var a = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 20f });
            var b = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 30f });
            var c = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 40f, ImplicitRectHeight = 10f });

            var arranged = FlexMeasure.Arrange(store, rootId, 130f, 100f);

            Assert.AreEqual(new FlexItemLayoutResult(a, 0f, 0f, 60f, 20f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 65f, 0f, 60f, 30f), arranged[1]);
            Assert.AreEqual(new FlexItemLayoutResult(c, 0f, 35f, 40f, 10f), arranged[2]);
        }

        [Test]
        public void Arrange_WrapReverse_Reverses_Line_Stack_Order()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.WrapReverse,
                    justifyContent = JustifyContent.FlexStart,
                    alignItems = AlignItems.FlexStart,
                    alignContent = AlignContent.FlexStart,
                    mainGap = 5f,
                    crossGap = 5f,
                },
            });

            var a = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 20f });
            var b = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 30f });
            var c = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 40f, ImplicitRectHeight = 10f });

            var arranged = FlexMeasure.Arrange(store, rootId, 130f, 100f);

            Assert.AreEqual(new FlexItemLayoutResult(a, 0f, 70f, 60f, 20f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 65f, 70f, 60f, 30f), arranged[1]);
            Assert.AreEqual(new FlexItemLayoutResult(c, 0f, 55f, 40f, 10f), arranged[2]);
        }

        [Test]
        public void Arrange_Wrap_AlignContent_AllEnums_ProduceExpected_LineStack()
        {
            var cases = new (AlignContent alignContent, float firstLineY, float secondLineY)[]
            {
                (AlignContent.Stretch, 0f, 62.5f),
                (AlignContent.FlexStart, 0f, 35f),
                (AlignContent.Center, 27.5f, 62.5f),
                (AlignContent.FlexEnd, 55f, 90f),
                (AlignContent.SpaceBetween, 0f, 90f),
                (AlignContent.SpaceAround, 13.75f, 76.25f),
                (AlignContent.SpaceEvenly, 18.33333f, 71.66667f),
            };

            for (var i = 0; i < cases.Length; i++)
            {
                var store = new FlexNodeStore();
                var rootId = store.CreateNode(new FlexNodeModel
                {
                    IsExplicit = true,
                    Style = new FlexStyle
                    {
                        flexDirection = FlexDirection.Row,
                        flexWrap = FlexWrap.Wrap,
                        justifyContent = JustifyContent.FlexStart,
                        alignItems = AlignItems.FlexStart,
                        alignContent = cases[i].alignContent,
                        mainGap = 5f,
                        crossGap = 5f,
                    },
                });

                var a = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 20f });
                var b = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 30f });
                var c = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 40f, ImplicitRectHeight = 10f });

                var arranged = FlexMeasure.Arrange(store, rootId, 130f, 100f);

                Assert.That(arranged[0].Y, Is.EqualTo(cases[i].firstLineY).Within(0.0001f), cases[i].alignContent.ToString());
                Assert.That(arranged[1].Y, Is.EqualTo(cases[i].firstLineY).Within(0.0001f), cases[i].alignContent.ToString());
                Assert.That(arranged[2].Y, Is.EqualTo(cases[i].secondLineY).Within(0.0001f), cases[i].alignContent.ToString());
                Assert.That(arranged[0].Height, Is.EqualTo(20f).Within(0.0001f), cases[i].alignContent.ToString());
                Assert.That(arranged[1].Height, Is.EqualTo(30f).Within(0.0001f), cases[i].alignContent.ToString());
                Assert.That(arranged[2].Height, Is.EqualTo(10f).Within(0.0001f), cases[i].alignContent.ToString());
            }
        }

        [Test]
        public void Arrange_Wrap_AlignContent_Applies_On_Column_CrossAxis()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Column,
                    flexWrap = FlexWrap.Wrap,
                    justifyContent = JustifyContent.FlexStart,
                    alignItems = AlignItems.FlexStart,
                    alignContent = AlignContent.Center,
                    mainGap = 5f,
                    crossGap = 5f,
                },
            });

            var a = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 20f, ImplicitRectHeight = 45f });
            var b = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 30f, ImplicitRectHeight = 45f });
            var c = store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 10f, ImplicitRectHeight = 40f });

            var arranged = FlexMeasure.Arrange(store, rootId, 130f, 100f);

            Assert.That(arranged[0].X, Is.EqualTo(27.5f).Within(0.0001f));
            Assert.That(arranged[1].X, Is.EqualTo(27.5f).Within(0.0001f));
            Assert.That(arranged[2].X, Is.EqualTo(62.5f).Within(0.0001f));
            Assert.That(arranged[0].Width, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(arranged[1].Width, Is.EqualTo(30f).Within(0.0001f));
            Assert.That(arranged[2].Width, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void MeasureSubtree_Wrap_RootAutoHeight_Uses_Wrapped_Content_Size()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(100f),
                    height = FlexValue.Auto(),
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    mainGap = 5f,
                    crossGap = 5f,
                },
            });

            store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 20f });
            store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 30f });

            var measured = FlexMeasure.MeasureSubtree(store, rootId);

            Assert.AreEqual(new FlexMeasuredSize(100f, 55f), measured);
        }

        [Test]
        public void MeasureChildContent_Wrap_Padding_And_Gap_Affect_LineBreak_And_ContentSize()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(140f),
                    height = FlexValue.Auto(),
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.Wrap,
                    mainGap = 10f,
                    crossGap = 10f,
                    padding = new FlexEdges { left = 10f, right = 10f, top = 4f, bottom = 6f },
                },
            });

            store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 20f });
            store.CreateNode(new FlexNodeModel { ParentId = rootId, IsExplicit = false, ImplicitRectWidth = 60f, ImplicitRectHeight = 20f });

            var content = FlexMeasure.MeasureChildContent(store, store.GetNode(rootId));

            Assert.AreEqual(new FlexMeasuredSize(80f, 60f), content);
        }

        [Test]
        public void ArrangeSingleLine_Row_FlexStart_Places_Items_From_Left_With_Gap()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = JustifyContent.FlexStart,
                    alignItems = AlignItems.FlexStart,
                    mainGap = 10f,
                    crossGap = 10f,
                    padding = new FlexEdges { left = 5f, top = 3f },
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(50f),
                    height = FlexValue.Points(20f),
                },
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(30f),
                    height = FlexValue.Points(10f),
                },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 200f, 100f);

            Assert.AreEqual(new FlexItemLayoutResult(a, 5f, 3f, 50f, 20f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 65f, 3f, 30f, 10f), arranged[1]);
        }

        [Test]
        public void ArrangeSingleLine_Row_Center_Offsets_Main_Axis_Start()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = JustifyContent.Center,
                },
            });

            var child = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(40f),
                    height = FlexValue.Points(10f),
                },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 50f);

            Assert.AreEqual(new FlexItemLayoutResult(child, 30f, 0f, 40f, 50f), arranged[0]);
        }

        [Test]
        public void ArrangeSingleLine_Row_FlexEnd_Offsets_Main_Axis_Start()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = JustifyContent.FlexEnd,
                },
            });

            var child = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(40f),
                    height = FlexValue.Points(10f),
                },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 50f);

            Assert.AreEqual(new FlexItemLayoutResult(child, 60f, 0f, 40f, 50f), arranged[0]);
        }

        [Test]
        public void ArrangeSingleLine_NoWrap_Overflow_Does_Not_Overlap_When_Shrink_Item_Clamps_To_Zero()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = FlexWrap.NoWrap,
                    justifyContent = JustifyContent.FlexStart,
                    mainGap = 5f,
                    crossGap = 5f,
                    alignItems = AlignItems.FlexStart,
                },
            });

            var firstId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = false,
                ImplicitRectWidth = 100f,
                ImplicitRectHeight = 20f,
            });

            var secondId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(180f),
                    height = FlexValue.Points(70f),
                    flexShrink = 0f,
                },
            });

            var thirdId = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(80f),
                    height = FlexValue.Points(40f),
                    flexShrink = 1f,
                },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 200f, 100f);

            Assert.AreEqual(new FlexItemLayoutResult(firstId, 0f, 0f, 100f, 20f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(secondId, 105f, 0f, 180f, 70f), arranged[1]);
            Assert.AreEqual(new FlexItemLayoutResult(thirdId, 290f, 0f, 0f, 40f), arranged[2]);
        }

        [Test]
        public void ArrangeSingleLine_Row_Center_Allows_TwoSided_Overflow_When_Content_Exceeds_Container()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = JustifyContent.Center,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(80f), height = FlexValue.Points(10f), flexShrink = 0f },
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(80f), height = FlexValue.Points(10f), flexShrink = 0f },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 20f);

            Assert.AreEqual(new FlexItemLayoutResult(a, -30f, 0f, 80f, 20f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 50f, 0f, 80f, 20f), arranged[1]);
        }

        [Test]
        public void ArrangeSingleLine_Row_FlexEnd_Allows_MainStart_Overflow_When_Content_Exceeds_Container()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = JustifyContent.FlexEnd,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(80f), height = FlexValue.Points(10f), flexShrink = 0f },
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(80f), height = FlexValue.Points(10f), flexShrink = 0f },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 20f);

            Assert.AreEqual(new FlexItemLayoutResult(a, -60f, 0f, 80f, 20f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 20f, 0f, 80f, 20f), arranged[1]);
        }

        [Test]
        public void ArrangeSingleLine_Row_SpaceDistribution_Falls_Back_To_FlexStart_When_Content_Overflows()
        {
            var justifies = new[]
            {
                JustifyContent.SpaceBetween,
                JustifyContent.SpaceAround,
                JustifyContent.SpaceEvenly,
            };

            foreach (var justify in justifies)
            {
                var store = new FlexNodeStore();
                var rootId = store.CreateNode(new FlexNodeModel
                {
                    IsExplicit = true,
                    Style = new FlexStyle
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = justify,
                    },
                });

                var a = store.CreateNode(new FlexNodeModel
                {
                    ParentId = rootId,
                    IsExplicit = true,
                    Style = new FlexStyle { width = FlexValue.Points(80f), height = FlexValue.Points(10f), flexShrink = 0f },
                });

                var b = store.CreateNode(new FlexNodeModel
                {
                    ParentId = rootId,
                    IsExplicit = true,
                    Style = new FlexStyle { width = FlexValue.Points(80f), height = FlexValue.Points(10f), flexShrink = 0f },
                });

                var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 20f);

                Assert.AreEqual(new FlexItemLayoutResult(a, 0f, 0f, 80f, 20f), arranged[0], justify.ToString());
                Assert.AreEqual(new FlexItemLayoutResult(b, 80f, 0f, 80f, 20f), arranged[1], justify.ToString());
            }
        }

        [Test]
        public void ArrangeSingleLine_Row_AlignSelf_Overrides_Parent_AlignItems_On_Cross_Axis()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = AlignItems.Center,
                    padding = new FlexEdges { top = 10f, bottom = 10f },
                },
            });

            var child = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(20f),
                    height = FlexValue.Points(20f),
                    alignSelf = AlignSelf.FlexStart,
                },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 100f);

            Assert.AreEqual(new FlexItemLayoutResult(child, 0f, 10f, 20f, 20f), arranged[0]);
        }

        [Test]
        public void ArrangeSingleLine_Column_FlexStart_Places_Items_From_Top()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = JustifyContent.FlexStart,
                    alignItems = AlignItems.FlexStart,
                    mainGap = 5f,
                    crossGap = 5f,
                    padding = new FlexEdges { left = 4f, top = 2f },
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(20f),
                    height = FlexValue.Points(30f),
                },
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(10f),
                    height = FlexValue.Points(15f),
                },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 80f);

            Assert.AreEqual(new FlexItemLayoutResult(a, 4f, 2f, 20f, 30f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 4f, 37f, 10f, 15f), arranged[1]);
        }

        [Test]
        public void ArrangeSingleLine_Row_SpaceBetween_Distributes_Extra_Main_Space()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = JustifyContent.SpaceBetween,
                    alignItems = AlignItems.FlexStart,
                },
            });

            var a = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(10f), height = FlexValue.Points(10f) },
            });

            var b = store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(10f), height = FlexValue.Points(10f) },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 20f);

            Assert.AreEqual(new FlexItemLayoutResult(a, 0f, 0f, 10f, 10f), arranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(b, 90f, 0f, 10f, 10f), arranged[1]);
        }

        [Test]
        public void ArrangeSingleLine_Row_SpaceAround_And_SpaceEvenly_Use_Correct_Main_Offsets()
        {
            var store = new FlexNodeStore();
            var rootSpaceAroundId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = JustifyContent.SpaceAround,
                    alignItems = AlignItems.FlexStart,
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootSpaceAroundId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(10f), height = FlexValue.Points(10f) },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootSpaceAroundId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(10f), height = FlexValue.Points(10f) },
            });

            var around = FlexMeasure.ArrangeSingleLine(store, rootSpaceAroundId, 100f, 20f);
            Assert.AreEqual(20f, around[0].X, 0.0001f);
            Assert.AreEqual(70f, around[1].X, 0.0001f);

            var rootSpaceEvenlyId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = JustifyContent.SpaceEvenly,
                    alignItems = AlignItems.FlexStart,
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootSpaceEvenlyId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(10f), height = FlexValue.Points(10f) },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootSpaceEvenlyId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(10f), height = FlexValue.Points(10f) },
            });

            var evenly = FlexMeasure.ArrangeSingleLine(store, rootSpaceEvenlyId, 100f, 20f);
            Assert.AreEqual(26.66667f, evenly[0].X, 0.0001f);
            Assert.AreEqual(63.33333f, evenly[1].X, 0.0001f);
        }

        [Test]
        public void ArrangeSingleLine_Reverse_Directions_Place_Items_From_Main_End()
        {
            var store = new FlexNodeStore();
            var rowReverseRootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.RowReverse,
                    justifyContent = JustifyContent.FlexStart,
                    mainGap = 5f,
                    crossGap = 5f,
                },
            });

            var rowA = store.CreateNode(new FlexNodeModel
            {
                ParentId = rowReverseRootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(20f), height = FlexValue.Points(10f) },
            });

            var rowB = store.CreateNode(new FlexNodeModel
            {
                ParentId = rowReverseRootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(10f), height = FlexValue.Points(10f) },
            });

            var rowArranged = FlexMeasure.ArrangeSingleLine(store, rowReverseRootId, 100f, 20f);
            Assert.AreEqual(new FlexItemLayoutResult(rowA, 80f, 0f, 20f, 20f), rowArranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(rowB, 65f, 0f, 10f, 20f), rowArranged[1]);

            var columnReverseRootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.ColumnReverse,
                    justifyContent = JustifyContent.FlexStart,
                    mainGap = 5f,
                    crossGap = 5f,
                },
            });

            var colA = store.CreateNode(new FlexNodeModel
            {
                ParentId = columnReverseRootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(12f), height = FlexValue.Points(20f) },
            });

            var colB = store.CreateNode(new FlexNodeModel
            {
                ParentId = columnReverseRootId,
                IsExplicit = true,
                Style = new FlexStyle { width = FlexValue.Points(10f), height = FlexValue.Points(10f) },
            });

            var colArranged = FlexMeasure.ArrangeSingleLine(store, columnReverseRootId, 100f, 30f);
            Assert.AreEqual(new FlexItemLayoutResult(colA, 0f, 80f, 30f, 20f), colArranged[0]);
            Assert.AreEqual(new FlexItemLayoutResult(colB, 0f, 65f, 30f, 10f), colArranged[1]);
        }

        [Test]
        public void ArrangeSingleLine_Excludes_Children_Outside_Normal_Flow()
        {
            var store = new FlexNodeStore();
            var rootId = store.CreateNode(new FlexNodeModel
            {
                IsExplicit = true,
                Style = new FlexStyle
                {
                    flexDirection = FlexDirection.Row,
                },
            });

            store.CreateNode(new FlexNodeModel
            {
                ParentId = rootId,
                IsExplicit = true,
                Style = new FlexStyle
                {
                    width = FlexValue.Points(10f),
                    height = FlexValue.Points(10f),
                    positionType = PositionType.Absolute,
                },
            });

            var arranged = FlexMeasure.ArrangeSingleLine(store, rootId, 100f, 100f);

            Assert.AreEqual(0, arranged.Count);
        }

        [Test]
        public void DrivenTracker_Implicit_Child_Drives_Position_Only()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootLayout = root.AddComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(200f);
            rootLayout.style.height = FlexValue.Points(100f);

            var child = new GameObject("ImplicitChild", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);

            rootLayout.MarkLayoutDirty();

            var childRect = child.GetComponent<RectTransform>();
            var driven = GetDrivenProperties(childRect);
            Assert.IsTrue((driven & DrivenTransformProperties.AnchoredPositionX) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.AnchoredPositionY) != 0);
            Assert.IsFalse((driven & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsFalse((driven & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DrivenTracker_Implicit_Child_With_Grow_Default_Drives_Size()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNodeComponent));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var rootLayout = root.GetComponent<FlexLayout>();
            var rootNode = root.GetComponent<FlexNodeComponent>();
            rootLayout.style.width = FlexValue.Points(200f);
            rootLayout.style.height = FlexValue.Points(100f);
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.style.alignItems = AlignItems.FlexStart;
            rootLayout.implicitItemDefaults.flexGrow = 1f;
            rootNode.style.width = FlexValue.Points(200f);
            rootNode.style.height = FlexValue.Points(100f);

            var child = new GameObject("ImplicitChild", typeof(RectTransform));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(40f, 20f);
            childRect.SetParent(root.transform, false);

            rootLayout.MarkLayoutDirty();

            var driven = GetDrivenProperties(childRect);
            Assert.IsTrue((driven & DrivenTransformProperties.AnchoredPositionX) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.AnchoredPositionY) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaY) != 0);
            Assert.That(childRect.sizeDelta.x, Is.EqualTo(200f).Within(0.01f));
            Assert.That(childRect.sizeDelta.y, Is.EqualTo(20f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarkLayoutDirty_Repeated_With_ImplicitGrow_Is_Idempotent()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout), typeof(FlexNodeComponent));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var rootLayout = root.GetComponent<FlexLayout>();
            var rootNode = root.GetComponent<FlexNodeComponent>();
            rootLayout.style.width = FlexValue.Points(900f);
            rootLayout.style.height = FlexValue.Points(220f);
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.style.alignItems = AlignItems.FlexStart;
            rootLayout.style.justifyContent = JustifyContent.FlexStart;
            rootLayout.style.mainGap = 0f;
            rootLayout.style.crossGap = 0f;
            rootLayout.style.padding = new FlexEdges { left = 10f, right = 10f, top = 10f, bottom = 10f };
            rootLayout.implicitItemDefaults.width = FlexValue.Auto();
            rootLayout.implicitItemDefaults.height = FlexValue.Auto();
            rootLayout.implicitItemDefaults.flexGrow = 1f;
            rootLayout.implicitItemDefaults.flexShrink = 1f;
            rootLayout.implicitItemDefaults.flexBasis = FlexValue.Auto();
            rootLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexStart;

            rootNode.style.width = FlexValue.Points(900f);
            rootNode.style.height = FlexValue.Points(220f);

            var red = new GameObject("Red", typeof(RectTransform), typeof(FlexNodeComponent), typeof(FlexItem));
            var redRect = red.GetComponent<RectTransform>();
            redRect.anchorMin = Vector2.up;
            redRect.anchorMax = Vector2.up;
            redRect.pivot = Vector2.up;
            redRect.SetParent(root.transform, false);

            var redNode = red.GetComponent<FlexNodeComponent>();
            redNode.style.width = FlexValue.Points(0f);
            redNode.style.height = FlexValue.Points(180f);
            redNode.style.maxWidth = FlexOptionalFloat.Enabled(220f);
            redNode.style.positionType = PositionType.Relative;

            var redItem = red.GetComponent<FlexItem>();
            redItem.style.flexGrow = 1f;
            redItem.style.flexShrink = 1f;
            redItem.style.flexBasis = FlexValue.Auto();
            redItem.style.alignSelf = AlignSelf.FlexStart;

            var green = new GameObject("GreenImplicit", typeof(RectTransform));
            var greenRect = green.GetComponent<RectTransform>();
            greenRect.anchorMin = Vector2.up;
            greenRect.anchorMax = Vector2.up;
            greenRect.pivot = Vector2.up;
            greenRect.sizeDelta = new Vector2(120f, 180f);
            greenRect.SetParent(root.transform, false);

            rootLayout.MarkLayoutDirty();

            var expectedRedWidth = redRect.sizeDelta.x;
            var expectedGreenWidth = greenRect.sizeDelta.x;
            var expectedRootWidth = rootRect.sizeDelta.x;

            for (var i = 0; i < 8; i++)
            {
                rootLayout.MarkLayoutDirty();
                Assert.That(rootRect.sizeDelta.x, Is.EqualTo(expectedRootWidth).Within(0.01f));
                Assert.That(redRect.sizeDelta.x, Is.EqualTo(expectedRedWidth).Within(0.01f));
                Assert.That(greenRect.sizeDelta.x, Is.EqualTo(expectedGreenWidth).Within(0.01f));
            }

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarkLayoutDirty_Implicit_DefaultAlignSelfStretch_Changes_CrossSize()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var rootLayout = root.GetComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(200f);
            rootLayout.style.height = FlexValue.Points(100f);
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.style.alignItems = AlignItems.FlexStart;
            rootLayout.implicitItemDefaults.alignSelf = AlignSelf.Stretch;

            var child = new GameObject("ImplicitChild", typeof(RectTransform));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.sizeDelta = new Vector2(40f, 20f);
            childRect.SetParent(root.transform, false);

            rootLayout.MarkLayoutDirty();

            Assert.That(childRect.sizeDelta.x, Is.EqualTo(40f).Within(0.01f));
            Assert.That(childRect.sizeDelta.y, Is.EqualTo(100f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DrivenTracker_Explicit_Relative_Child_Drives_Position_And_Self_Size()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootLayout = root.AddComponent<FlexLayout>();
            var rootNode = root.AddComponent<FlexNodeComponent>();
            rootLayout.style.width = FlexValue.Points(200f);
            rootLayout.style.height = FlexValue.Points(100f);
            rootNode.style.width = FlexValue.Points(200f);
            rootNode.style.height = FlexValue.Points(100f);

            var child = new GameObject("RelativeChild", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            child.AddComponent<FlexLayout>();
            var childNode = child.AddComponent<FlexNodeComponent>();
            child.AddComponent<FlexItem>();
            childNode.style.positionType = PositionType.Relative;
            childNode.style.width = FlexValue.Points(60f);
            childNode.style.height = FlexValue.Points(30f);

            rootLayout.MarkLayoutDirty();

            var childRect = child.GetComponent<RectTransform>();
            var driven = GetDrivenProperties(childRect);
            Assert.IsTrue((driven & DrivenTransformProperties.AnchoredPositionX) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.AnchoredPositionY) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DrivenTracker_Explicit_Absolute_Child_Drives_Self_Size_Only()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootLayout = root.AddComponent<FlexLayout>();
            var rootNode = root.AddComponent<FlexNodeComponent>();
            rootLayout.style.width = FlexValue.Points(200f);
            rootLayout.style.height = FlexValue.Points(100f);
            rootNode.style.width = FlexValue.Points(200f);
            rootNode.style.height = FlexValue.Points(100f);

            var child = new GameObject("AbsoluteChild", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            child.AddComponent<FlexLayout>();
            var childNode = child.AddComponent<FlexNodeComponent>();
            child.AddComponent<FlexItem>();
            childNode.style.positionType = PositionType.Absolute;
            childNode.style.width = FlexValue.Points(60f);
            childNode.style.height = FlexValue.Points(30f);

            rootLayout.MarkLayoutDirty();

            var childRect = child.GetComponent<RectTransform>();
            var driven = GetDrivenProperties(childRect);
            Assert.IsFalse((driven & DrivenTransformProperties.AnchoredPositionX) != 0);
            Assert.IsFalse((driven & DrivenTransformProperties.AnchoredPositionY) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((driven & DrivenTransformProperties.SizeDeltaY) != 0);

            Object.DestroyImmediate(root);
        }

        public void RootLayout_Does_Not_Drive_Grandchild_Through_NonLayout_Intermediate()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var rootLayout = root.AddComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(120f);

            var middle = new GameObject("Middle", typeof(RectTransform));
            var middleRect = middle.GetComponent<RectTransform>();
            middleRect.anchorMin = Vector2.up;
            middleRect.anchorMax = Vector2.up;
            middleRect.pivot = Vector2.up;
            middleRect.sizeDelta = new Vector2(80f, 40f);
            middleRect.SetParent(root.transform, false);

            var grandchild = new GameObject("Grandchild", typeof(RectTransform));
            var grandchildRect = grandchild.GetComponent<RectTransform>();
            grandchildRect.anchorMin = Vector2.up;
            grandchildRect.anchorMax = Vector2.up;
            grandchildRect.pivot = Vector2.up;
            grandchildRect.sizeDelta = new Vector2(30f, 20f);
            grandchildRect.anchoredPosition = new Vector2(17f, -11f);
            grandchildRect.SetParent(middle.transform, false);

            rootLayout.MarkLayoutDirty();

            Assert.That(GetDrivenProperties(middleRect), Is.Not.EqualTo(DrivenTransformProperties.None));
            Assert.AreEqual(DrivenTransformProperties.None, GetDrivenProperties(grandchildRect));
            Assert.That(grandchildRect.anchoredPosition.x, Is.EqualTo(17f).Within(0.01f));
            Assert.That(grandchildRect.anchoredPosition.y, Is.EqualTo(-11f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void NestedLayout_Drives_Its_Own_Direct_Child_While_Root_Does_Not_Skip_Levels()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var rootLayout = root.AddComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(120f);
            rootLayout.style.flexDirection = FlexDirection.Row;

            var middle = new GameObject("Middle", typeof(RectTransform));
            var middleRect = middle.GetComponent<RectTransform>();
            middleRect.anchorMin = Vector2.up;
            middleRect.anchorMax = Vector2.up;
            middleRect.pivot = Vector2.up;
            middleRect.SetParent(root.transform, false);

            var middleLayout = middle.AddComponent<FlexLayout>();
            middleLayout.style.width = FlexValue.Points(100f);
            middleLayout.style.height = FlexValue.Points(60f);
            middleLayout.style.flexDirection = FlexDirection.Row;
            middleLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexStart;

            var grandchild = new GameObject("Grandchild", typeof(RectTransform));
            var grandchildRect = grandchild.GetComponent<RectTransform>();
            grandchildRect.anchorMin = Vector2.up;
            grandchildRect.anchorMax = Vector2.up;
            grandchildRect.pivot = Vector2.up;
            grandchildRect.sizeDelta = new Vector2(30f, 20f);
            grandchildRect.anchoredPosition = new Vector2(17f, -11f);
            grandchildRect.SetParent(middle.transform, false);

            rootLayout.MarkLayoutDirty();

            Assert.That(GetDrivenProperties(middleRect), Is.Not.EqualTo(DrivenTransformProperties.None));
            Assert.That(GetDrivenProperties(grandchildRect), Is.Not.EqualTo(DrivenTransformProperties.None));
            Assert.That(grandchildRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(grandchildRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarkLayoutDirty_Applies_Root_Size_From_Content_When_Auto()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.sizeDelta = Vector2.zero;

            var rootLayout = root.AddComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Auto();
            rootLayout.style.height = FlexValue.Auto();
            rootLayout.style.flexDirection = FlexDirection.Row;
            var rootNode = root.AddComponent<FlexNodeComponent>();
            rootNode.style.width = FlexValue.Auto();
            rootNode.style.height = FlexValue.Auto();

            var child = new GameObject("Child", typeof(RectTransform));
            var childRect = child.GetComponent<RectTransform>();
            childRect.SetParent(root.transform, false);
            childRect.anchorMin = new Vector2(0f, 1f);
            childRect.anchorMax = new Vector2(0f, 1f);
            childRect.pivot = new Vector2(0f, 1f);
            childRect.sizeDelta = new Vector2(40f, 20f);

            rootLayout.MarkLayoutDirty();

            Assert.AreEqual(new Vector2(40f, 20f), rootRect.sizeDelta);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarkLayoutDirty_Applies_Row_Child_Position_And_Size()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);

            var rootLayout = root.AddComponent<FlexLayout>();
            var rootNode = root.AddComponent<FlexNodeComponent>();
            rootLayout.style.width = FlexValue.Points(100f);
            rootLayout.style.height = FlexValue.Points(50f);
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexStart;
            rootNode.style.width = FlexValue.Points(100f);
            rootNode.style.height = FlexValue.Points(50f);
            rootLayout.style.mainGap = 10f;
            rootLayout.style.crossGap = 10f;

            var a = new GameObject("A", typeof(RectTransform));
            var aRect = a.GetComponent<RectTransform>();
            aRect.SetParent(root.transform, false);
            aRect.anchorMin = new Vector2(0f, 1f);
            aRect.anchorMax = new Vector2(0f, 1f);
            aRect.pivot = new Vector2(0f, 1f);
            aRect.sizeDelta = new Vector2(30f, 10f);

            var b = new GameObject("B", typeof(RectTransform));
            var bRect = b.GetComponent<RectTransform>();
            bRect.SetParent(root.transform, false);
            bRect.anchorMin = new Vector2(0f, 1f);
            bRect.anchorMax = new Vector2(0f, 1f);
            bRect.pivot = new Vector2(0f, 1f);
            bRect.sizeDelta = new Vector2(20f, 15f);

            rootLayout.MarkLayoutDirty();

            Assert.AreEqual(new Vector2(30f, 10f), aRect.sizeDelta);
            Assert.AreEqual(new Vector2(0f, 0f), aRect.anchoredPosition);
            Assert.AreEqual(new Vector2(20f, 15f), bRect.sizeDelta);
            Assert.AreEqual(new Vector2(40f, 0f), bRect.anchoredPosition);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarkLayoutDirty_Does_Not_Move_Absolute_Child()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);

            var rootLayout = root.AddComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(100f);
            rootLayout.style.height = FlexValue.Points(50f);
            rootLayout.style.flexDirection = FlexDirection.Row;

            var child = new GameObject("Absolute", typeof(RectTransform));
            var childRect = child.GetComponent<RectTransform>();
            childRect.SetParent(root.transform, false);
            childRect.anchorMin = new Vector2(0f, 1f);
            childRect.anchorMax = new Vector2(0f, 1f);
            childRect.pivot = new Vector2(0f, 1f);
            childRect.sizeDelta = new Vector2(20f, 10f);

            child.AddComponent<FlexLayout>();
            var childNode = child.AddComponent<FlexNodeComponent>();
            child.AddComponent<FlexItem>();
            childNode.style.width = FlexValue.Points(20f);
            childNode.style.height = FlexValue.Points(10f);
            childNode.style.positionType = PositionType.Absolute;
            childRect.anchoredPosition = new Vector2(13f, -17f);

            rootLayout.MarkLayoutDirty();

            Assert.AreEqual(new Vector2(20f, 10f), childRect.sizeDelta);
            Assert.AreEqual(new Vector2(13f, -17f), childRect.anchoredPosition);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarkLayoutDirty_Implicit_Preserves_Size_While_Updating_Position()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var implicitChild = new GameObject("ImplicitChild", typeof(RectTransform));
            var implicitRect = implicitChild.GetComponent<RectTransform>();
            implicitRect.anchorMin = Vector2.zero;
            implicitRect.anchorMax = Vector2.zero;
            implicitRect.pivot = Vector2.up;
            implicitRect.SetParent(root.transform, false);
            implicitRect.sizeDelta = new Vector2(80f, 40f);

            var rootLayout = root.GetComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(120f);
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.style.alignItems = AlignItems.FlexStart;
            rootLayout.implicitItemDefaults.alignSelf = AlignSelf.FlexStart;
            rootLayout.style.padding = new FlexEdges { left = 5f, top = 3f };

            rootLayout.MarkLayoutDirty();

            Assert.That(implicitRect.anchorMin, Is.EqualTo(Vector2.up));
            Assert.That(implicitRect.anchorMax, Is.EqualTo(Vector2.up));
            Assert.That(implicitRect.sizeDelta.x, Is.EqualTo(80f).Within(0.01f));
            Assert.That(implicitRect.sizeDelta.y, Is.EqualTo(40f).Within(0.01f));
            Assert.That(implicitRect.anchoredPosition.x, Is.EqualTo(5f).Within(0.01f));
            Assert.That(implicitRect.anchoredPosition.y, Is.EqualTo(-3f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Disable_Parent_FlexLayout_Refreshes_Child_Tracker_Owner()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FlexLayout));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.SetParent(root.transform, false);

            var rootLayout = root.GetComponent<FlexLayout>();
            var childLayout = child.GetComponent<FlexLayout>();
            var rootNode = root.AddComponent<FlexNodeComponent>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(100f);
            rootNode.style.width = FlexValue.Points(300f);
            rootNode.style.height = FlexValue.Points(100f);
            var childNode = child.AddComponent<FlexNodeComponent>();
            child.AddComponent<FlexItem>();
            childNode.style.width = FlexValue.Points(80f);
            childNode.style.height = FlexValue.Points(30f);
            childNode.style.positionType = PositionType.Relative;

            rootLayout.MarkLayoutDirty();
            childRect.anchoredPosition = new Vector2(77f, -11f);
            rootLayout.MarkLayoutDirty();
            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));

            rootLayout.enabled = false;
            childRect.anchoredPosition = new Vector2(77f, -11f);
            childLayout.MarkLayoutDirty();
            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(77f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(-11f).Within(0.01f));
            Assert.That(childRect.sizeDelta.x, Is.EqualTo(80f).Within(0.01f));
            Assert.That(childRect.sizeDelta.y, Is.EqualTo(30f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Disabled_Child_FlexLayout_Is_Not_Driven_By_Parent()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FlexLayout));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.SetParent(root.transform, false);

            var rootLayout = root.GetComponent<FlexLayout>();
            var childLayout = child.GetComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(100f);
            childLayout.style.width = FlexValue.Points(80f);
            childLayout.style.height = FlexValue.Points(30f);
            childLayout.style.positionType = PositionType.Relative;

            rootLayout.MarkLayoutDirty();
            childRect.anchoredPosition = new Vector2(51f, -19f);
            rootLayout.MarkLayoutDirty();
            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));

            childLayout.enabled = false;
            childRect.anchoredPosition = new Vector2(51f, -19f);
            rootLayout.MarkLayoutDirty();
            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(51f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(-19f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Disabled_Child_FlexLayout_At_Initialization_Is_Not_Driven_By_Parent()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FlexLayout));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.SetParent(root.transform, false);

            var rootLayout = root.GetComponent<FlexLayout>();
            var childLayout = child.GetComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(100f);
            childLayout.style.width = FlexValue.Points(80f);
            childLayout.style.height = FlexValue.Points(30f);
            childLayout.style.positionType = PositionType.Relative;
            childLayout.enabled = false;

            childRect.anchoredPosition = new Vector2(41f, -23f);
            rootLayout.MarkLayoutDirty();

            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(41f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(-23f).Within(0.01f));
            Assert.That(GetDrivenProperties(childRect), Is.EqualTo(DrivenTransformProperties.None));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Disabled_FlexLayout_At_Initialization_Has_No_Self_Tracker()
        {
            var go = new GameObject("Node", typeof(RectTransform), typeof(FlexLayout));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(120f, 60f);

            var layout = go.GetComponent<FlexLayout>();
            layout.style.width = FlexValue.Points(120f);
            layout.style.height = FlexValue.Points(60f);
            layout.enabled = false;

            rect.anchoredPosition = new Vector2(17f, -29f);
            rect.sizeDelta = new Vector2(133f, 77f);

            Assert.That(rect.anchoredPosition.x, Is.EqualTo(17f).Within(0.01f));
            Assert.That(rect.anchoredPosition.y, Is.EqualTo(-29f).Within(0.01f));
            Assert.That(rect.sizeDelta.x, Is.EqualTo(133f).Within(0.01f));
            Assert.That(rect.sizeDelta.y, Is.EqualTo(77f).Within(0.01f));
            Assert.That(GetDrivenProperties(rect), Is.EqualTo(DrivenTransformProperties.None));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void RefreshDrivenProperties_Does_Not_ReTrack_When_Disabled()
        {
            var go = new GameObject("Node", typeof(RectTransform), typeof(FlexLayout));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;

            var layout = go.GetComponent<FlexLayout>();
            layout.style.width = FlexValue.Points(120f);
            layout.style.height = FlexValue.Points(60f);
            layout.MarkLayoutDirty();

            var drivenBeforeDisable = GetDrivenProperties(rect);
            Assert.That(drivenBeforeDisable, Is.EqualTo(DrivenTransformProperties.None));

            layout.enabled = false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var refreshMethod = typeof(FlexLayout).GetMethod("RefreshDrivenProperties", flags);
            Assert.NotNull(refreshMethod);
            refreshMethod.Invoke(layout, null);

            Assert.That(GetDrivenProperties(rect), Is.EqualTo(DrivenTransformProperties.None));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Enabling_Child_FlexLayout_Rebuilds_On_Editor_Delay_Tick()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FlexLayout));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.SetParent(root.transform, false);

            var rootLayout = root.GetComponent<FlexLayout>();
            var childLayout = child.GetComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(120f);
            childLayout.style.width = FlexValue.Points(80f);
            childLayout.style.height = FlexValue.Points(30f);
            childLayout.style.positionType = PositionType.Relative;

            rootLayout.MarkLayoutDirty();
            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));

            childLayout.enabled = false;
            childRect.anchoredPosition = new Vector2(57f, -13f);
            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(57f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(-13f).Within(0.01f));

            childLayout.enabled = true;

#if UNITY_EDITOR
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var applyEnableMethod = typeof(FlexLayout).GetMethod("ApplyEditorDirty", flags);
            Assert.NotNull(applyEnableMethod);
            applyEnableMethod.Invoke(childLayout, null);
#endif

            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

#if UNITY_EDITOR
        [Test]
        public void OnValidate_ChildPositionTypeToRelative_ImmediatelyRefreshesParentTracker()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FlexLayout));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.SetParent(root.transform, false);

            var rootLayout = root.GetComponent<FlexLayout>();
            var rootNode = root.AddComponent<FlexNodeComponent>();
            var childLayout = child.GetComponent<FlexLayout>();
            var childNode = child.AddComponent<FlexNodeComponent>();
            child.AddComponent<FlexItem>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(120f);
            rootNode.style.width = FlexValue.Points(300f);
            rootNode.style.height = FlexValue.Points(120f);
            childNode.style.width = FlexValue.Points(80f);
            childNode.style.height = FlexValue.Points(30f);
            childNode.style.positionType = PositionType.Absolute;

            rootLayout.MarkLayoutDirty();
            var drivenAsAbsolute = GetDrivenProperties(childRect);
            Assert.IsFalse((drivenAsAbsolute & DrivenTransformProperties.AnchoredPositionX) != 0);
            Assert.IsFalse((drivenAsAbsolute & DrivenTransformProperties.AnchoredPositionY) != 0);
            Assert.IsTrue((drivenAsAbsolute & DrivenTransformProperties.SizeDeltaX) != 0);
            Assert.IsTrue((drivenAsAbsolute & DrivenTransformProperties.SizeDeltaY) != 0);

            childNode.style.positionType = PositionType.Relative;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var validateMethod = typeof(FlexNodeComponent).GetMethod("OnValidate", flags);
            Assert.NotNull(validateMethod);
            validateMethod.Invoke(childNode, null);

            var applyMethod = typeof(FlexLayout).GetMethod("ApplyEditorDirty", flags);
            Assert.NotNull(applyMethod);
            applyMethod.Invoke(rootLayout, null);

            var drivenAsRelative = GetDrivenProperties(childRect);
            Assert.IsTrue((drivenAsRelative & DrivenTransformProperties.AnchoredPositionX) != 0);
            Assert.IsTrue((drivenAsRelative & DrivenTransformProperties.AnchoredPositionY) != 0);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void RequestLayoutDirty_EditMode_Queues_Delayed_Rebuild()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FlexLayout));
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.up;
            childRect.anchorMax = Vector2.up;
            childRect.pivot = Vector2.up;
            childRect.SetParent(root.transform, false);

            var rootLayout = root.GetComponent<FlexLayout>();
            var childLayout = child.GetComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(120f);
            childLayout.style.width = FlexValue.Points(80f);
            childLayout.style.height = FlexValue.Points(30f);
            childLayout.style.positionType = PositionType.Relative;

            rootLayout.MarkLayoutDirty();
            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));

            childRect.anchoredPosition = new Vector2(53f, -9f);
            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(53f).Within(0.01f));

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var requestMethod = typeof(FlexLayout).GetMethod("RequestLayoutDirty", flags, null, System.Type.EmptyTypes, null);
            Assert.NotNull(requestMethod);
            requestMethod.Invoke(childLayout, null);

            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(53f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(-9f).Within(0.01f));

            var applyMethod = typeof(FlexLayout).GetMethod("ApplyEditorDirty", flags);
            Assert.NotNull(applyMethod);
            applyMethod.Invoke(rootLayout, null);

            Assert.That(childRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(childRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void OnValidate_FlexDirectionToColumn_Produces_No_SendMessage_Warning_And_Applies_Layout()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var childA = new GameObject("ChildA", typeof(RectTransform), typeof(FlexLayout));
            var childARect = childA.GetComponent<RectTransform>();
            childARect.anchorMin = Vector2.up;
            childARect.anchorMax = Vector2.up;
            childARect.pivot = Vector2.up;
            childARect.SetParent(root.transform, false);

            var childB = new GameObject("ChildB", typeof(RectTransform), typeof(FlexLayout));
            var childBRect = childB.GetComponent<RectTransform>();
            childBRect.anchorMin = Vector2.up;
            childBRect.anchorMax = Vector2.up;
            childBRect.pivot = Vector2.up;
            childBRect.SetParent(root.transform, false);

            var rootLayout = root.GetComponent<FlexLayout>();
            var rootNode = root.AddComponent<FlexNodeComponent>();
            var childALayout = childA.GetComponent<FlexLayout>();
            var childBLayout = childB.GetComponent<FlexLayout>();
            var childANode = childA.AddComponent<FlexNodeComponent>();
            childA.AddComponent<FlexItem>();
            var childBNode = childB.AddComponent<FlexNodeComponent>();
            childB.AddComponent<FlexItem>();
            rootLayout.style.width = FlexValue.Points(300f);
            rootLayout.style.height = FlexValue.Points(140f);
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootNode.style.width = FlexValue.Points(300f);
            rootNode.style.height = FlexValue.Points(140f);
            childANode.style.width = FlexValue.Points(80f);
            childANode.style.height = FlexValue.Points(30f);
            childBNode.style.width = FlexValue.Points(60f);
            childBNode.style.height = FlexValue.Points(40f);

            rootLayout.MarkLayoutDirty();
            Assert.That(childBRect.anchoredPosition.x, Is.GreaterThan(0f));
            Assert.That(childBRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));

            rootLayout.style.flexDirection = FlexDirection.Column;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var validateMethod = typeof(FlexLayout).GetMethod("OnValidate", flags);
            Assert.NotNull(validateMethod);
            validateMethod.Invoke(rootLayout, null);

            // If OnValidate tries to write RectTransform directly, Unity emits warning.
            LogAssert.NoUnexpectedReceived();

            var applyMethod = typeof(FlexLayout).GetMethod("ApplyEditorDirty", flags);
            Assert.NotNull(applyMethod);
            applyMethod.Invoke(rootLayout, null);

            Assert.That(childBRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(childBRect.anchoredPosition.y, Is.LessThan(0f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void OnValidate_FlexNode_Does_Not_Write_Rect_Immediately_And_Emits_No_SendMessage_Warning()
        {
            var go = new GameObject("Node", typeof(RectTransform), typeof(FlexNodeComponent));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(10f, 10f);

            var node = go.GetComponent<FlexNodeComponent>();
            node.style.width = FlexValue.Points(123f);
            node.style.height = FlexValue.Points(45f);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var validateMethod = typeof(FlexNodeComponent).GetMethod("OnValidate", flags);
            Assert.NotNull(validateMethod);
            validateMethod.Invoke(node, null);

            // OnValidate must not write RectTransform directly.
            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(new Vector2(10f, 10f), rect.sizeDelta);

            // Delayed editor apply performs actual standalone sizing.
            var delayedApplyMethod = typeof(FlexNodeComponent).GetMethod("ApplyStandaloneSizingOnEditorDelayCall", flags);
            Assert.NotNull(delayedApplyMethod);
            delayedApplyMethod.Invoke(node, null);
            Assert.AreEqual(new Vector2(123f, 45f), rect.sizeDelta);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AspectRatioInspector_Enable_Handler_Refreshes_Value_From_Current_Rect()
        {
            var go = new GameObject("Node", typeof(RectTransform), typeof(FlexNodeComponent));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(120f, 60f);

            var node = go.GetComponent<FlexNodeComponent>();
            node.style.aspectRatio = new FlexOptionalFloat
            {
                enabled = false,
                value = 9f,
            };

            var serializedObject = new SerializedObject(node);
            var aspectProperty = serializedObject.FindProperty("style.aspectRatio");
            Assert.NotNull(aspectProperty);

            var inspectorType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEngine.UI.Flex.Editor.FlexNodeInspector", false))
                .FirstOrDefault(type => type != null);
            Assert.NotNull(inspectorType);

            var inspector = Editor.CreateEditor(node, inspectorType);
            Assert.NotNull(inspector);
            var onEnabledValueChanged = inspectorType.GetMethod("OnAspectRatioEnabledValueChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onEnabledValueChanged);
            var evt = ChangeEvent<bool>.GetPooled(false, true);
            onEnabledValueChanged.Invoke(inspector, new object[] { evt, aspectProperty });

            serializedObject.Update();
            Assert.That(serializedObject.FindProperty("style.aspectRatio.value").floatValue, Is.EqualTo(2f).Within(0.001f));

            Object.DestroyImmediate(inspector);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EditMode_Update_Detects_Implicit_Size_Change_And_Queues_Rebuild()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(FlexLayout));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.up;
            rootRect.anchorMax = Vector2.up;
            rootRect.pivot = Vector2.up;

            var childA = new GameObject("ChildA", typeof(RectTransform));
            var childARect = childA.GetComponent<RectTransform>();
            childARect.anchorMin = Vector2.up;
            childARect.anchorMax = Vector2.up;
            childARect.pivot = Vector2.up;
            childARect.SetParent(root.transform, false);
            childARect.sizeDelta = new Vector2(80f, 30f);

            var childB = new GameObject("ChildB", typeof(RectTransform));
            var childBRect = childB.GetComponent<RectTransform>();
            childBRect.anchorMin = Vector2.up;
            childBRect.anchorMax = Vector2.up;
            childBRect.pivot = Vector2.up;
            childBRect.SetParent(root.transform, false);
            childBRect.sizeDelta = new Vector2(60f, 30f);

            var rootLayout = root.GetComponent<FlexLayout>();
            rootLayout.style.width = FlexValue.Points(400f);
            rootLayout.style.height = FlexValue.Points(120f);
            rootLayout.style.flexDirection = FlexDirection.Row;
            rootLayout.style.mainGap = 10f;
            rootLayout.style.crossGap = 10f;

            rootLayout.MarkLayoutDirty();
            Assert.That(childBRect.anchoredPosition.x, Is.EqualTo(90f).Within(0.01f));

            childARect.sizeDelta = new Vector2(140f, 30f);
            Assert.That(childBRect.anchoredPosition.x, Is.EqualTo(90f).Within(0.01f));

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var updateMethod = typeof(FlexLayout).GetMethod("Update", flags);
            Assert.NotNull(updateMethod);
            updateMethod.Invoke(rootLayout, null);

            var applyMethod = typeof(FlexLayout).GetMethod("ApplyEditorDirty", flags);
            Assert.NotNull(applyMethod);
            applyMethod.Invoke(rootLayout, null);

            Assert.That(childBRect.anchoredPosition.x, Is.EqualTo(150f).Within(0.01f));

            Object.DestroyImmediate(root);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator OfficialHorizontalLayoutGroup_Driven_Child_Size_Is_Not_Persisted_In_Scene_File()
        {
            const string tempScenePath = "Assets/Scenes/__Temp_OfficialLayout_SaveTest.unity";

            var previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(tempScenePath) != null)
                {
                    AssetDatabase.DeleteAsset(tempScenePath);
                }

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var root = new GameObject("Root", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.up;
                rootRect.anchorMax = Vector2.up;
                rootRect.pivot = Vector2.up;
                rootRect.sizeDelta = new Vector2(300f, 200f);

                var group = root.GetComponent<HorizontalLayoutGroup>();
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = false;
                group.spacing = 0f;
                group.padding = new RectOffset(0, 0, 0, 0);

                var child = new GameObject("Child", typeof(RectTransform), typeof(LayoutElement));
                var childRect = child.GetComponent<RectTransform>();
                childRect.SetParent(root.transform, false);
                childRect.anchorMin = Vector2.zero;
                childRect.anchorMax = Vector2.zero;
                childRect.pivot = new Vector2(0.5f, 0.5f);
                childRect.sizeDelta = new Vector2(100f, 100f);

                var layoutElement = child.GetComponent<LayoutElement>();
                layoutElement.preferredWidth = 100f;
                layoutElement.preferredHeight = 100f;

                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                Assert.That(childRect.sizeDelta.x, Is.EqualTo(100f).Within(0.01f));
                Assert.That(childRect.sizeDelta.y, Is.EqualTo(100f).Within(0.01f));

                Assert.IsTrue(EditorSceneManager.SaveScene(scene, tempScenePath));

                var serializedSize = ReadSceneSerializedSizeDelta(tempScenePath, "Child");
                Assert.That(serializedSize.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(serializedSize.y, Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);

                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(tempScenePath) != null)
                {
                    AssetDatabase.DeleteAsset(tempScenePath);
                    AssetDatabase.Refresh();
                }
            }

            yield break;
        }

#endif
#endif

        private static FlexNodeId CreateTestContainer(FlexNodeStore store, FlexDirection direction, FlexWrap wrap)
        {
            var style = FlexStyle.Default;
            style.flexDirection = direction;
            style.flexWrap = wrap;
            style.alignItems = AlignItems.Stretch;
            style.alignContent = AlignContent.Stretch;
            style.justifyContent = JustifyContent.FlexStart;
            style.mainGap = 4f;
            style.crossGap = 2f;
            style.padding = FlexEdges.Zero;

            return store.CreateNode(new FlexNodeModel
            {
                ParentId = default,
                Style = style,
                IsExplicit = true,
                HasNodeSource = true,
                HasItemSource = true,
                IsContainer = true,
                HasLayoutComponent = true,
            });
        }

        private static FlexNodeId CreateTestLeaf(FlexNodeStore store, FlexNodeId parentId, float width, float height)
        {
            var style = FlexStyle.Default;
            style.width = FlexValue.Points(width);
            style.height = FlexValue.Points(height);
            style.positionType = PositionType.Relative;

            return store.CreateNode(new FlexNodeModel
            {
                ParentId = parentId,
                Style = style,
                IsExplicit = true,
                HasNodeSource = true,
                HasItemSource = true,
                IsContainer = false,
                HasLayoutComponent = false,
                ContentWidth = width,
                ContentHeight = height,
                ImplicitRectWidth = width,
                ImplicitRectHeight = height,
            });
        }

        private static Vector2 ReadSceneSerializedSizeDelta(string scenePath, string gameObjectName)
        {
            var lines = File.ReadAllLines(scenePath);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains($"m_Name: {gameObjectName}"))
                {
                    continue;
                }

                for (var j = i; j < lines.Length && j < i + 40; j++)
                {
                    var line = lines[j].Trim();
                    if (!line.StartsWith("m_SizeDelta:"))
                    {
                        continue;
                    }

                    var xToken = "x:";
                    var yToken = "y:";
                    var xStart = line.IndexOf(xToken, System.StringComparison.Ordinal);
                    var yStart = line.IndexOf(yToken, System.StringComparison.Ordinal);
                    if (xStart < 0 || yStart < 0)
                    {
                        break;
                    }

                    var xText = line.Substring(xStart + xToken.Length, yStart - (xStart + xToken.Length)).Trim().TrimEnd(',');
                    var yText = line.Substring(yStart + yToken.Length).Trim().TrimEnd('}');
                    return new Vector2(float.Parse(xText), float.Parse(yText));
                }
            }

            Assert.Fail($"Failed to find serialized sizeDelta for '{gameObjectName}' in scene '{scenePath}'.");
            return default;
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
