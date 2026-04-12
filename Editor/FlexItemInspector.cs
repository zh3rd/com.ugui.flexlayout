using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using FlexItem = UnityEngine.UI.Flex.FlexItem;

namespace UnityEngine.UI.Flex.Editor
{
    [CustomEditor(typeof(FlexItem))]
    public sealed class FlexItemInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new PropertyField { bindingPath = "style.flexGrow" });
            root.Add(new PropertyField { bindingPath = "style.flexShrink" });
            root.Add(new PropertyField { bindingPath = "style.flexBasis" });
            root.Add(new PropertyField { bindingPath = "style.alignSelf" });
            root.Bind(serializedObject);
            return root;
        }
    }
}
