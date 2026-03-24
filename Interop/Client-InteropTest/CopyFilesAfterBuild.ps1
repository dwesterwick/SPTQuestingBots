param (
    [string]$modName,
    [string]$configuration
)

Write-Host ('Copying client files for {0}...' -f $modName)

Set-Location $PSScriptRoot

$destinationAbsolute = Join-Path $PSScriptRoot ('..\..\..\..\BepInEx\plugins\{0}\' -f $modName)
$clientLibraryAbsolute = Join-Path $PSScriptRoot ('bin\{0}\{1}.dll' -f $configuration, $modName)

try
{
    if (!(Test-Path -PathType Container $destinationAbsolute))
    {
        New-Item -Path $destinationAbsolute -ItemType Directory
    }

    Copy-Item -Path $clientLibraryAbsolute -Destination $destinationAbsolute -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not copy file {0} to {1}: {2}' -f $clientLibraryAbsolute, $destinationAbsolute, $_.Exception.Message)
    exit 1
}

Write-Host ('Copying client files for {0}...done.' -f $modName)