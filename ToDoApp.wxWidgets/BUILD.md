# Nuitkaでexe化する方法

## 前提条件

1. Python 3.13以上がインストールされていること
2. Nuitkaがインストールされていること（`pyproject.toml`に含まれています）
3. wxPythonがインストールされていること

## インストール

```bash
# 仮想環境をアクティベート（推奨）
# Windows
.\venv\Scripts\activate

# Linux/Mac
source venv/bin/activate

# 依存関係をインストール
pip install -e .
```

## ビルド方法

### Windows

```bash
# 方法1: ビルドスクリプトを使用（推奨）
build.bat

# 方法2: 手動でコマンドを実行
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
```

### Linux/Mac

```bash
# 方法1: ビルドスクリプトを使用（推奨）
chmod +x build.sh
./build.sh

# 方法2: 手動でコマンドを実行
python -m nuitka \
    --standalone \
    --include-package=wx \
    --include-package-data=wx \
    --include-module=models \
    --include-module=views \
    --include-module=controllers \
    --include-module=utils \
    --output-dir=dist \
    --output-filename=TodoApp \
    main.py
```

## オプション説明

- `--standalone`: スタンドアロン実行ファイルを作成（依存関係を含む。出力は `dist/main.dist/` フォルダ）
- `--include-package=wx`: wxPythonパッケージを含める（重要！）
- `--include-package-data=wx`: wxPythonのデータファイルを含める
- `--windows-console-mode=disable`: Windowsでコンソールウィンドウを非表示（GUIアプリ用）
- `--include-module`: Pythonモジュールを含める
- `--output-dir=dist`: 出力ディレクトリを指定
- `--output-filename=TodoApp.exe`: 出力ファイル名を指定

> Note: `--onefile` は使わない（Windows Defender が onefile スタブを誤検知しやすいため）。配布時は `dist/main.dist/` 一式をコピーする。

## アイコンの設定（オプション）

アイコンファイル（`icon.ico`）をプロジェクトルートに配置すると、exeファイルにアイコンが設定されます。

```bash
# アイコンを含める場合（Windows）
--windows-icon-from-ico=icon.ico
```

## トラブルシューティング

### ビルドが失敗する場合

1. **Nuitkaのバージョンを確認**
   ```bash
   python -m nuitka --version
   ```

2. **依存関係を再インストール**
   ```bash
   pip install --upgrade nuitka wxpython
   ```

3. **キャッシュをクリア**
   ```bash
   # Nuitkaのキャッシュを削除
   python -m nuitka --remove-output
   ```

### exeファイルが大きい場合

- `--onefile`を外すと、複数ファイルに分割され、サイズが小さくなります
- ただし、配布時はすべてのファイルが必要です

### 実行時にエラーが発生する場合

- `--windows-console-mode=enable`に変更して、コンソール出力を確認
- データディレクトリが正しく含まれているか確認

## 配布

ビルドが完了すると、`dist/main.dist/TodoApp.exe`（Windows）または同等のバイナリが生成されます。

`main.dist` フォルダごとコピーして実行します（同じ OS 向け）。onefile 単体 exe は Defender 誤検知の対象になりやすいです。

