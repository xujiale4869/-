$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$r=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$saveRoot=[Environment]::GetFolderPath('LocalApplicationData')+'Low/GuichaoStudio/深海灯塔 · 海湾生活 v21'
$source=$saveRoot+'/sea-bay-expansion-v30.json';$target=$saveRoot+'/sea-bay-expansion-v31.json'
if(!(Test-Path -LiteralPath ($r+'/Tools/SeaVillageV31/delivery.json'))){throw 'Finalize the isolated test/baseline proofs first'}
if(Test-Path -LiteralPath $target){throw 'First normal migration already occurred; do not repeat this historical acceptance script'}
if(Get-Process Unity,Guichao-SeaVillageV31 -ErrorAction SilentlyContinue){throw 'Existing game/editor active'}
$oldHash=(Get-FileHash -LiteralPath $source).Hash
$old=Get-Content -LiteralPath $source -Raw -Encoding UTF8|ConvertFrom-Json
$log=$r+'/Verification/village-v31-normal-startup.log'
$args=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$log+'"'))
$p=Start-Process ($r+'/Builds/SeaVillageV31/Guichao-SeaVillageV31.exe') -ArgumentList $args -WorkingDirectory $r -PassThru
$null=$p.Handle;$deadline=[DateTime]::UtcNow.AddSeconds(60);$text=''
while([DateTime]::UtcNow -lt $deadline){
 if($p.HasExited){throw 'Normal player exited before acceptance'}
 if(Test-Path -LiteralPath $log){$fs=[IO.File]::Open($log,'Open','Read','ReadWrite');$reader=New-Object IO.StreamReader($fs);try{$text=$reader.ReadToEnd()}finally{$reader.Dispose();$fs.Dispose()};if($text -match '(?m)^\w*Exception:' -or $text -match 'BAY_V28_(LOAD|SAVE)_FAILED'){throw 'Normal startup exception/save failure; see player log'};if($text.Contains('SEAMLESS_V29_READY')){break}}
 Start-Sleep -Milliseconds 300
}
if(!$text.Contains('SEAMLESS_V29_READY')){throw 'Normal startup readiness deadline'}
$newHash=(Get-FileHash -LiteralPath $target).Hash;$new=Get-Content -LiteralPath $target -Raw -Encoding UTF8|ConvertFrom-Json
$unchanged=(Get-FileHash -LiteralPath $source).Hash -eq $oldHash
$loaded=[int]([regex]::Match($text,'BAY_V28_READY structures=(\d+)').Groups[1].Value)
if(!$unchanged -or $newHash -ne $oldHash -or $new.placedBuildings.Count -ne $old.placedBuildings.Count -or $loaded -ne $old.placedBuildings.Count){throw 'Normal migration is not an exact retained-layout copy'}
$proof=@{passed=$true;scope='First ordinary v31 startup, no check/isolation flags; game intentionally left open';uiBase='v30';sourceLayout='sea-bay-expansion-v30.json';newLayout='sea-bay-expansion-v31.json';sourceSHA256=$oldHash;newSHA256=$newHash;sourceBuildings=$old.placedBuildings.Count;copiedBuildings=$new.placedBuildings.Count;loadedBuildings=$loaded;oldV30LayoutUnchanged=$unchanged;byteExactCopy=$true;ready=$true;pid=$p.Id;time=[DateTime]::UtcNow.ToString('o')}
$proof|ConvertTo-Json -Depth 5|Set-Content -LiteralPath ($r+'/Docs/SeaVillageV31/normal-startup.json') -Encoding UTF8
[IO.File]::WriteAllText($r+'/Docs/SeaVillageV31/normal-startup.log',$text)
Write-Output ('NORMAL_STARTUP_OK copied/loaded='+$loaded+' pid='+$p.Id)
