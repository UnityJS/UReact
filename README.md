# UReact

A swift ugui framework for Unity3D.

## How to use

**step 1**

- Modify the `UI.Text` component properties `text` to `"My Level is {{level+1}}"`

- Or add the `UReact.View` component
  - Bind the data you need under the current GameObject
  - Input value `'"My Level is "+(level+1)'`

**step 2**
* Coding in the script
  ```C#
  using UReact;
  ViewModel.SetGlobal("level", 11);
  ```

* Or add the `UReact.ViewModel` component to control children's `UReact.View`
  ```C#
  myViewModel.Set("level", 11);
  ```
  
## Contact
