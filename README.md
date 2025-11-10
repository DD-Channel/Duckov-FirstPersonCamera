# FirstPersonCamera - 第一人称相机 Mod

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Version](https://img.shields.io/badge/version-1.2.0-blue.svg)](https://github.com/xiaomao-miao/FPmod)

为《Escape from Duckov》游戏添加完整的第一人称视角模式，支持完整的鼠标控制、角色同步和丰富的自定义选项。

## 功能特性

### 核心功能
- 🎮 **第一人称/第三人称切换** - 按 F5 键快速切换视角模式
- 🖱️ **完整的鼠标控制** - 支持独立的 X/Y 轴灵敏度设置
- 🎯 **倍镜灵敏度支持** - 为不同倍镜（1.2x, 2x, 4x, 8x）设置独立的灵敏度倍数
- 📐 **相机位置调整** - 可自定义相机高度、前后和左右偏移
- 🔍 **视野角度调节** - 可调整 FOV（视野角度）
- 🎨 **防抖功能** - 减少角色移动时的相机抖动

### 战斗功能
- 🎯 **瞄准系统** - 完整的瞄准和准星系统
- 💥 **后坐力系统** - 可调节的后坐力强度
- 🔫 **武器检视** - 支持武器和近战武器检视
- 🔴 **激光指示器控制** - 快捷键控制激光开关
- 🎪 **命中效果** - 第一人称下的命中特效

### 渲染功能
- 👁️ **遮挡处理** - 自动处理角色模型遮挡相机的问题
- 🌫️ **战争迷雾控制** - 第一人称下可选择去除战争迷雾
- 🎬 **后处理效果控制** - 可禁用模糊等后处理效果
- 📊 **性能优化** - 可调节的性能参数

### UI 功能
- 🧭 **指南针** - 第一人称下的指南针显示
- ❤️ **血条显示** - 第一人称下的血条 UI
- ⚡ **体力显示** - 体力条 UI
- 🎛️ **完整设置界面** - 独立的设置窗口，支持所有选项配置
- ⌨️ **按键重绑定** - 支持自定义所有快捷键

### 其他功能
- 🏃 **跳跃功能** - 可选的跳跃功能支持
- 👀 **侧头功能** - 支持左右侧头（Peek）
- 🔄 **Mod 兼容性 API** - 提供 API 供其他 mod 使用
- 🌐 **多语言支持准备** - 代码结构支持多语言

## 安装说明

### 前置要求
- 《Escape from Duckov》游戏
- .NET Standard 2.1 运行时
- 0Harmony.dll（已包含在项目中）

### 安装步骤

1. **下载模组**
   - 从 Releases 页面下载最新版本的 `FirstPersonCamera.dll` 和 `info.ini`
   - 或者从源代码编译（见编译指南）

2. **安装到游戏**
   - 将 `FirstPersonCamera.dll` 和 `info.ini` 复制到游戏的 Mods 目录
   - 通常路径为：`游戏目录/Mods/FirstPersonCamera/`

3. **启动游戏**
   - 启动游戏，模组会自动加载
   - 在游戏设置中可以看到"第一人称相机"选项

## 编译指南

### 环境要求
- Visual Studio 2019 或更高版本（或 JetBrains Rider）
- .NET SDK（支持 .NET Standard 2.1）
- 《Escape from Duckov》游戏安装

### 编译步骤

1. **克隆仓库**
   ```bash
   git clone https://github.com/xiaomao-miao/FPmod.git
   cd FPmod
   ```

2. **配置游戏路径**

   有三种方式配置游戏路径：

   **方式 1：使用环境变量（推荐）**
   ```bash
   # Windows (PowerShell)
   $env:DuckovPath = "C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov"
   
   # Windows (CMD)
   set DuckovPath=C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov
   
   # Linux/Mac
   export DuckovPath="~/steamapps/common/Escape from Duckov"
   ```

   **方式 2：使用用户配置文件（推荐用于开发）**
   - 复制 `FirstPersonCamera.csproj.user.example` 为 `FirstPersonCamera.csproj.user`
   - 编辑 `FirstPersonCamera.csproj.user`，修改 `DuckovPath` 为你的游戏路径
   - 此文件不会被提交到 Git，每个开发者可以有自己的配置

   **方式 3：直接修改项目文件**
   - 编辑 `FirstPersonCamera.csproj`
   - 修改第 14 行的默认路径（不推荐，因为会被 Git 跟踪）

3. **编译项目**
   ```bash
   dotnet build -c Release
   ```

4. **获取编译结果**
   - 编译后的 DLL 文件位于 `bin/Release/netstandard2.1/FirstPersonCamera.dll`
   - 将 DLL 和 `info.ini` 复制到游戏的 Mods 目录

## 使用方法

### 基本操作
- **F5** - 切换第一人称/第三人称视角
- **鼠标移动** - 控制相机视角
- **ESC** - 打开菜单（自动解锁鼠标）

### 设置界面
1. 打开游戏设置
2. 点击"第一人称相机"标签页
3. 在独立设置窗口中配置所有选项

### 主要设置项

#### 灵敏度设置
- **鼠标 X 轴灵敏度** - 水平视角灵敏度（0.01 - 1.0）
- **鼠标 Y 轴灵敏度** - 垂直视角灵敏度（0.01 - 1.0）
- **倍镜灵敏度倍数** - 为不同倍镜设置灵敏度倍数

#### 相机设置
- **相机高度偏移** - 调整相机垂直位置
- **相机前向偏移** - 调整相机前后位置
- **相机右向偏移** - 调整相机左右位置
- **视野角度 (FOV)** - 相机视野角度
- **防抖强度** - 减少移动时的相机抖动

#### 按键设置
- **切换视角键** - 默认 F5，可自定义
- **左侧头键** - 默认 Q，可自定义
- **右侧头键** - 默认 E，可自定义
- **武器检视键** - 可自定义
- **激光开关键** - 可自定义

#### 显示设置
- **禁用后处理模糊** - 禁用第一人称下的模糊效果
- **禁用遮挡透视** - 禁用角色模型透视效果
- **禁用准星虚化** - 禁用瞄准时的准星虚化
- **去除战争迷雾** - 第一人称下去除战争迷雾
- **显示指南针** - 显示/隐藏指南针
- **显示血条** - 显示/隐藏血条
- **显示体力条** - 显示/隐藏体力条

#### 性能设置
- **瞄准重算间隔** - 瞄准系统更新间隔
- **瞄准角度阈值** - 瞄准角度判断阈值
- **身体对齐角度阈值** - 身体对齐角度阈值
- **射线检测距离** - 遮挡检测的射线距离

#### 战斗设置
- **后坐力强度** - 武器后坐力强度（0 - 0.5）
- **启用跳跃** - 启用/禁用跳跃功能
- **跳跃中允许翻滚** - 跳跃过程中是否允许翻滚



## 项目结构

```
FirstPersonCamera/
├── src/                    # 源代码目录
│   ├── Controller/         # 控制器模块
│   │   ├── Camera/         # 相机控制
│   │   ├── Combat/         # 战斗系统
│   │   ├── Effects/        # 特效系统
│   │   ├── Movement/       # 移动系统
│   │   ├── Rendering/      # 渲染系统
│   │   └── Settings/       # 设置管理
│   ├── Patches/            # Harmony 补丁
│   ├── UI/                 # UI 系统
│   ├── Utilities/          # 工具类
│   ├── Compatibility/      # 兼容性支持
│   ├── FirstPersonCameraAPI.cs  # 公共 API
│   └── ModBehaviour.cs     # Mod 主类
├── libs/                   # 第三方库
│   └── 0Harmony.dll        # Harmony 库
├── FirstPersonCamera.csproj    # 项目文件
├── info.ini                # Mod 信息文件
├── LICENSE                 # 许可证文件
└── README.md               # 本文件
```

## 兼容性

### 已知兼容的 Mod
- ScavDialogueBubble - 对话气泡兼容性支持

### Mod 兼容性 API
本 mod 提供了公共 API 供其他 mod 使用。主要 API 包括：
- `FirstPersonCameraAPI.IsFirstPersonMode()` - 检查是否处于第一人称模式
- `FirstPersonCameraAPI.GetToggleKeyCode()` - 获取切换键
- `FirstPersonCameraAPI.SwitchToTopDown()` - 切换到第三人称俯视角
- `FirstPersonCameraAPI.OnSwitchToThirdPersonTopDown` - 视角切换事件

详细的 API 文档请查看 `src/FirstPersonCameraAPI.cs` 文件中的注释。

## 故障排除

### 常见问题

**Q: 模组无法加载？**
- 检查 DLL 文件是否正确放置在 Mods 目录
- 检查 info.ini 文件是否存在
- 检查游戏日志查看错误信息

**Q: 编译失败？**
- 检查游戏路径配置是否正确
- 检查是否安装了 .NET SDK
- 检查所有依赖 DLL 是否可访问

**Q: 第一人称视角不正常？**
- 检查相机偏移设置
- 尝试重置为默认设置
- 检查是否有其他 mod 冲突

**Q: 性能问题？**
- 调整性能设置中的参数
- 禁用不必要的视觉效果
- 检查游戏本身的性能设置

## 贡献

欢迎贡献代码！请查看 [CONTRIBUTING.md](CONTRIBUTING.md) 了解贡献指南。

## 许可证

本项目采用 GNU General Public License v3.0 (GPL v3) 许可证。详情请查看 [LICENSE](LICENSE) 文件。

GPL v3 是一个 copyleft 许可证，要求使用本项目的代码也必须以相同的许可证发布。这意味着：
- 您可以自由使用、修改和分发本项目的代码
- 如果您修改或分发本项目，您必须：
  - 保留原始版权声明
  - 以相同的 GPL v3 许可证发布您的修改
  - 提供源代码或提供获取源代码的方式
  - 说明您对代码所做的更改

更多信息请访问 [GNU GPL v3 官方网站](https://www.gnu.org/licenses/gpl-3.0.html)。

## 致谢

- 感谢《Escape from Duckov》游戏开发者
- 感谢 Harmony 库的开发者
- 感谢所有贡献者和测试者

## 更新日志

### v1.2.0
- 当前版本
- 完整的第一人称相机系统
- 丰富的自定义选项
- Mod 兼容性 API

## 联系方式

- 问题报告: [GitHub Issues](https://github.com/xiaomao-miao/FPmod/issues)
- 功能建议: [GitHub Discussions](https://github.com/xiaomao-miao/FPmod/discussions)

---

**注意**: 这是一个社区开发的 mod，与游戏官方无关。使用 mod 可能存在风险，请自行承担。

