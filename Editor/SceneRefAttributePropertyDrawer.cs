using UnityEditor;
using UnityEngine;
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

        public static readonly string sceneRefPropClass = "scene-ref-attribute";
        public static readonly string sceneRefDecoratorClass = "scene-ref-decorator";
        // public static readonly string warningLabelClass = "";

        VisualElement sceneRefDecorator;

        SceneRefAttribute sceneRefAttribute => (SceneRefAttribute)attribute;

        public override VisualElement CreatePropertyGUI() {
            sceneRefDecorator = new VisualElement();
            sceneRefDecorator.name = "SceneRefDecorator";
            sceneRefDecorator.AddToClassList(sceneRefDecoratorClass);

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
            propertyField.AddToClassList(sceneRefPropClass);

            if (sceneRefAttribute.HasFlags(Flag.Editable)){
                return;
            }
            // disable the property
            propertyField.SetEnabled(false);
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