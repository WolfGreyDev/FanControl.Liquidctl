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
        public static string liquidctlexe = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "liquidctl.exe");

        private static Dictionary<string, Process> liquidctlBackends = new Dictionary<string, Process>();
        private static bool hasLastCallFailed = false;

        internal static IPluginLogger logger;

        internal static void Initialize() {
            logger = pluginLogger;
            LiquidctlCall($"--json initialize all");
        }
        internal static List<LiquidctlStatusJSON> ReadStatus() {
            Process process = LiquidctlCall($"--json status");
            // return JsonConvert.DeserializeObject<List<LiquidctlStatusJSON>>(process.StandardOutput.ReadToEnd());
            return ParseStatuses(process.StandardOutput.ReadToEnd());
        }
        internal static List<LiquidctlStatusJSON> ReadStatus(string address) {
            Process process = GetLiquidCtlBackend(address);
            process.StandardInput.WriteLine("status");
            string line = process.StandardOutput.ReadLine();
            // restart if liquidctl crashed
            if (line == null) {
                Initialize();
                process = RestartLiquidCtlBackend(process, address);
                process.StandardInput.WriteLine("status");
                line = process.StandardOutput.ReadLine();
                if (line == null) {
                    throw new Exception($"liquidctl returns empty line. Remaining stdout:\n{process.StandardOutput.ReadToEnd()} Last stderr output:\n{process.StandardError.ReadToEnd()}");
                }
            }
            JObject result = JObject.Parse(line);
            string status = (string)result.SelectToken("status");
            hasLastCallFailed = false;
            if (status == "success")
                return result.SelectToken("data").ToObject<List<LiquidctlStatusJSON>>();
            throw new Exception((string)result.SelectToken("data"));
        }
        internal static void SetPump(string address, int value) {
            Process process = GetLiquidCtlBackend(address);
            process.StandardInput.WriteLine($"set pump speed {(value)}");
            JObject result = JObject.Parse(process.StandardOutput.ReadLine());
            string status = (string)result.SelectToken("status");
            if (status == "success")
                return;
            throw new Exception((string)result.SelectToken("data"));
        }

        internal static void SetFanNumber(string address, int index, int value) {
            LiquidctlCall($"--address {address} set fan{index} speed {(value)}");
        }

        private static Process RestartLiquidCtlBackend(Process oldProcess, string address) {
            liquidctlBackends.Remove(address);
            try {
                oldProcess.StandardInput.WriteLine("exit");
                oldProcess.WaitForExit(200);
            } catch (Exception) {
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

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0) {
                // try to initialize again
                if (process.ExitCode == 1 && !hasLastCallFailed) {
                    hasLastCallFailed = true;
                    Initialize();
                    return LiquidctlCall(arguments);
                }
                throw new Exception($"liquidctl returned non-zero exit code {process.ExitCode}. Last stderr output:\n{process.StandardError.ReadToEnd()}");
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
                    LiquidctlStatusJSON status = statusObject.ToObject<LiquidctlStatusJSON>();
                    statuses.Add(status);
                }
                catch (Exception e) {
                    logger.Log($"Unable to parse {statusObject}\n{e.Message}");
                }
            }

            return statuses;
        }
    }
}
