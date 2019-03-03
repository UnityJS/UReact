
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace UnityMVVM
{
    internal abstract class BaseInvokableCall
    {
        public abstract void Invoke(object arg);
    }

    class InvokableCall<T1> : BaseInvokableCall
    {
        protected event UnityAction<T1> Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
        {
            Delegate += (UnityAction<T1>)theFunction.CreateDelegate(typeof(UnityAction<T1>), target);
        }

        public override void Invoke(object arg)
        {
            if (arg != null && !(arg is T1))
                throw new ArgumentException("Passed argument 'arg' is of the wrong type. Type:" + arg.GetType() + " Expected:" + typeof(T1));
            //if (AllowInvoke(Delegate))
            Delegate((T1)arg);
        }
    }

    [Serializable]
    class PersistentCall
    {
        [SerializeField] public Object target;
        [SerializeField] public string methodName;
        [SerializeField] public PersistentListenerMode[] modes;
        [SerializeField] public string argument;

        internal static MethodInfo FindMethod(object target, string methodName, PersistentListenerMode[] modes)
        {
            //var type = typeof(Object);
            //if (!string.IsNullOrEmpty(call.arguments.unityObjectArgumentAssemblyTypeName))
            //    type = Type.GetType(call.arguments.unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);

            switch (modes[0])
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
            //call += (UnityAction<float>)theFunction.CreateDelegate(typeof(UnityAction<float>), target);
        }

        internal static BaseInvokableCall GetRuntimeCall(object target, MethodInfo method, PersistentListenerMode[] modes)
        {
            //var method = FindMethod(target, methodName, modes[0]);
            //if (method == null)
            //    return null;
            switch (modes[0])
            {
                //case PersistentListenerMode.EventDefined:
                //    return theEvent.GetDelegate(target, method);
                case PersistentListenerMode.Object:
                    return new InvokableCall<object>(target, method);
                case PersistentListenerMode.Float:
                    return new InvokableCall<float>(target, method);
                case PersistentListenerMode.Int:
                    return new InvokableCall<int>(target, method);//UnityAction<int>(target, method, m_Arguments.intArgument);
                case PersistentListenerMode.String:
                    return new InvokableCall<string>(target, method);//UnityAction<string>(target, method, m_Arguments.stringArgument);
                case PersistentListenerMode.Bool:
                    return new InvokableCall<bool>(target, method);//UnityAction<bool>(target, method, m_Arguments.boolArgument);
                                                                   //case PersistentListenerMode.Void:
                                                                   //    return new InvokableCall(target, method);
            }
            return null;
        }

    }
    public class DataBindingCall
    {
        PersistentListenerMode[] modes;
        private BaseInvokableCall _runtimeCall = null;
        private bool _dirty = true;
        private BaseExpression _expression = null;
        private ParserArgument _parser = new ParserArgument();

        public DataBindingCall(object target, MethodInfo method, PersistentListenerMode[] modes, string argument)
        {
            _runtimeCall = PersistentCall.GetRuntimeCall(target, method, modes);
            _expression = _parser.Parser(argument);
            this.modes = modes;
        }

        public bool AttachView(ViewData viewData/*, View view*/)
        {
            if (!_parser.dataExpressions.ContainsKey(viewData.name)) return false;
            _parser.dataExpressions[viewData.name].data = viewData;
            return true;
        }

        public void SetDirty()
        {
            //if (_dirty) return;
            _dirty = true;
        }

        public void Update()
        {
            if (!_dirty) return;
            _dirty = false;
            if (_expression != null)
                Invoke(_expression.GetValue());
        }

        public void Invoke(object value)
        {

            if (_runtimeCall != null)
            {
                switch (modes[0])
                {
                    case PersistentListenerMode.Object:
                        /*if (!(value is float))
                        {
                            return;
                        }*/
                        break;
                    case PersistentListenerMode.Float:
                        if (!(value is float))
                        {
                            value = Convert.ToSingle(value);
                        }
                        break;
                    case PersistentListenerMode.Int:
                        if (!(value is int))
                        {
                            value = Convert.ToInt32(value);
                        }
                        break;
                    case PersistentListenerMode.String:
                        if (!(value is string))
                        {
                            value = value.ToString();
                        }
                        break;

                    case PersistentListenerMode.Bool:
                        if (value is string)
                            value = (value != "flase" && value != "0");
                        else
                            value = Convert.ToBoolean(value);
                        break;
                }
                _runtimeCall.Invoke(value);
            }
        }
    }

    [Serializable]
    public class DataBinding
    {
        [SerializeField] private List<PersistentCall> calls = new List<PersistentCall>();

        public virtual void Rebuild(List<DataBindingCall> bindingCalls)
        {
            var count = calls.Count;
            for (var i = 0; i < count; ++i)
            {
                var persistentCall = calls[i];
                var methodInfo = PersistentCall.FindMethod(persistentCall.target, persistentCall.methodName, persistentCall.modes);
                if (methodInfo == null) continue;
                var bindingCall = new DataBindingCall(persistentCall.target, methodInfo, persistentCall.modes, persistentCall.argument);
                bindingCalls.Add(bindingCall);
            }
        }
        public void AddBinding(Object targetObj, string methodName, PersistentListenerMode[] modes, string argument)
        {
            var persistentCall = new PersistentCall();
            persistentCall.target = targetObj;
            persistentCall.methodName = methodName;
            persistentCall.modes = modes;
            persistentCall.argument = argument;
            calls.Add(persistentCall);
        }
    }
}
