# List all installed Windows TTS voices
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$voices = $synth.GetInstalledVoices()

Write-Host "`nInstalled TTS Voices:" -ForegroundColor Green
foreach ($voice in $voices) {
    $info = $voice.VoiceInfo
    Write-Host "`nName: $($info.Name)" -ForegroundColor Cyan
    Write-Host "  Age: $($info.Age)"
    Write-Host "  Gender: $($info.Gender)"
    Write-Host "  Culture: $($info.Culture)"
    Write-Host "  Description: $($info.Description)"
    Write-Host "  Enabled: $($voice.Enabled)"
}

$synth.Dispose()
