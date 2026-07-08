# YBBvideozip macOS Version Log

本文档记录 macOS 版的目录结构、制作方式、版本状态和清理规则。后续升级时优先看这里，再看源码和测试。

## 当前版本

| 项目 | 内容 |
| --- | --- |
| 当前版本 | 1.1.1 |
| 平台 | macOS arm64 |
| 主要测试机 | M4 MacBook Pro |
| 技术栈 | .NET 8, Avalonia 12, Avalonia WebView |
| 应用 Bundle ID | `cn.shenlouar.ybbvideozip` |
| 最终产物 | `macos/dist/YBBvideozip-macos-arm64.zip` |
| FFmpeg 策略 | 不打包进 app，首次运行从 OSS 下载 runtime |
| 发布签名 | Developer ID Application + Apple notarization + stapled ticket |

## 目录结构

```text
macos/
  assets/
    YBBvideozip-icon.png        应用图标源图
  dist/
    YBBvideozip-macos-arm64.zip 当前保留的可交付包
  scripts/
    package-local-app.sh        Mac 上生成 .app，写 Info.plist，生成 icns，ad-hoc 签名
    sign-notarize-app.sh        Developer ID 签名、公证、staple、生成正式 zip
    test-macos-local.sh         测试、打包、打开 app 的便捷脚本
    build-macos-arm64.ps1       Windows/PowerShell 风格的 publish 脚本，主要作备用
  entitlements.plist            .NET/Avalonia hardened runtime 权限
  src/
    Ads/                        广告配置、HTML 播放层、广告门控状态
    Compression/                FFmpeg/ffprobe 调用、参数构造、进度解析、输出路径
    Controls/                   简单弹窗控件
    Licensing/                  本地 Pro 授权存储和验证
    Models/                     压缩任务模型
    Platform/                   macOS 平台信息和浏览器打开
    Runtime/                    FFmpeg runtime manifest、下载、SHA256 校验、安装
    App.axaml                   Avalonia 全局样式
    MainWindow.axaml            主窗口固定布局
    MainWindow.axaml.cs         主窗口逻辑
    LicenseDialog.axaml         Pro 授权弹窗
    Program.cs                  应用入口
    YBBvideozip.Mac.csproj      .NET/Avalonia 项目文件
  tests/
    *.cs                        xUnit 测试，覆盖 UI 布局、广告、runtime、授权和压缩参数
```

## 软件制作方式

### 1. 准备

Mac 端项目路径：

```bash
/Users/yani/Projects/YBB-VIDEOZIP
```

优先使用项目内 dotnet：

```bash
/Users/yani/Projects/YBB-VIDEOZIP/tools/dotnet/dotnet
```

如果项目内没有 dotnet，则需要系统安装 .NET 8 SDK。

### 2. 测试

```bash
cd /Users/yani/Projects/YBB-VIDEOZIP
tools/dotnet/dotnet test macos/tests/YBBvideozip.Mac.Tests.csproj --no-restore
```

当前基线：45 个测试全部通过。

### 3. 生成 app

```bash
cd /Users/yani/Projects/YBB-VIDEOZIP
macos/scripts/package-local-app.sh
```

脚本行为：

- `dotnet publish` 到 `macos/dist/osx-arm64/publish`
- 复制 publish 文件到 `macos/dist/YBBvideozip.app/Contents/MacOS`
- 使用 `assets/YBBvideozip-icon.png` 生成完整 iconset 和 `YBBvideozip.icns`
- 写入 `Info.plist`
- 使用 ad-hoc `codesign --force --deep --sign -`

图标规则：

- `CFBundleIconFile` 必须是 `YBBvideozip`
- 不写 `CFBundleIconName`
- `YBBvideozip.icns` 位于 `Contents/Resources/`

不要把 `CFBundleIconName` 写回去。当前项目没有 asset catalog，写回后 Finder/Dock 可能走错图标路径。

### 4. 生成最终 zip

```bash
cd /Users/yani/Projects/YBB-VIDEOZIP
macos/scripts/sign-notarize-app.sh
```

`sign-notarize-app.sh` 会使用 `Developer ID Application` 证书、hardened runtime、Apple notarytool 和 stapler 生成正式发布 zip。脚本优先使用 `syspolicy_check distribution` 做 macOS 分发预检；旧版系统没有该命令时回退到 `spctl`。`ditto` 用于保留 macOS bundle 元数据。不要用普通 Windows zip 工具重打包 `.app`。

首次公证前，在 Mac 上保存 notarytool 凭据：

```bash
xcrun notarytool store-credentials "YBB_NOTARY" --apple-id "Apple ID 邮箱" --team-id "LQ4QJJS827"
```

这里需要 Apple 的 app-specific password，不要把密码写进脚本或仓库。

## OSS 依赖

应用本体不包含 FFmpeg。首次运行如本机缺少 runtime，会读取：

```text
https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/runtime-manifest.json
```

当前 macOS arm64 runtime：

```text
https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip
```

runtime manifest 中保留 Windows 和 macOS 两个平台条目。不要用 macOS runtime 覆盖 Windows runtime 文件。

广告配置：

```text
https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ad-config.json
```

Pro 页面：

```text
https://shenlouar.cn/YBBvideozipFFmpeg/pro.html
```

## 1.1.1 版本记录

日期：2026-07-07

主要状态：

- macOS 版主窗口按 70% 尺寸收缩，字体保持原始可读大小。
- 虚线框和广告视频容器按 16:9 对齐。
- 广告视频层使用 WebView 内部同层绘制黑底、圆角裁切和虚线边框，避免 macOS 原生层盖过 Avalonia 裁切。
- 广告默认有声音，右上角按钮可切换静音。
- 压缩完成后执行按钮状态能变为完成，可返回主界面。
- 按钮使用自定义 `Button.YbbButton` 模板和 `TextBlock.ButtonLabel`，修正 macOS 默认按钮文字竖向不居中问题。
- app icon 使用传统 `.icns` 方式，移除 `CFBundleIconName`，避免 Finder/Dock 图标丢失。
- FFmpeg 不打包进 app，由 runtime installer 从 OSS 下载并校验 SHA256。

验证记录：

```text
dotnet test: 45/45 passed
codesign --verify --deep --strict: passed
app bundle FFmpeg/ffprobe count: 0
NSWorkspace.iconForFile(): 返回 YBB 自定义图标
```

当前最终 zip：

```text
macos/dist/YBBvideozip-macos-arm64.zip
SHA256: DD737BD368B5DCC37EAC5C75EECABFEF9D507379B13456853077B48331BDBFE7
Size: 42,930,080 bytes
```

## 1.1.1 正式签名记录

日期：2026-07-08

签名状态：

- Developer ID Application: `Buyi Weiran (Shanghai) Culture Technology Co., Ltd. (LQ4QJJS827)`
- Apple notarization: passed
- stapler ticket: passed
- `syspolicy_check distribution`: passed
- Launchpad: 拖入 `Applications` 后可显示图标并启动
- FFmpeg/ffprobe bundled count: 0

重要实现：

- `sign-notarize-app.sh` 扫描 `Contents/MacOS` 下全部 Mach-O 文件并逐个 Developer ID 签名，避免漏签 .NET 自带的 `createdump`。
- 使用 `entitlements.plist` 开启 .NET/Avalonia 在 hardened runtime 下需要的 JIT、unsigned executable memory 和 library validation 例外。
- 正式分发预检优先使用 `syspolicy_check distribution`，旧系统无该命令时再回退 `spctl`。

## 清理规则

长期保留：

- `assets/`
- `scripts/`
- `src/`
- `tests/`
- `dist/YBBvideozip-macos-arm64.zip`
- `README.md`
- `VERSION_LOG.md`
- `LICENSE`
- `THIRD_PARTY_NOTICES.md`

本地可保留但不进 git：

- `handoff/`：签名、公证完成后的迁移包或临时交接包。

可以删除并重新生成：

- `src/bin`
- `src/obj`
- `tests/bin`
- `tests/obj`
- `dist/YBBvideozip.app`
- `dist/YBBvideozip.iconset`
- `dist/osx-arm64`
- `dist/runtime-package`
- `dist/screenshots`
- `test-output`
- `.DS_Store`

不应放在 `macos/` 下：

- `macos/oss/`：OSS 文件应在项目根目录 `oss/YBBvideozipFFmpeg/`
- 临时 SSH、截图、探针、下载缓存
- FFmpeg runtime 安装目录

## 后续升级注意事项

- 改 UI 后先更新或新增 `MainWindowLayoutTests`、`AdGateTests`，再跑测试。
- 改版本号时同时改 `MainWindow.axaml.cs` 的 `AppVersion` 和 `package-local-app.sh` 的 `CFBundleVersion`、`CFBundleShortVersionString`。
- 改 icon 时必须验证 `Info.plist`、`YBBvideozip.icns`、LaunchServices 和 `NSWorkspace.iconForFile()`。
- 改 FFmpeg runtime 时必须更新根目录 `oss/YBBvideozipFFmpeg/runtime-manifest.json` 和对应 `.sha256`。
- 发布给用户的文件是 zip，不是 `YBBvideozip.app` 目录。

## Windows 长期保存策略

Windows 是长期保存位置，当前 Mac 只是临时构建机。为了以后换 Mac 后能继续修改和打包，Windows 侧需要保留：

- `macos/`：macOS 源码、测试、脚本、文档和最终用户发布 zip。
- `oss/`：广告配置、Pro 页面、runtime manifest 和 FFmpeg runtime 包。
- `tools/`：项目内 .NET SDK 和 FFmpeg 工具。
- `macos/handoff/YBB-VIDEOZIP-macos-workspace-2026-07-08.tar.gz`：保留 Unix 权限的当前 Mac 工作区迁移包。

正常后续开发优先使用最新的 workspace 迁移包。`.dotnet-home/` 和 `.cache/` 是 Mac 上的 dotnet/NuGet/下载缓存，体积大，可重建，不再单独保留 full-env 包。Developer ID 证书、私钥和 notarytool 凭据不应进入迁移包，需要在新 Mac 上重新安装或授权。
