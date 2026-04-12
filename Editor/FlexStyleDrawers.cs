using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.UI.Flex.Editor
{
    internal abstract class FlexCompositePropertyFieldBase : BaseField<object>
    {
        protected FlexCompositePropertyFieldBase(string label) : base(label, null)
        {
            AddToClassList(alignedFieldUssClassName);

            var input = this.Q(className: inputUssClassName);
            if (input == null)
            {
                return;
            }

            input.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
            InputContainer = input;
        }

        protected VisualElement InputContainer { get; }

        protected void ApplyInputLayoutRule()
        {
            if (InputContainer == null || InputContainer.childCount == 0)
            {
                return;
            }

            var first = InputContainer[0];
            first.style.marginLeft = 0f;
            first.style.marginRight = 3f;

            for (var i = 1; i < InputContainer.childCount; i++)
            {
                InputContainer[i].style.flexGrow = 1f;
            }
        }

        public override void SetValueWithoutNotify(object newValue)
        {
            base.SetValueWithoutNotify(newValue);
        }
    }

    internal sealed class FlexValuePropertyField : FlexCompositePropertyFieldBase
    {
        private readonly SerializedProperty modeProperty;
        private readonly FloatField valueField;

        public FlexValuePropertyField(SerializedProperty property) : base(property.displayName)
        {
            modeProperty = property.FindPropertyRelative("mode");
            var valueProperty = property.FindPropertyRelative("value");

            if (InputContainer == null)
            {
                return;
            }

            var modeField = new EnumField((FlexSizeMode)modeProperty.enumValueIndex);
            modeField.BindProperty(modeProperty);
            InputContainer.Add(modeField);

            valueField = new FloatField();
            valueField.BindProperty(valueProperty);
            InputContainer.Add(valueField);
            ApplyInputLayoutRule();

            RefreshVisibility();
            this.TrackPropertyValue(modeProperty, _ => RefreshVisibility());
        }

        private void RefreshVisibility()
        {
            if (valueField == null)
            {
                return;
            }

            var mode = (FlexSizeMode)modeProperty.enumValueIndex;
            valueField.style.display = mode == FlexSizeMode.Auto
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }

    internal sealed class FlexOptionalFloatPropertyField : FlexCompositePropertyFieldBase
    {
        private readonly SerializedProperty enabledProperty;
        private readonly FloatField valueField;
        public event Action<ChangeEvent<bool>, SerializedProperty> EnabledValueChanged;

        public FlexOptionalFloatPropertyField(SerializedProperty property) : base(property.displayName)
        {
            enabledProperty = property.FindPropertyRelative("enabled");
            var valueProperty = property.FindPropertyRelative("value");

            if (InputContainer == null)
            {
                return;
            }

            var enabledField = new UnityEngine.UIElements.Toggle();
            enabledField.BindProperty(enabledProperty);
            enabledField.RegisterValueChangedCallback(OnEnabledValueChanged);
            InputContainer.Add(enabledField);

            valueField = new FloatField();
            valueField.BindProperty(valueProperty);
            InputContainer.Add(valueField);
            ApplyInputLayoutRule();

            RefreshVisibility();
            this.TrackPropertyValue(enabledProperty, _ => RefreshVisibility());
        }

        private void RefreshVisibility()
        {
            if (valueField == null)
            {
                return;
            }

            valueField.style.display = enabledProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnEnabledValueChanged(ChangeEvent<bool> evt)
        {
            EnabledValueChanged?.Invoke(evt, enabledProperty.serializedObject.FindProperty(enabledProperty.propertyPath[..^".enabled".Length]));
            RefreshVisibility();
        }
    }

    [CustomPropertyDrawer(typeof(FlexValue))]
    public sealed class FlexValueDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new FlexValuePropertyField(property);
        }
    }

    [CustomPropertyDrawer(typeof(FlexOptionalFloat))]
    public sealed class FlexOptionalFloatDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new FlexOptionalFloatPropertyField(property);
        }
    }

    [CustomPropertyDrawer(typeof(FlexImplicitItemStyleData))]
    public sealed class FlexImplicitItemStyleDataDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.Add(new PropertyField(property.FindPropertyRelative("width")));
            root.Add(new PropertyField(property.FindPropertyRelative("height")));
            root.Add(new PropertyField(property.FindPropertyRelative("flexGrow")));
            root.Add(new PropertyField(property.FindPropertyRelative("flexShrink")));
            root.Add(new PropertyField(property.FindPropertyRelative("flexBasis")));
            root.Add(new PropertyField(property.FindPropertyRelative("alignSelf")));
            return root;
        }
    }
}
