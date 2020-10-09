SET BUILDCONFIG=Debug
FOR /F %%I IN ("%0") DO SET CURRENTDIR=%%~dpI

REM http://www.sysnet.pe.kr/temp/app/dumpwriter/MinidumpWriter.application

msbuild .\x64DumpWriter\x64DumpWriter.csproj /p:Configuration=%BUILDCONFIG%;Platform=AnyCPU;SolutionDir=%CURRENTDIR%
msbuild .\x86DumpWriter\x86DumpWriter.csproj /p:Configuration=%BUILDCONFIG%;Platform=AnyCPU;SolutionDir=%CURRENTDIR%

robocopy .\bin\%BUILDCONFIG% .\Libraries x64*.exe x86*.exe

tf checkout .\MinidumpWriter\MinidumpWriter.csproj
IncrementVersionInfo.exe /inc_clickonce_build .\MinidumpWriter\MinidumpWriter.csproj

msbuild .\MinidumpWriter\MinidumpWriter.csproj /p:Configuration=%BUILDCONFIG%;Platform=AnyCPU;SolutionDir=%CURRENTDIR% /target:publish /property:PublishUrl=http://www.sysnet.pe.kr/temp/app/dumpwriter/


robocopy .\bin\%BUILDCONFIG%\app.publish D:\workshop\SysnetWebSite\Sources\MyWeb\SysnetWebApp\temp\app\dumpwriter /S /W:5

