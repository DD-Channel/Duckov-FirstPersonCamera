# UI文件结构说明

## 目录结构

### 游戏内UI（Gameplay UI）
这些文件用于第一人称模式下的游戏内UI显示，不是设置界面：
- `Compass.cs` - 指南针UI
- `HealthBars.cs` - 血条UI
- `StaminaUI.cs` - 体力UI

### 设置界面系统（Settings UI）

#### 独立UI系统（StandaloneUI/）
完全自己绘制的独立设置窗口，不依赖游戏官方UI：
- `StandaloneOptionsWindow.cs` - 独立选项窗口主类
- `StandaloneUICreator.cs` - 独立UI创建器（创建滑块、开关、按键绑定等组件）
- `StandaloneKeyRebinder.cs` - 独立UI的按键重绑定组件

#### 原版设置集成（Legacy Integration）
用于在游戏原版设置界面中添加入口：
- `FirstPersonOptionsUI.cs` - 在原版设置中添加"第一人称相机"标签页按钮，点击后打开独立设置窗口

#### 选项UI辅助类（OptionsUI/）
提供选项系统的辅助功能：
- `OptionsUIConstants.cs` - 所有选项键、默认值、范围等常量定义（被广泛使用）
- `UIHelpers.cs` - UI辅助方法（反射相关，用于访问私有字段）
- `KeybindFactory.cs` - 按键绑定工厂（用于创建按键绑定UI组件）

#### 按键重绑定系统（Key Rebinding）
用于按键重绑定的基类和实现：
- `KeyRebinderBase.cs` - 按键重绑定基类（抽象类）
- `FpsToggleKeyRebinder.cs` - 切换视角按键重绑定（继承自KeyRebinderBase）
- `FpsPeekKeyRebinder.cs` - 偏头按键重绑定（继承自KeyRebinderBase）

## 已移除的文件

以下文件已被移除，因为不再使用：
- `OptionsUI/UICreator.cs` - 旧的UI创建器（已被StandaloneUICreator替代）
- `OptionsUI/TabCreator.cs` - 旧的标签页创建器（不再使用）
- `OptionsUI/ToggleConfigurator.cs` - 旧的开关配置器（不再使用）
- `OptionsUI/SliderConfigurator.cs` - 旧的滑块配置器（不再使用）
- `OptionsUI/DropdownConfigurator.cs` - 旧的下拉菜单配置器（不再使用）
- `OptionsUI/SliderFactory.cs` - 旧的滑块工厂（不再使用）
- `OptionsUI/OptionsSynchronizer.cs` - 旧的选项同步器（不再使用）
- `OptionsUI/SimpleComponents.cs` - 旧的简单组件（不再使用）
- `FpsToggleKeyOptionsProvider.cs` - 旧的切换键选项提供者（不再使用）

## 使用说明

### 主要设置界面
用户通过以下方式打开设置：
1. 打开游戏原版设置界面
2. 点击"第一人称相机"标签页按钮
3. 自动打开独立设置窗口

### 独立UI系统
`StandaloneUI/` 目录下的文件是新的独立UI系统，完全自己绘制，不依赖游戏官方UI。

### 按键重绑定
目前有两套按键重绑定系统：
1. **旧系统**：`KeyRebinderBase`、`FpsToggleKeyRebinder`、`FpsPeekKeyRebinder` - 被`KeybindFactory`使用
2. **新系统**：`StandaloneKeyRebinder` - 被独立UI系统使用

建议未来统一使用新系统，移除旧系统。

