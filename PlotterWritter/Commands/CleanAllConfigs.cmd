Echo on
Echo CleanAllConfigs.cmd
Del /f /s /q ..\*.sdf
Del /f /s /q ..\*.pch
Del /f /s /q ..\*.pdb
Del /f /s /q ..\*.resources
Del /f /s /q ..\*.dll
Del /f /s /q ..\*.exe
Del /f /s /q ..\*.obj
Del /f /s /q ..\*.tlog
Del /f /s /q ..\*.baml
RD  /s       ..\ipch
pause