# 开源准备检查清单

## 已完成 ✅

- [x] 创建 `.gitignore` 文件
- [x] 创建 `LICENSE` 文件 (GPL v3)
- [x] 创建 `README.md` 文件
- [x] 创建 `CONTRIBUTING.md` 文件
- [x] 修复项目文件中的硬编码路径
- [x] 创建用户配置示例文件
- [x] 在主源文件中添加 GPL v3 版权声明
- [x] 更新所有文档中的许可证信息

## 待完成 ⏳

### 必须完成（在开源前）

1. **替换 GitHub 用户名占位符** ✅
   - [x] 在 `README.md` 中替换所有 `yourusername` 为实际的 GitHub 用户名 (xiaomao-miao)
   - [x] 在 `CONTRIBUTING.md` 中替换所有 `yourusername` 为实际的 GitHub 用户名 (xiaomao-miao)
   - [x] 已更新所有 GitHub 链接为 https://github.com/xiaomao-miao/FPmod

2. **验证项目可以编译**
   - [ ] 确保项目可以正常编译
   - [ ] 检查所有依赖是否可用
   - [ ] 验证编译后的 DLL 可以正常工作

3. **检查敏感信息**
   - [ ] 确认没有硬编码的个人信息
   - [ ] 确认没有 API 密钥或密码
   - [ ] 确认没有硬编码的本地路径（已修复）

### 推荐完成（可选但建议）

4. **创建 GitHub 模板文件**
   - [ ] 创建 `.github/ISSUE_TEMPLATE/bug_report.md` - Bug 报告模板
   - [ ] 创建 `.github/ISSUE_TEMPLATE/feature_request.md` - 功能请求模板
   - [ ] 创建 `.github/pull_request_template.md` - Pull Request 模板

5. **添加版权信息到所有源文件**（可选）
   - [ ] 考虑是否需要在所有 `.cs` 文件开头添加 GPL v3 版权声明
   - 注意：GPL v3 不强制要求每个文件都有版权声明，但建议至少在主文件中添加

6. **创建发布说明**
   - [ ] 创建 `CHANGELOG.md` 文件记录版本更新
   - [ ] 或者在 README.md 中维护更新日志

7. **添加项目标签和描述**
   - [ ] 在 GitHub 仓库设置中添加项目描述
   - [ ] 添加相关标签（topics）
   - [ ] 设置项目网站（如果有）

8. **创建 CI/CD 配置**（可选）
   - [ ] 创建 GitHub Actions 工作流
   - [ ] 自动化构建和测试
   - [ ] 自动化发布

## 开源后

1. **创建第一个 Release**
   - [ ] 创建 v1.2.0 版本的 Release
   - [ ] 上传编译好的 DLL 文件
   - [ ] 添加 Release 说明

2. **推广项目**
   - [ ] 在相关社区分享项目
   - [ ] 添加项目到相关 mod 列表
   - [ ] 创建项目演示视频或截图

3. **维护项目**
   - [ ] 及时回复 Issues
   - [ ] 审查 Pull Requests
   - [ ] 定期更新文档

## 注意事项

- **GPL v3 许可证要求**：
  - 所有使用本项目代码的衍生作品必须也使用 GPL v3
  - 必须提供源代码
  - 必须保留版权声明
  
- **0Harmony.dll 许可证**：
  - 确认 0Harmony.dll 的许可证是否与 GPL v3 兼容
  - 如果包含在项目中，需要注明其许可证信息

- **游戏资源**：
  - 确保没有包含游戏官方的资源文件
  - 只包含 mod 自己的代码和资源

## 快速检查命令

```bash
# 检查是否有敏感信息
grep -r "password\|api_key\|secret" --include="*.cs" src/

# 检查是否有硬编码路径
grep -r "D:\\\\\|C:\\\\" --include="*.cs" src/

# 检查占位符
grep -r "yourusername" README.md CONTRIBUTING.md
```

---

最后更新：2024年

