using System;
using OmenUndervolt;

internal static class WmiReadOnlyCheck
{
    private static int Main()
    {
        string outputPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wmi-readonly-output.txt");
        try
        {
            byte[] capability = HpBiosWmi.ReadCapability();
            int offset = HpBiosWmi.ReadProcessorOffsetMv();
            System.IO.File.WriteAllText(outputPath,
                "Capability: " + BitConverter.ToString(capability, 0, 4) + Environment.NewLine
                + "Undervolting supported: " + SafetyProtocol.IsUndervoltingAvailable(capability) + Environment.NewLine
                + "Processor offset: " + offset + " mV" + Environment.NewLine);
            return SafetyProtocol.IsUndervoltingAvailable(capability) ? 0 : 2;
        }
        catch (Exception exception)
        {
            System.IO.File.WriteAllText(outputPath, exception.ToString());
            return 1;
        }
    }
}
