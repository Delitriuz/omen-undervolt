using System;
using System.Management;

namespace OmenUndervolt
{
    internal sealed class BiosReply
    {
        internal uint ReturnCode;
        internal byte[] Data;
    }

    internal static class HpBiosWmi
    {
        private const uint Command = 0x20008;
        private const uint CapabilityType = 0x35;
        private const uint ReadType = 0x36;
        private const uint WriteType = 0x37;

        internal static byte[] ReadCapability()
        {
            BiosReply reply = Execute(CapabilityType, 4, new byte[4]);
            EnsureSuccess(reply);
            return reply.Data;
        }

        internal static int ReadProcessorOffsetMv()
        {
            byte[] request = new byte[4];
            request[1] = 3;
            BiosReply reply = Execute(ReadType, 4, request);
            EnsureSuccess(reply);
            return SafetyProtocol.DecodeOffset(reply.Data);
        }

        internal static void WriteProcessorOffsetMv(int magnitudeMv)
        {
            BiosReply reply = Execute(WriteType, 128, SafetyProtocol.EncodeOffset(magnitudeMv));
            EnsureSuccess(reply);
        }

        private static BiosReply Execute(uint commandType, uint inputSize, byte[] input)
        {
            ManagementScope scope = new ManagementScope(@"\\.\root\wmi");
            scope.Connect();

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                scope, new ObjectQuery("SELECT * FROM hpqBIntM")))
            using (ManagementObjectCollection devices = searcher.Get())
            {
                foreach (ManagementObject device in devices)
                {
                    using (device)
                    using (ManagementClass inputClass = new ManagementClass(scope, new ManagementPath("hpqBDataIn"), null))
                    using (ManagementBaseObject inputData = inputClass.CreateInstance())
                    using (ManagementBaseObject parameters = device.GetMethodParameters("hpqBIOSInt128"))
                    {
                        inputData["Command"] = Command;
                        inputData["CommandType"] = commandType;
                        inputData["Size"] = inputSize;
                        inputData["Sign"] = new byte[] { 0x53, 0x45, 0x43, 0x55 };
                        inputData["hpqBData"] = input;
                        parameters["InData"] = inputData;

                        using (ManagementBaseObject result = device.InvokeMethod("hpqBIOSInt128", parameters, null))
                        {
                            ManagementBaseObject output = result["OutData"] as ManagementBaseObject;
                            if (output == null)
                                throw new InvalidOperationException("HP BIOS 接口未返回数据。");

                            try
                            {
                                return new BiosReply
                                {
                                    ReturnCode = Convert.ToUInt32(output["rwReturnCode"]),
                                    Data = output["Data"] as byte[]
                                };
                            }
                            finally
                            {
                                output.Dispose();
                            }
                        }
                    }
                }
            }

            throw new InvalidOperationException("找不到 HP BIOS WMI 接口 hpqBIntM。");
        }

        private static void EnsureSuccess(BiosReply reply)
        {
            if (reply.ReturnCode != 0)
                throw new InvalidOperationException("HP BIOS 返回错误码 0x" + reply.ReturnCode.ToString("X") + "。");
        }
    }
}
