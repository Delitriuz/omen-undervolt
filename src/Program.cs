using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Security.Principal;
using System.Windows.Forms;

namespace OmenUndervolt
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private static readonly Color AlertColor = Color.FromArgb(0xC4, 0x2B, 0x1C);
        private static readonly string RangeSummary =
            "可用幅度 0–" + SafetyProtocol.MaximumMagnitudeMv + " mV · 步进 1 mV";

        private readonly Label deviceModel;
        private readonly Label deviceDetail;
        private readonly Label firmwareStatus;
        private readonly Label currentValue;
        private readonly Label selectedValue;
        private readonly Label rangeLabel;
        private readonly TrackBar slider;
        private readonly NumericUpDown numericValue;
        private readonly Button refreshButton;
        private readonly Button applyButton;
        private readonly Button resetButton;
        private bool supportedTarget;
        private bool firmwareReady;
        private bool loading;
        private string pendingOffset;

        internal MainForm()
        {
            SuspendLayout();
            Text = "OMEN 本机降压工具";
            Icon = LoadLogoIcon();
            Font = SystemFonts.MessageBoxFont;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(560, 520);
            MinimumSize = SizeFromClientSize(new Size(520, 480));
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;

            TableLayoutPanel layout = CreateTable(1);
            layout.Dock = DockStyle.Fill;
            layout.AutoSize = false;
            layout.Padding = new Padding(16);
            layout.RowCount = 7;
            for (int row = 0; row < 7; row++)
                layout.RowStyles.Add(new RowStyle(row == 4 ? SizeType.Percent : SizeType.AutoSize, row == 4 ? 100F : 0F));

            TableLayoutPanel header = CreateTable(2);
            header.ColumnStyles.Clear();
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.Margin = new Padding(0, 0, 0, 12);
            PictureBox logo = new PictureBox
            {
                Size = new Size(32, 32),
                Margin = new Padding(0, 0, 10, 0),
                Anchor = AnchorStyles.Left,
                Image = LoadLogoImage(),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false
            };
            TableLayoutPanel heading = CreateTable(1);
            Label title = CreateLabel("OMEN 本机降压工具");
            title.Font = new Font(Font.FontFamily, 12F, FontStyle.Regular);
            title.Margin = new Padding(0, 0, 0, 2);
            Label subtitle = CreateLabel("处理器电压偏移 · HP BIOS 固件接口");
            subtitle.ForeColor = SystemColors.GrayText;
            heading.Controls.Add(title, 0, 0);
            heading.Controls.Add(subtitle, 0, 1);
            header.Controls.Add(logo, 0, 0);
            header.Controls.Add(heading, 1, 0);
            layout.Controls.Add(header, 0, 0);

            GroupBox statusGroup = CreateGroup("设备状态");
            TableLayoutPanel statusLayout = CreateTable(1);
            deviceModel = CreateLabel("正在检查本机配置…");
            deviceDetail = CreateLabel("读取处理器与固件信息");
            deviceDetail.ForeColor = SystemColors.GrayText;
            firmwareStatus = CreateLabel("正在读取 HP BIOS 降压接口…");
            firmwareStatus.ForeColor = SystemColors.GrayText;
            firmwareStatus.Margin = new Padding(0, 4, 0, 0);
            statusLayout.Controls.Add(deviceModel, 0, 0);
            statusLayout.Controls.Add(deviceDetail, 0, 1);
            statusLayout.Controls.Add(firmwareStatus, 0, 2);
            statusGroup.Controls.Add(statusLayout);
            layout.Controls.Add(statusGroup, 0, 1);

            GroupBox valuesGroup = CreateGroup("电压偏移");
            TableLayoutPanel valuesLayout = CreateTable(2);
            Label currentCaption = CreateLabel("当前设置");
            Label targetCaption = CreateLabel("待应用目标");
            currentCaption.ForeColor = targetCaption.ForeColor = SystemColors.GrayText;
            currentCaption.Margin = targetCaption.Margin = new Padding(0, 0, 0, 4);
            currentValue = CreateLabel("未读取");
            selectedValue = CreateLabel("0 mV");
            currentValue.Font = new Font(Font.FontFamily, 14F, FontStyle.Regular);
            selectedValue.Font = new Font(Font.FontFamily, 14F, FontStyle.Regular);
            valuesLayout.Controls.Add(currentCaption, 0, 0);
            valuesLayout.Controls.Add(targetCaption, 1, 0);
            valuesLayout.Controls.Add(currentValue, 0, 1);
            valuesLayout.Controls.Add(selectedValue, 1, 1);
            valuesGroup.Controls.Add(valuesLayout);
            layout.Controls.Add(valuesGroup, 0, 2);

            GroupBox controlGroup = CreateGroup("降压幅度");
            TableLayoutPanel controlLayout = CreateTable(3);
            controlLayout.ColumnStyles.Clear();
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            slider = new TrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = SafetyProtocol.MaximumMagnitudeMv,
                TickFrequency = 20,
                SmallChange = 1,
                LargeChange = 10,
                Margin = new Padding(0, 0, 16, 0),
                AccessibleName = "降压幅度滑块",
                TabIndex = 0
            };
            numericValue = new NumericUpDown
            {
                Width = 76,
                Minimum = 0,
                Maximum = SafetyProtocol.MaximumMagnitudeMv,
                Increment = 1,
                TextAlign = HorizontalAlignment.Right,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 8, 0),
                AccessibleName = "降压幅度，单位毫伏",
                TabIndex = 1
            };
            Label unitLabel = CreateLabel("mV");
            unitLabel.Anchor = AnchorStyles.Left;
            controlLayout.Controls.Add(slider, 0, 0);
            controlLayout.Controls.Add(numericValue, 1, 0);
            controlLayout.Controls.Add(unitLabel, 2, 0);
            rangeLabel = CreateLabel(RangeSummary);
            rangeLabel.ForeColor = SystemColors.GrayText;
            rangeLabel.Margin = new Padding(0, 8, 0, 0);
            controlLayout.Controls.Add(rangeLabel, 0, 1);
            controlLayout.SetColumnSpan(rangeLabel, 3);
            controlGroup.Controls.Add(controlLayout);
            layout.Controls.Add(controlGroup, 0, 3);

            Label note = CreateLabel("BIOS 报告支持不代表写入一定成功；应用后需要重启复核。");
            note.ForeColor = SystemColors.GrayText;
            note.Margin = new Padding(0, 8, 0, 8);
            layout.Controls.Add(note, 0, 5);

            TableLayoutPanel actions = CreateTable(4);
            actions.ColumnStyles.Clear();
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            refreshButton = CreateButton("重新读取");
            resetButton = CreateButton("恢复 0 mV");
            applyButton = CreateButton("应用设置");
            refreshButton.Margin = new Padding(0, 0, 8, 0);
            actions.Controls.Add(refreshButton, 0, 0);
            actions.Controls.Add(resetButton, 1, 0);
            actions.Controls.Add(applyButton, 3, 0);
            layout.Controls.Add(actions, 0, 6);

            Controls.Add(layout);
            ResumeLayout(true);

            slider.ValueChanged += delegate { if (!loading) SetMagnitude(slider.Value); };
            numericValue.ValueChanged += delegate { if (!loading) SetMagnitude((int)numericValue.Value); };
            refreshButton.Click += delegate { RefreshFirmwareState(); };
            applyButton.Click += delegate { WriteSelectedOffset((int)numericValue.Value); };
            resetButton.Click += delegate { SetMagnitude(0); WriteSelectedOffset(0); };
            Load += delegate { InitializeTool(); };
            SetMagnitude(0);
        }

        private void InitializeTool()
        {
            if (!IsAdministrator())
            {
                deviceModel.Text = "需要管理员权限";
                deviceDetail.Text = "请重新启动程序，并在 Windows 提示中确认。";
                SetFirmwareStatus("未取得管理员权限，写入已禁用。", true);
                SetWriteControlsEnabled(false);
                return;
            }

            try
            {
                DeviceInfo device = ReadDeviceInfo();
                deviceModel.Text = string.IsNullOrWhiteSpace(device.Model) ? "未知型号" : device.Model.Trim();
                deviceDetail.Text = DeviceDetail(device);
                supportedTarget = SafetyProtocol.IsSupportedTarget(device);
                RefreshFirmwareState();
            }
            catch (Exception exception)
            {
                deviceModel.Text = "设备检查失败";
                deviceDetail.Text = "无法读取本机型号与固件信息。";
                SetFirmwareStatus(exception.Message, true);
                SetWriteControlsEnabled(false);
            }
        }

        private void RefreshFirmwareState()
        {
            firmwareReady = false;
            SetWriteControlsEnabled(false);
            currentValue.Text = "读取中…";
            rangeLabel.Text = RangeSummary;
            SetFirmwareStatus("正在读取 HP BIOS 降压接口…", false);
            try
            {
                byte[] capability = HpBiosWmi.ReadCapability();
                if (!SafetyProtocol.IsUndervoltingAvailable(capability))
                    throw new InvalidOperationException("BIOS 未报告处理器降压能力，该机型可能不支持此命令。");

                int offset = HpBiosWmi.ReadProcessorOffsetMv();
                currentValue.Text = FormatOffset(offset);
                SetMagnitude(SafetyProtocol.GetSliderMagnitude(offset));
                firmwareReady = true;
                if (!supportedTarget)
                    SetFirmwareStatus("非 HP OMEN Intel 机型，写入已禁用；仅可读取当前偏移。", true);
                else if (pendingOffset != null)
                    SetFirmwareStatus("BIOS 已接受 " + pendingOffset + " 请求；重启后才能复核。", false);
                else
                    SetFirmwareStatus("HP BIOS 已报告支持降压；写入后需要重启复核。", false);
            }
            catch (Exception exception)
            {
                currentValue.Text = "未读取";
                rangeLabel.Text = "未取得可用范围，调整已禁用。";
                SetFirmwareStatus("无法使用 HP BIOS 降压接口：" + exception.Message, true);
            }
            finally
            {
                SetWriteControlsEnabled(CanWrite);
            }
        }

        private void WriteSelectedOffset(int magnitudeMv)
        {
            if (!CanWrite)
                return;

            string offset = FormatOffset(-magnitudeMv);
            DialogResult confirmation = MessageBox.Show(
                "将请求 BIOS 设置处理器偏移为 " + offset + "。\n\n"
                + "BIOS 报告支持只表示它接受这条命令，不代表写入一定生效；该机型也可能尚未验证。\n"
                + "写入可能导致系统不稳定；成功后可选择立即重启或稍后重启。继续吗？",
                "确认电压偏移",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirmation != DialogResult.Yes)
                return;

            SetWriteControlsEnabled(false);
            try
            {
                HpBiosWmi.WriteProcessorOffsetMv(magnitudeMv);
                pendingOffset = offset;
                currentValue.Text = "待重启";
                SetFirmwareStatus("BIOS 已接受 " + offset + " 请求；等待重启。", false);
                using (RestartChoiceDialog dialog = new RestartChoiceDialog(offset))
                {
                    if (dialog.ShowDialog(this) == DialogResult.Yes)
                    {
                        SetFirmwareStatus("正在请求 Windows 重启…", false);
                        RequestWindowsRestart();
                    }
                }
            }
            catch (Exception exception)
            {
                SetFirmwareStatus("操作未完成：" + exception.Message, true);
                MessageBox.Show(
                    "操作未完成：" + exception.Message + "\n\n若 BIOS 已接受写入，请稍后手动重启。",
                    "HP BIOS / 重启错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                SetWriteControlsEnabled(CanWrite);
            }
        }

        private void SetMagnitude(int magnitude)
        {
            magnitude = Math.Min(SafetyProtocol.MaximumMagnitudeMv, Math.Max(0, magnitude));
            loading = true;
            slider.Value = magnitude;
            numericValue.Value = magnitude;
            selectedValue.Text = FormatOffset(-magnitude);
            loading = false;
        }

        /// <summary>能力查询通过且当前偏移读取成功，才允许写入。</summary>
        private bool CanWrite
        {
            get { return supportedTarget && firmwareReady; }
        }

        private void SetWriteControlsEnabled(bool enabled)
        {
            slider.Enabled = enabled;
            numericValue.Enabled = enabled;
            applyButton.Enabled = enabled;
            resetButton.Enabled = enabled;
        }

        private void SetFirmwareStatus(string text, bool alert)
        {
            firmwareStatus.Text = text;
            firmwareStatus.ForeColor = alert ? AlertColor : SystemColors.GrayText;
        }

        private static string DeviceDetail(DeviceInfo device)
        {
            string detail = string.IsNullOrWhiteSpace(device.Processor) ? "未知处理器" : device.Processor.Trim();
            if (!string.IsNullOrWhiteSpace(device.Board))
                detail += " · " + device.Board.Trim();
            if (!string.IsNullOrWhiteSpace(device.BiosVersion))
                detail += " · " + device.BiosVersion.Trim();
            return detail;
        }

        private static bool IsAdministrator()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static DeviceInfo ReadDeviceInfo()
        {
            return new DeviceInfo
            {
                Manufacturer = ReadWmiProperty("Win32_ComputerSystem", "Manufacturer"),
                Model = ReadWmiProperty("Win32_ComputerSystem", "Model"),
                Board = ReadWmiProperty("Win32_BaseBoard", "Product"),
                Processor = ReadWmiProperty("Win32_Processor", "Name"),
                BiosVersion = ReadWmiProperty("Win32_BIOS", "SMBIOSBIOSVersion")
            };
        }

        private static string ReadWmiProperty(string className, string propertyName)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT " + propertyName + " FROM " + className))
            using (ManagementObjectCollection results = searcher.Get())
            {
                foreach (ManagementObject result in results)
                using (result)
                    return Convert.ToString(result[propertyName]);
            }
            return string.Empty;
        }

        private static string FormatOffset(int offsetMv)
        {
            return offsetMv.ToString("+0;-0;0") + " mV";
        }

        private static Image LoadLogoImage()
        {
            using (Stream stream = typeof(MainForm).Assembly.GetManifestResourceStream("OmenUndervolt.Assets.OmenLogo.png"))
            {
                if (stream == null)
                    throw new InvalidOperationException("程序中缺少 OMEN 图标资源。");
                using (Image source = Image.FromStream(stream))
                    return new Bitmap(source);
            }
        }

        private static Icon LoadLogoIcon()
        {
            using (Stream stream = typeof(MainForm).Assembly.GetManifestResourceStream("OmenUndervolt.Assets.OmenIcon.ico"))
            {
                if (stream == null)
                    throw new InvalidOperationException("程序中缺少 OMEN 窗口图标资源。");
                return new Icon(stream);
            }
        }

        private static TableLayoutPanel CreateTable(int columns)
        {
            TableLayoutPanel table = new TableLayoutPanel
            {
                ColumnCount = columns,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            for (int column = 0; column < columns; column++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
            return table;
        }

        internal static Label CreateLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Margin = Padding.Empty, Anchor = AnchorStyles.Left };
        }

        private static GroupBox CreateGroup(string text)
        {
            return new GroupBox
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(12, 8, 12, 12),
                Margin = new Padding(0, 0, 0, 12)
            };
        }

        internal static Button CreateButton(string text)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(96, 28),
                Padding = new Padding(10, 2, 10, 2),
                Margin = Padding.Empty,
                FlatStyle = FlatStyle.Standard,
                UseVisualStyleBackColor = true
            };
        }

        private static void RequestWindowsRestart()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "shutdown.exe"),
                Arguments = "/r /t 0",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("无法启动 Windows 重启命令。");
                if (process.WaitForExit(5000) && process.ExitCode != 0)
                    throw new InvalidOperationException("Windows 重启命令返回错误码 " + process.ExitCode + "。");
            }
        }
    }

    internal sealed class RestartChoiceDialog : Form
    {
        internal RestartChoiceDialog(string offset)
        {
            Text = "降压请求已接受";
            Font = SystemFonts.MessageBoxFont;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(420, 170);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 3,
                BackColor = SystemColors.Window,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Label message = MainForm.CreateLabel("BIOS 已接受 " + offset + " 写入请求。\n重启后才能复核设置状态。");
            message.Margin = new Padding(0, 0, 0, 12);
            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };
            Button restartLater = MainForm.CreateButton("稍后重启");
            restartLater.DialogResult = DialogResult.No;
            restartLater.Margin = new Padding(0, 0, 8, 0);
            Button restartNow = MainForm.CreateButton("立即重启");
            restartNow.DialogResult = DialogResult.Yes;
            actions.Controls.Add(restartLater);
            actions.Controls.Add(restartNow);
            layout.Controls.Add(message, 0, 0);
            layout.Controls.Add(actions, 0, 2);
            Controls.Add(layout);
            AcceptButton = restartLater;
            CancelButton = restartLater;
        }
    }
}
