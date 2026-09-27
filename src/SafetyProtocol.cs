using System;

namespace OmenUndervolt
{
    internal sealed class DeviceInfo
    {
        public string Model { get; set; }
        public string Board { get; set; }
        public string Processor { get; set; }
        public string BiosVersion { get; set; }
    }

    internal static class SafetyProtocol
    {
        internal const int MaximumMagnitudeMv = 200;

        /// <summary>
        /// 0x35 返回的第 3 个字节是位标志，bit 0 表示 BIOS 支持处理器降压
        /// （OmenHwCtl 与 OmenMon 的记录同为 0x01，实机观察到 0x03）。
        /// </summary>
        internal static bool IsUndervoltingAvailable(byte[] capabilityData)
        {
            return capabilityData != null && capabilityData.Length >= 3
                && (capabilityData[2] & 0x01) != 0;
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
