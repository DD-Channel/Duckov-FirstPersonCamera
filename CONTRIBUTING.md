# 贡献指南

感谢您对 FirstPersonCamera 项目的兴趣！我们欢迎各种形式的贡献。

## 如何贡献

### 报告问题

如果您发现了 bug 或有功能建议，请通过以下方式报告：

1. **检查现有问题** - 在提交新问题之前，请先检查 [Issues](https://github.com/xiaomao-miao/FPmod/issues) 中是否已有类似问题

2. **创建新问题** - 如果问题不存在，请创建新问题并包含：
   - 清晰的问题描述
   - 重现步骤
   - 预期行为 vs 实际行为
   - 环境信息（游戏版本、mod 版本等）
   - 相关日志或截图

### 提交代码

#### 开发流程

1. **Fork 仓库**
   ```bash
   git clone https://github.com/xiaomao-miao/FPmod.git
   cd FPmod
   ```

2. **创建分支**
   ```bash
   git checkout -b feature/your-feature-name
   # 或
   git checkout -b fix/your-bug-fix
   ```

3. **进行开发**
   - 编写代码
   - 添加注释（中文或英文）
   - 确保代码符合项目规范

4. **测试**
   - 测试您的更改
   - 确保没有破坏现有功能
   - 检查编译是否成功

5. **提交更改**
   ```bash
   git add .
   git commit -m "描述您的更改"
   ```

6. **推送并创建 Pull Request**
   ```bash
   git push origin feature/your-feature-name
   ```
   - 在 GitHub 上创建 Pull Request
   - 填写详细的 PR 描述
   - 等待代码审查

## 代码规范

### 命名规范

- **类名**: PascalCase（如 `FirstPersonCameraController`）
- **方法名**: PascalCase（如 `EnableFirstPerson`）
- **私有字段**: camelCase，可加下划线前缀（如 `isFirstPersonMode`）
- **常量**: PascalCase（如 `ModVersion`）
- **命名空间**: PascalCase（如 `FirstPersonCamera`）

### 代码风格

- 使用 4 个空格缩进（不使用 Tab）
- 使用中文注释（项目主要使用中文）
- 为公共 API 添加 XML 文档注释
- 保持代码简洁和可读性

### 注释规范

```csharp
/// <summary>
/// 方法或类的简要描述
/// </summary>
/// <param name="parameter">参数描述</param>
/// <returns>返回值描述</returns>
public void ExampleMethod(int parameter)
{
    // 实现代码
}
```

### 文件组织

- 按功能模块组织代码
- 使用 partial class 将大文件分割
- 相关功能放在同一目录下

## 项目结构

```
src/
├── Controller/         # 控制器模块
│   ├── Camera/         # 相机控制
│   ├── Combat/         # 战斗系统
│   ├── Effects/        # 特效系统
│   ├── Movement/       # 移动系统
│   ├── Rendering/      # 渲染系统
│   └── Settings/       # 设置管理
├── Patches/            # Harmony 补丁
├── UI/                 # UI 系统
├── Utilities/          # 工具类
└── Compatibility/      # 兼容性支持
```

## 提交信息规范

提交信息应该清晰描述更改内容：

### 格式
```
<类型>: <简短描述>

<详细描述（可选）>
```

### 类型
- `feat`: 新功能
- `fix`: 修复 bug
- `docs`: 文档更新
- `style`: 代码格式调整（不影响功能）
- `refactor`: 代码重构
- `perf`: 性能优化
- `test`: 测试相关
- `chore`: 构建过程或辅助工具的变动

### 示例
```
feat: 添加新的倍镜灵敏度设置

- 支持为 1.2x 倍镜设置独立灵敏度
- 添加对应的 UI 选项
- 更新默认配置
```

## Pull Request 规范

### PR 标题
- 使用清晰的标题描述更改
- 包含类型前缀（如 `[Feature]`, `[Fix]`）

### PR 描述
应该包含：
- **更改说明**: 描述做了什么更改
- **原因**: 为什么要做这个更改
- **测试**: 如何测试这个更改
- **截图**: 如果有 UI 更改，请提供截图
- **相关 issue**: 如果有相关的 issue，请链接

### 示例 PR 描述
```markdown
## 更改说明
添加了新的倍镜灵敏度设置功能，允许用户为不同倍镜设置独立的灵敏度。

## 原因
用户反馈不同倍镜使用相同灵敏度体验不佳，需要独立的灵敏度设置。

## 测试
1. 打开设置界面
2. 调整不同倍镜的灵敏度
3. 在游戏中测试倍镜灵敏度是否正确应用

## 相关 issue
Closes #123
```

## 开发环境设置

### 必需工具
- Visual Studio 2019+ 或 JetBrains Rider
- .NET SDK（支持 .NET Standard 2.1）
- 《Escape from Duckov》游戏

### 配置步骤

1. **克隆仓库**
   ```bash
   git clone https://github.com/xiaomao-miao/FPmod.git
   cd FPmod
   ```

2. **配置游戏路径**
   - 复制 `FirstPersonCamera.csproj.user.example` 为 `FirstPersonCamera.csproj.user`
   - 编辑 `FirstPersonCamera.csproj.user`，设置你的游戏路径

3. **编译项目**
   ```bash
   dotnet build
   ```

4. **运行测试**
   - 将编译后的 DLL 复制到游戏 Mods 目录
   - 启动游戏测试功能

## 代码审查

所有代码提交都需要经过审查：

- 代码审查者会检查代码质量、规范符合性和功能正确性
- 可能需要修改后才能合并
- 请耐心等待审查，并及时响应审查意见

## 问题报告模板

创建 issue 时，请使用以下模板：

```markdown
### 问题描述
清晰简洁地描述问题

### 重现步骤
1. 
2. 
3. 

### 预期行为
描述您期望发生什么

### 实际行为
描述实际发生了什么

### 环境信息
- 游戏版本: 
- Mod 版本: 
- 操作系统: 
- 其他相关 mod: 

### 附加信息
添加任何其他相关信息、截图或日志
```

## 功能请求模板

```markdown
### 功能描述
清晰描述您想要的功能

### 使用场景
描述在什么情况下会使用这个功能

### 建议实现
如果有实现建议，请描述

### 附加信息
添加任何其他相关信息
```

## 行为准则

### 我们的承诺

为了营造开放和友好的环境，我们承诺：
- 尊重所有贡献者
- 接受建设性批评
- 专注于对社区最有利的事情
- 对其他社区成员表示同理心

### 不可接受的行为

- 使用性化的语言或图像
- 人身攻击、侮辱性/贬损性评论
- 公开或私下骚扰
- 发布他人的私人信息
- 其他不道德或不专业的行为

## 许可证

通过贡献代码，您同意您的贡献将在 GNU General Public License v3.0 (GPL v3) 许可证下授权。

这意味着：
- 您的贡献将成为项目的一部分
- 您的贡献将以 GPL v3 许可证发布
- 所有使用本项目的用户都可以自由使用、修改和分发您的贡献
- 使用本项目的代码（包括您的贡献）必须遵守 GPL v3 许可证的要求

## 联系方式

如果您有任何问题，可以通过以下方式联系：
- GitHub Issues
- GitHub Discussions

感谢您的贡献！

