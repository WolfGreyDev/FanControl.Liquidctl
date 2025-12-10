using System;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.Liquidctl
{
    internal class LiquidctlDevice
    {
        public class LiquidTemperature : IPluginSensor
        {
            public LiquidTemperature(LiquidctlStatusJSON output)
            {
                _id = $"{output.GetAddress().ToLower()}-liqtmp";
                _name = $"Liquid Temp. - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = (float)output.status.Single(entry => entry.key == "Liquid temperature").GetValueAsFloat();
            }
            public string Id => _id;
            string _id;

            public string Name => _name;
            string _name;

            public float? Value => _value;
            float _value;

            public void Update()
            { } // plugin updates sensors
        }
        public class PumpSpeed : IPluginSensor
        {
            public PumpSpeed(LiquidctlStatusJSON output)
            {
                _id = $"{output.GetAddress().ToLower()}-pumprpm";
                _name = $"Pump - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = (float)output.status.Single(entry => entry.key == "Pump speed").GetValueAsFloat();
            }
            public string Id => _id;
            readonly string _id;

            public string Name => _name;
            readonly string _name;

            public float? Value => _value;
            float _value;

            public void Update()
            { } // plugin updates sensors
        }
        public class PumpDuty : IPluginControlSensor
        {
            public PumpDuty(LiquidctlStatusJSON output)
            {
                _address = output.GetAddress();
                _id = $"{_address.ToLower()}-pumpduty";
                _name = $"Pump Control - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = (float)output.status.Single(entry => entry.key == "Pump duty").GetValueAsFloat();
            }
            public string Id => _id;
            string _id;
            string _address;

            public string Name => _name;
            string _name;

            public float? Value => _value;
            float _value;

            public void Reset()
            {
                Set(100.0f);
            }

            public void Set(float val)
            {
                if (Value == val) return;
                LiquidctlCLIWrapper.SetPump(_address, (int)val);
            }

            public void Update()
            { } // plugin updates sensors

        }

        // Try to get the speeds for multiple fans
        public class FanSpeedMultiple : IPluginSensor
        {

            public FanSpeedMultiple(int index, LiquidctlStatusJSON output)
            {
                _id = $"{output.address}-fan{index}rpm";
                _name = $"Fan {index} - {output.description}";

                UpdateFromJSON(index, output);
            }

            public void UpdateFromJSON(int index, LiquidctlStatusJSON output)
            {
                string currentKey = KEY.Replace("###", index.ToString());
                _value = (float)output.status.Single(entry => entry.key == currentKey).value;
            }

            public static string KEY = "Fan ### speed";

            public string Id => _id;
            readonly string _id;

            public string Name => _name;
            readonly string _name;

            public float? Value => _value;
            float _value;

            public void Update() { } // plugin updates sensors

        }

        // Try to control multiple fans
        public class FanControlMultiple : IPluginControlSensor
        {

            public FanControlMultiple(int index, LiquidctlStatusJSON output)

            {
                _address = output.address;
                _id = $"{output.address}-fan{index}ctrl";
                _name = $"Fan {index} Control - {output.description}";
                _index = index;

                UpdateFromJSON(index, output);

            }

            // We can only estimate, as it is not provided in any output
            public void UpdateFromJSON(int index, LiquidctlStatusJSON output)
            {
                string currentKey = FanSpeedMultiple.KEY.Replace("###", index.ToString());
                float reading = (float)output.status.Single(entry => entry.key == currentKey).value;
                //_value = reading > MAX_RPM ? 100.0f : (float)Math.Ceiling(100.0f * reading / MAX_RPM);
                _value = RPM_LOOKUP.OrderBy(e => Math.Abs(e.Key - reading)).FirstOrDefault().Value;
            }

            public static string KEY = "Fan ### speed";
            //public static string KEY = $"Fan {_index} speed";

            static readonly Dictionary<int, int> RPM_LOOKUP = new Dictionary<int, int>

            { // We can only estimate, as it is not provided in any output. Hence I applied this ugly hack
                {520, 20}, {521, 21}, {522, 22}, {523, 23}, {524, 24}, {525, 25}, {526, 26}, {527, 27}, {528, 28}, {529, 29},
                {530, 30}, {532, 31}, {534, 32}, {536, 33}, {538, 34}, {540, 35}, {542, 36}, {544, 37}, {546, 38}, {548, 39},
                {550, 40}, {571, 41}, {592, 42}, {613, 43}, {634, 44}, {655, 45}, {676, 46}, {697, 47}, {718, 48}, {739, 49},
                {760, 50}, {781, 51}, {802, 52}, {823, 53}, {844, 54}, {865, 55}, {886, 56}, {907, 57}, {928, 58}, {949, 59},
                {970, 60}, {989, 61}, {1008, 62}, {1027, 63}, {1046, 64}, {1065, 65}, {1084, 66}, {1103, 67}, {1122, 68}, {1141, 69},
                {1160, 70}, {1180, 71}, {1200, 72}, {1220, 73}, {1240, 74}, {1260, 75}, {1280, 76}, {1300, 77}, {1320, 78}, {1340, 79},
                {1360, 80}, {1377, 81}, {1394, 82}, {1411, 83}, {1428, 84}, {1445, 85}, {1462, 86}, {1479, 87}, {1496, 88}, {1513, 89},
                {1530, 90}, {1550, 91}, {1570, 92}, {1590, 93}, {1610, 94}, {1630, 95}, {1650, 96}, {1670, 97}, {1690, 98}, {1720, 99},
                {MAX_RPM, 100}
            };

            static readonly int MAX_RPM = 1980;

            public string Id => _id;
            readonly string _id;
            string _address;

            public string Name => _name;
            readonly string _name;

            public float? Value => _value;
            float _value;

            public int Index => _index;
            int _index;

            public void Reset()
            {
                Set(50.0f);
            }

            public void Set(float val)
            {
                LiquidctlCLIWrapper.SetFanNumber(_address, _index, (int)val);
            }

            public void Update() { } // plugin updates sensors

        }

        public LiquidctlDevice(LiquidctlStatusJSON output)
        {
            address = output.GetAddress();

            hasPumpSpeed = output.status.Exists(entry => entry.key == "Pump speed" && !(entry.GetValueAsFloat() is null));
            if (hasPumpSpeed)
                pumpSpeed = new PumpSpeed(output);

            hasPumpDuty = output.status.Exists(entry => entry.key == "Pump duty" && !(entry.GetValueAsFloat() is null));
            if (hasPumpDuty)
                pumpDuty = new PumpDuty(output);

            hasLiquidTemperature = output.status.Exists(entry => entry.key == "Liquid temperature" && !(entry.GetValueAsFloat() is null));
            if (hasLiquidTemperature)
                liquidTemperature = new LiquidTemperature(output);

            // Get the info for multiple fans
            for (int i = 0; i < 20; i++)
            {
                int index = i + 1;
                string currentKey = FanSpeedMultiple.KEY.Replace("###", index.ToString());
                hasMultipleFanSpeed[i] = output.status.Exists(entry => entry.key == currentKey && !(entry.value is null));

                if (hasMultipleFanSpeed[i])
                {
                    fanSpeedMultiple[i] = new FanSpeedMultiple(index, output);
                    fanControlMultiple[i] = new FanControlMultiple(index, output);
                }
            }
        }

        public readonly bool hasPumpSpeed, hasPumpDuty, hasLiquidTemperature;
        public readonly bool[] hasMultipleFanSpeed = new bool[20];

        public void UpdateFromJSON(LiquidctlStatusJSON output)
        {
            if (hasLiquidTemperature) liquidTemperature.UpdateFromJSON(output);
            if (hasPumpSpeed) pumpSpeed.UpdateFromJSON(output);
            if (hasPumpDuty) pumpDuty.UpdateFromJSON(output);

            for (int i = 0; i < 20; i++)
            {
                if (hasMultipleFanSpeed[i])
                {
                    fanSpeedMultiple[i].UpdateFromJSON(i + 1, output);
                    fanControlMultiple[i].UpdateFromJSON(i + 1, output);
                }
            }
        }

        public string address;
        public LiquidTemperature liquidTemperature;
        public PumpSpeed pumpSpeed;
        public PumpDuty pumpDuty;
        public FanSpeedMultiple[] fanSpeedMultiple = new FanSpeedMultiple[20];
        public FanControlMultiple[] fanControlMultiple = new FanControlMultiple[20];

        public void LoadJSON()
        {
            try
            {
                LiquidctlStatusJSON output = LiquidctlCLIWrapper.ReadStatus(address).First();
                UpdateFromJSON(output);
            }
            catch (InvalidOperationException)
            {
                throw new Exception($"Device {address} not showing up");
            }
        }

        public String GetDeviceInfo()
        {
            String ret = $"Device @ {address}";
            if (hasLiquidTemperature) ret += $", Liquid @ {liquidTemperature.Value}";
            if (hasPumpSpeed) ret += $", Pump @ {pumpSpeed.Value}";
            if (hasPumpDuty) ret += $"({pumpDuty.Value})";
            if (hasFanSpeed) ret += $", Fan @ {fanSpeed.Value} ({fanControl.Value})";

            for (int i = 0; i < 20; i++)
            {
                if (hasMultipleFanSpeed[i])
                {
                    ret += $", Fan{i + 1} @ {fanSpeedMultiple[i].Value} ({fanControlMultiple[i].Value})";
                }
            }

            return ret;
        }
    }
}
