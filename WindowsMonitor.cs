using System.Runtime.InteropServices;
using System.Text.Json;
using LibreHardwareMonitor.Hardware;

using Microsoft.VisualStudio.TestTools.UnitTesting;


public static class HardwareInfo
{
    private static readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsNetworkEnabled = true,
        IsStorageEnabled = true
    };
    private static readonly UpdateVisitor _visitor = new();

    static HardwareInfo()
    {
        _computer.Open();
        _computer.Accept(_visitor);
    }

    // FIXME: remember to remove the comment on the following line when compiling natively
    // [UnmanagedCallersOnly(EntryPoint = "GetHardwareInfo")]
    public static IntPtr GetHardwareInfo(IntPtr hardwareNamesPtr)
    {
        // Convert the IntPtr to an array of strings
        string[] hardwareNames = Marshal
            .PtrToStringAnsi(hardwareNamesPtr)
            .Split(',', StringSplitOptions.RemoveEmptyEntries);

        var result = new Dictionary<string, HardwareInfoObject>();


        foreach (IHardware hardware in _computer.Hardware)
        {
            string hardwareType = hardware.HardwareType.ToString();
            if (hardwareNames.Contains(hardwareType))
            {
                HardwareInfoObject hardwareInfoObject =
                    new()
                    {
                        Name = hardwareType.Contains("Gpu") ? "Gpu" : hardwareType,
                        HardwareType = hardware.HardwareType
                    };

                foreach (ISensor sensor in hardware.Sensors)
                {
                    float SensorValue = sensor.Value != null ? (float)sensor.Value : 0;
                    SensorInfoObject sensorInfoObject =
                        new() { Name = sensor.Name, Value = SensorValue, Type = hardwareType };

                    hardwareInfoObject.Sensors.Add(sensorInfoObject);
                }

                result.Add(hardware.Name, hardwareInfoObject);
            }
        }


        // Convert the result to a stringified JSON using System.Text.Json
        string jsonResult = System.Text.Json.JsonSerializer.Serialize(result,new JsonSerializerOptions { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals });
        IntPtr ptr = Marshal.StringToHGlobalAnsi(jsonResult);

        return ptr;
    }

    public class HardwareInfoObject
    {
        public string Name { get; set; }
        public HardwareType HardwareType { get; set; }
        public List<SensorInfoObject> Sensors { get; set; }

        public HardwareInfoObject()
        {
            Sensors = new List<SensorInfoObject>();
        }
    }

    public class SensorInfoObject
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public float Value { get; set; }

        public SensorInfoObject() { }
    }
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}

[TestClass]
public class HardwareInfoTest
{
    [TestMethod]
    public void TestGetHardwareInfo()
    {
        // Arrange
        string[] hardwareNames = new string[]
        {
            "Cpu",
            "NvidiaGpu",
            "Motherboard",
            "Memory",
            "Psu"
         };

        IntPtr hardwareNamesPtr = Marshal.StringToHGlobalAnsi(string.Join(",", hardwareNames));

        // Act
        IntPtr resultPtr = HardwareInfo.GetHardwareInfo(hardwareNamesPtr);

        // Convert the result back to a string
        string jsonResult = Marshal.PtrToStringAnsi(resultPtr);

        // Log the result
        Console.WriteLine(jsonResult);

        // Convert the JSON string to a Dictionary
        var result = System.Text.Json.JsonSerializer.Deserialize<
            Dictionary<string, HardwareInfo.HardwareInfoObject>
        >(jsonResult);

        // Assert
        Assert.AreEqual(hardwareNames.Length, result.Count);
        foreach (string hardwareName in hardwareNames)
        {
            Assert.IsTrue(result.ContainsKey(hardwareName));
        }

        // Clean up
        Marshal.FreeHGlobal(hardwareNamesPtr);
        Marshal.FreeHGlobal(resultPtr);
    }
}
