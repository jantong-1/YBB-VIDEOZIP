# YBBvideozip

视频压缩工具项目按平台隔离维护。

## 目录

```text
win64/    Windows 10/11 64 位版本，基于 .NET Framework 4.x + WinForms
macos/    macOS Apple Silicon arm64 版本，基于 .NET 8 + Avalonia
oss/      远程资源留存区，包含 Pro 页面、广告配置和 FFmpeg runtime 清单示例
docs/     跨平台目录结构和 macOS 本地测试文档
```

## 当前策略

- Windows 和 macOS 不使用 shared 目录。
- 两个平台需要的源码、脚本、资源各放一份。
- 广告配置和 Pro 页面保持一套远程资源。
- FFmpeg runtime 按平台和架构拆分，放 OSS 后由各客户端按平台下载。
- Windows 和 macOS 发布包在 GitHub Releases 中并行提供。
- 当前 macOS 发布包只覆盖 Apple Silicon arm64；Intel Mac 版本后续再补。

## 常用命令

Windows 版：

```powershell
cd .\win64
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

macOS 版在 M4 MBP 上测试：

```bash
cd macos
chmod +x scripts/*.sh
scripts/test-macos-local.sh
```

macOS 正式签名、公证和打包：

```bash
macos/scripts/sign-notarize-app.sh
```
