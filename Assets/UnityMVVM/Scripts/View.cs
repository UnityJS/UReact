
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace UnityMVVM
{
    public class View : MonoBehaviour
    {
        [SerializeField] private DataBinding dataBinding = new DataBinding();
        [SerializeField] private DataModel dataModel = new DataModel();
        private List<DataBindingCall> bindingCalls = new List<DataBindingCall>();
        private List<DataModelCall> modelCalls = new List<DataModelCall>();
        private Dictionary<string, Object> modelTarget = new Dictionary<string, Object>();
        private bool _callsDirty = true;

        private ViewModel _viewModel;
        public ViewModel viewModel
        {
            get { return this._viewModel; }
        }

        public void AddDataBinding(Object target, string methodName, PersistentListenerMode[] modes, string argument)
        {
            /*var methodInfo = PersistentCall.FindMethod(target, methodName, modes);
            if (methodInfo == null) return;
            var bindingCall = new DataBindingCall(target, methodInfo, modes, argument);
            bindingCalls.Add(bindingCall);*/
            dataBinding.AddBinding(target, methodName, modes, argument);
            _callsDirty = true;
        }

        public void AttachViewData(ViewData viewData, List<DataBindingCall> dataCalls)
        {
            var count = bindingCalls.Count;
            for (var i = 0; i < count; ++i)
            {
                var call = bindingCalls[i];
                if (call.AttachView(viewData))
                {
                    dataCalls.Add(call);
                    //Debug.Log("AttachView " + name + " View " + _calls.Count);
                }
            }
            //this.dataModel.Attach(viewData, dataCalls);
        }

        public void AttachViewModel(ViewModel viewModel)
        {
            if (viewModel == null) viewModel = ViewModel.global;
            if (viewModel == this._viewModel) return;
            DetachViewModel();
            this._viewModel = viewModel;
            if (_callsDirty)
            {
                bindingCalls.Clear();
                modelCalls.Clear();
                modelTarget.Clear();
                dataBinding.Rebuild(bindingCalls);
                dataModel.Rebuild(bindingCalls, modelCalls, modelTarget);
                _callsDirty = false;
            }
            viewModel.AttachView(this);

            foreach (var it in modelTarget)
            {
                viewModel.Set(it.Key, it.Value);
            }
            var count = modelCalls.Count;
            for (var i = 0; i < count; ++i)
            {
                var call = modelCalls[i];
                var viewData = viewModel.GetViewData(call.name);
                viewData._dataModelCall = call;
            }
        }

        public void DetachViewModel()
        {
            if (this._viewModel == null) return;
            this._viewModel.DetachView(this);
            this._viewModel = null;
        }

        void Awake()
        {
            // 只有Awake后 才会调用OnDestroy，没有Awake且属于Global的View，需要Global ViewMode去强制调用和销毁
            //Debug.Log(this.gameObject.name + " Awake");
            AttachViewModel(this.transform.GetComponentInParent<ViewModel>());
        }

        void OnDestroy()
        {
            //Debug.Log(this.gameObject.name + " OnDestroy");
            DetachViewModel();
        }
        void OnBeforeTransformParentChanged()
        {
            //Debug.Log(this.gameObject.name + " OnBeforeTransformParentChanged");
            DetachViewModel();
        }
        void OnTransformParentChanged()
        {
            //Debug.Log(this.gameObject.name + " OnTransformParentChanged");
            AttachViewModel(this.transform.GetComponentInParent<ViewModel>());
        }
    }
}
