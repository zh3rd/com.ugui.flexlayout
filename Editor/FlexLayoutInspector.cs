using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.UI.Flex.Editor
{
    [CustomEditor(typeof(FlexLayout))]
    public sealed class FlexLayoutInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var containerSection = BuildContainerSection();
            var implicitDefaultsSection = BuildImplicitDefaultsSection();

            root.Add(new HelpBox(
                "FlexLayout controls container rules and implicit child item defaults. Use FlexNode for self size/position and FlexItem for per-node grow/shrink/basis.",
                HelpBoxMessageType.Info));
            root.Add(containerSection);
            root.Add(implicitDefaultsSection);
            root.Bind(serializedObject);

            return root;
        }

        private VisualElement BuildContainerSection()
        {
            var foldout = new Foldout
            {
                text = "As Container",
                value = true,
            };

            foldout.Add(CreateField("style.flexDirection"));
            foldout.Add(CreateField("style.flexWrap"));
            foldout.Add(CreateField("style.justifyContent"));
            foldout.Add(CreateField("style.alignItems"));
            foldout.Add(CreateField("style.alignContent"));
            foldout.Add(CreateField("style.mainGap"));
            foldout.Add(CreateField("style.crossGap"));
            foldout.Add(CreateField("style.padding"));

            return foldout;
        }

        private VisualElement BuildImplicitDefaultsSection()
        {
            var foldout = new Foldout
            {
                text = "Implicit Item Defaults",
                value = true,
            };

            foldout.Add(CreateField("implicitItemDefaults"));
            return foldout;
        }

        private PropertyField CreateField(string bindingPath)
        {
            return new PropertyField { bindingPath = bindingPath };
        }
    }
}
