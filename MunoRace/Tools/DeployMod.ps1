param([string]$DeployToolRoot = 'E:\aow\ModPack\xmlremove\bin\Release\net8.0-windows')

#部署已编译的运行目录，不启动游戏。
$ErrorActionPreference = 'Stop'
$source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (Get-Process -Name RimWorld,RimWorldWin64 -ErrorAction SilentlyContinue) { throw '请先退出 RimWorld 再部署谬诺。' }
foreach ($relative in @('1.7\Assemblies\MunoRaceLib.dll','1.7\Data\RimUnity\catalog.json','1.7\Defs\ShaderDefs\MunoRace_RimUnity.xml')) {
    if (!(Test-Path -LiteralPath (Join-Path $source $relative))) { throw "部署内容缺少 $relative" }
}
& (Join-Path $DeployToolRoot 'xmlremove.exe') deploy --source $source --target-name MunoRace
if ($LASTEXITCODE -ne 0) { throw "谬诺部署失败：$LASTEXITCODE" }
