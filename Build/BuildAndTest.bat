@echo This script simulates what the build server is doing
@rem  /p:AdditionalBuildProperties="/v:d /p:MSBuildTargetsVerbose=true"
%windir%\microsoft.net\framework\v4.0.30319\msbuild Automated.NRefactory.proj /p:ArtefactsOutputDir="%CD%\results" /p:TestReportsDir="%CD%\results"
@IF %ERRORLEVEL% NEQ 0 GOTO err
@exit /B 0
:err
@PAUSE
@exit /B 1