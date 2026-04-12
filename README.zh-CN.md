# UGUI FlexLayout

`com.ugui.flexlayout` 是一个面向 Unity UGUI 的 flex 风格布局包，用来驱动 `RectTransform` 层级的排布。

## 包的定位

当前包提供四类核心组件：

- `FlexLayout`
  - 容器语义
  - 隐式子节点默认规则
  - dirty 调度与重建入口
- `FlexNode`
  - 自身宽高策略
  - min/max
  - aspect ratio
  - relative / absolute 模式
- `FlexItem`
  - 作为父容器子项时的 item 语义
- `FlexText`
  - 用于 TMP 文本节点的测量覆盖

它实现的是适合 UGUI 的 flex 子集，而不是完整 CSS 语义复制。

## 基本使用方式

1. 在容器 `RectTransform` 上挂 `FlexLayout`。
2. 如果容器自身也需要由组件控制宽高，再在同一对象上挂 `FlexNode`。
3. 子节点只有在需要显式覆盖默认行为时，才挂 `FlexNode`、`FlexItem` 或 `FlexText`。
4. 普通子节点可以不挂任何 flex 组件，直接走父容器的隐式规则。

常见组合：

- 父节点：`FlexLayout`
- 子节点需要显式宽高：`FlexNode`
- 子节点需要显式 grow、shrink、basis：`FlexItem`
- 子节点同时需要 self 和 item 控制：`FlexNode + FlexItem`
- TMP 文本节点需要按文本测量：`FlexText`

## 作者化模型

### 隐式子节点

没有显式 flex 组件的子节点，仍然会作为父 `FlexLayout` 的直接子项参与布局。

隐式子节点的行为来源于两部分：

- 父 `FlexLayout` 上的 implicit item defaults
- 子节点当前 `RectTransform` 尺寸，作为默认 node content source

这意味着：

- 普通 UGUI 子节点不需要为了参与布局而逐个挂组件
- 子节点在保持 flow 内时，位置由父布局驱动
- item 侧默认行为由父容器统一提供
- self sizing 默认仍然来自子节点自己的 `RectTransform`

只有在默认行为不够时，才需要显式挂组件：

- 挂 `FlexNode`，覆盖自身宽高语义
- 挂 `FlexItem`，覆盖 grow、shrink、basis、align-self
- 挂 `FlexText`，把测量源从 rect 尺寸切换成 TMP 文本测量

### 尺寸语义

- `Points`
  - 组件上存储的确定值
- `Auto`
  - 取节点自己的 content source
  - 普通节点默认取当前 `RectTransform` 尺寸
  - `FlexText` 取文本测量结果
- `Percent`
  - 在父级可用尺寸是 definite 时，按父级尺寸解析

### 位置语义

- `Relative`
  - 正常参与 flex 流
- `Absolute`
  - 脱离 flex 流
  - 仍然使用父 `RectTransform` 坐标系，但不再参与 flex 行内排布

## Tracker 与控制权

这个包不是“算完布局但允许手动随便改 RectTransform”，而是按控制权决定哪些字段由 flex 驱动。

实际规则是：

- 某个值一旦由 flex 拥有，对应的 `RectTransform` 字段就会被 driven
- flex 不拥有的值，不会额外序列化一份镜像，也不会强行接管

对 `FlexLayout` 的直接子节点：

- 仍在 flow 内的子节点，位置由父布局驱动
- 尺寸是否由布局驱动，取决于解析后的 node 规则，而不是“是否显式挂了组件”
- 隐式子节点同样进入 tracker 规则，不是放任不管的普通 child

对独立节点：

- `FlexNode` 即使没有父 `FlexLayout`，也可以驱动自己的尺寸
- `Auto` 取自身内容尺寸
- `Percent` 在存在直接父 `RectTransform` 且父尺寸 definite 时生效

Tracker 的刷新策略和作者化模型保持一致：

- 控制组件禁用后，要立即释放对应 driven ownership
- 父布局语义变化后，要立刻刷新直接子节点的控制权
- 编辑器模式优先保证修改后立刻看到结果
- 运行时通过 staged rebuild pipeline 做统一调度

## 运行时架构

当前运行时按阶段拆分：

- collect
  - 从 Unity 组件读取 authoring 数据
- compute
  - resolve、measure、allocate、arrange
- apply
  - 将结果写回 `RectTransform`

核心代码位于：

- `Runtime/Core/FlexBridge*.cs`
- `Runtime/Core/FlexMeasure*.cs`
- `Runtime/Core/FlexRebuildPipeline.cs`

dirty 行为约定：

- `MarkLayoutDirty()`
  - 立即重建
- `RequestLayoutDirty()`
  - 进入运行时或编辑器延迟队列

## 设计原则

- `FlexLayout` 只负责容器语义。
- `FlexNode` 负责自身尺寸语义。
- `FlexItem` 负责作为父容器子项时的语义。
- `FlexText` 只覆盖节点测量来源，不重定义整套布局规则。
- 布局作用范围只到直接子节点，不直接影响孙子节点。

## 注意事项

- 这是一个 flex 风格子集，不是完整 CSS 布局引擎。
- 文本结果会受到 TMP 当前配置和测量行为影响。
- `Absolute` 当前重点是“脱离流式布局”，不包含 CSS 风格的 inset 偏移语义。
- 当前实现重点仍然是直接父子关系下的布局行为。
