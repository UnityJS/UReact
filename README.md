# UnityMVVM

A swift MVVM framework for Unity3D.

## How to use

**step 1**

- Modify the `UI.Text` component properties `text` to `"My Level is {{level+1}}"`

- Or and the `UnityMVVM.View` component
  - Bind the data you need under the current GameObject
  - Input value `'"My Level is "+(level+1)'`

**step 2**
* Coding in the script
  ```C#
  UnityMVVM.ViewModel.global.Set("level", 11);
  ```

* Or and the `UnityMVVM.ViewModel` component to control children's `UnityMVVM.View`
  ```C#
  myViewModel.Set("level", 11);
  ```
