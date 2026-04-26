param(
    [string]$GameDir = "E:\Game\steam\steamapps\common\SimplePlanes 2"
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
chcp 65001 > $null

$ErrorActionPreference = "Stop"

$sourceRoot = $PSScriptRoot
$pluginSourceRoot = Join-Path $sourceRoot "BepInEx\plugins\SimplePlanes2PartEditor"
$pluginTargetRoot = Join-Path $GameDir "BepInEx\plugins\SimplePlanes2PartEditor"
$settingsSourcePath = Join-Path $pluginSourceRoot "settings.json"
$settingsTargetPath = Join-Path $pluginTargetRoot "settings.json"
$bepInExCoreSourceRoot = Join-Path $sourceRoot "BepInEx\core"
$bepInExCoreTargetRoot = Join-Path $GameDir "BepInEx\core"

function Update-SettingsWithMissingDefaults {
    param(
        [string]$DefaultSettingsPath,
        [string]$TargetSettingsPath
    )

    if (-not (Test-Path $DefaultSettingsPath)) {
        return
    }

    if (-not (Test-Path $TargetSettingsPath)) {
        Copy-Item -Path $DefaultSettingsPath -Destination $TargetSettingsPath -Force
        return
    }

    try {
        $defaultSettings = Get-Content -Raw $DefaultSettingsPath | ConvertFrom-Json
        $targetSettings = Get-Content -Raw $TargetSettingsPath | ConvertFrom-Json
        $targetPropertyNames = @($targetSettings.PSObject.Properties.Name)
        $changed = $false

        foreach ($property in $defaultSettings.PSObject.Properties) {
            if ($targetPropertyNames -notcontains $property.Name) {
                $targetSettings | Add-Member -NotePropertyName $property.Name -NotePropertyValue $property.Value
                $changed = $true
            }
        }

        if ($changed) {
            $targetSettings | ConvertTo-Json -Depth 8 | Set-Content -Path $TargetSettingsPath -Encoding UTF8
        }
    }
    catch {
        Write-Warning "Unable to merge missing settings into $TargetSettingsPath. Existing settings were left unchanged."
    }
}

if (-not (Test-Path (Join-Path $GameDir "SimplePlanes 2.exe"))) {
    throw "Game directory does not contain SimplePlanes 2.exe: $GameDir"
}

if (-not (Test-Path $pluginSourceRoot)) {
    throw "This installer should be run from the extracted release package root."
}

if (Test-Path (Join-Path $sourceRoot "winhttp.dll")) {
    Copy-Item -Path (Join-Path $sourceRoot "winhttp.dll") -Destination (Join-Path $GameDir "winhttp.dll") -Force
}

if (Test-Path (Join-Path $sourceRoot "doorstop_config.ini")) {
    Copy-Item -Path (Join-Path $sourceRoot "doorstop_config.ini") -Destination (Join-Path $GameDir "doorstop_config.ini") -Force
}

if (Test-Path (Join-Path $sourceRoot ".doorstop_version")) {
    Copy-Item -Path (Join-Path $sourceRoot ".doorstop_version") -Destination (Join-Path $GameDir ".doorstop_version") -Force
}

if (Test-Path $bepInExCoreSourceRoot) {
    New-Item -ItemType Directory -Force -Path $bepInExCoreTargetRoot | Out-Null
    Copy-Item -Path (Join-Path $bepInExCoreSourceRoot "*") -Destination $bepInExCoreTargetRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $pluginTargetRoot | Out-Null
Copy-Item -Path (Join-Path $pluginSourceRoot "*") -Destination $pluginTargetRoot -Recurse -Force -Exclude "settings.json"
Update-SettingsWithMissingDefaults -DefaultSettingsPath $settingsSourcePath -TargetSettingsPath $settingsTargetPath
Write-Host "Installed SimplePlanes2PartEditor to: $pluginTargetRoot"
