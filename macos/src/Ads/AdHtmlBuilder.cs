using System.Net;

namespace YBBvideozip.Mac.Ads;

public static class AdHtmlBuilder
{
    public static string Build(string videoUrl, bool muted)
    {
        var escapedUrl = WebUtility.HtmlEncode(videoUrl);
        var mutedAttribute = muted ? " muted" : "";
        var mutedValue = muted ? "true" : "false";
        var volumeValue = muted ? "0" : "1";
        var soundButtonText = muted ? "声音" : "静音";

        return $$"""
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <style>
    html, body { margin:0; width:100%; height:100%; background:transparent; overflow:hidden; }
    body { position:relative; font-family:-apple-system,BlinkMacSystemFont,"PingFang SC","Microsoft YaHei",sans-serif; }
    .ad-frame { position:absolute; inset:0; background:transparent; overflow:hidden; }
    .video-clip { position:absolute; inset:3px; overflow:hidden; border-radius:13px; background:#000; z-index:1; }
    .dash-frame { position:absolute; inset:0; box-sizing:border-box; border:2px dashed #111; border-radius:15px; pointer-events:none; z-index:5; }
    video { position:absolute; inset:0; width:100%; height:100%; object-fit:cover; background:#000; }
    .chrome { position:absolute; inset:0; pointer-events:none; }
    .top, .bottom { position:absolute; left:14px; right:14px; display:flex; align-items:center; justify-content:space-between; }
    .top { top:12px; }
    .bottom { bottom:12px; }
    .badge, button { height:30px; display:inline-flex; align-items:center; justify-content:center; box-sizing:border-box; border:0; padding:0 13px; font-size:14px; font-weight:700; line-height:30px; white-space:nowrap; user-select:none; cursor:default; pointer-events:auto; }
    .badge, .countdown { color:#fff; background:#232323; border-radius:15px; }
    .action { color:#232323; background:#fff; border-radius:15px; min-width:76px; }
    .details { min-width:88px; }
    .close { display:none; min-width:66px; margin-left:10px; }
    .action:hover, .action:active, .action:focus { color:#232323; background:#fff; outline:0; }
    .badge:hover, .badge:active, .badge:focus, .countdown:hover, .countdown:active, .countdown:focus { color:#fff; background:#232323; outline:0; }
  </style>
</head>
<body>
  <div class="ad-frame">
    <div class="video-clip">
      <video id="ad" src="{{escapedUrl}}" autoplay playsinline{{mutedAttribute}}></video>
      <div class="chrome">
        <div class="top">
          <div class="badge">赞助内容</div>
          <button id="soundButton" class="action" type="button">{{soundButtonText}}</button>
        </div>
        <div class="bottom">
          <button id="countdownButton" class="countdown" type="button">15 秒后可关闭</button>
          <div>
            <button id="detailsButton" class="action details" type="button">查看详情</button>
            <button id="closeButton" class="action close" type="button">关闭</button>
          </div>
        </div>
      </div>
    </div>
    <div class="dash-frame"></div>
  </div>
  <script>
    const video = document.getElementById('ad');
    const soundButton = document.getElementById('soundButton');
    const countdownButton = document.getElementById('countdownButton');
    const detailsButton = document.getElementById('detailsButton');
    const closeButton = document.getElementById('closeButton');
    let muted = {{mutedValue}};

    function post(action) {
      if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
        window.chrome.webview.postMessage(action);
        return;
      }
      if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.ybb) {
        window.webkit.messageHandlers.ybb.postMessage(action);
      }
    }

    function applySoundState() {
      video.muted = muted;
      video.volume = muted ? 0 : 1;
      soundButton.textContent = muted ? '声音' : '静音';
    }

    window.setYbbAdState = function(text, canClose) {
      countdownButton.textContent = text;
      closeButton.style.display = canClose ? 'inline-flex' : 'none';
    };

    soundButton.addEventListener('click', function() {
      muted = !muted;
      applySoundState();
      video.play().catch(() => {});
    });
    detailsButton.addEventListener('click', function() { post('details'); });
    closeButton.addEventListener('click', function() { post('close'); });

    video.muted = {{mutedValue}};
    video.volume = {{volumeValue}};
    applySoundState();
    video.play().catch(() => {});
  </script>
</body>
</html>
""";
    }
}
