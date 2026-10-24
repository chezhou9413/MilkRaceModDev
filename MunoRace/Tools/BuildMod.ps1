param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')

#同步 1.7 内容并编译，不启动 Unity 或游戏。
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& (Join-Path $PSScriptRoot 'Prepare-1.7.ps1')
if (!(Test-Path -LiteralPath (Join-Path $modRoot '1.7\Data\RimUnity\catalog.json'))) {
    throw '缺少标准资源，请先运行 Tools\Build\Export-RimUnity.ps1。'
}
& (Join-Path $PSScriptRoot 'Build\Build-RadiationShader.ps1')
dotnet build (Join-Path $modRoot '1.7\Source\MunoRaceLib\MunoRaceLib.csproj') -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw '谬诺 1.7 编译失败。' }
