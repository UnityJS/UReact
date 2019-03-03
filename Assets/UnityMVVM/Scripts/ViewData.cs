using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMVVM
{
    public class ViewData
    {
        public ViewData(string name) { this.name = name; }
        public string name;
        public object value;
        private Dictionary<View, List<DataBindingCall>> _calls = new Dictionary<View, List<DataBindingCall>>();
        public DataModelCall _dataModelCall = null;
        private bool _dirty = true;

        public void AttachView(View view)
        {
            if (_calls.ContainsKey(view))
            {
                Debug.LogError("ViewData try to detach view that already exist!");
                return;
            }
            var calls = new List<DataBindingCall>();
            _calls.Add(view, calls);
            view.AttachViewData(this, calls);
        }
        public void DetachView(View view)
        {
            if (!_calls.ContainsKey(view))
            {
                Debug.LogError("ViewData try to detach view that don't exist!");
                return;
            }
            _calls.Remove(view);
        }
        public void Clear()
        {
            _calls.Clear();
        }

       public T GetValue<T>()
        {
            var ret = value;
            if(_dataModelCall!=null)
                ret = _dataModelCall.GetValue();
            if (value == null) return default(T);
            //Debug.Log(name + " [" + _data[name].value.GetType().Name + "] [" + default(T).GetType().Name + "]");
            return (T)value;
        }

        public void SetValue(object value, bool dirtyCheck = true)
        {
            if (dirtyCheck)
            {
                if (value is bool)
                {
                    if (Convert.ToBoolean(value) == Convert.ToBoolean(this.value)) return;
                }
                else if (value is int || value is float)
                {
                    if (Convert.ToSingle(value) == Convert.ToSingle(this.value)) return;
                }
                else if (value is string)
                {
                    if (Convert.ToString(value) == Convert.ToString(this.value)) return;
                }
                else
                {
                    if (value == this.value) return;
                }
            }

            this.value = value;

            if (_dirty) return;
            _dirty = true;
            foreach (var it in _calls)
            {
                var calls = it.Value;
                var count = calls.Count;
                for (var i = 0; i < count; ++i)
                {
                    calls[i].SetDirty();
                }
            }
        }

        public void Update()
        {
            if (!_dirty) return;
            _dirty = false;
            foreach (var it in _calls)
            {
                var calls = it.Value;
                var count = calls.Count;
                for (var i = 0; i < count; ++i)
                {
                    calls[i].Update();
                }
            }
        }
    }
}