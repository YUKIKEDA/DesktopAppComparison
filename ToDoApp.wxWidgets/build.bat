@echo off
REM Build script for Windows using Nuitka

echo Building TodoApp with Nuitka...

python -m nuitka ^
    --standalone ^
    --onefile ^
    --include-package=wx ^
    --include-package-data=wx ^
    --windows-console-mode=disable ^
    --include-module=models ^
    --include-module=views ^
    --include-module=controllers ^
    --include-module=utils ^
    --output-dir=dist ^
    --output-filename=TodoApp.exe ^
    main.py

echo.
echo Build complete! Check the dist folder for TodoApp.exe
pause

