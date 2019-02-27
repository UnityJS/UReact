
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
        [SerializeField]
        public DataBinding dataBinding = new DataBinding();

        //[SerializeField]
        //public UnityjsDataBinding forIn = new UnityjsDataBinding();

        private ViewModel _viewModel;
        public ViewModel viewModel
        {
            get { return this._viewModel; }
        }

        public void AddDataBinding(Object target, string methodName, PersistentListenerMode mode, string argument)
        {
            var call = new PersistentCall(target, methodName, mode, argument);
            dataBinding.calls.Add(call);
            call.Init(this);
        }

        public void AttachViewModel(ViewModel viewModel)
        {
            if (viewModel == null) viewModel = ViewModel.global;
            if (viewModel == this._viewModel) return;
            DetachViewModel();
            this._viewModel = viewModel;
            viewModel.AttachView(this);
        }

        public void DetachViewModel()
        {
            if (this._viewModel)
            {
                this._viewModel.DetachView(this);
                this._viewModel = null;
            }
        }

        void Awake()
        {
            // 只有Awake后 才会调用OnDestroy，没有Awake且属于Global的View，需要Global ViewMode去强制调用和销毁
            //Debug.Log(this.gameObject.name + " Awake");
            var viewCalls = dataBinding.calls;
            var count = viewCalls.Count;
            for (var i = 0; i < count; ++i)
            {
                viewCalls[i].Init(this);
            }
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
