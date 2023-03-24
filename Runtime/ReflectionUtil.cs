using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace KBCore.Refs {
    internal class ReflectionUtil {

        internal struct AttributedField<T>
            where T : Attribute {
            public T Attribute;
            public FieldInfo FieldInfo;
        }

        internal static void GetFieldsWithAttributeFromType<T>(
            Type classToInspect,
            List<AttributedField<T>> output,
            BindingFlags reflectionFlags = BindingFlags.Default
        )
            where T : Attribute {
            Type type = typeof(T);
            do {
                FieldInfo[] allFields = classToInspect.GetFields(reflectionFlags);
                for (int f = 0; f < allFields.Length; f++) {
                    FieldInfo fieldInfo = allFields[f];
                    Attribute[] attributes = Attribute.GetCustomAttributes(fieldInfo);
                    for (int a = 0; a < attributes.Length; a++) {
                        Attribute attribute = attributes[a];
                        if (!type.IsInstanceOfType(attribute))
                            continue;

                        output.Add(new AttributedField<T> {
                            Attribute = attribute as T,
                            FieldInfo = fieldInfo
                        });
                        break;
                    }
                }

                classToInspect = classToInspect.BaseType;
            }
            while (classToInspect != null);
        }

#if UNITY_EDITOR

         /// <summary>
        /// Get the serialized property from a Decorator drawer.
        /// Uses reflection, so cache if possible.
        /// must be called after geochanged.
        /// </summary>
        /// <param name="rootElement"></param>
        /// <returns>the serialized property of the related property field</returns>
        public static SerializedProperty GetBindedPropertyFromDecorator(VisualElement rootElement) {
            PropertyField propertyField = rootElement.GetFirstAncestorOfType<PropertyField>();
            if (propertyField == null) {
                Debug.LogError($"GetBindedPropertyFromDecorator mustbe called from a decorator root after GeometryChanged! no propertyField for '{rootElement.name}'");
                return null;
            }
            // try to get on inspector
            InspectorElement inspectorElement = propertyField.GetFirstAncestorOfType<InspectorElement>();
            if (inspectorElement == null) {
                Debug.LogError($"GetBindedPropertyFromDecorator {rootElement.name} inspectorElement null");
                return null;
            }
            // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/UIElements/Inspector/InspectorElement.cs
            if (TryGetValue<SerializedObject>(inspectorElement, "boundObject", out SerializedObject so)) {
                SerializedProperty serializedPropertyI = so.FindProperty(propertyField.bindingPath);
                if (serializedPropertyI != null) {
                    return serializedPropertyI;
                }
            }

            // try to get on custom inspectorfield 
            // InspectorField inspectorField = propertyField.GetFirstAncestorOfType<InspectorField>();
            // if (inspectorField != null) {
            //     SerializedProperty serializedPropertyIF = inspectorField.SerializedObject.FindProperty(propertyField.bindingPath);
            //     if (serializedPropertyIF != null) {
            //         return serializedPropertyIF;
            //     }
            // }

            // try to get on editor
            VisualElement editorElement = inspectorElement.parent;
            if (editorElement == null) {
                Debug.LogError($"GetBindedPropertyFromDecorator {rootElement.name} {inspectorElement.name} editorElement null");
                return null;
            }
            // EditorElement is internal, so get the editor via reflection
            if (!TryGetValue<Editor>(editorElement, "editor", out Editor editor)) {
                Debug.LogError($"GetBindedPropertyFromDecorator {rootElement.name} {editorElement.name} editor null");
                return null;
            }

            SerializedObject serializedObject = editor.serializedObject;
            if (serializedObject == null) {
                Debug.LogError($"GetBindedSPropFromDecorator {rootElement.name} {editorElement.name} serializedObject null");
                return null;
            }
            SerializedProperty serializedProperty = serializedObject.FindProperty(propertyField.bindingPath);
            if (serializedProperty == null) {
                Debug.LogError($"GetBindedSPropFromDecorator {rootElement.name} {editor} {propertyField.bindingPath} serializedProperty null");
                return null;
            }
            return serializedProperty;
        }

#endif


        public static BindingFlags defFlags = BindingFlags.Public
                                                    | BindingFlags.NonPublic
                                                    | BindingFlags.Instance
                                                    | BindingFlags.Static;


        /// <summary>
        /// attempts to set a value on an object at a given path
        /// </summary>
        /// <param name="value">value to set target to</param>
        /// <param name="target">object the value is on</param>
        /// <param name="memberPath">path of the property (nesting is '.' delimited)</param>
        /// <returns>true if successful</returns>
        public static bool TrySetValue(object value, object target, string memberPath) {
            if (TryGetMemberInfo(ref target, memberPath, defFlags, out var memberInfo)) {
                return TrySetValue(value, target, memberInfo);
            }
            return false;
        }
        public static bool TryCallMethod(object target, string memberPath, object[] parameters = null) {
            if (TryGetMemberInfo(ref target, memberPath, defFlags, out var memberInfo)) {
                return TryCallMethod(target, memberInfo, parameters);
            }
            return false;
        }
        /// <summary>
        /// Attempts to get a value on an object at a given path (supports nesting)
        /// </summary>
        /// <param name="target">object the value is on</param>
        /// <param name="memberPath">path of the property (nesting is '.' delimited)</param>
        /// <param name="value">output. gives the wanted value</param>
        /// <typeparam name="T">type of wanted value</typeparam>
        /// <returns>true if successful</returns>
        public static bool TryGetValue<T>(object target, string memberPath, out T value) {
            return TryGetValue<T>(target, memberPath, defFlags, out value);
        }
        /// <summary>
        /// Attempts to get a value on an object at a given path (supports nesting)
        /// </summary>
        /// <param name="target">object the value is on</param>
        /// <param name="memberPath">path of the property (nesting is '.' delimited)</param>
        /// <param name="flags">custom binding flags </param>
        /// <param name="value">output. gives the wanted value</param>
        /// <typeparam name="T">type of wanted value</typeparam>
        /// <returns>true if successful</returns>
        public static bool TryGetValue<T>(object target, string memberPath, BindingFlags flags, out T value) {
            if (TryGetMemberInfo(ref target, memberPath, flags, out var memberInfo)) {
                return TryGetValue<T>(target, memberInfo, out value);
            }
            value = default;
            return false;
        }
        static bool TryGetMemberInfo(ref object target, string memberPath, BindingFlags flags, out MemberInfo memberInfo) {
            if (target == null) {
                memberInfo = default;
                return false;
            }
            if (memberPath.Contains('.')) {
                // nested
                string[] splitpath = memberPath.Split('.', 2);
                // get child target and try again on that
                // Debug.LogWarning("TryGetValue path " + memberPath);
                const string arrayStr = "Array";
                if (splitpath[1].Contains(arrayStr) && splitpath[1].Split('.', 2)[0].Equals(arrayStr)) {
                    // this element is an enumerable
                    var newpath = splitpath[1].Split('.', 2)[1];
                    // can't use IEnumerable<System.Object> because it might be a struct
                    bool gotarray = TryGetValue<IEnumerable>(target, splitpath[0], defFlags, out var ntargets);
                    if (!gotarray) {
                        Debug.LogWarning($"TryGetValue Failed to get enumerable {memberPath} on {target}");
                        memberInfo = default; return false;
                    }
                    // "data[" = 5 characters
                    newpath = newpath.Remove(0, 5);
                    var t = newpath.Split(']', 2);
                    int arrayIndex = 0;
                    if (!int.TryParse(t[0], out arrayIndex)) {
                        Debug.LogWarning($"TryGetValue Failed to parse path {memberPath} on {target}");
                        memberInfo = default; return false;
                    }
                    newpath = t[1];
                    if (newpath.Contains('.')) {
                        // no more path inside of array
                        newpath = newpath.Remove(0, 1);
                    } else {
                        Debug.LogWarning("TryGetValue array base member check");
                        // value = default; return false;
                    }
                    var enumerator = ntargets.GetEnumerator();
                    enumerator.MoveNext();// 0
                    for (int n = 0; n < arrayIndex; n++) {
                        enumerator.MoveNext();
                    }
                    var ntarget = enumerator.Current;// .ToArray()[arrayIndex];
                    target = ntarget;
                    return TryGetMemberInfo(ref target, newpath, flags, out memberInfo);
                } else {
                    // get nested object
                    TryGetValue<System.Object>(target, splitpath[0], defFlags, out var ntarget);
                    target = ntarget;
                    return TryGetMemberInfo(ref target, splitpath[1], flags, out memberInfo);
                }
            } else {
                MemberInfo checkMeminfo = GetMemberInfo(target.GetType(), memberPath, flags);
                if (checkMeminfo == null) {
                    memberInfo = default;
                    return false;
                }
                memberInfo = checkMeminfo;
                // Debug.Log($"found member {memberInfo} {target}");
                return true;
            }
        }
        /// <summary>
        /// Gets a value on an object given the MemberInfo
        /// </summary>
        /// <param name="target"></param>
        /// <param name="memberInfo"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>true if successful</returns>
        public static bool TryGetValue<T>(object target, MemberInfo memberInfo, out T value) {
            object obj;
            // Debug.Log($"{target}.{memberInfo.Name} checkt type {typeof(T)}!");
            if (memberInfo is FieldInfo) {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                // Debug.Log($"{target}-{fieldInfo.Name} ({fieldInfo.FieldType}) fcheck type {typeof(T)}");
                var targetObj = fieldInfo.IsStatic ? null : target;
                obj = fieldInfo.GetValue(targetObj);
            } else if (memberInfo is PropertyInfo) {
                PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                // Debug.Log($"{target}-{propertyInfo} ({propertyInfo.PropertyType}) pcheck type {typeof(T)}");
                obj = propertyInfo.GetValue(target);
            } else if (memberInfo is MethodInfo) {
                MethodInfo methodInfo = memberInfo as MethodInfo;
                var targetObj = methodInfo.IsStatic ? null : target;
                // Debug.Log($"{target}-{methodInfo.Name} ({methodInfo.ReturnType}) ncheck type {typeof(T)}");
                obj = methodInfo.Invoke(targetObj, new object[0]);
            } else {
                Debug.LogWarning($"Failed to find valid member info on '{target}' {memberInfo}");
                value = default;
                return false;
            }
            try {
                value = (T)obj;
                return true;
            } catch {
                Debug.LogWarning($"{target}.{memberInfo.Name} is not of type {typeof(T)}!");
                value = default;
                return false;
            }
        }
        public static bool TrySetValue(object value, object target, MemberInfo memberInfo) {
            if (memberInfo is FieldInfo) {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                var targetObj = fieldInfo.IsStatic ? null : target;
                // Debug.Log($"{target}-{fieldInfo.Name}({fieldInfo.FieldType}) fset to {value}({value.GetType()})");
                fieldInfo.SetValue(targetObj, value);
                // Debug.Log($"fset {value} = {fieldInfo}:{fieldInfo.GetValue(targetObj)}");
                // return value.Equals(fieldInfo.GetValue(targetObj));
                return true;
            } else if (memberInfo is PropertyInfo) {
                PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                if (propertyInfo.CanWrite && propertyInfo.SetMethod != null) {
                    propertyInfo.SetValue(target, value);
                    return true;
                } else {
                    Debug.LogWarning($"Cannot set value to a property without a setter on{target}.{memberInfo.Name}");
                    return false;
                }
            }
            Debug.LogWarning($"Cannot set value to {target}.{memberInfo}");
            return false;
        }
        public static bool TryCallMethod(object target, MemberInfo memberInfo, object[] parameters = null) {
            if (memberInfo is MethodInfo) {
                MethodInfo methodInfo = memberInfo as MethodInfo;
                var targetObj = methodInfo.IsStatic ? null : target;
                // Debug.Log($"{target}-{methodInfo.Name} ({methodInfo.ReturnType}) ncheck type {typeof(T)}");
                parameters ??= new object[0];
                methodInfo.Invoke(targetObj, parameters);
                return true;
            }
            Debug.LogWarning($"Cannot call method {target}.{memberInfo}");
            return false;
        }
        public static MemberInfo GetMemberInfo(Type objectType, string fname, Type matchType = null) {
            return GetMemberInfo(objectType, fname, defFlags, matchType);
        }
        public static MemberInfo GetMemberInfo(Type objectType, string fname, BindingFlags flags, Type matchType = null) {
            while (objectType != null && objectType != typeof(object)) {
                MemberInfo[] memberInfos = objectType.GetMember(fname, flags);
                if (memberInfos.Length > 0) {
                    MemberInfo memberInfo = memberInfos[0];
                    // Debug.Log("type is " + memberInfo.MemberType.ToString());
                    switch (memberInfo.MemberType) {
                        case MemberTypes.Field:
                            FieldInfo fieldInfo = objectType.GetField(fname, flags);
                            if (matchType != null && fieldInfo.FieldType != matchType) {
                                Debug.LogWarning($"GetValidFieldInfo Type Mismatch: expected:{matchType} found:{fieldInfo.FieldType}");
                            } else {
                                return fieldInfo;
                            }
                            break;
                        case MemberTypes.Property:
                            PropertyInfo propertyInfo = objectType.GetProperty(fname, flags);
                            if (matchType != null && propertyInfo.PropertyType != matchType) {
                                Debug.LogWarning($"GetMemberInfo Type Mismatch: expected:{matchType} found:{propertyInfo.PropertyType}");
                            } else {
                                return propertyInfo;
                            }

                            break;
                        case MemberTypes.Method:
                            MethodInfo methodInfo = objectType.GetMethod(fname, flags);
                            if (methodInfo.GetParameters().Length > 0) {
                                continue;
                            }
                            if (matchType != null && methodInfo.ReturnType != matchType) {
                                Debug.LogWarning($"GetMemberInfo Type Mismatch: expected:{matchType} found:{methodInfo.ReturnType}");
                            } else {
                                return methodInfo;
                            }
                            break;
                        default:
                            break;
                    }
                }
                objectType = objectType.BaseType;
            }
            return null;
        }
    }
}