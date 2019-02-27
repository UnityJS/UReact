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
    [CustomPropertyDrawer(typeof(DataBinding), true)]
    public class DataBindingDrawer : PropertyDrawer
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
        private const string modePath = "mode";
        private const string methodNamePath = "methodName";
        private const string argumentPath = "argument";

        string _headerText;
        SerializedProperty _prop;
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
                state._reorderableList.elementHeight = lineHeight + verticalSpacing * 2;
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

        static MethodInfo FindMethod(object target, string methodName, PersistentListenerMode mode)
        {
            switch (mode)
            {
                case PersistentListenerMode.Void:
                    return UnityEventBase.GetValidMethodInfo(target, methodName, new Type[0]);
                case PersistentListenerMode.Float:
                    return UnityEventBase.GetValidMethodInfo(target, methodName, new[] { typeof(float) });
                case PersistentListenerMode.Int:
                    return UnityEventBase.GetValidMethodInfo(target, methodName, new[] { typeof(int) });
                case PersistentListenerMode.Bool:
                    return UnityEventBase.GetValidMethodInfo(target, methodName, new[] { typeof(bool) });
                case PersistentListenerMode.String:
                    return UnityEventBase.GetValidMethodInfo(target, methodName, new[] { typeof(string) });
                //case PersistentListenerMode.Object:
                //    return  UnityEventBase.GetValidMethodInfo(target, methodName, new[] { argumentType ?? typeof(Object) });
                default:
                    return null;
            }
        }

        void DrawCall(Rect rect, int index, bool isactive, bool isfocused)
        {
            var propCall = _callsProp.GetArrayElementAtIndex(index);

            rect.height = lineHeight;
            rect.y += verticalSpacing;

            Rect functionRect = rect;
            functionRect.width *= 0.4f;

            Rect argRect = rect;
            argRect.xMin = functionRect.xMax + 5;

            var propTarget = propCall.FindPropertyRelative(targetPath);
            var propMethodName = propCall.FindPropertyRelative(methodNamePath);
            var propMode = propCall.FindPropertyRelative(modePath);
            var propArgument = propCall.FindPropertyRelative(argumentPath);
            var targetObject = propTarget.objectReferenceValue;

            Color c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            var modeEnum = GetMode(propMode);
            if (targetObject != null && !string.IsNullOrEmpty(propMethodName.stringValue))
                EditorGUI.PropertyField(argRect, propArgument, GUIContent.none);

            using (new EditorGUI.DisabledScope(targetObject == null))
            {
                EditorGUI.BeginProperty(functionRect, GUIContent.none, propMethodName);
                {
                    var buttonLabel = new StringBuilder();
                    if (targetObject == null || string.IsNullOrEmpty(propMethodName.stringValue))
                    {
                        buttonLabel.Append(strNoBinding);
                    }
                    else
                    {
                        if (FindMethod(targetObject, propMethodName.stringValue, modeEnum) == null)
                            buttonLabel.Append("<Missing>");
                        buttonLabel.Append(targetObject.GetType().Name);
                        if (!string.IsNullOrEmpty(propMethodName.stringValue))
                        {
                            buttonLabel.Append(".");
                            if (propMethodName.stringValue.StartsWith("set_"))
                                buttonLabel.Append(propMethodName.stringValue.Substring(4));
                            else
                                buttonLabel.Append(propMethodName.stringValue);
                        }
                    }

                    if (GUI.Button(functionRect, buttonLabel.ToString(), EditorStyles.popup))
                        BuildPopupList(targetObject, propCall).DropDown(functionRect);
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
            propCall.FindPropertyRelative(modePath).enumValueIndex = (int)PersistentListenerMode.Void;
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
            public PersistentListenerMode mode;
        }

        static IEnumerable<ValidMethodMap> CalculateMethodMap(Object target, Type[] t, bool allowSubclasses)
        {
            var validMethods = new List<ValidMethodMap>();
            if (target == null || t == null)
                return validMethods;

            Type componentType = target.GetType();
            var componentMethods = componentType.GetMethods().Where(x => !x.IsSpecialName).ToList();

            var wantedProperties = componentType.GetProperties().AsEnumerable();
            wantedProperties = wantedProperties.Where(x => x.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0 && x.GetSetMethod() != null);
            componentMethods.AddRange(wantedProperties.Select(x => x.GetSetMethod()));

            foreach (var componentMethod in componentMethods)
            {
                var componentParamaters = componentMethod.GetParameters();
                if (componentParamaters.Length != t.Length)
                    continue;

                if (componentMethod.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0)
                    continue;

                if (componentMethod.ReturnType != typeof(void))
                    continue;

                bool paramatersMatch = true;
                for (int i = 0; i < t.Length; i++)
                {
                    if (!componentParamaters[i].ParameterType.IsAssignableFrom(t[i]))
                        paramatersMatch = false;

                    if (allowSubclasses && t[i].IsAssignableFrom(componentParamaters[i].ParameterType))
                        paramatersMatch = true;
                }

                if (paramatersMatch)
                {
                    var vmm = new ValidMethodMap();
                    vmm.target = target;
                    vmm.methodInfo = componentMethod;
                    validMethods.Add(vmm);
                }
            }
            return validMethods;
        }

        static GenericMenu BuildPopupList(Object target, SerializedProperty propCall)
        {
            var targetToUse = target;
            if (targetToUse is Component)
                targetToUse = (target as Component).gameObject;


            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(strNoBinding),
                string.IsNullOrEmpty(propCall.FindPropertyRelative(methodNamePath).stringValue),
                ClearEventFunction,
                new UnityEventFunction(propCall, null, null, PersistentListenerMode.Void));

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

            methods.Clear();
            GetMethodsForTargetAndMode(target, new[] { typeof(float) }, methods, PersistentListenerMode.Float);
            GetMethodsForTargetAndMode(target, new[] { typeof(int) }, methods, PersistentListenerMode.Int);
            GetMethodsForTargetAndMode(target, new[] { typeof(string) }, methods, PersistentListenerMode.String);
            GetMethodsForTargetAndMode(target, new[] { typeof(bool) }, methods, PersistentListenerMode.Bool);
            GetMethodsForTargetAndMode(target, new[] { typeof(Color) }, methods, PersistentListenerMode.String);
            GetMethodsForTargetAndMode(target, new[] { typeof(Object) }, methods, PersistentListenerMode.Object);
            //GetMethodsForTargetAndMode(target, new Type[] { }, methods, PersistentListenerMode.Void);
            if (methods.Count > 0)
            {
                AddMethodsToMenu(menu, propCall, methods, targetName);
            }
        }

        private static void AddMethodsToMenu(GenericMenu menu, SerializedProperty propCall, List<ValidMethodMap> methods, string targetName)
        {
            IEnumerable<ValidMethodMap> orderedMethods = methods.OrderBy(e => e.methodInfo.Name.StartsWith("set_") ? 0 : 1).ThenBy(e => e.methodInfo.Name);
            foreach (var validMethod in orderedMethods)
                AddFunctionsForScript(menu, propCall, validMethod, targetName);
        }

        private static void GetMethodsForTargetAndMode(Object target, Type[] delegateArgumentsTypes, List<ValidMethodMap> methods, PersistentListenerMode mode)
        {
            IEnumerable<ValidMethodMap> newMethods = CalculateMethodMap(target, delegateArgumentsTypes, mode == PersistentListenerMode.Object);
            foreach (var m in newMethods)
            {
                var method = m;
                method.mode = mode;
                methods.Add(method);
            }
        }

        static void AddFunctionsForScript(GenericMenu menu, SerializedProperty propCall, ValidMethodMap method, string targetName)
        {
            PersistentListenerMode mode = method.mode;

            var propTarget = propCall.FindPropertyRelative(targetPath).objectReferenceValue;
            var propMethodName = propCall.FindPropertyRelative(methodNamePath).stringValue;
            var propMode = GetMode(propCall.FindPropertyRelative(modePath));

            var args = new StringBuilder();
            var count = method.methodInfo.GetParameters().Length;
            for (int index = 0; index < count; index++)
            {
                var methodArg = method.methodInfo.GetParameters()[index];
                args.Append(string.Format("{0}", GetTypeName(methodArg.ParameterType)));

                if (index < count - 1)
                    args.Append(", ");
            }

            var isCurrentlySet = propTarget == method.target
                && propMethodName == method.methodInfo.Name
                && mode == propMode;

            string path = GetFormattedMethodName(targetName, method.methodInfo.Name, args.ToString(), mode == PersistentListenerMode.EventDefined);
            menu.AddItem(new GUIContent(path),
                isCurrentlySet,
                SetEventFunction,
                new UnityEventFunction(propCall, method.target, method.methodInfo, mode));
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

        static string GetFormattedMethodName(string targetName, string methodName, string args, bool dynamic)
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
                    return string.Format("{0}/{2} {1}", targetName, methodName.Substring(4), args);
                else
                    return string.Format("{0}/{1} ({2})", targetName, methodName, args);
            }
        }

        static void SetEventFunction(object source)
        {
            ((UnityEventFunction)source).Assign();
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
            readonly PersistentListenerMode _mode;

            public UnityEventFunction(SerializedProperty prop, Object target, MethodInfo method, PersistentListenerMode mode)
            {
                _prop = prop;
                _target = target;
                _method = method;
                _mode = mode;
            }

            public void Assign()
            {
                _prop.FindPropertyRelative(targetPath).objectReferenceValue = _target;
                _prop.FindPropertyRelative(methodNamePath).stringValue = _method.Name;
                _prop.FindPropertyRelative(modePath).enumValueIndex = (int)_mode;
                _prop.serializedObject.ApplyModifiedProperties();
            }

            public void Clear()
            {
                _prop.FindPropertyRelative(methodNamePath).stringValue = null;
                _prop.FindPropertyRelative(modePath).enumValueIndex = (int)PersistentListenerMode.Void;
                _prop.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
