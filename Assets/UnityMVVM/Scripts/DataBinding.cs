
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
    public class PersistentCall
    {
        public PersistentCall()
        {
        }
        public PersistentCall(Object target, string methodName, PersistentListenerMode mode, string argument)
        {
            this.target = target;
            this.methodName = methodName;
            this.mode = mode;
            this.argument = argument;
        }
        [SerializeField] private Object target;

        [SerializeField] private string methodName;

        [SerializeField] private PersistentListenerMode mode = PersistentListenerMode.Void;

        [SerializeField] private string argument;

        BaseInvokableCall runtimeCall = null;

        public View view = null;
        private bool _dirty = true;

        private BaseExpression expression = null;
        private ParserArgument _parser = new ParserArgument();

        public void Init(View view)
        {
            this.view = view;
            if (expression == null)
                expression = _parser.Parser(argument);
        }

        public bool AttachView(ViewData viewData, View view)
        {
            this.view = view;
            if (expression == null)
                expression = _parser.Parser(argument);
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
            if (expression != null)
                Invoke(expression.GetValue());
        }

        internal static MethodInfo FindMethod(object target, string methodName, PersistentListenerMode mode)
        {
            //var type = typeof(Object);
            //if (!string.IsNullOrEmpty(call.arguments.unityObjectArgumentAssemblyTypeName))
            //    type = Type.GetType(call.arguments.unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);

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
            //call += (UnityAction<float>)theFunction.CreateDelegate(typeof(UnityAction<float>), target);
        }

        internal BaseInvokableCall GetRuntimeCall()
        {
            var method = FindMethod(target, methodName, mode);
            if (method == null)
                return null;
            switch (mode)
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
                                                                   // case PersistentListenerMode.Void:
                                                                   //     return method.MyInvokableCall<object> (typeof(UnityAction), target);//UnityAction(target, method);
            }
            return null;
        }

        public void Invoke(object value)
        {
            if (runtimeCall == null)
            {
                runtimeCall = GetRuntimeCall();
            }

            if (runtimeCall != null)
            {
                switch (mode)
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
                            value = Convert.ToDouble(value);
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
                runtimeCall.Invoke(value);
            }
        }
    }

    [Serializable]
    public class DataBinding
    {
        [SerializeField] public List<PersistentCall> calls = new List<PersistentCall>();
    }
}
