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

Write-Host ('Packaging {0} v{1}...' -f $modName, $modVersion)

Set-Location $PSScriptRoot

# Create the build folder
$packageFolderAbsolute = Join-Path $PSScriptRoot "..\Dist"

try
{
    New-Item -ItemType Directory -Path $packageFolderAbsolute -Force -errorAction stop | Out-Null
    Remove-Item -Path ('{0}\*' -f $packageFolderAbsolute) -Recurse -Force -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not create Dist directory or empty its contents: {0}' -f $_.Exception.Message)
    exit 1
}


# Create server and client folders
$serverFolderAbsolute = Join-Path $packageFolderAbsolute ('SPT\user\mods\{0}' -f $modName)
$clientFolderAbsolute = Join-Path $packageFolderAbsolute ('BepInEx\plugins\{0}' -f $modName)

try
{
    New-Item -ItemType Directory -Path $serverFolderAbsolute -Force -errorAction stop | Out-Null
    New-Item -ItemType Directory -Path $clientFolderAbsolute -Force -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not create Dist directory subfolders: {0}' -f $_.Exception.Message)
    exit 1
}

# Copy all files

Write-Host ('Packaging {0} v{1}...copying files...' -f $modName, $modVersion)

$configFileAbsolute = Join-Path $PSScriptRoot 'Config\config.json'
$serverLibraryAbsolute = Join-Path $PSScriptRoot ('..\Server\bin\Release\{0}-Server\{0}-Server.dll' -f $modName)

$clientLibraryAbsolute = Join-Path $PSScriptRoot ('..\Client\bin\Release\netstandard2.1\{0}-Client.dll' -f $modName)

try
{
    Copy-Item -Path $configFileAbsolute -Destination $serverFolderAbsolute -errorAction stop | Out-Null
    Copy-Item -Path $serverLibraryAbsolute -Destination $serverFolderAbsolute -errorAction stop | Out-Null

    Copy-Item -Path $clientLibraryAbsolute -Destination $clientFolderAbsolute -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not copy files to Dist directory: {0}' -f $_.Exception.Message)
    exit 1
}

# Create 7zip archive

Write-Host ('Packaging {0} v{1}...creating archive...' -f $modName, $modVersion)

$archiveName = Join-Path $packageFolderAbsolute ('{0}-{1}.7z' -f $modName, $modVersion)
$sourceFiles = Join-Path $packageFolderAbsolute '*'
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

Write-Host ('Packaging {0} v{1}...done.' -f $modName, $modVersion)