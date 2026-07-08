# M4 MBP Local Test Guide

测试机器：Apple Silicon M4 MBP 14 寸 Pro。

## 1. 安装前置工具

安装 .NET 8 SDK：

```bash
dotnet --info
```

如果没有 SDK，安装官方 .NET 8 SDK 后重开终端。

安装 FFmpeg，用于首次本地测试：

```bash
brew install ffmpeg
ffmpeg -version
ffprobe -version
```

说明：本地测试脚本会把当前机器的 `ffmpeg` 和 `ffprobe` 复制进 `.app`。这只用于你的 MBP 本地验证，不代表正式可分发 runtime 包。

## 2. 复制项目到 MBP

进入项目根目录：

```bash
cd YBB-VIDEOZIP
```

给脚本执行权限：

```bash
chmod +x macos/scripts/*.sh
```

## 3. 跑测试并生成本地 app

```bash
macos/scripts/test-macos-local.sh
```

脚本会执行：

```bash
dotnet test macos/tests/YBBvideozip.Mac.Tests.csproj
dotnet publish macos/src/YBBvideozip.Mac.csproj --runtime osx-arm64 --self-contained true
open macos/dist/YBBvideozip.app
```

## 4. 手动验证清单

- App 是否能启动。
- 拖入 `.mp4`、`.mov` 是否进入任务列表。
- 默认输出是否在源文件旁。
- 自定义输出目录是否生效。
- CPU + H.264 是否能压缩完成。
- CPU + H.265 是否能压缩完成。
- GPU + H.264 是否能压缩完成。
- GPU + H.265 是否能压缩完成。
- 广告是否显示。
- 广告倒计时结束后是否能关闭。
- 点击广告详情是否打开浏览器。
- 输入 Pro 授权码后是否变成 `Pro 已激活`。
- Pro 状态下再次压缩是否不显示广告。

## 5. 如果 app 被系统拦截

当前阶段不做签名、公证。macOS 可能提示无法验证开发者。

本地测试可以右键打开 app，或者执行：

```bash
open macos/dist/YBBvideozip.app
```

如果仍被隔离属性拦截，可以只对本地测试产物移除 quarantine：

```bash
xattr -dr com.apple.quarantine macos/dist/YBBvideozip.app
open macos/dist/YBBvideozip.app
```

## 6. FFmpeg OSS runtime 后续

正式走自动下载前，需要准备：

```text
ffmpeg-runtime-macos-arm64-gpl-8.1.1-ybb.zip
ffmpeg-runtime-macos-x64-gpl-8.1.1-ybb.zip
runtime-manifest.json
```

M4 测试优先只需要 `macos-arm64`。zip 内建议结构：

```text
ffmpeg/
  bin/
    ffmpeg
    ffprobe
```

上传到 OSS/ECS 后，把 `runtime-manifest.example.json` 复制为 `runtime-manifest.json`，填写真实 URL 和 SHA256。
