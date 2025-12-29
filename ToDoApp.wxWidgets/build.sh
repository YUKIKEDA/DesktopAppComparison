#!/bin/bash
# Build script for Linux/Mac using Nuitka

echo "Building TodoApp with Nuitka..."

python -m nuitka \
    --standalone \
    --onefile \
    --include-package=wx \
    --include-package-data=wx \
    --include-module=models \
    --include-module=views \
    --include-module=controllers \
    --include-module=utils \
    --output-dir=dist \
    --output-filename=TodoApp \
    main.py

echo ""
echo "Build complete! Check the dist folder for TodoApp"
echo "On Linux/Mac, you may need to make it executable: chmod +x dist/TodoApp"

