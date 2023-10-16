using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

public class HardwareInfo
{
    private static Computer _computer;

    public HardwareInfo()
    {
        _computer = new Computer(); // Initialize the `_computer` field.
    }

    public void Open()
    {
        _computer.Open();
    }

    public void Close()
    {
        _computer.Close();
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

    public static Dictionary<string, HardwareInfoObject> GetHardwareInfo(string[] hardwareNames)
    {
        Dictionary<string, HardwareInfoObject> hardwareInfoObjects =
            new Dictionary<string, HardwareInfoObject>();

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

                hardwareInfoObjects.Add(hardware.Name, hardwareInfoObject);
            }
        }

        return hardwareInfoObjects;
    }
}
