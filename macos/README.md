# YBBvideozip macOS

macOS 版本基于 .NET 8、Avalonia 12 和 Avalonia WebView。目录内保留独立的 UI、业务逻辑、测试、脚本和资源，不依赖 `win64/` 源码。

详细版本记录和回溯说明见 [VERSION_LOG.md](VERSION_LOG.md)。

## 目录结构

```text
macos/
  assets/                    应用图标源文件
  dist/                      最终可交付 zip，只保留 release 包
  scripts/                   macOS 构建、测试、打包脚本
  src/                       Avalonia macOS 应用源码
  tests/                     xUnit 测试
  entitlements.plist         Developer ID hardened runtime 权限
  LICENSE                    GPL 许可文本
  THIRD_PARTY_NOTICES.md     第三方组件说明
  README.md                  当前入口说明
  VERSION_LOG.md             版本记录、制作方式、回溯说明
```

不要长期保留以下可重建目录：`src/bin`、`src/obj`、`tests/bin`、`tests/obj`、`dist/YBBvideozip.app`、`dist/osx-arm64`、`dist/YBBvideozip.iconset`、`test-output`。

## 本地测试目标

- M4 MBP 上启动本地 `.app`
- 拖入视频
- CPU H.264/H.265 压缩
- GPU VideoToolbox H.264/H.265 压缩
- 广告播放、倒计时、点击跳转
- Pro 授权码去广告

## 测试和打包

```bash
chmod +x scripts/*.sh
scripts/package-local-app.sh
```

`package-local-app.sh` 会生成本地测试用 `dist/YBBvideozip.app`，并只做 ad-hoc 签名。

正式发布给用户前，需要使用 Developer ID 签名和 Apple 公证：

```bash
cd /Users/yani/Projects/YBB-VIDEOZIP
macos/scripts/sign-notarize-app.sh
```

脚本会生成 `dist/YBBvideozip-macos-arm64.zip`。首次使用公证前，Mac 钥匙串里需要有 `Developer ID Application` 证书，并用 `xcrun notarytool store-credentials` 保存 notarytool 凭据。脚本优先使用 `syspolicy_check distribution` 做 macOS 分发预检；旧版系统没有该命令时才回退到 `spctl`。

脚本不会把 `ffmpeg` 和 `ffprobe` 打进 app。首次运行时如果本机缺少运行组件，程序会读取：

```text
https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/runtime-manifest.json
```

并下载对应的 macOS FFmpeg runtime。
