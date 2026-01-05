$compress = @{
  Path = ".\bin\Release\net8.0-windows\FanControl.Liquidctl.dll", ".\liquidctl.exe", ".\liquidctl-license.txt", ".\config.json"
  DestinationPath = ".\FanControl.Liquidctl.zip"
}
Compress-Archive @compress -Force