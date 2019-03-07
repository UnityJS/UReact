
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
        protected BaseInvokableCall(object target, MethodInfo function)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (function == null)
                throw new ArgumentNullException("function");
        }

        public abstract void Invoke(object[] args);

        protected static void ThrowOnInvalidArg<T>(object arg)
        {
            if (arg != null && !(arg is T))
                throw new ArgumentException(string.Format("Passed argument 'args[0]' is of the wrong type. Type:{0} Expected:{1}", arg.GetType(), typeof(T)));
        }

        internal static BaseInvokableCall GetRuntimeCall(object target, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var count = parameters.Length;
            var types = new Type[count];
            for (var i = 0; i < count; ++i)
            {
                types[i] = parameters[i].ParameterType;
            }
            var generic = typeof(InvokableCall<>);
            var specific = generic.MakeGenericType(types);
            var ci = specific.GetConstructor(new[] { typeof(Object), typeof(MethodInfo) });
            return ci.Invoke(new object[] { target, method }) as BaseInvokableCall;
        }
    }

    class InvokableCall<T1> : BaseInvokableCall
    {
        protected event UnityAction<T1> Delegate;

        public InvokableCall(object target, MethodInfo theFunction) : base(target, theFunction)
        {
            Delegate += (UnityAction<T1>)theFunction.CreateDelegate(typeof(UnityAction<T1>), target);
        }

        public override void Invoke(object[] args)
        {
            if (args.Length != 1)
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 1");
            ThrowOnInvalidArg<T1>(args[0]);

            //if (AllowInvoke(Delegate))
            Delegate((T1)args[0]);
        }
    }

    class InvokableCall<T1, T2> : BaseInvokableCall
    {
        protected event UnityAction<T1, T2> Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            Delegate = (UnityAction<T1, T2>)theFunction.CreateDelegate(typeof(UnityAction<T1, T2>), target);
        }

        public override void Invoke(object[] args)
        {
            if (args.Length != 2)
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 2");
            ThrowOnInvalidArg<T1>(args[0]);
            ThrowOnInvalidArg<T2>(args[1]);

            //if (AllowInvoke(Delegate))
            Delegate((T1)args[0], (T2)args[1]);
        }
    }

    class InvokableCall<T1, T2, T3> : BaseInvokableCall
    {
        protected event UnityAction<T1, T2, T3> Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            Delegate = (UnityAction<T1, T2, T3>)theFunction.CreateDelegate(typeof(UnityAction<T1, T2, T3>), target);
        }

        public override void Invoke(object[] args)
        {
            if (args.Length != 3)
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 3");
            ThrowOnInvalidArg<T1>(args[0]);
            ThrowOnInvalidArg<T2>(args[1]);
            ThrowOnInvalidArg<T3>(args[2]);

            //if (AllowInvoke(Delegate))
            Delegate((T1)args[0], (T2)args[1], (T3)args[2]);
        }
    }

    class InvokableCall<T1, T2, T3, T4> : BaseInvokableCall
    {
        protected event UnityAction<T1, T2, T3, T4> Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            Delegate = (UnityAction<T1, T2, T3, T4>)theFunction.CreateDelegate(typeof(UnityAction<T1, T2, T3, T4>), target);
        }

        public override void Invoke(object[] args)
        {
            if (args.Length != 4)
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 4");
            ThrowOnInvalidArg<T1>(args[0]);
            ThrowOnInvalidArg<T2>(args[1]);
            ThrowOnInvalidArg<T3>(args[2]);
            ThrowOnInvalidArg<T4>(args[3]);

            //if (AllowInvoke(Delegate))
            Delegate((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[2]);
        }
    }

    [Serializable]
    class PersistentCall
    {
        [SerializeField] public Object target;
        [SerializeField] public string methodName;
        [SerializeField] public PersistentListenerMode[] modes;
        [SerializeField] public string argument;

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
        internal static MethodInfo FindMethod(object target, string methodName, PersistentListenerMode[] modes)
        {
            var count = modes.Length;
            var types = new Type[count];
            for (var i = 0; i < count; ++i)
            {
                types[i] = Mode2Type(modes[i]);
            }

            var method = UnityEventBase.GetValidMethodInfo(target, methodName, types);
            if (method == null)
                Debug.LogError(string.Format("Method({0}) is invalid", methodName));
            return method;
        }
    }

    public class DataBindingCall
    {
        private BaseInvokableCall _runtimeCall = null;
        private bool _dirty = true;
        private ArrayExpression _expression = null;
        private ParserArgument _parser = new ParserArgument();
        //private object[] args;
        private Type[] types;
        //private PersistentListenerMode[] modes;

        public DataBindingCall(object target, MethodInfo method, string argument)
        {
            var parameters = method.GetParameters();
            var count = parameters.Length;
            if (count == 0 || count > 4)
            {
                Debug.LogError(string.Format("Expression \"{1}\" is invalid parameters size({0})", count, argument));
                return;
            }
            _parser.Parser(argument);
            _expression = _parser.rootExpression;
            if (_expression == null)
            {
                Debug.LogError(string.Format("Expression parsing error! \"{0}\"", argument));
                return;
            }
            if (_expression.Length != count)
            {
                Debug.LogError(string.Format("Expression \"{2}\" is invalid parameters size({0}). Expected size is {1}", _expression.Length, count, argument));
                _expression = null;
                return;
            }

            types = new Type[count];
            for (var i = 0; i < count; ++i)
            {
                types[i] = parameters[i].ParameterType;
            }
            Type generic;
            if (count == 1) generic = typeof(InvokableCall<>);
            else if (count == 2) generic = typeof(InvokableCall<,>);
            else if (count == 3) generic = typeof(InvokableCall<,,>);
            else generic = typeof(InvokableCall<,,,>);

            var specific = generic.MakeGenericType(types);
            var ci = specific.GetConstructor(new[] { typeof(Object), typeof(MethodInfo) });
            _runtimeCall = ci.Invoke(new object[] { target, method }) as BaseInvokableCall;
        }

        public bool isVaild() { return _expression != null; }

        public bool AttachViewModel(ViewModel viewModel)
        {
            foreach (var it in _parser.dataExpressions)
            {
                var viewData = viewModel.GetViewData(it.Key);
                it.Value.data = viewData;
                viewData.AttachCall(this);
            }
            _dirty = true;
            return true;
        }

        public bool DetachViewModel(ViewModel viewModel)
        {
            foreach (var it in _parser.dataExpressions)
            {
                it.Value.data.DetachCall(this);
                it.Value.data=null;
            }
            _dirty = true;
            return true;
        }

        public void SetDirty()
        {
            _dirty = true;
        }

        private static Type type_float = typeof(float);
        private static Type type = typeof(float);
        public void Update()
        {
            if (!_dirty) return;
            _dirty = false;
            var args = _expression.GetValue() as object[];
            var count = types.Length;
            for (var i = 0; i < count; ++i)
            {
                //if(args[i]==null)
                //args[i] =
                if (types[i] != args[i].GetType())
                    args[i] = Convert.ChangeType(args[i], types[i]);
            }
            _runtimeCall.Invoke(args);
        }

        /*public void Invoke(object value)
        {

            if (_runtimeCall != null)
            {
                switch (modes[0])
                {
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
                            //if (value == null) value = "";
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

    }}*/
    }

    [Serializable]
    public class DataBinding
    {
        [SerializeField] private List<PersistentCall> calls = new List<PersistentCall>();

        public virtual void Rebuild(GameObject gameObejct, List<DataBindingCall> bindingCalls)
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
                var methodInfo = PersistentCall.FindMethod(target, persistentCall.methodName, persistentCall.modes);
                if (methodInfo == null) continue;
                var bindingCall = new DataBindingCall(target, methodInfo, persistentCall.argument);
                if (!bindingCall.isVaild()) continue;
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
