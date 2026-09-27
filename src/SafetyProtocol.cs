using System;

namespace OmenUndervolt
{
    internal sealed class DeviceInfo
    {
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Board { get; set; }
        public string Processor { get; set; }
        public string BiosVersion { get; set; }
    }

    internal static class SafetyProtocol
    {
        internal const int MaximumMagnitudeMv = 200;

        /// <summary>
        /// 只按厂商、产品线和处理器做粗筛：命令属于 HP 的 BIOS WMI 接口，只有 HP OMEN 的 Intel 机型可能响应。
        /// 机型、主板和 BIOS 版本不再参与判断，是否真的支持由 <see cref="IsUndervoltingAvailable"/> 的能力查询决定。
        /// </summary>
        internal static bool IsSupportedTarget(DeviceInfo device)
        {
            if (device == null)
                return false;

            string manufacturer = device.Manufacturer == null ? string.Empty : device.Manufacturer.Trim();
            return (manufacturer.StartsWith("HP", StringComparison.OrdinalIgnoreCase)
                    || manufacturer.StartsWith("Hewlett-Packard", StringComparison.OrdinalIgnoreCase))
                && device.Model != null
                && device.Model.IndexOf("OMEN", StringComparison.OrdinalIgnoreCase) >= 0
                && device.Processor != null
                && device.Processor.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsUndervoltingAvailable(byte[] capabilityData)
        {
            return capabilityData != null && capabilityData.Length >= 3
                && (capabilityData[2] == 1 || capabilityData[2] == 2);
        }

        internal static int GetSliderMagnitude(int offsetMv)
        {
            if (offsetMv >= 0)
                return 0;
            return Math.Min(MaximumMagnitudeMv, -offsetMv);
        }

        internal static byte[] EncodeOffset(int magnitudeMv)
        {
            if (magnitudeMv < 0 || magnitudeMv > MaximumMagnitudeMv)
                throw new ArgumentOutOfRangeException("magnitudeMv", "允许范围为 0 到 200 mV。");

            byte[] data = new byte[128];
            data[0] = 0;
            data[1] = 3;
            data[2] = magnitudeMv == 0 ? (byte)1 : (byte)0;
            data[3] = (byte)(magnitudeMv >> 8);
            data[4] = (byte)(magnitudeMv & 0xff);
            return data;
        }

        internal static int DecodeOffset(byte[] data)
        {
            if (data == null || data.Length < 9)
                throw new InvalidOperationException("HP BIOS 返回的数据长度不足。");
            if (data[0] != 0 || data[1] != 3)
                throw new InvalidOperationException("HP BIOS 返回了非处理器电压偏移数据。");

            int magnitude = (data[3] << 8) | data[4];
            return data[2] == 0 ? -magnitude : magnitude;
        }

    }
}
