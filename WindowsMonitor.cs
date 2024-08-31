using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibreHardwareMonitor.Hardware;


[JsonSerializable(typeof(Dictionary<string, HardwareInfo.HardwareInfoObject>))]
[JsonSerializable(typeof(HardwareInfo.HardwareInfoObject))]
[JsonSerializable(typeof(HardwareInfo.SensorInfoObject))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
)]
public partial class HardwareInfoJsonContext : JsonSerializerContext { }


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


    [UnmanagedCallersOnly(EntryPoint = "GetHardwareInfo")]
    public static IntPtr GetHardwareInfo(IntPtr hardwareNamesPtr) { return GetHardwareInfoInternal(hardwareNamesPtr); }
    public static IntPtr GetHardwareInfoInternal(IntPtr hardwareNamesPtr)
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
                        new() { Name = sensor.Name, Value = SensorValue };

                    hardwareInfoObject.Sensors.Add(sensorInfoObject);
                }

                result.Add(hardware.Name, hardwareInfoObject);
            }
        }


        string jsonResult = JsonSerializer.Serialize(result, HardwareInfoJsonContext.Default.DictionaryStringHardwareInfoObject);


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
