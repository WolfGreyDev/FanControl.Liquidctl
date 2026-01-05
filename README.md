# FanControl.Liquidctl

This is a fork of [jmarucha](https://github.com/jmarucha/FanControl.Liquidctl)'s original. It stops liquidctl from starting and stopping all the time by using a custom made [fork](https://github.com/SuspiciousActivity/liquidctl) that has an interactive mode. Works for my machine (Kraken X63) but not tested for anything else at all, **use at your own risk**. This applies to the liquidctl repo as well.

This is a simple plugin that uses [liquidctl](https://github.com/liquidctl/liquidctl) to provide sensor data and pump control to variety of AIOs. So far it is tested with NZXT Kraken X63, but in principle shall work with [supported devices](https://github.com/liquidctl/liquidctl#supported-devices)

## Installation

Grab a release and unpack it to `Plugins` directory of your FanControl installation. It contains the required `liquidctl.exe` backend.

## Performance & Monitoring

- **Interactive Backend**: Unlike the original plugin, this version keeps a persistent `liquidctl` process open, eliminating the overhead of starting/stopping the backend every second.
- **Sensor Filtering**: Devices with no supported sensors are automatically ignored to save system resources.
- **Direct Duty Cycle**: Reads reported duty cycle directly from supported hardware (e.g., Kraken X2/X3, Smart Device V2) for 1:1 control mapping.

## Configuration

You can configure the plugin via the `config.json` file in the plugin folder.

### Logging Levels

| Level | Description |
| :--- | :--- |
| **Error** | Critical failures (e.g., backend crash, parse errors). **Always enabled.** |
| **Info** | (Default) Major events: Plugin startup, device discovery, and re-initializations. |
| **Debug** | Command tracking: Logs when a "set speed" command is actually sent to the hardware. |
| **Trace** | Full verbosity: Logs every single sensor update (~1 per second). **Very noisy.** |

```json
{
  "_comment": "LogLevel options: Error, Info, Debug, Trace",
  "LogLevel": "Info"
}
```

## Setting up the developer environment

The project uses modern C# SDK-style project files.

- **Dependencies**: References `FanControl.Plugins.dll` from a configurable `FanControlPath`.
- **Custom FanControl Path**: You can set your FanControl installation path in the `.csproj` file:
  ```xml
  <FanControlPath>C:\Program Files\FanControl</FanControlPath>
  ```
- **Backend Build**: Requires Python 3.9+. Use the provided `full-build.ps1` (PowerShell) to build the backend and plugin simultaneously. Additional python packages are automatically imported via `pip` with our `full-build.ps1` script.

## Screenshots

![Fluid temperature sensor](/docs/images/FluidTemp.png)
![Pump speed and control](/docs/images/PumpControl.png)

## License
MIT license, because it's superior.
```
Copyright (c) 2022 Jan K. Marucha

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```

liquidctl, which is used by this plugin is provided on [GPLv3](https://github.com/liquidctl/liquidctl/blob/main/LICENSE.txt).