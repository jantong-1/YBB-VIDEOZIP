using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace VideoCompressorUI
{
    internal sealed class LicenseDialog : Form
    {
        private readonly TextBox licenseTextBox;
        private readonly Label statusLabel;

        public bool LicenseActivated { get; private set; }

        public LicenseDialog(bool alreadyActivated)
        {
            Text = "升级 Pro";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(430, 250);
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            Label titleLabel = new Label
            {
                AutoSize = false,
                Location = new Point(24, 20),
                Size = new Size(380, 30),
                Text = "Pro 版：9.9 元永久去广告",
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(35, 35, 35)
            };
            Controls.Add(titleLabel);

            LinkLabel purchaseLink = new LinkLabel
            {
                AutoSize = false,
                Location = new Point(24, 62),
                Size = new Size(380, 28),
                Text = AdConfigManager.DefaultPurchaseUrl,
                LinkColor = Color.FromArgb(35, 90, 170)
            };
            purchaseLink.Click += delegate { OpenPurchasePage(); };
            Controls.Add(purchaseLink);

            Label inputLabel = new Label
            {
                AutoSize = false,
                Location = new Point(24, 104),
                Size = new Size(380, 24),
                Text = "输入授权码：",
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            Controls.Add(inputLabel);

            licenseTextBox = new TextBox
            {
                Location = new Point(24, 132),
                Size = new Size(380, 28),
                Font = new Font("Consolas", 11F, FontStyle.Regular, GraphicsUnit.Point)
            };
            Controls.Add(licenseTextBox);

            statusLabel = new Label
            {
                AutoSize = false,
                Location = new Point(24, 164),
                Size = new Size(380, 26),
                ForeColor = alreadyActivated ? Color.FromArgb(40, 120, 70) : Color.FromArgb(120, 120, 120),
                Text = alreadyActivated ? "当前已激活 Pro。" : "付款后输入授权码即可去广告。"
            };
            Controls.Add(statusLabel);

            Button activateButton = new Button
            {
                Location = new Point(224, 204),
                Size = new Size(92, 32),
                Text = "激活"
            };
            activateButton.Click += ActivateButtonClick;
            Controls.Add(activateButton);

            Button closeButton = new Button
            {
                Location = new Point(324, 204),
                Size = new Size(80, 32),
                Text = "关闭",
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(closeButton);

            AcceptButton = activateButton;
            CancelButton = closeButton;
        }

        private void ActivateButtonClick(object sender, EventArgs e)
        {
            string message;
            if (LicenseManager.SaveLicenseCode(licenseTextBox.Text, out message))
            {
                LicenseActivated = true;
                statusLabel.ForeColor = Color.FromArgb(40, 120, 70);
                statusLabel.Text = message;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            statusLabel.ForeColor = Color.FromArgb(170, 40, 40);
            statusLabel.Text = message;
        }

        private void OpenPurchasePage()
        {
            try
            {
                Process.Start(AdConfigManager.DefaultPurchaseUrl);
            }
            catch
            {
                statusLabel.ForeColor = Color.FromArgb(170, 40, 40);
                statusLabel.Text = "无法打开购买页面，请手动访问上方链接。";
            }
        }
    }
}
