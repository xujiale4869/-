param([switch]$Build)

$ErrorActionPreference = 'Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$manifest = Get-Content -LiteralPath ($PSScriptRoot + '/accepted-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json

foreach ($inputFile in $manifest.files) {
    $path = Join-Path $project $inputFile.path
    if ((Get-FileHash -LiteralPath $path).Hash -ne $inputFile.sha256) {
        throw ('Frozen input differs: ' + $inputFile.path)
    }
}
Write-Output ('Verified ' + $manifest.files.Count + ' frozen inputs.')
if (!$Build) { return }
if (Get-Process Unity,Guichao-SeaVillageV31.1 -ErrorAction SilentlyContinue) {
    throw 'Close the current Unity editor or v31.1 player before rebuilding.'
}
$unity = 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe'
$arguments = @(
    '-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'Deepsea.Editor.SeaVillageBuildV31.BuildCoralContrast',
    '-logFile', ('"' + $project + '/Temp/coral-v311-rebuild.log"')
)
$process = Start-Process $unity -ArgumentList $arguments -PassThru
$null = $process.Handle
$process.WaitForExit()
$process.Refresh()
if ($process.ExitCode -ne 0) { throw ('Unity failed: ' + $process.ExitCode) }
