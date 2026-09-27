# 构建与本地检查

## 构建（WinForms）

要求 Windows、Windows PowerShell 和 .NET Framework 4.8 开发工具（脚本使用系统 `csc.exe`）。在 `undervolt-tool/` 目录运行：

```powershell
.\build.ps1
```

脚本编译 x64 WinForms 主程序、纯本地协议测试程序和只读 WMI 检查程序。主程序输出到 `dist/OmenUndervolt.exe`；如果同名程序正在运行，则输出 `dist/OmenUndervolt.next.exe`，避免覆盖运行中的文件。生成的检查程序位于 `tests/artifacts/`。

纯本地协议／界面检查可运行：

```powershell
.\tests\artifacts\ProtocolTests.exe
```

此程序覆盖偏移编码解码、范围、设备匹配、能力码和部分 WinForms 控件行为，不调用 WMI，也不写入固件。另有 `WmiReadOnlyCheck.exe` 用于只读检查；它会访问当前 Windows 上的 HP WMI 接口，不执行设置写入。

输出与中间文件都位于项目自己的 `dist/` 路径，构建不需要 NuGet 包或 .NET SDK。

## 维护时的核对点

- 若变更偏移数据格式或范围，同步检查 `SafetyProtocol` 与 `tests/ProtocolTests.cs` 中的断言。
- 若变更 HP WMI 命令字段，核对 `HpBiosWmi` 的参数和响应处理。
- 若变更设备支持范围，先确认目标 BIOS 与接口行为，再更新 `IsSupportedTarget` 和对应说明。
