using System;
using System.IO;
using Newtonsoft.Json;

namespace FanControl.Liquidctl
{
    public enum LogLevel
    {
        Error = 0,
        Info = 1,
        Debug = 2,
        Trace = 3
    }

    public class LiquidctlConfig
    {
        private static readonly string ConfigPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty, "config.json");
        private static LiquidctlConfig? _instance;
        private static readonly object _lock = new object();

        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        public static LiquidctlConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }

        public static LiquidctlConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonConvert.DeserializeObject<LiquidctlConfig>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to defaults if we can't load the config
                // Since we don't have a logger initialized yet, we can't log here easily
            }

            return new LiquidctlConfig();
        }
    }
}
