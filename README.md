# UGUI FlexLayout

`com.ugui.flexlayout` is a Unity package for arranging UGUI `RectTransform` hierarchies with a flex-style model.

## Scope

This package targets Unity UGUI authoring and runtime layout. It provides:

- `FlexLayout`
  - container behavior
  - implicit child defaults
  - dirty scheduling and rebuild entry
- `FlexNode`
  - self sizing
  - min/max
  - aspect ratio
  - relative / absolute participation mode
- `FlexItem`
  - item behavior inside a parent flex container
- `FlexText`
  - text-oriented node measurement override for TMP-based content

The package focuses on a practical flex subset for UGUI instead of full CSS parity.

## Quick Start

1. Add `FlexLayout` to a container `RectTransform`.
2. Optionally add `FlexNode` to the same object when the container also needs explicit self sizing.
3. Add `FlexNode`, `FlexItem`, or `FlexText` only to children that need explicit overrides.
4. Leave simple children without flex components to use the container's implicit defaults.

Typical setup:

- parent: `FlexLayout`
- child with explicit width or height: `FlexNode`
- child with explicit grow, shrink, or basis: `FlexItem`
- child with both self sizing and item overrides: `FlexNode` + `FlexItem`
- TMP text child with text-based measurement: `FlexText`

## Authoring Model

### Implicit child behavior

Children without explicit flex authoring components are still treated as in-flow children of the parent `FlexLayout`.

Their behavior comes from:

- implicit item defaults stored on the parent `FlexLayout`
- current `RectTransform` size as the default node content source

This keeps ordinary UGUI hierarchies light:

- the child still participates in the parent's layout
- the parent drives its position while it remains in flow
- item-side defaults come from the parent layout
- self sizing still defaults to the child `RectTransform`

Use explicit components only when the default behavior must change:

- add `FlexNode` to override self sizing rules
- add `FlexItem` to override grow, shrink, basis, or align-self
- add `FlexText` to measure from TMP content instead of plain rect size

### Size semantics

- `Points`
  - definite value stored on the component
- `Auto`
  - resolves from the node content source
  - for a normal node, content defaults to current `RectTransform` size
  - for `FlexText`, content comes from text measurement
- `Percent`
  - resolves against the available parent size when that size is definite

### Position semantics

- `Relative`
  - participates in normal flex flow
- `Absolute`
  - leaves flex flow
  - still uses the parent `RectTransform` coordinate space, but not flex line placement

## Tracker And Ownership

The package uses driven `RectTransform` ownership rules instead of leaving layout-controlled values freely editable.

The practical rule is:

- when flex owns a value, the corresponding `RectTransform` field is driven
- when flex does not own a value, the package does not mirror or serialize a second copy of it

For direct children of a `FlexLayout`:

- in-flow children have position driven by the parent layout
- size is driven only when the resolved node policy says flex owns that axis
- implicit children still participate in tracker ownership; they are not unmanaged children

For standalone nodes:

- `FlexNode` can drive its own size without a parent `FlexLayout`
- `Auto` resolves from the node content source
- `Percent` resolves against the direct parent `RectTransform` only when a definite parent size exists

Tracker refresh follows the resolved authoring model:

- disabling a controlling component releases the corresponding driven ownership
- changing parent layout semantics refreshes direct child ownership immediately
- edit mode favors immediate visible feedback
- play mode favors queued rebuilds through the staged rebuild pipeline

## Runtime Architecture

The runtime is split into staged parts:

- collect
  - read authoring state from Unity components
- compute
  - resolve, measure, allocate, arrange
- apply
  - write final values back to `RectTransform`

Core files live under:

- `Runtime/Core/FlexBridge*.cs`
- `Runtime/Core/FlexMeasure*.cs`
- `Runtime/Core/FlexRebuildPipeline.cs`

Dirty behavior:

- `MarkLayoutDirty()`
  - immediate rebuild
- `RequestLayoutDirty()`
  - delayed rebuild through runtime or editor queue

## Design Notes

- `FlexLayout` is a container component.
- `FlexNode` owns self sizing.
- `FlexItem` owns participation inside a parent flex container.
- `FlexText` specializes node measurement for text content.
- The engine works on direct parent-child relationships only. A layout affects its direct children, not grandchildren directly.

## Notes And Limitations

- The package implements a flex-style subset, not a full CSS layout engine.
- Text behavior depends on TMP measurement and current text settings.
- Absolute mode currently focuses on flow exclusion, not CSS-like inset properties.
- The current implementation focuses on direct parent-child layout relationships.
