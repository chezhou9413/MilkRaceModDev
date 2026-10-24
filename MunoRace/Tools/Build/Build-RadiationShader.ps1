#离线更新辐射 Shader 的标准资源，保留现有清单中的其他资源。
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$dataRoot = Join-Path $modRoot '1.7\Data\RimUnity'
$sourceFile = Join-Path $modRoot 'Tools\Shaders\MunoTriangleRadiation.shader'
$buildRoot = Join-Path ([IO.Path]::GetTempPath()) 'MunoRace\RadiationShader'
$utf8 = [Text.UTF8Encoding]::new($false)
$catalogFile = Join-Path $dataRoot 'catalog.json'
$catalog = Get-Content -LiteralPath $catalogFile -Raw -Encoding UTF8 | ConvertFrom-Json
$asset = @($catalog.assets | Where-Object { $_.name -eq 'MunoRace/Effects/TriangleRadiation' })
if ($asset.Count -ne 1) { throw '资源清单必须包含唯一的缪诺辐射 Shader。' }
$asset = $asset[0]
$shader = Get-Content -LiteralPath (Join-Path $dataRoot $asset.file) -Raw -Encoding UTF8 | ConvertFrom-Json

& 'E:\RimModDev\RimUnity\Tools\bin\ShaderTool\RimUnity.ShaderTool.exe' 'E:\steam\steamapps\common\RimWorld' shaderlab $sourceFile $buildRoot world ',' -1 Linear
if ($LASTEXITCODE -ne 0) { throw '辐射 Shader 离线编译失败。' }
$manifest = Get-Content -LiteralPath (Join-Path $buildRoot 'shader.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.states.Blend -ne 'SrcAlpha One, Zero One') { throw '辐射 Shader 必须使用加色混合并保留场景 Alpha。' }
$shader.srcColor = 'SrcAlpha'
$shader.dstColor = 'One'
$shader.srcAlpha = 'Zero'
$shader.dstAlpha = 'One'

#阶段程序按实际内容命名，描述中的相对路径保持与标准导出器一致。
foreach ($field in @('vertexFile', 'fragmentFile')) {
    $stageFile = Join-Path $buildRoot $manifest.$field
    $fileName = (Get-FileHash -LiteralPath $stageFile -Algorithm SHA256).Hash.ToLowerInvariant() + '.glsl'
    Copy-Item -LiteralPath $stageFile -Destination (Join-Path $dataRoot "blobs\$fileName") -Force
    $manifest.$field = $fileName
    $shader.$field = "blobs/$fileName"
}
$manifestText = $manifest | ConvertTo-Json -Depth 32 -Compress
$manifestFile = Join-Path $buildRoot 'shader.packed.json'
[IO.File]::WriteAllText($manifestFile, $manifestText, $utf8)
$manifestHash = (Get-FileHash -LiteralPath $manifestFile -Algorithm SHA256).Hash.ToLowerInvariant()
$shader.descriptorFile = "blobs/$manifestHash.json"
Copy-Item -LiteralPath $manifestFile -Destination (Join-Path $dataRoot $shader.descriptorFile) -Force

#从已编译的属性描述生成默认值，让逐实例参数和 XML 材质参数使用同一套声明。
$shader.defaults = @(
    foreach ($property in $manifest.properties) {
        if ($property.type -in @('Color', 'Vector')) {
            $value = @{ kind = 'vector'; number = 0.0; vector = @($property.defaultValue) }
        } elseif ($property.type -in @('Float', 'Range')) {
            $value = @{ kind = 'number'; number = $property.defaultValue[0] }
        } else {
            throw "辐射 Shader 包含未处理的属性类型：$($property.type)"
        }
        @{ name = $property.name; property = $false; value = $value }
    }
)
$sourceHash = (Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash.ToLowerInvariant()
$shader.key = "RimUnity_Custom_${sourceHash}_${manifestHash}"
$shader.approximations = $manifest.approximations
$asset.id = $shader.key
$asset.description = 'Tools/Shaders/MunoTriangleRadiation.shader'
$asset.file = "exports/radiation/$manifestHash.json"
$descriptorFile = Join-Path $dataRoot $asset.file
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($descriptorFile)) | Out-Null
[IO.File]::WriteAllText($descriptorFile, ($shader | ConvertTo-Json -Depth 32 -Compress), $utf8)
[IO.File]::WriteAllText($catalogFile, ($catalog | ConvertTo-Json -Depth 32 -Compress), $utf8)
Write-Output '辐射 Shader 标准资源已更新。'
