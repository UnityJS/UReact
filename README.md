# UnityjsMVVM

A swift MVVM framework for Unity3D.

## How to use

**step 1**

- Modify the `UI.Text` component properties `text` to `"My Level is {{level+1}}"`

- Or and the `UnityJSMVVM.View` component
  - Bind the data you need under the current GameObject
  - Input value `'"My Level is "+(level+1)'`

**step 2**
* Coding in the script
  ```C#
  UnityJSMVVM.ViewModel.global.Set("level", 11);
  ```

* Or and the `UnityJSMVVM.ViewModel` component to control children's `UnityJSMVVM.View`
  ```C#
  myViewModel.Set("level", 11);
  ```
