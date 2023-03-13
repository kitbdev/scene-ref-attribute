#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#if UNITY_2022_2_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace KBCore.Refs
{
    /// <summary>
    /// Custom property drawer for the reference attributes, making them read-only.
    /// 
    /// Note: Does not apply to the Anywhere attribute as that needs to remain editable. 
    /// </summary>
    [CustomPropertyDrawer(typeof(SelfAttribute))]
    [CustomPropertyDrawer(typeof(ChildAttribute))]
    [CustomPropertyDrawer(typeof(ParentAttribute))]
#if UNITY_2022_2_OR_NEWER
    public class SceneRefAttributePropertyDrawer : DecoratorDrawer
    {
        
        public static readonly string sceneRefPropClass = "scene-ref-attribute";
        
        VisualElement sceneRefDecorator;

        public override VisualElement CreatePropertyGUI() {
            sceneRefDecorator = new VisualElement();
            sceneRefDecorator.name = "SceneRefDecorator";

            sceneRefDecorator.RegisterCallback<GeometryChangedEvent>(OnDecoratorGeometryChanged);
            return sceneRefDecorator;
        }

        private void OnDecoratorGeometryChanged(GeometryChangedEvent changedEvent) {
            // only need to do once
            sceneRefDecorator.UnregisterCallback<GeometryChangedEvent>(OnDecoratorGeometryChanged);
            // get the property field, as decorators dont have access by default
            PropertyField propertyField = sceneRefDecorator.GetFirstAncestorOfType<PropertyField>();
            if (propertyField == null) {
                Debug.LogError($"SceneRefAttributePropertyDrawer failed to find containing property! {sceneRefDecorator.name}");
                return;
            }
            propertyField.AddToClassList(sceneRefPropClass);
            // disable the property
            propertyField.SetEnabled(false);
        }
    }
#else
    public class SceneRefAttributePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) 
            => EditorGUI.GetPropertyHeight(property, label);
    }
#endif
}
#endif