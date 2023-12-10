using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

using Microsoft.VisualStudio.TestTools.UnitTesting;

public static class HardwareInfo
{
    private static Computer _computer = new Computer();

    public static void OpenAndClose(Action action)
    {
        _computer.Open();
        try
        {
            action();
        }
        finally
        {
            _computer.Close();
        }
    }

    // Modify the method signature in HardwareInfo.cs
    [UnmanagedCallersOnly(EntryPoint = "GetHardwareInfo")]
    public static IntPtr GetHardwareInfo(IntPtr hardwareNamesPtr)
    {
        // Convert the IntPtr to an array of strings
        string[] hardwareNames = Marshal
            .PtrToStringAnsi(hardwareNamesPtr)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);

        var result = new Dictionary<string, HardwareInfoObject>();

        OpenAndClose(() =>
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                if (hardwareNames.Contains(hardware.Name))
                {
                    HardwareInfoObject hardwareInfoObject = new HardwareInfoObject();
                    hardwareInfoObject.Name = hardware.Name.Contains("Gpu") ? "Gpu" : hardware.Name;
                    hardwareInfoObject.HardwareType = hardware.HardwareType;

                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        SensorInfoObject sensorInfoObject = new SensorInfoObject();
                        sensorInfoObject.Name = sensor.Name;
                        sensorInfoObject.Value = (float)sensor.Value;

                        hardwareInfoObject.Sensors.Add(sensorInfoObject);
                    }

                    result.Add(hardware.Name, hardwareInfoObject);
                }
            }
        });

        // Convert the result to a stringified JSON using System.Text.Json
        string jsonResult = System.Text.Json.JsonSerializer.Serialize(result);
        IntPtr ptr = Marshal.StringToHGlobalAnsi(jsonResult);

        return ptr;
    }

    public class HardwareInfoObject
    {
        public string Name { get; set; }
        public LibreHardwareMonitor.Hardware.HardwareType HardwareType { get; set; }
        public List<SensorInfoObject> Sensors { get; set; }

        public HardwareInfoObject()
        {
            Sensors = new List<SensorInfoObject>();
        }
    }

    public class SensorInfoObject
    {
        public string Name { get; set; }
        public float Value { get; set; }

        public SensorInfoObject() { }
    }
}
