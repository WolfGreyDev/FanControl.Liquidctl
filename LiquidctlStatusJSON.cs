using System.Collections.Generic;
using System.Globalization;

namespace FanControl.Liquidctl
{
    public class LiquidctlStatusJSON
    {
        public class StatusRecord
        {
            public string key { get; set; } = null!;
            public string? description { get; set; }
            public object value { get; set; } = null!;
            public string unit { get; set; } = null!;

            public float? GetValueAsFloat()
            {
                if (value is float f) return f;
                if (value is double d) return (float)d;
                if (value is int i) return (float)i;
                if (value is long l) return (float)l;
                if (value is string s && float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float result)) return result;
                return null;
            }
        }
        public string bus { get; set; } = null!;
        public string address { get; set; } = null!;
        public string port { get; set; } = null!;
        public string? version { get; set; }
        public string description { get; set; } = null!;

        public List<StatusRecord> status { get; set; } = null!;

        public string GetAddress()
        {
            if (bus.StartsWith("usb"))
                return $"usb#{port}";
            return address;
        }

        public static KeyValuePair<string, string> GetBusAndAddress(string address)
        {
            if (address.StartsWith("usb#"))
                return new KeyValuePair<string, string>("usb", address.Split('#')[1]);
            return new KeyValuePair<string, string>("hid", address);
        }
    }
}
