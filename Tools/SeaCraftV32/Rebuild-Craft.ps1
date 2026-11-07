param(
    [string]$Blender = 'C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe',
    [string]$Unity = 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe',
    [switch]$BakeOnly
)

$ErrorActionPreference = 'Stop'
$project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (Get-Process Unity,Guichao-SeaCraftV32 -ErrorAction SilentlyContinue) {
    throw 'Close the Unity editor or v32 player before this build.'
}
if (!(Test-Path -LiteralPath $Blender) -or !(Test-Path -LiteralPath $Unity)) {
    throw 'Set the Blender and Unity executable paths.'
}

Push-Location -LiteralPath $project
try {
    [IO.Directory]::CreateDirectory((Join-Path $project 'Temp')) | Out-Null
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
    $generator = Join-Path $PSScriptRoot 'generate_crafted_houses_v32.py'
    $blenderLog = Join-Path $project "Temp/craft-v32-blender-$stamp.log"
    & $Blender -b --python-exit-code 13 -P $generator -- --project $project *> $blenderLog
    if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $blenderLog -SimpleMatch 'CRAFT_V32_MESHES 4 ' -Quiet)) {
        throw ('Blender generation failed. See ' + $blenderLog)
    }
    $method = if ($BakeOnly) { 'Prepare' } else { 'PrepareAndBuild' }
    $unityLog = Join-Path $project "Temp/craft-v32-unity-$stamp.log"
    $arguments = @('-batchmode','-quit','-projectPath',('"' + $project + '"'),
        '-executeMethod',('Deepsea.Editor.SeaCraftBuildV32.' + $method),'-logFile',('"' + $unityLog + '"'))
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru
    $null = $process.Handle
    $process.WaitForExit()
    $process.Refresh()
    $marker = if ($BakeOnly) { 'CRAFT_V32_BAKED models=4' } else { 'CRAFT_V32_BUILD_OK' }
    if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $unityLog -SimpleMatch $marker -Quiet)) {
        throw ('Unity bake or build failed. See ' + $unityLog)
    }
    Write-Output ($marker + ' ' + $unityLog)
} finally { Pop-Location }
