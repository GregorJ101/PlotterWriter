@Echo off
Echo SaveAndIncrement.cmd for PlotterWriter
Echo Please close Visual Studio before clearing temporary files
Pause

@Echo on
Del /f /s /q ..\*.sdf
Del /f /s /q ..\*.pch
Del /f /s /q ..\*.pdb
Del /f /s /q ..\*.metagen
Del /f /s /q ..\*.manifest
Del /f /s /q ..\*.dll
Del /f /s /q ..\*.exe
Del /f /s /q ..\*.obj
Del /f /s /q ..\*.tlog
Del /f /s /q ..\*.ilk
Del /f /s /q ..\*.cache
Del /f /s /q ..\PlotterBuffer\obj\Debug\*.*
Del /f /s /q ..\PlotterBuffer\obj\Release\*.*
Del /f /s /q ..\PlotterDriver\Debug\*.*
Del /f /s /q ..\PlotterDriver\Release\*.*
Del /f /s /q ..\PlotterWritterDLLTester\obj\x86\Debug\*.*
Del /f /s /q ..\PlotterWritterDLLTester\obj\x86\Release\*.*
RD  /s    /q ..\PlotterBuffer\obj\Debug
RD  /s    /q ..\PlotterBuffer\obj\Release
RD  /s    /q ..\PlotterDriver\obj\Debug
RD  /s    /q ..\PlotterDriver\obj\Release
RD  /s    /q ..\PlotterWritterDLLTester\obj\x86\Debug
RD  /s    /q ..\PlotterWritterDLLTester\obj\x86\Release

Rem @Echo off
Rem Echo About to increment build version
Pause

@Echo on
C:\Tools\IncrementBuild.exe ..\PlotterBuffer\Properties\AssemblyInfo.cs

@Echo off
Echo You may now restart Visual Studio
Pause