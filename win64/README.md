# YBBvideozip

Windows 绿色版视频压缩工具。

## 功能

- 拖入视频批量压缩
- H.264 / H.265
- CPU / NVIDIA GPU
- 高质量 / 均衡 / 小体积
- 默认输出到源文件旁，可自定义目录
- 首次运行自动安装 FFmpeg 组件（约 70MB）

## 支持格式

- 输入：`.mp4` `.mov` `.mkv` `.avi` `.webm` `.m4v`
- 输出：`.mp4`

## 下载

- [dist/YBBvideozip-green.zip](dist/YBBvideozip-green.zip)

解压后运行：

```text
YBBvideozip/
  YBBvideozip.exe
```

## 系统要求

- Windows 10/11 64 位
- .NET Framework 4.x
- 首次运行需要联网下载 FFmpeg 组件

不支持 32 位 Windows。

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-app.ps1
```

## FFmpeg

本软件调用 FFmpeg 命令行程序。FFmpeg 组件首次运行时自动下载并校验 SHA256。

第三方组件说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

## 许可证

GPL-3.0，见 [LICENSE](LICENSE)。
