param(
    [string]$BepInExVersion = "5.4.23.5",
    [string]$UnityModulesVersion = "2021.3.33"
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
chcp 65001 > $null

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$artifactsDir = Join-Path $projectRoot "artifacts"
$releaseRoot = Join-Path $projectRoot "release"
$releaseNotesPath = Join-Path $releaseRoot "RELEASE_NOTES.md"
$releaseZipPath = Join-Path $releaseRoot "SimplePlanes2PartEditor-Plugin.zip"
$depsRoot = Join-Path $projectRoot ".ci-deps"
$pluginDllPath = Join-Path $artifactsDir "SimplePlanes2PartEditor.dll"
$unityPackagePath = Join-Path $depsRoot "UnityEngine.Modules.$UnityModulesVersion.nupkg"
$unityPackageZipPath = Join-Path $depsRoot "UnityEngine.Modules.$UnityModulesVersion.zip"
$unityPackageRoot = Join-Path $depsRoot "UnityEngine.Modules.$UnityModulesVersion"
$unityLibRoot = Join-Path $unityPackageRoot "lib\net35"
$bepInExZipPath = Join-Path $depsRoot "BepInEx_win_x64_$BepInExVersion.zip"
$bepInExRoot = Join-Path $depsRoot "BepInEx_win_x64_$BepInExVersion"
$bepInExCoreDir = Join-Path $bepInExRoot "BepInEx\core"

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

function Invoke-DownloadFile {
    param(
        [string]$Uri,
        [string]$OutFile
    )

    if (Test-Path $OutFile) {
        return
    }

    Invoke-WebRequest -Uri $Uri -OutFile $OutFile
}

function Expand-ZipIfNeeded {
    param(
        [string]$ZipPath,
        [string]$Destination
    )

    if (Test-Path $Destination) {
        return
    }

    Expand-Archive -Path $ZipPath -DestinationPath $Destination -Force
}

function Restore-CiDependencies {
    New-Item -ItemType Directory -Force -Path $depsRoot | Out-Null

    Invoke-DownloadFile `
        -Uri "https://github.com/BepInEx/BepInEx/releases/download/v$BepInExVersion/BepInEx_win_x64_$BepInExVersion.zip" `
        -OutFile $bepInExZipPath
    Expand-ZipIfNeeded -ZipPath $bepInExZipPath -Destination $bepInExRoot

    Invoke-DownloadFile `
        -Uri "https://www.nuget.org/api/v2/package/UnityEngine.Modules/$UnityModulesVersion" `
        -OutFile $unityPackagePath

    if (-not (Test-Path $unityPackageZipPath)) {
        Copy-Item -Path $unityPackagePath -Destination $unityPackageZipPath -Force
    }

    Expand-ZipIfNeeded -ZipPath $unityPackageZipPath -Destination $unityPackageRoot
}

function Get-PluginVersion {
    $pluginSourcePath = Join-Path $projectRoot "src\SimplePlanes2PartEditorPlugin.cs"
    $pluginSource = Get-Content -Raw $pluginSourcePath
    $match = [regex]::Match($pluginSource, 'PluginVersion\s*=\s*"([^"]+)"')

    if (-not $match.Success) {
        throw "Unable to read PluginVersion from $pluginSourcePath"
    }

    return $match.Groups[1].Value
}

function Assert-ReleaseVersionMatches {
    $pluginVersion = Get-PluginVersion
    $index = Get-Content -Raw (Join-Path $projectRoot "index.json") | ConvertFrom-Json
    $indexVersion = [string]$index.version
    $gitHubRefName = [Environment]::GetEnvironmentVariable("GITHUB_REF_NAME")

    if ($indexVersion -ne $pluginVersion) {
        throw "index.json version ($indexVersion) does not match PluginVersion ($pluginVersion)."
    }

    if (-not [string]::IsNullOrWhiteSpace($gitHubRefName) -and $gitHubRefName.StartsWith("v")) {
        $tagVersion = $gitHubRefName.Substring(1)
        if ($tagVersion -ne $pluginVersion) {
            throw "Git tag ($gitHubRefName) does not match PluginVersion ($pluginVersion)."
        }
    }
}

function Write-ReleaseNotes {
    $index = Get-Content -Raw (Join-Path $projectRoot "index.json") | ConvertFrom-Json
    $pluginVersion = Get-PluginVersion
    $notes = [string]$index.releaseNotes

    if ([string]::IsNullOrWhiteSpace($notes)) {
        $notes = "SimplePlanes2PartEditor $pluginVersion"
    }

    Set-Content -Path $releaseNotesPath -Encoding UTF8 -Value $notes
}

function Write-ModManifest {
    param([string]$ManifestPath)

    $pluginVersion = Get-PluginVersion
    $manifest = [ordered]@{
        id = "SimplePlanes2PartEditor"
        name = "SimplePlanes 2 Part Editor"
        version = $pluginVersion
        description = "In-game SimplePlanes 2 part data editor."
        fileName = "SimplePlanes2PartEditor-Plugin.zip"
        entryDll = "BepInEx/plugins/SimplePlanes2PartEditor/SimplePlanes2PartEditor.dll"
        pluginDirectory = "BepInEx/plugins/SimplePlanes2PartEditor"
        configFiles = @(
            "BepInEx/plugins/SimplePlanes2PartEditor/settings.json"
        )
    }

    $manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $ManifestPath -Encoding UTF8
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

    New-Item -ItemType Directory -Force -Path $packageRoot, $pluginRoot, $localizationRoot | Out-Null
    Write-ModManifest -ManifestPath (Join-Path $packageRoot "mod.json")
    Copy-Item -Path $pluginDllPath -Destination (Join-Path $pluginRoot "SimplePlanes2PartEditor.dll") -Force
    Copy-Item -Path (Join-Path $projectRoot "content\settings.json") -Destination (Join-Path $pluginRoot "settings.json") -Force
    Copy-DirectoryContents -Source (Join-Path $projectRoot "content\localization") -Destination $localizationRoot
    Copy-Item -Path (Join-Path $projectRoot "README.md") -Destination (Join-Path $packageRoot "README.md") -Force
    Copy-Item -Path (Join-Path $projectRoot "README.en.md") -Destination (Join-Path $packageRoot "README.en.md") -Force

    Compress-PackageContents -PackageRoot $packageRoot -ZipPath $releaseZipPath
}

New-Item -ItemType Directory -Force -Path $artifactsDir, $releaseRoot | Out-Null
Assert-ReleaseVersionMatches
Restore-CiDependencies
Assert-RequiredFile (Join-Path $bepInExCoreDir "BepInEx.dll")
Assert-RequiredFile (Join-Path $bepInExCoreDir "0Harmony.dll")
Assert-RequiredFile (Join-Path $unityLibRoot "UnityEngine.dll")
Assert-RequiredFile (Join-Path $unityLibRoot "UnityEngine.CoreModule.dll")
Assert-RequiredFile (Join-Path $unityLibRoot "UnityEngine.IMGUIModule.dll")
Assert-RequiredFile (Join-Path $unityLibRoot "UnityEngine.InputLegacyModule.dll")
Assert-RequiredFile (Join-Path $unityLibRoot "UnityEngine.TextRenderingModule.dll")

$sourceFiles = Get-ChildItem (Join-Path $projectRoot "src") -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }
$references = @(
    (Join-Path $bepInExCoreDir "BepInEx.dll"),
    (Join-Path $bepInExCoreDir "0Harmony.dll"),
    (Join-Path $unityLibRoot "UnityEngine.dll"),
    (Join-Path $unityLibRoot "UnityEngine.CoreModule.dll"),
    (Join-Path $unityLibRoot "UnityEngine.IMGUIModule.dll"),
    (Join-Path $unityLibRoot "UnityEngine.InputLegacyModule.dll"),
    (Join-Path $unityLibRoot "UnityEngine.TextRenderingModule.dll"),
    "System.Xml.Linq.dll"
)
$referenceArgs = $references | ForEach-Object { "/r:$_" }
$csc = Get-CSharpCompilerPath

& $csc /nologo /target:library /optimize+ /out:$pluginDllPath $referenceArgs $sourceFiles
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed."
}

New-ReleasePackage
Write-ReleaseNotes

Write-Host "CI build completed: $releaseRoot"
