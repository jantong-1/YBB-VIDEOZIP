# OSS Files

该目录只作为远程资源留存区，不放进 `win64/` 或 `macos/`。

## 共用资源

```text
pro.html
ad-config.json
ad-config.example.json
```

广告和 Pro 配置由 Windows/macOS 共用。客户端按 `platform` 字段筛选广告：

```json
{
  "platform": "Windows"
}
```

```json
{
  "platform": "macOS"
}
```

跨平台广告可以使用：

```json
{
  "platform": "*"
}
```

## FFmpeg runtime

runtime 需要按平台和架构分开。当前已准备：

```text
ffmpeg-runtime-win64-gpl-8.1.1-ybb.zip
ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip
ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip.sha256
runtime-manifest.json
```

macOS x64 runtime 还没有生成，后续如需支持 Intel Mac，再补 `ffmpeg-runtime-macos-x64-*.zip` 并更新 `runtime-manifest.json`。

`runtime-manifest.example.json` 是示例，不能直接上传为正式 manifest。`runtime-manifest.json` 是正式清单，必须和对应 runtime zip 上传到同一个 OSS 公共目录。

正式下载域名使用：

```text
https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/
```

不要把本目录作为 OSS 覆盖式同步源。本目录不是线上 OSS 的完整镜像，当前没有保存 Windows runtime zip。发布 macOS arm64 runtime 时只上传：

```text
ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip
ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip.sha256
runtime-manifest.json
```
