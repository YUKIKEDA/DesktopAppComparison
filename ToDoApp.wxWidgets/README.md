# Todo App - wxPython

wxPythonで実装されたTodoアプリケーション。Electron版と同じ機能を提供します。

## 機能

- **テーブル表示**: 仮想リストを使用した高性能なテーブル表示
- **CRUD操作**: アイテムの追加、編集、削除
- **フィルタリング**: タイトル・説明での検索、ステータス・優先度でのフィルタ
- **ソート**: 各カラムでのソート機能
- **データ永続化**: JSON形式での自動保存
- **キーボードショートカット**: 
  - `Ctrl+N`: 新規追加
  - `Ctrl+S`: 保存
  - `Ctrl+F`: 検索フィールドにフォーカス
  - `Delete`: 選択アイテムを削除
- **エクスポート/インポート**: JSON形式でのデータのエクスポート・インポート

## 要件

- Python 3.13以上
- wxPython 4.2.4以上

## インストール

```bash
# 仮想環境の作成（推奨）
python -m venv .venv

# 仮想環境の有効化
# Windows:
.venv\Scripts\activate
# Linux/Mac:
source .venv/bin/activate

# 依存関係のインストール
pip install -e .
```

または、uvを使用する場合：

```bash
uv sync
```

## 実行

```bash
python main.py
```

## プロジェクト構造

```
ToDoApp.wxWidgets/
├── main.py                 # アプリケーションエントリーポイント
├── models/                 # データモデル
│   ├── todo_item.py       # TodoItemデータモデル
│   └── data_service.py     # データ永続化サービス
├── views/                  # UIコンポーネント
│   ├── main_frame.py       # メインフレーム
│   ├── todo_table.py       # テーブルビュー（仮想リスト）
│   ├── todo_form_dialog.py # フォームダイアログ
│   ├── filter_bar.py       # フィルタバー
│   └── toolbar.py          # ツールバー
├── controllers/            # ビジネスロジック
│   └── todo_controller.py  # Todoコントローラー
└── utils/                  # ユーティリティ
    └── constants.py        # 定数定義
```

## アーキテクチャ

このアプリケーションはMVC（Model-View-Controller）パターンに従って実装されています：

- **Model**: `models/` - データモデルと永続化ロジック
- **View**: `views/` - UIコンポーネント
- **Controller**: `controllers/` - ビジネスロジックと状態管理

## データ保存

データは以下の場所に保存されます：

- **Windows**: `%APPDATA%\TodoApp\data\project.json`
- **Linux/Mac**: `~/.local/share/TodoApp/data/project.json`

## ベストプラクティス

この実装では、wxPythonのベストプラクティスに従っています：

1. **MVCパターン**: モデル、ビュー、コントローラーの明確な分離
2. **仮想リスト**: 大量データに対応するための仮想リストコントロールの使用
3. **イベント駆動**: wxPythonのイベントシステムを活用
4. **カスタムコントロール**: 再利用可能なコンポーネントの作成
5. **データバインディング**: コントローラーを通じたデータ管理

## ライセンス

このプロジェクトは比較目的で作成されています。

