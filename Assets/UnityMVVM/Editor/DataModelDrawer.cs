using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using EditorGUI = UnityEditor.EditorGUI;
using UnityEditorInternal;

namespace UnityMVVM
{
    [CustomPropertyDrawer(typeof(DataModel), true)]
    public class DataModelDrawer : PropertyDrawer
    {
        internal const float lineHeight = 16f;
        internal const int verticalSpacing = 2;
        protected class State
        {
            internal ReorderableList _reorderableList;
            public int lastSelectedIndex;
        }

        private const string strNoBinding = "No Binding";
        private const string targetPath = "target";
        private const string modesPath = "modes";
        private const string methodNamePath = "methodName";
        private const string argumentPath = "argument";

        string _headerText;
        SerializedProperty _prop;
        GameObject _targetGameObject;
        SerializedProperty _callsProp;
        ReorderableList _reorderableList;
        int _lastSelectedIndex;
        Dictionary<string, State> _states = new Dictionary<string, State>();

        private State GetState(SerializedProperty prop)
        {
            State state;
            string key = prop.propertyPath;
            _states.TryGetValue(key, out state);
            if (state == null || state._reorderableList.serializedProperty.serializedObject != prop.serializedObject)
            {
                if (state == null)
                    state = new State();

                SerializedProperty callsProp = prop.FindPropertyRelative("calls");
                state._reorderableList = new ReorderableList(prop.serializedObject, callsProp, false, true, true, true);
                state._reorderableList.drawHeaderCallback = DrawCallHeader;
                state._reorderableList.drawElementCallback = DrawCall;
                state._reorderableList.onSelectCallback = SelectCall;
                state._reorderableList.onReorderCallback = EndDragChild;
                state._reorderableList.onAddCallback = AddCall;
                state._reorderableList.onRemoveCallback = RemoveButton;
                state._reorderableList.elementHeight = lineHeight * 1 + verticalSpacing * 2;
                _states[key] = state;
            }
            return state;
        }

        private State RestoreState(SerializedProperty property)
        {
            State state = GetState(property);
            _callsProp = state._reorderableList.serializedProperty;
            _reorderableList = state._reorderableList;
            _lastSelectedIndex = state.lastSelectedIndex;
            _reorderableList.index = _lastSelectedIndex;
            return state;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _prop = property;
            var targetToUse = _prop.serializedObject.targetObject;
            if (targetToUse is Component)
                _targetGameObject = (targetToUse as Component).gameObject;
            else _targetGameObject = targetToUse as GameObject;

            _headerText = label.text;
            State state = RestoreState(property);
            OnGUI(position);
            state.lastSelectedIndex = _lastSelectedIndex;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            RestoreState(property);
            float height = 0f;
            if (_reorderableList != null)
            {
                height = _reorderableList.GetHeight();
            }
            return height;
        }

        public void OnGUI(Rect position)
        {
            if (_callsProp == null || !_callsProp.isArray)
                return;

            if (_reorderableList != null)
            {
                var oldIdentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                _reorderableList.DoList(position);
                EditorGUI.indentLevel = oldIdentLevel;
            }
        }

        protected virtual void DrawCallHeader(Rect headerRect)
        {
            headerRect.height = 16;
            GUI.Label(headerRect, (string.IsNullOrEmpty(_headerText) ? "Data Binding" : _headerText));
        }

        static PersistentListenerMode GetMode(SerializedProperty mode)
        {
            return (PersistentListenerMode)mode.enumValueIndex;
        }
        static PersistentListenerMode Type2Mode(Type type)
        {
            if (typeof(float).IsAssignableFrom(type)) return PersistentListenerMode.Float;
            if (typeof(int).IsAssignableFrom(type)) return PersistentListenerMode.Int;
            if (typeof(bool).IsAssignableFrom(type)) return PersistentListenerMode.Bool;
            if (typeof(string).IsAssignableFrom(type)) return PersistentListenerMode.String;
            if (typeof(Object).IsAssignableFrom(type)) return PersistentListenerMode.Object;
            return PersistentListenerMode.Void;
        }

        static Type Mode2Type(PersistentListenerMode mode)
        {
            switch (mode)
            {
                case PersistentListenerMode.Float: return typeof(float);
                case PersistentListenerMode.Int: return typeof(int);
                case PersistentListenerMode.Bool: return typeof(bool);
                case PersistentListenerMode.String: return typeof(string);
                case PersistentListenerMode.Object: return typeof(Object);
                default: return typeof(Object);
            }
        }
        static string GetMethodParameters(MethodInfo methodInfo)
        {
            var args = new StringBuilder();
            var componentParamaters = methodInfo.GetParameters();
            var count = componentParamaters.Length;
            for (int index = 0; index < count; index++)
            {
                args.Append(string.Format("{0}", GetTypeName(componentParamaters[index].ParameterType)));
                if (index < count - 1)
                    args.Append(", ");
            }
            return args.ToString();
        }

        void DrawCall(Rect rect, int index, bool isactive, bool isfocused)
        {
            var propCall = _callsProp.GetArrayElementAtIndex(index);

            rect.height = lineHeight;
            rect.y += verticalSpacing;

            Rect functionRect = rect;
            functionRect.width *= 0.6f;
            Rect argRect = rect;
            argRect.xMin = functionRect.xMax + 5;

            var propTarget = propCall.FindPropertyRelative(targetPath);
            var propMethodName = propCall.FindPropertyRelative(methodNamePath);
            var propModes = propCall.FindPropertyRelative(modesPath);
            var propArgument = propCall.FindPropertyRelative(argumentPath);
            var targetObject = propTarget.objectReferenceValue;

            Color c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            if (targetObject != null)
            {
                EditorGUI.PropertyField(argRect, propArgument, GUIContent.none);
                if (string.IsNullOrEmpty(propArgument.stringValue))
                {
                    GUI.Label(argRect, "Data Name", EditorStyles.centeredGreyMiniLabel);
                }
            }

            using (new EditorGUI.DisabledScope(targetObject == null))
            {
                EditorGUI.BeginProperty(functionRect, GUIContent.none, propMethodName);
                {
                    var buttonLabel = new StringBuilder();
                    if (targetObject == null)
                    {
                        buttonLabel.Append(strNoBinding);
                    }
                    else
                    {
                        if (_targetGameObject != (targetObject is Component ? (targetObject as Component).gameObject : targetObject))
                            buttonLabel.Append("<Missing>");
                        buttonLabel.Append(targetObject.GetType().Name);

                        if (!string.IsNullOrEmpty(propMethodName.stringValue))
                        {
                            var modeCount = propModes.arraySize;
                            var args = new StringBuilder();
                            var types = new Type[modeCount];
                            for (var i = 0; i < modeCount; ++i)
                            {
                                types[i] = Mode2Type((PersistentListenerMode)propModes.GetArrayElementAtIndex(i).enumValueIndex);
                                args.Append(GetTypeName(types[i]));
                                if (i < modeCount - 1)
                                    args.Append(", ");
                            }
                            buttonLabel.Append(".");
                            MethodInfo methodInfo = null;
                            if (_targetGameObject == targetObject is Component ? (targetObject as Component).gameObject : targetObject)
                                methodInfo = UnityEventBase.GetValidMethodInfo(targetObject, propMethodName.stringValue, types);
                            if (methodInfo == null)
                                buttonLabel.Append("<Missing>");
                            else if (string.IsNullOrEmpty(propArgument.stringValue))
                                GUI.Label(argRect, GetMethodParameters(methodInfo), EditorStyles.centeredGreyMiniLabel);
                            if (propMethodName.stringValue.StartsWith("set_"))
                                buttonLabel.Append(string.Format("{0} : {1}", propMethodName.stringValue.Substring(4), args.ToString()));
                            else
                                buttonLabel.Append(string.Format("{0} ({1})", propMethodName.stringValue, args.ToString()));
                        }
                    }

                    if (GUI.Button(functionRect, buttonLabel.ToString(), EditorStyles.popup))
                        BuildPopupList(_targetGameObject, propCall).DropDown(functionRect);
                }
                EditorGUI.EndProperty();
            }
            GUI.backgroundColor = c;
        }

        void RemoveButton(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            _lastSelectedIndex = list.index;
        }

        private void AddCall(ReorderableList list)
        {
            if (_callsProp.hasMultipleDifferentValues)
            {
                foreach (var targetObject in _callsProp.serializedObject.targetObjects)
                {
                    var temSerialziedObject = new SerializedObject(targetObject);
                    var callsProperty = temSerialziedObject.FindProperty(_callsProp.propertyPath);
                    callsProperty.arraySize += 1;
                    temSerialziedObject.ApplyModifiedProperties();
                }
                _callsProp.serializedObject.SetIsDifferentCacheDirty();
                _callsProp.serializedObject.Update();
                list.index = list.serializedProperty.arraySize - 1;
            }
            else
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);
            }

            _lastSelectedIndex = list.index;
            var propCall = _callsProp.GetArrayElementAtIndex(list.index);

            propCall.FindPropertyRelative(targetPath).objectReferenceValue = _prop.serializedObject.targetObject;
            propCall.FindPropertyRelative(methodNamePath).stringValue = null;
            propCall.FindPropertyRelative(modesPath).arraySize = 0;
            propCall.FindPropertyRelative(argumentPath).stringValue = null;
        }

        void SelectCall(ReorderableList list)
        {
            _lastSelectedIndex = list.index;
        }

        void EndDragChild(ReorderableList list)
        {
            _lastSelectedIndex = list.index;
        }

        struct ValidMethodMap
        {
            public Object target;
            public MethodInfo methodInfo;
            public PersistentListenerMode[] modes;
        }

        static GenericMenu BuildPopupList(Object target, SerializedProperty propCall)
        {
            var targetToUse = target;
            if (targetToUse is Component)
                targetToUse = (target as Component).gameObject;


            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(strNoBinding),
                targetToUse == null,
                ClearEventFunction,
                new UnityEventFunction(propCall, null, null));

            if (targetToUse == null)
                return menu;

            menu.AddSeparator("");

            GeneratePopUpForType(menu, targetToUse, false, propCall);
            if (targetToUse is GameObject)
            {
                Component[] comps = (targetToUse as GameObject).GetComponents<Component>();
                var duplicateNames = comps.Where(c => c != null).Select(c => c.GetType().Name).GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                foreach (Component comp in comps)
                {
                    if (comp == null)
                        continue;

                    GeneratePopUpForType(menu, comp, duplicateNames.Contains(comp.GetType().Name), propCall);
                }
            }

            return menu;
        }

        private static void GeneratePopUpForType(GenericMenu menu, Object target, bool useFullTargetName, SerializedProperty propCall)
        {
            var methods = new List<ValidMethodMap>();
            string targetName = useFullTargetName ? target.GetType().FullName : target.GetType().Name;

            Type[] t = new[] { typeof(float), typeof(int), typeof(string), typeof(bool), typeof(Color), typeof(Object) };
            PersistentListenerMode[] modeList = new[] { PersistentListenerMode.Float, PersistentListenerMode.Int, PersistentListenerMode.String, PersistentListenerMode.Bool, PersistentListenerMode.String, PersistentListenerMode.Object };
            if (target == null || t == null)
                return;

            Type componentType = target.GetType();

            var wantedProperties = componentType.GetProperties().AsEnumerable();
            wantedProperties = wantedProperties.Where(x => x.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0 && x.GetSetMethod() != null);
            var componentMethods = wantedProperties.Select(x => x.GetSetMethod());

            foreach (var componentMethod in componentMethods)
            {
                var componentParamaters = componentMethod.GetParameters();
                if (componentParamaters.Length != 1)
                    continue;

                if (componentMethod.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0)
                    continue;

                if (componentMethod.ReturnType != typeof(void))
                    continue;

                var modes = new PersistentListenerMode[componentParamaters.Length];

                bool paramatersMatch = true;
                for (int i = 0; i < componentParamaters.Length; i++)
                {
                    bool paramaterMatch = false;
                    for (int j = 0; j < t.Length; j++)
                    {
                        if (t[j].IsAssignableFrom(componentParamaters[i].ParameterType))
                        {
                            modes[i] = modeList[j];
                            paramaterMatch = true;
                            break;
                        }
                    }
                    if (!paramaterMatch)
                    {
                        paramatersMatch = false;
                        break;
                    }
                }

                if (paramatersMatch)
                {
                    var vmm = new ValidMethodMap();
                    vmm.target = target;
                    vmm.methodInfo = componentMethod;
                    vmm.modes = modes;
                    methods.Add(vmm);
                }
            }

            menu.AddItem(new GUIContent(targetName + "/Self"),
                 string.IsNullOrEmpty(propCall.FindPropertyRelative(methodNamePath).stringValue) && propCall.FindPropertyRelative(targetPath).objectReferenceValue == target,
                 SetTargetFunction,
                 new object[] { propCall, target });

            menu.AddSeparator(targetName + "/");

            if (methods.Count > 0)
            {
                var orderedFields = methods.OrderBy(e => e.methodInfo.Name);
                foreach (var validMethod in orderedFields)
                    AddFunctionsForScript(menu, propCall, validMethod, targetName, false);
            }
        }

        static void AddFunctionsForScript(GenericMenu menu, SerializedProperty propCall, ValidMethodMap method, string targetName, bool multiple)
        {
            var propTarget = propCall.FindPropertyRelative(targetPath).objectReferenceValue;
            var propMethodName = propCall.FindPropertyRelative(methodNamePath).stringValue;
            var propModes = propCall.FindPropertyRelative(modesPath);

            var isCurrentlySet = propTarget == method.target
                && propMethodName == method.methodInfo.Name
                && propModes.arraySize == method.modes.Length;
            if (isCurrentlySet)
            {
                for (var i = 0; i < propModes.arraySize; ++i)
                {
                    if ((PersistentListenerMode)propModes.GetArrayElementAtIndex(i).enumValueIndex != method.modes[i])
                    {
                        isCurrentlySet = false;
                        break;
                    }
                }
            }

            string path = GetFormattedMethodName(targetName, method.methodInfo.Name, GetMethodParameters(method.methodInfo), false, multiple);
            menu.AddItem(new GUIContent(path),
                isCurrentlySet,
                SetEventFunction,
                new UnityEventFunction(propCall, method.target, method.methodInfo));
        }

        private static string GetTypeName(Type t)
        {
            if (t == typeof(int))
                return "int";
            if (t == typeof(float))
                return "float";
            if (t == typeof(string))
                return "string";
            if (t == typeof(bool))
                return "bool";
            if (t == typeof(Color))
                return "color";
            return t.Name;
        }

        static string GetFormattedMethodName(string targetName, string methodName, string args, bool dynamic, bool multiple)
        {
            if (dynamic)
            {
                if (methodName.StartsWith("set_"))
                    return string.Format("{0}/{1}", targetName, methodName.Substring(4));
                else
                    return string.Format("{0}/{1}", targetName, methodName);
            }
            else
            {
                if (methodName.StartsWith("set_"))
                    return string.Format("{0}/{1} : {2}", targetName, methodName.Substring(4), args);
                else
                    return string.Format("{0}/{1} : {2}", targetName, methodName, args);
            }
        }

        static void SetEventFunction(object source)
        {
            ((UnityEventFunction)source).Assign();
        }
        static void SetTargetFunction(object source)
        {
            SerializedProperty propCall = ((object[])source)[0] as SerializedProperty;
            Object target = ((object[])source)[1] as Object;
            propCall.FindPropertyRelative(targetPath).objectReferenceValue = target;
            propCall.FindPropertyRelative(methodNamePath).stringValue = null;
            propCall.FindPropertyRelative(modesPath).arraySize = 0;
            propCall.serializedObject.ApplyModifiedProperties();
        }


        static void ClearEventFunction(object source)
        {
            ((UnityEventFunction)source).Clear();
        }

        struct UnityEventFunction
        {
            readonly SerializedProperty _prop;
            readonly Object _target;
            readonly MethodInfo _method;

            public UnityEventFunction(SerializedProperty prop, Object target, MethodInfo method)
            {
                _prop = prop;
                _target = target;
                _method = method;
            }

            public void Assign()
            {
                _prop.FindPropertyRelative(targetPath).objectReferenceValue = _target;
                _prop.FindPropertyRelative(methodNamePath).stringValue = _method.Name;

                var componentParamaters = _method.GetParameters();
                var count = componentParamaters.Length;
                var propModes = _prop.FindPropertyRelative(modesPath);
                propModes.arraySize = count;
                for (int i = 0; i < count; ++i)
                {
                    var propMode = propModes.GetArrayElementAtIndex(i);
                    propMode.enumValueIndex = (int)Type2Mode(componentParamaters[i].ParameterType);
                }
                _prop.serializedObject.ApplyModifiedProperties();
            }

            public void Clear()
            {
                _prop.FindPropertyRelative(methodNamePath).stringValue = null;
                _prop.FindPropertyRelative(modesPath).arraySize = 0;
                _prop.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
