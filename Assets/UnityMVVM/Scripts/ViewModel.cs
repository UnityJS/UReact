
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UnityMVVM
{
    public class ViewModel : MonoBehaviour
    {
        private static ViewModel _global = null;

        public static ViewModel global
        {
            get
            {
                if (_global == null)
                {
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    SceneManager.sceneUnloaded += OnSceneUnloaded;
                    var GlobalViewModel = new GameObject("GlobalViewModel");
                    DontDestroyOnLoad(GlobalViewModel);
                    _global = GlobalViewModel.AddComponent<ViewModel>();
                    AttachViewsInAllScenes();
                }
                return _global;
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachViewsInScene(scene);
        }

        // 事件触发顺序 Compent.[OnDisable/OnDestroy] -> sceneUnloaded -> Compent.[Awake/Start/OnEnable] -> sceneLoaded
        private static void OnSceneUnloaded(Scene scene)
        {
            _global.ClearViews();
            AttachViewsInAllScenes();// 清除后未必执行，所以强制刷新一遍场景
        }
        private static void AttachViewsInScene(Scene scene)
        {
            List<GameObject> rootGameObjects = new List<GameObject>();
            scene.GetRootGameObjects(rootGameObjects);
            var rootCount = rootGameObjects.Count;
            for (var i = 0; i < rootCount; ++i)
            {
                _global.FindAndAttachViews(rootGameObjects[i].transform);
            }
        }
        private static void AttachViewsInAllScenes()
        {
            var count = SceneManager.sceneCount;
            for (var i = 0; i < count; ++i)
            {
                AttachViewsInScene(SceneManager.GetSceneAt(i));
            }
        }

        public static void SetGlobal(string name, object value)
        {
            global.Set(name, value);
        }

        public List<View> views = new List<View>();
        private Dictionary<string, ViewData> _data = new Dictionary<string, ViewData>();

        public void Set(string name, object value)
        {
            ViewData viewData;
            if (!_data.ContainsKey(name))
            {
                viewData = new ViewData(name);
                _data[name] = viewData;
                for (var i = 0; i < views.Count; ++i)
                {
                    viewData.AttachView(views[i]);
                }
            }
            else if (_data[name] == value) return;
            else viewData = _data[name];
            viewData.value = value;
            viewData.SetDirty();
        }

        public void Update()
        {
            foreach (var it in _data)
            {
                it.Value.Update();
            }
        }

        public void AttachView(View view)
        {
            if (views.IndexOf(view) != -1)
            {
                Debug.LogError("Try to AttachView the same view");
                return;
            }
            views.Add(view);
            foreach (var it in _data)
            {
                var viewData = it.Value;
                viewData.AttachView(view);
            }
        }

        public void DetachView(View view)
        {
            views.Remove(view);
            foreach (var it in _data)
            {
                var viewData = it.Value;
                viewData.DetachView(view);
            }
        }

        public void DetachViewModel()
        {
            for (var i = views.Count - 1; i >= 0; --i)
            {
                views[i].DetachViewModel();
            }
            if (views.Count > 0)
                Debug.LogError("Views are not all cleared! " + views.Count + " left");
        }

        public void ClearViews()
        {
            views.Clear();
            foreach (var it in _data)
            {
                it.Value.Clear();
            }
        }

        void FindAndAttachViews(Transform t)
        {
            var view = t.GetComponent<View>();
            if (view)
            {
                view.AttachViewModel(this);
            }
            else
            {
                var text = t.GetComponent<Text>();
                if (text)
                {
                    var argument = text.text;
                    if (argument.IndexOf("{{") != -1)
                    {
                        argument = ("\"" + argument.Replace("{{", "\"+(").Replace("}}", ")+\"") + "\"").Replace("\"\"+", "").Replace("+\"\"", "");
                        //Debug.Log(argument);
                        view = t.gameObject.AddComponent<View>();
                        view.AddDataBinding(text, "set_text", PersistentListenerMode.String, argument);
                        view.DetachViewModel();
                        view.AttachViewModel(this);
                    }
                }
            }
            var childCount = t.childCount;
            for (var i = 0; i < childCount; ++i)
            {
                var child = t.GetChild(i);
                if (child.GetComponent<ViewModel>() == null)
                    FindAndAttachViews(child);
            }
        }
        void Awake()
        {
            FindAndAttachViews(this.transform);
        }

        void OnDestroy()
        {
            DetachViewModel();
        }

        void OnBeforeTransformParentChanged()
        {
            DetachViewModel();
        }

        void OnTransformParentChanged()
        {
            FindAndAttachViews(this.transform);
        }
    }
}