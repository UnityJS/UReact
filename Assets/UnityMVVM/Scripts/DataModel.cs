
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
        internal static BaseInvokableGetter GetRuntimeCall(object target, MethodInfo method)
        {
            var generic = typeof(InvokableGetter<>);
            var specific = generic.MakeGenericType(new Type[] { method.ReturnType });
            var ci = specific.GetConstructor(new[] { typeof(Object), typeof(MethodInfo) });
            return ci.Invoke(new object[] { target, method }) as BaseInvokableGetter;
        }
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
        public string name;
        PersistentListenerMode mode;
        private BaseInvokableGetter _runtimeGetter = null;

        public DataModelCall(object target, MethodInfo method, PersistentListenerMode mode, string argument)
        {
            name = argument;
            _runtimeGetter = BaseInvokableGetter.GetRuntimeCall(target, method);
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

        public virtual void Rebuild(GameObject gameObejct, List<DataBindingCall> bindingCalls, List<DataModelCall> modelCalls, Dictionary<string, Object> modelTarget)
        {
            var count = calls.Count;
            for (var i = 0; i < count; ++i)
            {
                var persistentCall = calls[i];
                var target = persistentCall.target;
                if (gameObejct != (target is Component ? (target as Component).gameObject : target))
                {
                    Debug.LogError(string.Format("Target {0}.{1} is invalid!", target.name, persistentCall.methodName));
                    continue;
                }
                if (string.IsNullOrEmpty(persistentCall.methodName))
                {
                    modelTarget.Add(persistentCall.argument, target);
                }
                else
                {
                    if (persistentCall.modes.Length != 0) continue;
                    var setterMethodInfo = PersistentCall.FindMethod(target, persistentCall.methodName, persistentCall.modes);
                    if (setterMethodInfo == null) continue;
                    var getterMethodInfo = GetGetterMethodInfo(target, persistentCall.methodName);
                    if (getterMethodInfo == null) continue;
                    var bindingCall = new DataBindingCall(target, setterMethodInfo, persistentCall.argument);
                    if (!bindingCall.isVaild()) continue;
                    bindingCalls.Add(bindingCall);
                    var call = new DataModelCall(target, getterMethodInfo, persistentCall.modes[0], persistentCall.argument);
                    modelCalls.Add(call);
                }
            }
        }
    }
}
