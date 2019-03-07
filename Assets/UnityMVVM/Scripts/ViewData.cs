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
        private List<DataBindingCall> _calls = new List<DataBindingCall>();
        public DataModelCall _dataModelCall = null;
        private bool _dirty = false;

        public void AttachCall(DataBindingCall call)
        {
            if (_calls.IndexOf(call) != -1)
            {
                Debug.LogError("ViewData try to attach the same call!");
                return;
            }
            if (value != null) _dirty = true;
            _calls.Add(call);
        }
        public void DetachCall(DataBindingCall call)
        {
            if (_calls.IndexOf(call) == -1)
            {
                Debug.LogError("ViewData try to detach call that don't exist!");
                return;
            }
            _calls.Remove(call);
        }

        public T GetValue<T>()
        {
            var ret = value;
            if (_dataModelCall != null)
                ret = _dataModelCall.GetValue();
            if (value == null) return default(T);
            //Debug.Log(name + " [" + _data[name].value.GetType().Name + "] [" + default(T).GetType().Name + "]");
            return (T)value;
        }

        public void SetValue(object value, bool dirtyCheck = true)
        {
            if (dirtyCheck)
            {
                if (value == this.value) return;
                if (value != null && this.value != null)
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
            }

            //Debug.Log(name +" : "+ value.ToString());
            this.value = value;
            SetDirty();
        }

        public void SetDirty()
        {
            if (_dirty) return;
            _dirty = true;
            var count = _calls.Count;
            for (var i = 0; i < count; ++i)
            {
                _calls[i].SetDirty();
            }
        }

        public void Update()
        {
            if (!_dirty) return;
            _dirty = false;
            var count = _calls.Count;
            for (var i = 0; i < count; ++i)
            {
                _calls[i].Update();
            }
        }
    }
}