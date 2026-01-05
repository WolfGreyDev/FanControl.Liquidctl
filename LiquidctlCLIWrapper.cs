using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FanControl.Plugins;

namespace FanControl.Liquidctl
{
    internal static class LiquidctlCLIWrapper
    {
        public static string liquidctlexe = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty, "liquidctl.exe");

        private static Dictionary<string, Process> liquidctlBackends = new Dictionary<string, Process>();
        private static Dictionary<string, int> lastSetValues = new Dictionary<string, int>();
        private static bool hasLastCallFailed = false;
        private static readonly object _lock = new object();

        internal static IPluginLogger? logger;
        internal static void Log(string message, LogLevel level = LogLevel.Info) {
            if (level <= LiquidctlConfig.Instance.LogLevel) {
                logger?.Log($"[Liquidctl] {message}");
            }
        }

        internal static void Initialize(IPluginLogger? pluginLogger = null) {
            logger = pluginLogger;
            lastSetValues.Clear();
            Log("Initializing all liquidctl devices...");
            LiquidctlCall($"--json initialize all");
        }
        internal static List<LiquidctlStatusJSON> ReadStatus() {
            Process process = LiquidctlCall($"--json status");
            // return JsonConvert.DeserializeObject<List<LiquidctlStatusJSON>>(process.StandardOutput.ReadToEnd());
            return ParseStatuses(process.StandardOutput.ReadToEnd());
        }
        internal static List<LiquidctlStatusJSON> ReadStatus(string address) {
            Process process = GetLiquidCtlBackend(address);
            string? line;
            lock (_lock)
            {
                process.StandardInput.WriteLine("status");
                line = process.StandardOutput.ReadLine();
            }
            // restart if liquidctl crashed
            if (line == null) {
                Initialize(logger);
                process = RestartLiquidCtlBackend(process, address);
                lock (_lock)
                {
                    process.StandardInput.WriteLine("status");
                    line = process.StandardOutput.ReadLine();
                }
                if (line == null) {
                    throw new Exception($"liquidctl returns empty line. Remaining stdout:\n{process.StandardOutput.ReadToEnd()} Last stderr output:\n{process.StandardError.ReadToEnd()}");
                }
            }
            JObject result = JObject.Parse(line);
            string? status = (string?)result.SelectToken("status");
            hasLastCallFailed = false;
            if (status == "success")
                return result.SelectToken("data")?.ToObject<List<LiquidctlStatusJSON>>() ?? new List<LiquidctlStatusJSON>();
            throw new Exception((string?)result.SelectToken("data") ?? "Unknown error");
        }
        public static void SetPump(string address, int value) {
            string key = $"{address}-pump";
            if (lastSetValues.TryGetValue(key, out int lastValue) && lastValue == value)
                return;

            Process process = GetLiquidCtlBackend(address);
            string? line;
            lock (_lock)
            {
                Log($"Setting pump speed: {value}", LogLevel.Debug);
                process.StandardInput.WriteLine($"set pump speed {(value)}");
                line = process.StandardOutput.ReadLine();
            }
            if (line == null) throw new Exception("liquidctl returned empty line on set pump");
            JObject result = JObject.Parse(line);
            string? status = (string?)result.SelectToken("status");
            if (status == "success")
            {
                lastSetValues[key] = value;
                return;
            }
            throw new Exception((string?)result.SelectToken("data") ?? "Unknown error");
        }

        internal static void SetFanNumber(string address, int index, int value) {
            string key = $"{address}-fan{index}";
            if (lastSetValues.TryGetValue(key, out int lastValue) && lastValue == value)
                return;

            Process process = GetLiquidCtlBackend(address);
            string? line;
            lock (_lock)
            {
                Log($"Setting fan{index} speed: {value}", LogLevel.Debug);
                process.StandardInput.WriteLine($"set fan{index} speed {(value)}");
                line = process.StandardOutput.ReadLine();
            }
            if (line == null) throw new Exception($"liquidctl returned empty line on set fan{index}");
            JObject result = JObject.Parse(line);
            string? status = (string?)result.SelectToken("status");
            if (status == "success")
            {
                lastSetValues[key] = value;
                return;
            }
            throw new Exception((string?)result.SelectToken("data") ?? "Unknown error");
        }
        private static Process RestartLiquidCtlBackend(Process oldProcess, string address)
        {
            Log($"Restarting liquidctl backend for {address}");
            liquidctlBackends.Remove(address);
            try
            {
                oldProcess.StandardInput.WriteLine("exit");
                oldProcess.WaitForExit(200);
            }
            catch (Exception)
            {
                if (!oldProcess.HasExited)
                    oldProcess.Kill();
            }
            return GetLiquidCtlBackend(address);
        }

        private static Process GetLiquidCtlBackend(string address) {
            Process process = liquidctlBackends.ContainsKey(address) ? liquidctlBackends[address] : null;
            if (process != null && !process.HasExited) {
                return process;
            }

            if (process != null) {
                liquidctlBackends.Remove(address);
            }

            KeyValuePair<string, string> identifier = LiquidctlStatusJSON.GetBusAndAddress(address);

            process = new Process();

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;

            process.StartInfo.FileName = liquidctlexe;
            switch (identifier.Key) {
                case "usb":
                    process.StartInfo.Arguments = $"--json --usb-port {identifier.Value} interactive";
                    break;
                case "hid":
                    process.StartInfo.Arguments = $"--json --address {address} interactive";
                    break;
            }

            liquidctlBackends.Add(address, process);

            Log($"Starting liquidctl interactive process: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            process.Start();

            return process;
        }

        private static Process LiquidctlCall(string arguments) {
            Process process = new Process();

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.StartInfo.FileName = liquidctlexe;
            process.StartInfo.Arguments = arguments;

            // Log($"Executing liquidctl call: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0) {
                // try to initialize again
                if (process.ExitCode == 1 && !hasLastCallFailed) {
                    hasLastCallFailed = true;
                    Initialize(logger);
                    return LiquidctlCall(arguments);
                }
                string stderr = process.StandardError.ReadToEnd();
                Log($"liquidctl returned non-zero exit code {process.ExitCode}. Arguments: {arguments}. Stderr:\n{stderr}", LogLevel.Error);
                throw new Exception($"liquidctl returned non-zero exit code {process.ExitCode}. Last stderr output:\n{stderr}");
            }

            hasLastCallFailed = false;

            return process;
        }

        // Code by akotulu
        // See https://github.com/jmarucha/FanControl.Liquidctl/pull/29/commits/145978bdf1c2d1a464b2a036b4fc26f559bb77dc#diff-d7a2c0cf4c270870ed263c55d2cd4fc41258347085a3cded3a78b48e73f78092

        private static List<LiquidctlStatusJSON> ParseStatuses(string json) {
            JArray statusArray = JArray.Parse(json);
            List<LiquidctlStatusJSON> statuses = new List<LiquidctlStatusJSON>();


            foreach (JObject statusObject in statusArray) {
                try {
                    LiquidctlStatusJSON? status = statusObject.ToObject<LiquidctlStatusJSON>();
                    if (status != null)
                        statuses.Add(status);
                }
                catch (Exception e) {
                    Log($"Unable to parse {statusObject}\n{e.Message}", LogLevel.Error);
                }
            }

            return statuses;
        }
    }
}
