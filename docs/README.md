# OMEN 降压工具 · Code Wiki

本 Wiki 根据当前源码整理，帮助理解代码入口、BIOS 通信路径、安全限制和构建方式。

## 范围

本目录对应 `undervolt-tool`：一个 .NET Framework 4.8 的 WinForms 应用，通过 HP BIOS WMI 接口读取和写入处理器电压偏移。

## 页面

- [系统架构与运行流程](architecture.md)：代码职责、界面状态和 WMI 调用顺序。
- [安全边界与设备校验](safety.md)：设备粗筛、能力探测、偏移编码和写入限制。
- [构建与本地检查](build-and-checks.md)：构建入口、测试范围及输出目录。

## 快速定位

| 关注点 | 文件 |
|---|---|
| 程序入口、WinForms 界面和重启确认 | [src/Program.cs](../src/Program.cs) |
| HP BIOS WMI 通信封装 | [src/HpBiosWmi.cs](../src/HpBiosWmi.cs) |
| 目标机校验、能力码、偏移数据格式 | [src/SafetyProtocol.cs](../src/SafetyProtocol.cs) |
| 构建脚本 | [build.ps1](../build.ps1) |
| 纯本地协议与界面断言 | [tests/ProtocolTests.cs](../tests/ProtocolTests.cs) |
| 只读 WMI 检查 | [tests/WmiReadOnlyCheck.cs](../tests/WmiReadOnlyCheck.cs) |

## 重要限制

工具面向 HP OMEN 的 Intel 机型，是否支持降压由 HP BIOS 的能力查询决定；能力查询未报告支持、当前偏移读取失败或接口不可用时会禁用写入并说明原因。运行需要管理员权限及 HP BIOS WMI/ACPI 驱动。WMI 写入路径尚未实机验证；固件返回成功或接受请求都不能证明重启后设置已生效，也不代表运行稳定。

