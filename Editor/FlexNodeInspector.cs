using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.UI.Flex.Editor
{
    [CustomEditor(typeof(FlexNodeBase), true)]
    public sealed class FlexNodeInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            if (target is FlexText)
            {
                root.Add(new HelpBox(
                    "FlexText is a text-specific Flex node. It uses TMP preferred content measurement while exposing the same node size/position policy fields.",
                    HelpBoxMessageType.Info));
            }

            root.Add(new PropertyField { bindingPath = "style.width" });
            root.Add(new PropertyField { bindingPath = "style.height" });
            root.Add(CreateAspectRatioField());
            root.Add(new PropertyField { bindingPath = "style.minWidth" });
            root.Add(new PropertyField { bindingPath = "style.maxWidth" });
            root.Add(new PropertyField { bindingPath = "style.minHeight" });
            root.Add(new PropertyField { bindingPath = "style.maxHeight" });
            root.Add(new PropertyField { bindingPath = "style.positionType" });
            root.Bind(serializedObject);
            return root;
        }

        private FlexOptionalFloatPropertyField CreateAspectRatioField()
        {
            var property = serializedObject.FindProperty("style.aspectRatio");
            var field = new FlexOptionalFloatPropertyField(property);
            field.EnabledValueChanged += OnAspectRatioEnabledValueChanged;
            return field;
        }

        private void OnAspectRatioEnabledValueChanged(ChangeEvent<bool> evt, SerializedProperty property)
        {
            if (!evt.newValue || evt.previousValue == evt.newValue || property == null)
            {
                return;
            }

            SyncAspectRatioValueFromCurrentRect(property);
        }

        private static void SyncAspectRatioValueFromCurrentRect(SerializedProperty property)
        {
            foreach (var targetObject in property.serializedObject.targetObjects)
            {
                if (targetObject is not Component component || component.transform is not RectTransform rectTransform)
                {
                    continue;
                }

                var ratio = ResolveAspectRatioFromRect(rectTransform);
                if (ratio <= 0f)
                {
                    continue;
                }

                var targetSerializedObject = new SerializedObject(targetObject);
                var targetProperty = targetSerializedObject.FindProperty(property.propertyPath);
                var valueProperty = targetProperty?.FindPropertyRelative("value");
                if (valueProperty == null)
                {
                    continue;
                }

                targetSerializedObject.Update();
                valueProperty.floatValue = ratio;
                targetSerializedObject.ApplyModifiedProperties();
            }

            property.serializedObject.Update();
        }

        private static float ResolveAspectRatioFromRect(RectTransform rectTransform)
        {
            var width = ResolveAxisSize(rectTransform, RectTransform.Axis.Horizontal);
            var height = ResolveAxisSize(rectTransform, RectTransform.Axis.Vertical);
            if (width <= 0f || height <= 0f)
            {
                return 0f;
            }

            return width / height;
        }

        private static float ResolveAxisSize(RectTransform rectTransform, RectTransform.Axis axis)
        {
            var rectSize = axis == RectTransform.Axis.Horizontal
                ? rectTransform.rect.width
                : rectTransform.rect.height;
            if (rectSize > 0f)
            {
                return rectSize;
            }

            var sizeDelta = axis == RectTransform.Axis.Horizontal
                ? rectTransform.sizeDelta.x
                : rectTransform.sizeDelta.y;
            return Mathf.Abs(sizeDelta);
        }
    }
}
