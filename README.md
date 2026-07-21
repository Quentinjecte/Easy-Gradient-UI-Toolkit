# Easy Gradient UI Toolkit

A small Unity UI Toolkit package that renders multi-stop linear or radial color gradients as
a procedurally generated mesh, without any texture or shader graph.

## Features

- **`GradientStyle`**: a `ScriptableObject` describing a gradient (color stops, angle, Linear/Radial
  mode). Create one via **Assets → Create → UI Toolkit → Gradient Style**. Has a custom inspector
  with a live gradient preview and stop editing.
- **`GradientElement`**: a `VisualElement` that paints a `GradientStyle` as its background, usable
  from C# or from UXML. Supports an optional hover gradient (`gradient-style-hover`), handled in
  C# via `PointerEnterEvent`/`PointerLeaveEvent` since USS custom properties cannot reference a
  C# object like `GradientStyle`.
- **`GradientPainter`**: the static mesh-generation service used internally by `GradientElement`
  (exposed publicly in case you want to paint a gradient into your own `MeshGenerationContext`).

## Installation

This package lives locally under `Packages/com.easygradient.ui-toolkit` in this project, so Unity
picks it up automatically as an embedded package — no manifest edit needed.

To reuse it in another project, either copy the `com.easygradient.ui-toolkit` folder into that
project's `Packages` folder, or publish it to a Git repository and add it via
**Package Manager → Add package from git URL...**.

## Usage

### From UXML

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:eg="EasyGradient.UIToolkit">
    <eg:GradientElement gradient-style="UI/Gradients/haloGreen"
                         gradient-style-hover="UI/Gradients/haloGreenHover"/>
</ui:UXML>
```

The `gradient-style` / `gradient-style-hover` attributes are `Resources`-relative paths to a
`GradientStyle` asset (loaded via `Resources.Load<GradientStyle>`), so the referenced asset must
live under a folder named `Resources` somewhere in the project (this package ships its sample
gradients under `Runtime/Resources`).

### From C#

```csharp
var element = new GradientElement
{
    GradientStyle = myGradientStyleAsset,
    HoverGradientStyle = myHoverGradientStyleAsset,
};
```

## Package layout

```
Runtime/
  GradientStyle.cs        Gradient data (color stops, angle, Linear/Radial mode)
  GradientElement.cs       VisualElement rendering a GradientStyle, with hover support
  GradientPainter.cs       Static mesh-generation service used by GradientElement
  Resources/                Sample GradientStyle assets loadable via Resources.Load
Editor/
  GradientStyleEditor.cs    Custom inspector for GradientStyle (preview, stop editing)
Documentation~/
  Gradient_UI_Toolkit_Guide_EN.pdf   Original asset guide (excluded from the AssetDatabase)
```
