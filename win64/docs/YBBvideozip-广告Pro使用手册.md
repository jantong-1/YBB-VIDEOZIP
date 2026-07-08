# YBBvideozip 广告版 / Pro 版使用手册

## 1. 项目定位

YBBvideozip 是一个 Windows 本地视频压缩工具。

当前版本是一套软件、两种状态：

- 免费版：压缩功能可用，压缩期间展示广告视频。
- Pro 版：用户输入本地授权码后去广告。

第一版不做用户登录、不做服务器会员系统、不接第三方广告 SDK。广告配置通过 OSS 上的 JSON 文件远程更新，Pro 授权通过本地授权码完成。

## 2. 程序结构

主要源码位置：

- `src/VideoCompressorUI.cs`：主窗口、压缩流程、广告入口、Pro 入口。
- `src/AdConfig.cs`：广告配置模型、远程配置读取、广告随机选择。
- `src/AdDisplayPanel.cs`：广告视频播放区域、倒计时、关闭按钮、查看详情按钮。
- `src/LicenseManager.cs`：Pro 授权码生成、校验、本地保存。
- `src/LicenseDialog.cs`：升级 Pro / 输入授权码窗口。

构建脚本：

- `scripts/test.ps1`：运行业务逻辑测试。
- `scripts/build.ps1`：生成 `dist/YBBvideozip.exe`。
- `scripts/package-app.ps1`：生成绿色包 `dist/YBBvideozip-green.zip`。
- `scripts/generate-license.ps1`：给用户生成 Pro 授权码。

发布包仍然是绿色包。用户解压后运行 `YBBvideozip.exe` 即可。

## 3. 用户使用方式

用户流程：

1. 解压绿色包。
2. 运行 `YBBvideozip.exe`。
3. 拖入一个或多个视频文件。
4. 选择编码格式：
   - `H.264`
   - `H.265`
5. 选择压缩方式：
   - `CPU`
   - `GPU`
6. 选择压缩质量。
7. 点击执行。

免费用户压缩时会看到广告视频。广告倒计时结束后可以关闭广告查看结果。

Pro 用户输入有效授权码后，后续压缩不再展示广告。

## 4. 广告配置方式

程序启动时会读取这个远程配置文件：

```text
https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ad-config.json
```

如果远程配置读取失败，程序会使用 EXE 内置的默认广告视频兜底。

当前内置默认视频：

```text
https://vaers.oss-cn-beijing.aliyuncs.com/YBBffmpegVideo/ShowReel2024_h264.mp4
```

## 5. 如何更换广告视频

只需要修改 OSS 上的 `ad-config.json`，不需要重新打包 EXE。

本地已有可上传的配置文件：

```text
ad-config.json
```

上传到 OSS 后，需要能通过下面这个地址访问：

```text
https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ad-config.json
```

广告配置示例：

```json
{
  "ads": [
    {
      "id": "showreel-2024",
      "title": "YBBvideozip Pro",
      "videoUrl": "https://vaers.oss-cn-beijing.aliyuncs.com/YBBffmpegVideo/ShowReel2024_h264.mp4",
      "coverUrl": "",
      "clickUrl": "https://shenlouar.cn/YBBvideozipFFmpeg/pro.html",
      "enabled": true,
      "weight": 1,
      "minPlaySeconds": 15,
      "startAt": "",
      "endAt": "",
      "platform": "Windows",
      "appVersion": "*"
    }
  ]
}
```

字段说明：

- `id`：广告唯一编号，不要重复。
- `title`：广告标题。
- `videoUrl`：广告视频地址。
- `coverUrl`：封面图地址，当前版本可留空。
- `clickUrl`：用户点击“查看详情”后打开的页面。
- `enabled`：是否启用。下线广告时改为 `false`。
- `weight`：播放权重。数字越大，被随机选中的概率越高。
- `minPlaySeconds`：至少展示多少秒后允许关闭。
- `startAt` / `endAt`：广告生效和结束时间，当前可留空。
- `platform`：当前填 `Windows`。
- `appVersion`：当前填 `*`，表示所有版本可用。

新增广告时，在 `ads` 数组里继续增加一项即可。

## 6. 广告视频要求

推荐格式：

- 文件格式：`.mp4`
- 视频编码：H.264
- 音频编码：AAC
- 比例：16:9
- 分辨率：推荐 1920x1080
- 链接：必须是公网可直接访问的 HTTPS 链接

验证视频是否可访问：

```powershell
ffprobe -v error -show_entries format=format_name,duration -show_entries stream=codec_name,codec_type,width,height -of default=noprint_wrappers=1 "视频地址"
```

如果返回 `404 Not Found`，软件也无法播放。

## 7. 点击统计方式

程序会在点击链接后自动追加参数：

- `ad_id`
- `app_version`
- `platform`
- `source=desktop_app`
- `placement=compress_waiting`

例如：

```text
https://example.com/page?ad_id=showreel-2024&app_version=1.1.0&platform=Windows&source=desktop_app&placement=compress_waiting
```

你可以通过短链接后台、落地页统计、联盟后台或网站访问日志查看点击数据。

播放量可以通过 OSS / CDN / 视频点播后台日志查看。

## 8. Pro 付费流程

当前推荐人工付费流程：

1. 用户点击软件右上角“升级 Pro”。
2. 软件打开购买说明页：

```text
https://shenlouar.cn/YBBvideozipFFmpeg/pro.html
```

3. 用户按页面说明付款。
4. 你确认收款。
5. 你在本地生成授权码。
6. 把授权码发给用户。
7. 用户在软件里输入授权码。
8. 软件本地保存授权状态，后续压缩不展示广告。

## 9. 如何生成 Pro 授权码

使用脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\generate-license.ps1 -Payload ORDER123
```

脚本默认读取本机私钥：

```text
secrets/license-private-key.xml
```

这个私钥只保存在本机，不提交到 GitHub，不上传 OSS，也不要发给用户。EXE 内部只包含公钥，用于验证授权码，不能生成授权码。

示例输出：

```text
YBBPRO-ORDER123-一长串签名字符
```

`Payload` 建议使用订单号、付款备注号或你自己的人工订单编号。

要求：

- 只使用英文字母和数字。
- 长度 4 到 32 位。
- 不要使用用户密码、手机号全号、身份证号等敏感信息。

## 10. 授权码规则和限制

当前授权码是离线本地授权。

特点：

- 不需要登录。
- 不需要服务器。
- 不绑定机器。
- EXE 只包含公钥，公开源码后不能直接生成有效授权码。
- 用户重启软件后仍然保持 Pro 状态。

限制：

- 第一版不是强防破解方案。
- 授权码可能被用户转发。
- 私钥丢失后，旧版 EXE 对应的新增授权码将无法继续生成，需要更换公钥并重新打包。
- 当前重点是验证 9.9 元付费意愿和广告版接受度。

## 11. 发布和验证

测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

打包：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-app.ps1
```

最终绿色包：

```text
dist/YBBvideozip-green.zip
```

绿色包内只包含：

- `YBBvideozip.exe`
- `LICENSE`
- `THIRD_PARTY_NOTICES.md`

不需要把 `ad-config.json` 放进绿色包。它应该放在 OSS 上。

## 12. 日常运营检查清单

每次更换广告后检查：

- `ad-config.json` 能在浏览器打开。
- JSON 格式没有错误。
- 每条广告的 `id` 不重复。
- 每条启用广告的 `videoUrl` 能直接访问。
- 视频是 H.264 + AAC MP4。
- `clickUrl` 能正常打开。
- 需要下线的广告已经设置为 `enabled: false`。

每次发 Pro 授权码后记录：

- 订单编号。
- 付款时间。
- 用户联系方式。
- 授权码。
- 是否已发送。
