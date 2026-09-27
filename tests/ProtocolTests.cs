using System;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using OmenUndervolt;

internal static class ProtocolTests
{
    private static int assertions;

    private static int Main()
    {
        TestEncoding(0, 1, 0, 0);
        TestEncoding(10, 0, 0, 10);
        TestEncoding(130, 0, 0, 130);
        TestEncoding(200, 0, 0, 200);
        TestDecode();
        TestSliderMapping();
        TestRangeValidation();
        TestCapabilityValidation();
        TestMainForm();
        TestInitialization();
        TestRestartChoiceDialog();
        Console.WriteLine("通过 {0} 项协议与设备校验断言。", assertions);
        return 0;
    }

    private static void TestEncoding(int magnitude, byte sign, byte high, byte low)
    {
        byte[] data = SafetyProtocol.EncodeOffset(magnitude);
        Equal(128, data.Length, "写入缓冲区长度");
        Equal((byte)0, data[0], "处理器 ID 高字节");
        Equal((byte)3, data[1], "处理器 ID 低字节");
        Equal(sign, data[2], "符号");
        Equal(high, data[3], "幅度高字节");
        Equal(low, data[4], "幅度低字节");
        for (int index = 5; index < data.Length; index++)
            Equal((byte)0, data[index], "写入填充");
    }

    private static void TestDecode()
    {
        Equal(-130, SafetyProtocol.DecodeOffset(new byte[] { 0, 3, 0, 0, 130, 0, 0, 1, 244 }), "负偏移读取");
        Equal(0, SafetyProtocol.DecodeOffset(new byte[] { 0, 3, 1, 0, 0, 0, 0, 1, 244 }), "零偏移读取");
        Throws<InvalidOperationException>(delegate { SafetyProtocol.DecodeOffset(new byte[8]); }, "短返回数据");
        Throws<InvalidOperationException>(delegate { SafetyProtocol.DecodeOffset(new byte[] { 0, 1, 0, 0, 1, 0, 0, 0, 1 }); }, "错误控制 ID");
    }

    private static void TestRangeValidation()
    {
        Throws<ArgumentOutOfRangeException>(delegate { SafetyProtocol.EncodeOffset(-1); }, "负幅度");
        Throws<ArgumentOutOfRangeException>(delegate { SafetyProtocol.EncodeOffset(201); }, "超过上限");
    }

    private static void TestSliderMapping()
    {
        Equal(0, SafetyProtocol.GetSliderMagnitude(0), "零偏移映射");
        Equal(80, SafetyProtocol.GetSliderMagnitude(-80), "负偏移映射");
        Equal(130, SafetyProtocol.GetSliderMagnitude(-130), "现有降压设置映射");
        Equal(200, SafetyProtocol.GetSliderMagnitude(-200), "滑块上限映射");
        Equal(200, SafetyProtocol.GetSliderMagnitude(-240), "超上限偏移截到滑块范围");
        Equal(0, SafetyProtocol.GetSliderMagnitude(40), "正偏移不映射为降压幅度");
    }

    private static void TestCapabilityValidation()
    {
        // 第 3 字节按位判断：bit 0 表示支持降压，实机见过 0x03。
        True(SafetyProtocol.IsUndervoltingAvailable(new byte[] { 0, 0, 1, 0 }), "能力值 0x01");
        True(SafetyProtocol.IsUndervoltingAvailable(new byte[] { 0, 0, 3, 0 }), "能力值 0x03");
        True(SafetyProtocol.IsUndervoltingAvailable(new byte[] { 0, 0, 0x21, 0 }), "能力值 0x21");
        True(!SafetyProtocol.IsUndervoltingAvailable(new byte[] { 0, 0, 0, 0 }), "不支持值");
        True(!SafetyProtocol.IsUndervoltingAvailable(new byte[] { 0, 0, 2, 0 }), "仅有其它标志位");
        True(!SafetyProtocol.IsUndervoltingAvailable(new byte[] { 0, 0 }), "短能力数据");
        True(!SafetyProtocol.IsUndervoltingAvailable(null), "空能力数据");
    }

    private static void TestMainForm()
    {
        using (MainForm form = new MainForm())
        {
            Equal("OMEN 降压工具", form.Text, "窗口标题");
            True(form.Icon != null, "窗口图标已加载");
            PictureBox logo = Descendants(form).OfType<PictureBox>().Single();
            True(logo.Image != null, "界面图标已加载");
            Equal((byte)0, ((System.Drawing.Bitmap)logo.Image).GetPixel(0, 0).A, "界面图标透明背景");
            Equal(FormBorderStyle.Sizable, form.FormBorderStyle, "窗口可调整大小");
            Label heading = Descendants(form).OfType<Label>().Single(label => label.Text == "OMEN 降压工具");
            Label unit = Descendants(form).OfType<Label>().Single(label => label.Text == "mV");
            True(heading.Height >= heading.GetPreferredSize(System.Drawing.Size.Empty).Height, "主标题高度足够");
            True(unit.Width >= unit.GetPreferredSize(System.Drawing.Size.Empty).Width, "单位宽度足够");
            TrackBar slider = Descendants(form).OfType<TrackBar>().Single();
            NumericUpDown input = Descendants(form).OfType<NumericUpDown>().Single();
            // 使用 Windows 自带控件：分组框、标签、滑块和数值框，不做自绘。
            True(Descendants(form).OfType<GroupBox>().Count() >= 3, "使用系统分组框");
            True(Descendants(form).OfType<Label>().Any(label => label.Text == "正在读取设备信息…"), "分组框显示设备行");
            True(Descendants(form).OfType<Label>().Any(label => label.Text == "正在读取 HP BIOS 降压接口…"), "分组框显示状态行");
            Equal(0, slider.Minimum, "滑块下限");
            Equal(200, slider.Maximum, "滑块上限");
            Equal(0m, input.Minimum, "数值框下限");
            Equal(200m, input.Maximum, "数值框上限");
            Equal(3, Descendants(form).OfType<Button>().Count(), "界面操作按钮数量");
            slider.Value = 130;
            Equal(130m, input.Value, "滑块同步数值框");
            input.Value = 80;
            Equal(80, slider.Value, "数值框同步滑块");
            True(Descendants(form).OfType<Button>().All(button => button.FlatStyle == FlatStyle.Standard), "按钮使用系统样式");
            Equal(System.Drawing.SystemColors.Window, form.BackColor, "窗口使用系统背景色");
            int initialSliderWidth = slider.Width;
            form.ClientSize = new System.Drawing.Size(form.ClientSize.Width + 120, form.ClientSize.Height + 80);
            True(slider.Width > initialSliderWidth, "窗口加宽时滑块扩展");
            True(Descendants(form).OfType<Button>().Single(button => button.Text == "应用设置").Bottom < form.ClientSize.Height,
                "窗口加高时操作按钮保持可见");
        }
    }

    private static void TestInitialization()
    {
        using (MainForm form = new MainForm())
        {
            // 触发 Load 时的初始化流程；未提权时必须给出权限提示而不是抛异常。
            typeof(MainForm).GetMethod("InitializeTool", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(form, null);
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                True(Descendants(form).OfType<Label>().Any(label => label.Text == "需要管理员权限"),
                    "未提权时显示权限状态");
                True(Descendants(form).OfType<Label>().Any(label => label.Text == "未取得管理员权限，写入已禁用。"),
                    "未提权时状态条说明原因");
            }
        }
    }

    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child))
                yield return descendant;
        }
    }

    private static void TestRestartChoiceDialog()
    {
        using (RestartChoiceDialog dialog = new RestartChoiceDialog("-130 mV"))
        {
            Button immediate = Descendants(dialog).OfType<Button>().Single(button => button.Text == "立即重启");
            Button later = Descendants(dialog).OfType<Button>().Single(button => button.Text == "稍后重启");
            Equal(DialogResult.Yes, immediate.DialogResult, "立即重启动作");
            Equal(DialogResult.No, later.DialogResult, "稍后重启动作");
            True(object.ReferenceEquals(later, dialog.AcceptButton), "回车默认稍后重启");
            True(object.ReferenceEquals(later, dialog.CancelButton), "取消关闭不触发重启");
        }
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        assertions++;
        if (!object.Equals(expected, actual))
            throw new Exception(name + "：预期 " + expected + "，实际 " + actual);
    }

    private static void True(bool value, string name)
    {
        Equal(true, value, name);
    }

    private static void Throws<T>(Action action, string name) where T : Exception
    {
        assertions++;
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception(name + "：预期抛出 " + typeof(T).Name);
    }
}
