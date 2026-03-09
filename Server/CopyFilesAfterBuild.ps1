param (
    [string]$modName,
    [string]$configuration,
    [string]$debugPath,
    [string]$relPathToSptInstall
)

Write-Host ('Copying server files for {0}...' -f $modName)

Set-Location $PSScriptRoot

$destinationAbsolute = Join-Path $PSScriptRoot ('{0}..\SPT\user\mods\{1}\' -f $relPathToSptInstall, $modName)
$serverLibraryAbsolute = Join-Path $PSScriptRoot ('bin\{0}\{1}-Server\{1}-Server.dll' -f $configuration, $modName)
$serverLibraryPdbAbsolute = Join-Path $PSScriptRoot ('bin\{0}\{1}-Server\{1}-Server.pdb' -f $configuration, $modName)

try
{
    if (!(Test-Path -PathType Container $destinationAbsolute))
    {
        New-Item -Path $destinationAbsolute -ItemType Directory | Out-Null
    }

    Copy-Item -Path $serverLibraryAbsolute -Destination $destinationAbsolute -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not copy file {0} to {1}: {2}' -f $serverLibraryAbsolute, $destinationAbsolute, $_.Exception.Message)
    exit 1
}

if (($configuration -eq "DEBUG") -and (Test-Path -PathType Container $debugPath))
{
    Write-Host ('Copying server files for {0} to debug location...' -f $modName)

    $debugDestinationAbsolute = Join-Path $debugPath ('user\mods\{0}-Server\' -f $modName)
    if (!(Test-Path -PathType Container $debugDestinationAbsolute))
    {
        New-Item -Path $debugDestinationAbsolute -ItemType Directory | Out-Null
    }

    Copy-Item -Path $serverLibraryAbsolute -Destination $debugDestinationAbsolute -errorAction stop | Out-Null
    Copy-Item -Path $serverLibraryPdbAbsolute -Destination $debugDestinationAbsolute -errorAction stop | Out-Null
}

Write-Host ('Copying server files for {0}...done.' -f $modName)