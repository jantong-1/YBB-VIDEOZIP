# Directory Layout

项目按平台隔离：

```text
win64/
  src/
  assets/
  scripts/
  tests/
  docs/
  dist/

macos/
  src/
  assets/
  scripts/
  tests/
  dist/

oss/
  YBBvideozipFFmpeg/
```

约定：

- 不使用 `shared/`。
- Windows 和 macOS 重复逻辑各自保留一份源码。
- `oss/` 不放进平台目录。
- 广告配置和 Pro 页面继续共用。
- FFmpeg runtime 按平台、架构拆成独立 zip。
