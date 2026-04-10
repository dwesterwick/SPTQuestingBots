param (
    [string]$guid,
    [string]$modName,
    [string]$author,
    [string]$modVersion,
    [string]$sptVersion,
    [string]$relPathToSptInstall
)

Set-Location $PSScriptRoot

$modInfoRelativePath = "ModInfo.cs"
$modInfoAbsolutePath = Join-Path $PSScriptRoot $modInfoRelativePath

Write-Host ('Updating {0}...' -f $modInfoAbsolutePath)

# Create a Mutex object to ensure another project is not already trying to update ModInfo.cs
$mutexName = "Global\ModInfoFileMutex"
$mutex = New-Object System.Threading.Mutex($false, $mutexName)
$mutexTimeoutMs = 5000

# Read the original file contents
try
{
    If ($mutex.WaitOne($mutexTimeoutMs))
    {
        $originalContent = Get-Content -Path $modInfoAbsolutePath -Raw -errorAction stop
    }
    Else
    {
        throw ('Could not acquire {0} Mutex within {1} ms.' -f $mutexName, $mutexTimeoutMs)
    }
}
catch
{
    Write-Error ('Could not read file {0}: {1}' -f $modInfoAbsolutePath, $_.Exception.Message)
    exit 1
}
finally
{
    [void]$mutex.ReleaseMutex()
}

$updatedContent = $originalContent

# Update property values
Write-Host ('Updating {0}...setting mod properties...' -f $modInfoAbsolutePath)
$updatedContent = $updatedContent.Trim() -replace 'GUID = ".*"' , ('GUID = "{0}"' -f $guid)
$updatedContent = $updatedContent.Trim() -replace 'MODNAME = ".*"' , ('MODNAME = "{0}"' -f $modName)
$updatedContent = $updatedContent.Trim() -replace 'AUTHOR = ".*"' , ('AUTHOR = "{0}"' -f $author)
$updatedContent = $updatedContent.Trim() -replace 'MOD_VERSION = ".*"' , ('MOD_VERSION = "{0}"' -f $modVersion)
$updatedContent = $updatedContent.Trim() -replace 'SPT_VERSION_COMPATIBILITY = ".*"' , ('SPT_VERSION_COMPATIBILITY = "{0}"' -f $sptVersion)
$updatedContent = $updatedContent.Trim() -replace 'RELATIVE_PATH_TO_SPT_INSTALL = ".*"' , ('RELATIVE_PATH_TO_SPT_INSTALL = "{0}"' -f $relPathToSptInstall)

# Write modified contents back to the file
try
{
    If ($mutex.WaitOne($mutexTimeoutMs))
    {
        $updatedContent | Out-File -FilePath $modInfoAbsolutePath -errorAction stop
    }
    Else
    {
        throw ('Could not acquire {0} Mutex within {1} ms.' -f $mutexName, $mutexTimeoutMs)
    }
}
catch
{
    Write-Error ('Could not create file {0}: {1}' -f $modInfoAbsolutePath, $_.Exception.Message)
    exit 1
}
finally
{
    [void]$mutex.ReleaseMutex()
}

$mutex.Dispose()

Write-Host ('Updating {0}...done.' -f $modInfoAbsolutePath)