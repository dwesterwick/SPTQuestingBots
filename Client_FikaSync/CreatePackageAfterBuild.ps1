param (
    [string]$modName,
    [string]$modVersion,
    [string]$configuration
)

if ($configuration -eq "DEBUG")
{
    Write-Host ("Files will not be packaged for debug builds")
    exit 0
}

# Set path to 7-Zip executable
$pathTo7z = "C:\Program Files\7-Zip\7z.exe"

Write-Host ('Packaging {0}FikaSync v{1}...' -f $modName, $modVersion)

Set-Location $PSScriptRoot

# Create the build folders
$packageFolderAbsoluteFikaSync = Join-Path $PSScriptRoot "..\Dist_FikaSync"

try
{
    New-Item -ItemType Directory -Path $packageFolderAbsoluteFikaSync -Force -errorAction stop | Out-Null
    Remove-Item -Path ('{0}\*' -f $packageFolderAbsoluteFikaSync) -Recurse -Force -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not create Dist directory or empty its contents: {0}' -f $_.Exception.Message)
    exit 1
}

# Create server and client folders
$clientFolderAbsoluteFikaSync = Join-Path $packageFolderAbsoluteFikaSync ('BepInEx\plugins\{0}FikaSync' -f $modName)

try
{
    New-Item -ItemType Directory -Path $clientFolderAbsoluteFikaSync -Force -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not create Dist directory subfolders: {0}' -f $_.Exception.Message)
    exit 1
}

# Copy all files

Write-Host ('Packaging {0}FikaSync v{1}...copying files...' -f $modName, $modVersion)

$clientLibraryAbsoluteFikaSync = Join-Path $PSScriptRoot ('..\Client_FikaSync\bin\Release\netstandard2.1\{0}FikaSync-Client.dll' -f $modName)

try
{
    Copy-Item -Path $clientLibraryAbsoluteFikaSync -Destination $clientFolderAbsoluteFikaSync -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not copy files to Dist directory: {0}' -f $_.Exception.Message)
    exit 1
}

# Create 7zip archive

Write-Host ('Packaging {0}FikaSync v{1}...creating archive...' -f $modName, $modVersion)

$archiveName = Join-Path $packageFolderAbsoluteFikaSync ('{0}FikaSync-{1}.7z' -f $modName, $modVersion)
$sourceFiles = Join-Path $packageFolderAbsoluteFikaSync '*'
$arguments = "a", "-t7z", $archiveName, $sourceFiles

try
{
    & $pathTo7z $arguments | Out-Null
}
catch
{
    Write-Error ('Could not create 7-Zip archive {0}: {1}' -f $archiveName, $_.Exception.Message)
    exit 1
}

Write-Host ('Packaging {0}FikaSync v{1}...done.' -f $modName, $modVersion)