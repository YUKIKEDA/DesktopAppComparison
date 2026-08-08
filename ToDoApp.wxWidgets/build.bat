@echo off
REM Build script for Windows using Nuitka (standalone folder, not onefile)

echo Building TodoApp with Nuitka (standalone)...

python -m nuitka ^
    --standalone ^
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
echo Build complete! Run dist\main.dist\TodoApp.exe
pause

