using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VideoCompressorUI
{
    internal sealed class AdDisplayPanel : RoundedPanel
    {
        private static readonly string SponsoredText = "\u8d5e\u52a9\u5185\u5bb9";
        private static readonly string LoadingText = "\u5e7f\u544a\u52a0\u8f7d\u4e2d";
        private static readonly string UnavailableText = "\u5e7f\u544a\u89c6\u9891\u6682\u4e0d\u53ef\u7528";
        private static readonly string CloseAllowedText = "\u53ef\u4ee5\u5173\u95ed\u5e7f\u544a";
        private static readonly string DetailText = "\u67e5\u770b\u8be6\u60c5";
        private static readonly string CloseText = "\u5173\u95ed";
        private static readonly string MuteText = "\u9759\u97f3";
        private static readonly string SoundText = "\u58f0\u97f3";

        private readonly WindowsMediaPlayerHost player;
        private readonly OverlayPill badgePill;
        private readonly OverlayPill countdownPill;
        private readonly OverlayPill fallbackPill;
        private readonly OverlayPill clickButton;
        private readonly OverlayPill closeButton;
        private readonly OverlayPill soundButton;
        private readonly Timer timer;

        private DateTime startedAtUtc;
        private int minPlaySeconds = 15;
        private bool minimumRaised;
        private bool muted = false;

        public event EventHandler CloseRequested;
        public event EventHandler AdClicked;
        public event EventHandler MinimumPlayReached;

        public bool MinimumElapsed
        {
            get
            {
                return (DateTime.UtcNow - startedAtUtc).TotalSeconds >= minPlaySeconds;
            }
        }

        public AdDisplayPanel()
        {
            Radius = 13;
            BorderWidth = 0;
            DrawBorder = false;
            BorderColor = Color.Transparent;
            BackColor = Color.Black;
            Visible = false;

            try
            {
                player = new WindowsMediaPlayerHost
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black
                };
                Controls.Add(player);
            }
            catch
            {
                player = null;
            }

            fallbackPill = new OverlayPill
            {
                Text = LoadingText,
                FillColor = Color.White,
                TextColor = Color.FromArgb(45, 45, 45),
                BorderColor = Color.Black,
                BorderWidth = 1,
                Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Regular, GraphicsUnit.Point),
                Visible = false
            };
            Controls.Add(fallbackPill);

            badgePill = CreateDarkPill(SponsoredText, 11F);
            Controls.Add(badgePill);

            countdownPill = CreateDarkPill("", 11F);
            Controls.Add(countdownPill);

            clickButton = CreateLightButton(DetailText, 12F);
            clickButton.Cursor = Cursors.Hand;
            clickButton.Click += delegate { RaiseAdClicked(); };
            Controls.Add(clickButton);

            closeButton = CreateLightButton(CloseText, 12F);
            closeButton.Cursor = Cursors.Hand;
            closeButton.Visible = false;
            closeButton.Click += delegate { RaiseCloseRequested(); };
            Controls.Add(closeButton);

            soundButton = CreateLightButton(SoundText, 11F);
            soundButton.Cursor = Cursors.Hand;
            soundButton.Click += delegate { ToggleSound(); };
            Controls.Add(soundButton);

            timer = new Timer();
            timer.Interval = 500;
            timer.Tick += delegate { UpdateCountdown(); };
        }

        public void StartAd(AdItem ad)
        {
            if (ad == null)
            {
                return;
            }

            startedAtUtc = DateTime.UtcNow;
            minPlaySeconds = Math.Max(1, ad.MinPlaySeconds);
            minimumRaised = false;
            muted = false;
            closeButton.Visible = false;
            soundButton.Text = SoundText;
            fallbackPill.Text = LoadingText;
            fallbackPill.Visible = false;

            Visible = true;
            ApplyRoundedRegion();
            LayoutChildren();
            BringToFront();
            UpdateCountdown();

            if (String.IsNullOrWhiteSpace(ad.VideoUrl) || player == null)
            {
                ShowFallback(UnavailableText);
            }
            else
            {
                try
                {
                    player.Visible = true;
                    player.Play(ad.VideoUrl, muted);
                }
                catch (Exception ex)
                {
                    ShowFallback(UnavailableText + "\r\n" + ex.Message);
                }
            }

            BringOverlayToFront();
            timer.Start();
        }

        public void StopAd()
        {
            timer.Stop();
            if (player != null)
            {
                player.StopPlayback();
            }

            Visible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion();
            LayoutChildren();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundedRegion();
        }

        private static OverlayPill CreateDarkPill(string text, float fontSize)
        {
            return new OverlayPill
            {
                Text = text,
                FillColor = Color.FromArgb(35, 35, 35),
                TextColor = Color.White,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Font = new Font("Microsoft YaHei UI", fontSize, FontStyle.Regular, GraphicsUnit.Point)
            };
        }

        private static OverlayPill CreateLightButton(string text, float fontSize)
        {
            return new OverlayPill
            {
                Text = text,
                FillColor = Color.White,
                TextColor = Color.FromArgb(35, 35, 35),
                BorderColor = Color.Black,
                BorderWidth = 2,
                Font = new Font("Microsoft YaHei UI", fontSize, FontStyle.Regular, GraphicsUnit.Point)
            };
        }

        private void LayoutChildren()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            int margin = 10;
            Size badgeSize = badgePill.MeasurePreferredSize();
            badgePill.SetBounds(margin, margin, badgeSize.Width, badgeSize.Height);

            Size soundSize = soundButton.MeasurePreferredSize();
            soundButton.SetBounds(ClientSize.Width - margin - soundSize.Width, margin, soundSize.Width, soundSize.Height);

            Size countdownSize = countdownPill.MeasurePreferredSize();
            countdownPill.SetBounds(margin, ClientSize.Height - margin - countdownSize.Height, countdownSize.Width, countdownSize.Height);

            Size closeSize = closeButton.MeasurePreferredSize();
            Size clickSize = clickButton.MeasurePreferredSize();
            int buttonGap = 8;
            int closeX = ClientSize.Width - margin - closeSize.Width;
            closeButton.SetBounds(closeX, ClientSize.Height - margin - closeSize.Height, closeSize.Width, closeSize.Height);
            clickButton.SetBounds(closeX - buttonGap - clickSize.Width, ClientSize.Height - margin - clickSize.Height, clickSize.Width, clickSize.Height);

            Size fallbackSize = fallbackPill.MeasurePreferredSize();
            fallbackSize.Width = Math.Min(Math.Max(fallbackSize.Width, 170), Math.Max(170, ClientSize.Width - margin * 2));
            fallbackPill.SetBounds(
                (ClientSize.Width - fallbackSize.Width) / 2,
                (ClientSize.Height - fallbackSize.Height) / 2,
                fallbackSize.Width,
                fallbackSize.Height);
        }

        private void BringOverlayToFront()
        {
            fallbackPill.BringToFront();
            badgePill.BringToFront();
            soundButton.BringToFront();
            countdownPill.BringToFront();
            clickButton.BringToFront();
            closeButton.BringToFront();
        }

        private void ShowFallback(string text)
        {
            if (player != null)
            {
                player.StopPlayback();
                player.Visible = false;
            }

            fallbackPill.Text = text;
            fallbackPill.Visible = true;
            LayoutChildren();
            BringOverlayToFront();
        }

        private void UpdateCountdown()
        {
            double elapsed = Math.Max(0, (DateTime.UtcNow - startedAtUtc).TotalSeconds);
            int remaining = Math.Max(0, minPlaySeconds - (int)Math.Floor(elapsed));
            if (remaining > 0)
            {
                countdownPill.Text = remaining.ToString() + " \u79d2\u540e\u53ef\u5173\u95ed";
                closeButton.Visible = false;
            }
            else
            {
                countdownPill.Text = CloseAllowedText;
                closeButton.Visible = true;
                if (!minimumRaised)
                {
                    minimumRaised = true;
                    EventHandler handler = MinimumPlayReached;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
            }

            LayoutChildren();
            BringOverlayToFront();
        }

        private void RaiseCloseRequested()
        {
            EventHandler handler = CloseRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void RaiseAdClicked()
        {
            EventHandler handler = AdClicked;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void ToggleSound()
        {
            muted = !muted;
            soundButton.Text = muted ? MuteText : SoundText;
            if (player != null)
            {
                player.SetMuted(muted);
            }

            LayoutChildren();
            BringOverlayToFront();
        }

        private void ApplyRoundedRegion()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            using (GraphicsPath path = RoundedRectangle(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), Radius))
            {
                Region = new Region(path);
            }
        }
    }

    internal sealed class OverlayPill : Control
    {
        public Color FillColor { get; set; }
        public Color TextColor { get; set; }
        public Color BorderColor { get; set; }
        public int BorderWidth { get; set; }

        public OverlayPill()
        {
            FillColor = Color.White;
            TextColor = Color.Black;
            BorderColor = Color.Transparent;
            BorderWidth = 0;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            TabStop = false;
        }

        public Size MeasurePreferredSize()
        {
            Size measured = TextRenderer.MeasureText(
                String.IsNullOrEmpty(Text) ? " " : Text,
                Font,
                new Size(1000, 1000),
                TextFormatFlags.NoPadding);
            int horizontalPadding = BorderWidth > 0 ? 22 : 14;
            int verticalPadding = BorderWidth > 0 ? 10 : 8;
            return new Size(
                Math.Max(34, measured.Width + horizontalPadding),
                Math.Max(22, measured.Height + verticalPadding));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (GraphicsPath path = RoundedRectangle(new Rectangle(0, 0, Width, Height), CornerRadius()))
            {
                Region = new Region(path);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = RoundedRectangle(rect, CornerRadius()))
            using (SolidBrush brush = new SolidBrush(FillColor))
            {
                e.Graphics.FillPath(brush, path);
                if (BorderWidth > 0)
                {
                    using (Pen pen = new Pen(BorderColor, BorderWidth))
                    {
                        pen.LineJoin = LineJoin.Round;
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                rect,
                TextColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis);
        }

        private int CornerRadius()
        {
            return Math.Max(6, Math.Min(Height / 2, 12));
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            GraphicsPath path = new GraphicsPath();
            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class WindowsMediaPlayerHost : AxHost
    {
        private const string WindowsMediaPlayerClsid = "6BF52A52-394A-11D3-B153-00C04F79FAA6";

        public WindowsMediaPlayerHost()
            : base(WindowsMediaPlayerClsid)
        {
            TabStop = false;
        }

        public void Play(string url, bool muted)
        {
            dynamic player = GetPlayer();
            player.uiMode = "none";
            player.enableContextMenu = false;
            player.stretchToFit = true;
            player.settings.autoStart = true;
            player.settings.mute = muted;
            player.settings.volume = muted ? 0 : 80;
            player.URL = url;
            player.controls.play();
        }

        public void SetMuted(bool muted)
        {
            try
            {
                dynamic player = GetPlayer();
                player.settings.mute = muted;
                player.settings.volume = muted ? 0 : 80;
            }
            catch
            {
            }
        }

        public void StopPlayback()
        {
            try
            {
                dynamic player = GetPlayer();
                player.controls.stop();
                player.URL = "";
            }
            catch
            {
            }
        }

        private object GetPlayer()
        {
            if (!IsHandleCreated)
            {
                CreateControl();
            }

            object player = GetOcx();
            if (player == null)
            {
                throw new InvalidOperationException("Windows Media Player ActiveX is not available.");
            }

            return player;
        }
    }
}
