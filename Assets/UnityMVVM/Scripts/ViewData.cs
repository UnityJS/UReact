
using System.Collections.Generic;
using UnityEngine;

namespace UnityMVVM
{
    public class ViewData
    {
        public ViewData(string name) { this.name = name; }
        public string name;
        public object value;
        private List<PersistentCall> _calls = new List<PersistentCall>();
        private bool _dirty = true;

        public void AttachView(View view)
        {
            var viewCalls = view.dataBinding.calls;
            var count = viewCalls.Count;
            for (var i = 0; i < count; ++i)
            {
                var call = viewCalls[i];
                if (call.AttachView(this, view))
                {
                    if (_calls.IndexOf(call) != -1)
                    {
                        Debug.LogError("ViewData try to attach the same call");
                    }
                    _calls.Add(call);
                    //Debug.Log("AttachView " + name + " View " + _calls.Count);
                }
            }
        }
        public void DetachView(View view)
        {
            for (var i = _calls.Count - 1; i >= 0; --i)
            {
                if (_calls[i].view == view)
                {
                    _calls.RemoveAt(i);
                    //Debug.Log("DetachView " + name + " View " + _calls.Count);
                }
            }
        }
        public void Clear()
        {
            _calls.Clear();
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