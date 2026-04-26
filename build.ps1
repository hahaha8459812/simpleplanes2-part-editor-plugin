param(
    [string]$GameDir = "E:\Game\steam\steamapps\common\SimplePlanes 2",
    [switch]$InstallToGame
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
chcp 65001 > $null

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$artifactsDir = Join-Path $projectRoot "artifacts"
$releaseRoot = Join-Path $projectRoot "release"
$pluginDllPath = Join-Path $artifactsDir "SimplePlanes2PartEditor.dll"
$managedDir = Join-Path $GameDir "SimplePlanes 2_Data\Managed"
$bepInExCoreDir = Join-Path $GameDir "BepInEx\core"

function Get-CSharpCompilerPath {
    $candidates = @(
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Unable to find csc.exe from .NET Framework."
}

function Assert-RequiredFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Required file not found: $Path"
    }
}

function Copy-DirectoryContents {
    param(
        [string]$Source,
        [string]$Destination
    )

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Copy-Item -Path (Join-Path $Source "*") -Destination $Destination -Recurse -Force
}

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

function Compress-PackageContents {
    param(
        [string]$PackageRoot,
        [string]$ZipPath
    )

    if (Test-Path $ZipPath) {
        Remove-Item -Force $ZipPath
    }

    Compress-Archive -Path (Join-Path $PackageRoot "*") -DestinationPath $ZipPath -Force
}

function New-ReleasePackage {
    $packageRoot = Join-Path $releaseRoot "SimplePlanes2PartEditor-Release"
    $pluginRoot = Join-Path $packageRoot "BepInEx\plugins\SimplePlanes2PartEditor"
    $localizationRoot = Join-Path $pluginRoot "localization"

    if (Test-Path $packageRoot) {
        Remove-Item -Recurse -Force $packageRoot
    }

    New-Item -ItemType Directory -Force -Path $pluginRoot, $localizationRoot | Out-Null
    Copy-Item -Path $pluginDllPath -Destination (Join-Path $pluginRoot "SimplePlanes2PartEditor.dll") -Force
    Copy-Item -Path (Join-Path $projectRoot "content\settings.json") -Destination (Join-Path $pluginRoot "settings.json") -Force
    Copy-DirectoryContents -Source (Join-Path $projectRoot "content\localization") -Destination $localizationRoot
    Copy-Item -Path (Join-Path $projectRoot "index.json") -Destination (Join-Path $packageRoot "index.json") -Force
    Copy-Item -Path (Join-Path $projectRoot "README.md") -Destination (Join-Path $packageRoot "README.md") -Force
    Copy-Item -Path (Join-Path $projectRoot "README.en.md") -Destination (Join-Path $packageRoot "README.en.md") -Force
    Copy-Item -Path (Join-Path $projectRoot "install.ps1") -Destination (Join-Path $packageRoot "install.ps1") -Force

    Compress-PackageContents -PackageRoot $packageRoot -ZipPath (Join-Path $releaseRoot "SimplePlanes2PartEditor-Release.zip")
}

Assert-RequiredFile (Join-Path $bepInExCoreDir "BepInEx.dll")
Assert-RequiredFile (Join-Path $bepInExCoreDir "0Harmony.dll")
Assert-RequiredFile (Join-Path $managedDir "UnityEngine.dll")
Assert-RequiredFile (Join-Path $managedDir "UnityEngine.CoreModule.dll")
Assert-RequiredFile (Join-Path $managedDir "UnityEngine.IMGUIModule.dll")
Assert-RequiredFile (Join-Path $managedDir "UnityEngine.InputLegacyModule.dll")
Assert-RequiredFile (Join-Path $managedDir "UnityEngine.TextRenderingModule.dll")
Assert-RequiredFile (Join-Path $managedDir "netstandard.dll")

New-Item -ItemType Directory -Force -Path $artifactsDir, $releaseRoot | Out-Null

$sourceFiles = Get-ChildItem (Join-Path $projectRoot "src") -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }
$references = @(
    (Join-Path $bepInExCoreDir "BepInEx.dll"),
    (Join-Path $bepInExCoreDir "0Harmony.dll"),
    (Join-Path $managedDir "netstandard.dll"),
    (Join-Path $managedDir "UnityEngine.dll"),
    (Join-Path $managedDir "UnityEngine.CoreModule.dll"),
    (Join-Path $managedDir "UnityEngine.IMGUIModule.dll"),
    (Join-Path $managedDir "UnityEngine.InputLegacyModule.dll"),
    (Join-Path $managedDir "UnityEngine.TextRenderingModule.dll")
)
$referenceArgs = $references | ForEach-Object { "/r:$_" }
$csc = Get-CSharpCompilerPath

& $csc /nologo /target:library /optimize+ /out:$pluginDllPath $referenceArgs $sourceFiles
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed."
}

New-ReleasePackage

if ($InstallToGame) {
    $gamePluginRoot = Join-Path $GameDir "BepInEx\plugins\SimplePlanes2PartEditor"
    $gameLocalizationRoot = Join-Path $gamePluginRoot "localization"
    $gameSettingsPath = Join-Path $gamePluginRoot "settings.json"
    New-Item -ItemType Directory -Force -Path $gamePluginRoot, $gameLocalizationRoot | Out-Null
    Copy-Item -Path $pluginDllPath -Destination (Join-Path $gamePluginRoot "SimplePlanes2PartEditor.dll") -Force
    Update-SettingsWithMissingDefaults -DefaultSettingsPath (Join-Path $projectRoot "content\settings.json") -TargetSettingsPath $gameSettingsPath
    Copy-DirectoryContents -Source (Join-Path $projectRoot "content\localization") -Destination $gameLocalizationRoot
}

Write-Host "Build completed: $releaseRoot"
