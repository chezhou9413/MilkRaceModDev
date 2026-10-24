#同步共用内容；1.7 通过 CL 资源提供器加载 RimUnity 标准清单。
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$targetRoot = Join-Path $modRoot '1.7'
foreach ($folder in @('Defs','Textures','Sounds','Languages','Patches')) {
    $target = Join-Path $targetRoot $folder
    foreach ($source in @((Join-Path $modRoot $folder), (Join-Path $modRoot "1.6\$folder"))) {
        if (!(Test-Path -LiteralPath $source)) { continue }
        [IO.Directory]::CreateDirectory($target) | Out-Null
        #辐射的网格材质与播放定义由各版本独立维护。
        robocopy $source $target /E /XF MunoRace_TriangleRadiation.xml Muno_RadiationEffects.xml Muno_LaserCannonRadiation.xml /R:0 /W:0 /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -gt 7) { throw "同步运行资源失败：$source" }
    }
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $targetRoot 'Defs') -Recurse -Filter '*.xml') {
    $text = [IO.File]::ReadAllText($file.FullName)
    $text = [regex]::Replace($text, '(?s)\s*<ChezhouLib.LibDef.UnityShaderLord>.*?</ChezhouLib.LibDef.UnityShaderLord>', '')
    [IO.File]::WriteAllText($file.FullName, $text, [Text.UTF8Encoding]::new($false))
}
$provider = @'
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <ChezhouLib.LibDef.ClNativeResourceProvider Class="MunoRaceLib.Core.Assets.MunoNativeResourceProvider">
    <defName>MunoRace_RimUnityResources</defName>
  </ChezhouLib.LibDef.ClNativeResourceProvider>
</Defs>
'@
[IO.File]::WriteAllText((Join-Path $targetRoot 'Defs\ShaderDefs\MunoRace_RimUnity.xml'), $provider, [Text.UTF8Encoding]::new($false))
Write-Output '谬诺 1.7 内容已同步。'
