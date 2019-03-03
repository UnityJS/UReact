
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace UnityMVVM
{
    internal abstract class BaseInvokableGetter
    {
        public abstract object Invoke();
    }

    class InvokableGetter<T1> : BaseInvokableGetter
    {
        public delegate T1 GetterAction();
        protected event GetterAction Delegate;
        public InvokableGetter(object target, MethodInfo theFunction)
        {
            Delegate += (GetterAction)theFunction.CreateDelegate(typeof(GetterAction), target);
        }
        public override object Invoke()
        {
            return Delegate();
        }
    }

    public class DataModelCall
    {
        PersistentListenerMode mode;
        private BaseInvokableGetter _runtimeGetter = null;
        public string name;

        internal static BaseInvokableGetter GetRuntimeGetter(object target, MethodInfo method, PersistentListenerMode mode)
        {
            switch (mode)
            {
                //case PersistentListenerMode.EventDefined:
                //    return theEvent.GetDelegate(target, method);
                case PersistentListenerMode.Object:
                    return new InvokableGetter<object>(target, method);
                case PersistentListenerMode.Float:
                    return new InvokableGetter<float>(target, method);
                case PersistentListenerMode.Int:
                    return new InvokableGetter<int>(target, method);//UnityAction<int>(target, method, m_Arguments.intArgument);
                case PersistentListenerMode.String:
                    return new InvokableGetter<string>(target, method);//UnityAction<string>(target, method, m_Arguments.stringArgument);
                case PersistentListenerMode.Bool:
                    return new InvokableGetter<bool>(target, method);//UnityAction<bool>(target, method, m_Arguments.boolArgument);
                                                                     //case PersistentListenerMode.Void:
                                                                     //    return new InvokableCall(target, method);
            }
            return null;
        }

        public DataModelCall(object target, MethodInfo method, PersistentListenerMode mode, string argument)
        {
            name = argument;
            if (method != null)
                _runtimeGetter = GetRuntimeGetter(target, method, mode);
            this.mode = mode;
        }

        public object GetValue()
        {
            return _runtimeGetter.Invoke();
        }
    }

    [Serializable]
    public class DataModel
    {
        [SerializeField] private List<PersistentCall> calls = new List<PersistentCall>();

        public static MethodInfo GetGetterMethodInfo(object obj, string functionName)
        {
            var type = obj.GetType();
            while (type != typeof(object) && type != null)
            {
                var method = type.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
                if (method != null)
                {
                    if (method.GetParameters().Length != 0)
                    {
                        Debug.LogError("Parameters Length != 0");
                        return null;
                    }
                    return method;
                }
                type = type.BaseType;
            }
            return null;
        }

        public virtual void Rebuild(List<DataBindingCall> bindingCalls, List<DataModelCall> modelCalls, Dictionary<string, Object> modelTarget)
        {
            var count = calls.Count;
            for (var i = 0; i < count; ++i)
            {
                var persistentCall = calls[i];

                if (string.IsNullOrEmpty(persistentCall.methodName))
                {
                    modelTarget.Add(persistentCall.argument, persistentCall.target);
                }
                else
                {
                    if (persistentCall.modes.Length != 0) continue;
                    var setterMethodInfo = PersistentCall.FindMethod(persistentCall.target, persistentCall.methodName, persistentCall.modes);
                    if (setterMethodInfo == null) continue;
                    var getterMethodInfo = GetGetterMethodInfo(persistentCall.target, persistentCall.methodName);
                    if (getterMethodInfo == null) continue;
                    var bindingCall = new DataBindingCall(persistentCall.target, setterMethodInfo, persistentCall.modes, persistentCall.argument);
                    bindingCalls.Add(bindingCall);
                    var call = new DataModelCall(persistentCall.target, getterMethodInfo, persistentCall.modes[0], persistentCall.argument);
                    modelCalls.Add(call);
                }
            }
        }
    }
}
