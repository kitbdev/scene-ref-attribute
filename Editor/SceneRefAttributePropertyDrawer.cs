﻿using UnityEditor;
using UnityEngine;
using System.Reflection;
#if UNITY_2022_2_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace KBCore.Refs {
    /// <summary>
    /// Custom property drawer for the reference attributes, making them read-only.
    /// 
    /// Note: Does not apply to the Anywhere attribute as that needs to remain editable. 
    /// </summary>
    [CustomPropertyDrawer(typeof(GetAnywhereAttribute))]
    [CustomPropertyDrawer(typeof(GetOnSelfAttribute))]
    [CustomPropertyDrawer(typeof(GetOnChildAttribute))]
    [CustomPropertyDrawer(typeof(GetOnParentAttribute))]
    [CustomPropertyDrawer(typeof(GetInSceneAttribute))]
#if UNITY_2022_2_OR_NEWER
    // use a decorator to work on top level arrays
    public class SceneRefAttributePropertyDrawer : DecoratorDrawer {

        public static readonly string sceneRefPropFieldClass = "scene-ref-attribute";
        public static readonly string sceneRefDecoratorClass = "scene-ref-decorator";
        public static readonly string sceneRefHelpBoxClass = "scene-ref-help-box";

        VisualElement sceneRefDecorator;
        VisualElement propertyFieldVE;
        HelpBox missingRefBox;
        SerializedProperty sceneRefProp;
        InspectorElement inspectorElement;

        SceneRefAttribute sceneRefAttribute => (SceneRefAttribute)attribute;

        public override VisualElement CreatePropertyGUI() {
            sceneRefProp = null;

            sceneRefDecorator = new VisualElement();
            sceneRefDecorator.name = "SceneRefDecorator";
            sceneRefDecorator.AddToClassList(sceneRefDecoratorClass);

            missingRefBox = new HelpBox("Missing Reference!", HelpBoxMessageType.Error);
            missingRefBox.AddToClassList(sceneRefHelpBoxClass);
            missingRefBox.style.display = DisplayStyle.None;
            sceneRefDecorator.Add(missingRefBox);

            // get the property after the first layout change
            sceneRefDecorator.RegisterCallback<GeometryChangedEvent>(OnDecoratorGeometryChanged);
            return sceneRefDecorator;
        }

        private void OnDecoratorGeometryChanged(GeometryChangedEvent changedEvent) {
            sceneRefDecorator.UnregisterCallback<GeometryChangedEvent>(OnDecoratorGeometryChanged);

            // get the property field, as decorators dont have access by default
            PropertyField propertyField = sceneRefDecorator.GetFirstAncestorOfType<PropertyField>();
            if (propertyField == null) {
                Debug.LogError($"{this.GetType().Name} failed to find containing property!");
                return;
            }
            propertyField.AddToClassList(sceneRefPropFieldClass);
            if (propertyField.tooltip == null || propertyField.tooltip == "") {
                propertyField.tooltip = $"Reference from [{sceneRefAttribute.Loc.ToString()}] assigned in OnValidate";
            }
            propertyFieldVE = propertyField;
            if (propertyField.childCount > 1) {
                // the field is the first child after the decorator drawers container
                propertyFieldVE = propertyField[1];
            }

            if (!sceneRefAttribute.HasFlags(Flag.Editable)) {
                // disable the property
                propertyFieldVE.SetEnabled(false);
            }
            if (sceneRefAttribute.HasFlags(Flag.Hidden)) {
                propertyFieldVE.style.display = DisplayStyle.None;
            }

            UpdateField();

            // get inspector element to register an onvalidate callback
            inspectorElement = propertyField.GetFirstAncestorOfType<InspectorElement>();
            if (inspectorElement == null) {
                Debug.LogError($"SceneRefAttributePropertyDrawer - inspectorElement null!");
                return;
            }
            // this properly responds to all changes
            inspectorElement.RegisterCallback<SerializedPropertyChangeEvent>(OnUpdate);
            sceneRefDecorator.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }
        void OnUpdate(SerializedPropertyChangeEvent changeEvent) => UpdateField();
        void OnDetach(DetachFromPanelEvent detachFromPanelEvent) {
            inspectorElement.UnregisterCallback<SerializedPropertyChangeEvent>(OnUpdate);
            sceneRefProp = null;
        }
        void UpdateField() {
            sceneRefProp ??= ReflectionUtil.GetBindedPropertyFromDecorator(sceneRefDecorator);
            if (sceneRefProp != null) {
                // update helpbox

                // update display
                Object value = sceneRefProp.objectReferenceValue;
                bool hasRef = sceneRefAttribute.HasFlags(Flag.Optional) ||
                    !SceneRefAttributeValidator.IsEmptyOrNull(value, sceneRefProp.isArray);
                // Debug.Log($"sceneref {propertyFieldVE.name} has:{hasRef} opt{sceneRefAttribute.HasFlags(Flag.Optional)} val:{value?.ToString() ?? "none"}");
                missingRefBox.style.display = hasRef ? DisplayStyle.None : DisplayStyle.Flex;

                // update text
                // string typeName = mi?.ReflectedType?.Name ?? "";
                string typeName = sceneRefProp.type;
                var mi = ReflectionUtil.GetMemberInfo(sceneRefProp.serializedObject.targetObject.GetType(), sceneRefProp.propertyPath);
                if (mi != null && mi is FieldInfo fieldInfo) {
                    typeName = fieldInfo.FieldType.Name;
                }
                missingRefBox.text = $"Missing {typeName} reference '{sceneRefProp.propertyPath}' on {sceneRefAttribute.Loc}!";
            }
        }
    }
#else
    public class SceneRefAttributePropertyDrawer : PropertyDrawer {

        SceneRefAttribute sceneRefAttribute => (SceneRefAttribute)attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = sceneRefAttribute.HasFlags(Flag.Editable);
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label);
    }
#endif
}