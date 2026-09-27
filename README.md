# OMEN 降压工具

面向 HP OMEN 的 Intel 机型。使用 Windows 自带 .NET Framework 4.8 WinForms 和 HP BIOS WMI 接口，不调用 OGH。是否需要机型限制交给 BIOS 的能力查询判断，同系列其它 OMEN 机型也可以尝试。需要保留 HP BIOS WMI/ACPI 驱动。

以 Windows PowerShell 运行 `.\build.ps1` 编译。主程序通常位于 `dist\OmenUndervolt.exe`；若旧版程序仍在运行，构建脚本会生成 `dist\OmenUndervolt.next.exe`，避免覆盖正在使用的文件。启动时会请求管理员权限。程序会显示当前处理器电压偏移，并提供 0–200 mV 降压幅度、读取、设置和恢复 0 mV。写入需二次确认，BIOS 接受请求后可选择立即重启或稍后重启；立即重启使用 Windows 正常重启，不强制关闭应用。

界面只使用 WinForms 自带控件和系统颜色，保持 Windows 自带页面的朴素样式，不做自绘、不设深色主题。主界面自上而下是标题区、设备状态（型号、处理器与固件信息、HP BIOS 接口状态）、电压偏移（当前设置与待应用目标）、降压幅度（滑块、数值框与可用范围）、说明文字，以及窗口底部的操作按钮；设备不匹配或接口出错时，状态文字转为警示色。主界面与重启对话框采用表格／流式自动布局，统一外边距、分区间距和按钮尺寸；当前设置与待应用目标等宽对齐，滑块随窗口宽度伸展，数值框与单位保持对齐。窗口可调整大小，按钮固定在窗口底部。程序清单声明 Per-Monitor V2 DPI 感知，避免 Windows 对窗口做位图拉伸。每次读取偏移后，滑块和数值框会同步到对应的负偏移幅度；例如当前为 -130 mV，控件会定位到 130。

窗口和 EXE 图标使用透明背景的 OMEN 徽标，在左上角加入代表负电压的负号与向下箭头。原始矢量图和 PNG 保留在 `assets` 中，来源为 [HP OMEN 标志存档](https://commons.wikimedia.org/wiki/File:HP_Omen_logo.svg)，该页面注明原始来源为 HP；OMEN 为 HP 商标，本工具不是 HP 官方软件。

程序先确认设备是 HP OMEN 的 Intel 机型，再用 HP BIOS 的能力查询判断是否支持降压。非 HP OMEN Intel 机型、能力查询未报告支持、当前偏移读取失败，或 WMI 接口与驱动不可用时，界面会显示对应原因并禁用写入；机型、主板和 BIOS 版本不再参与限制。当前 WMI 写入路径尚未实机验证。

运行 `.\tests\artifacts\ProtocolTests.exe` 执行纯本地协议和设备校验测试；测试不会访问 WMI 或修改固件。

## 免责声明

本工具不是 HP 官方软件，OMEN 是 HP 的商标。它通过 HP BIOS WMI 接口写入处理器电压偏移，该写入路径尚未在实机验证；降压可能导致系统不稳定，请自行评估风险并承担使用后果。

## 许可证

[MIT](LICENSE)。
## Code Wiki

源码结构、WMI 流程、安全边界和构建说明见 [Code Wiki](docs/README.md)。

