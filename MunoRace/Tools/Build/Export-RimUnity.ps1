param([string]$UnityEditor = 'E:\UN\2022.3.35f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$project = 'E:\mygame\MunoRace'
if (Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -like "*$project*" -or $_.CommandLine -like "*$($project.Replace('\','/'))*" }) {
    throw '源工程已打开，请关闭后执行后台导出，或在编辑器调用 ModRimUnityExport.Export。'
}
& dotnet build 'E:\RimModDev\RimUnity\Source\RimUnity.ShaderTool\RimUnity.ShaderTool.csproj' -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'ShaderTool 编译失败' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ModRimUnityExport.cs') -Destination (Join-Path $project 'Assets\Editor\ModRimUnityExport.cs') -Force
$logDirectory = 'E:\ModdevMics\Temp\MunoRace'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logFile = Join-Path $logDirectory 'rimunity-export.log'
#纹理读回需要图形设备，批处理不使用 -nographics。
$process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'ModRimUnityExport.Export', '-logFile', ('"' + $logFile + '"')) -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "RimUnity 导出失败：$logFile" }
Write-Output "导出完成：$logFile"
