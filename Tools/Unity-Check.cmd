@echo off
setlocal
set "TASK_UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"
if not "%~2"=="" set "TASK_UNITY_EDITOR=%~2"
if not exist "%TASK_UNITY_EDITOR%" (
  echo Unity Editor not found. Pass the editor path as the second argument.
  exit /b 1
)
set "TASK_UNITY_METHOD=Deepsea.Editor.ProjectSetup.Verify"
set "TASK_UNITY_QUIT=-quit"
if /I "%~1"=="Play" (
  set "TASK_UNITY_METHOD=Deepsea.Editor.Verification.PlaySmoke"
  set "TASK_UNITY_QUIT="
)
if /I "%~1"=="Expedition" (
  set "TASK_UNITY_METHOD=Deepsea.Editor.ExpeditionPlayCheck.Run"
  set "TASK_UNITY_QUIT="
)
if /I "%~1"=="Build" set "TASK_UNITY_METHOD=Deepsea.Editor.ProjectSetup.BuildWindows"
if not defined ProgramData set "ProgramData=C:\ProgramData"
if not defined ALLUSERSPROFILE set "ALLUSERSPROFILE=C:\ProgramData"
if not defined TMP set "TMP=%TEMP%"
start "" /wait "%TASK_UNITY_EDITOR%" -batchmode %TASK_UNITY_QUIT% -projectPath "%~dp0.." -executeMethod %TASK_UNITY_METHOD% -logFile "%~dp0..\Verification\unity-cli.log"
if errorlevel 1 (
  echo Unity failed. See Verification\unity-cli.log.
  exit /b 1
)
echo Unity check completed.
