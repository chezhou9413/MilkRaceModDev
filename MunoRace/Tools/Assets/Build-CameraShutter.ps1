#生成摄像机快门音效，使用双段机械脉冲与确定性噪声，不依赖外部音频素材。
param([string]$ModRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$ErrorActionPreference = 'Stop'

#将两次短促机械闭合声合成为单声道十六位采样，并写入标准波形音频文件。
function Write-CameraShutter {
    param([string]$Destination)
    $sampleRate = 22050
    $sampleCount = [int]($sampleRate * 0.18)
    $dataSize = $sampleCount * 2
    $noise = [System.Random]::new(73)
    $writer = [System.IO.BinaryWriter]::new([System.IO.File]::Create($Destination))
    try {
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write([int](36 + $dataSize))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
        $writer.Write([int]16)
        $writer.Write([short]1)
        $writer.Write([short]1)
        $writer.Write([int]$sampleRate)
        $writer.Write([int]($sampleRate * 2))
        $writer.Write([short]2)
        $writer.Write([short]16)
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write([int]$dataSize)
        for ($sampleIndex = 0; $sampleIndex -lt $sampleCount; $sampleIndex++) {
            $time = $sampleIndex / [double]$sampleRate
            $firstPulse = [Math]::Exp(-$time * 110)
            $secondPulse = if ($time -ge 0.055) { 0.7 * [Math]::Exp(-($time - 0.055) * 90) } else { 0.0 }
            $envelope = ($firstPulse + $secondPulse) * [Math]::Min(1.0, $time / 0.001)
            $signal = (0.75 * ($noise.NextDouble() * 2 - 1) + 0.25 * [Math]::Sin($time * 2300 * 2 * [Math]::PI)) * $envelope
            $writer.Write([short][Math]::Round($signal * 15000))
        }
    }
    finally { $writer.Dispose() }
}

$soundDirectory = Join-Path $ModRoot 'Sounds\Camera'
[System.IO.Directory]::CreateDirectory($soundDirectory) | Out-Null
Write-CameraShutter -Destination (Join-Path $soundDirectory 'MunoCameraShutter.wav')
