# Todo App

モダンなデスクトップアプリケーション開発F/Wについて、メモリ使用量やCPU使用率を比較するための簡単なTodoアプリ

## 内容

### 比較対象フレームワーク

1. Avalonia
2. Compose Multiplatform
3. Electron
4. Flutter
5. GPUI
6. MAUI
7. React Native for Desktop
8. Tauri
9. WinForms
10. WPF
11. wxWidgets (wxPython)
12. WinUI3

### 機能要件

GitHub ProjectのTable viewを参考にした、テーブル形式のプロジェクト管理アプリケーションを実装します。

#### 1. テーブル表示

- **レイアウト**
  - 先頭列：チェックボックス列（アイテム選択用、ヘッダーに全選択/全解除）
  - データ列：各フィールドをカラムとして表示
  - スクロール可能（縦横両方向）

#### 2. カラム定義

| Column      | Field         | Type     | Description                              | Editable |
| ----------- | ------------- | -------- | ---------------------------------------- | -------- |
| ID          | `id`          | number   | Sequential (starts at 1, auto-generated) | No       |
| Title       | `title`       | string   | Max 200 chars, required                  | Yes      |
| Description | `description` | string   | Max 500 chars, optional                  | Yes      |
| Status      | `status`      | enum     | e.g. Not started, In progress, Done      | Yes      |
| Priority    | `priority`    | enum     | e.g. Low, Medium, High                   | Yes      |
| Due Date    | `dueDate`     | datetime | ISO 8601, optional                       | Yes      |
| Completed   | `isCompleted` | boolean  | Completion flag                          | Yes      |
| Created At  | `createdAt`   | datetime | ISO 8601, set automatically              | No       |
| Updated At  | `updatedAt`   | datetime | ISO 8601, updated automatically          | No       |

**ヘッダー**:
- Column: カラム名
- Field: フィールド名
- Type: データ型
- Description: 説明
- Editable: 編集可能

#### 3. CRUD操作

- **追加**
  - ツールバーの「+ 新しいアイテム」ボタンまたは`Ctrl+N`
  - フォームダイアログで入力
  - 保存/キャンセルボタン
- **編集**
  - 行をダブルクリックまたは編集ボタン
  - フォームダイアログで編集
  - 保存/キャンセルボタン
- **削除**
  - 単一削除：行の削除ボタンまたは右クリックメニュー
  - 一括削除：チェックボックスで複数選択後、ツールバーの削除ボタン
  - 削除確認ダイアログ表示

#### 4. フィルタリング

- **フィルタの種類**
  - テキストフィルタ：タイトル、説明での部分一致検索
  - 日付フィルタ：等しい、より前、より後、範囲指定
  - 選択フィルタ：ステータス、優先度での複数選択
- **UI**
  - 各カラムヘッダーにフィルタアイコン
  - フィルタパネルで条件設定
  - アクティブフィルタをバッジ表示

#### 5. ソート

- ヘッダークリックで昇順/降順/ソートなしを切り替え
- Shift+クリックで複数カラムソート
- ソート方向インジケーター（↑↓）表示
- ソート解除で作成日時の順序に戻す

#### 6. データ永続化

- **保存形式**
  - JSON形式で`data/project.json`に保存
  - 自動保存（変更後数秒）と手動保存（`Ctrl+S`）
- **データ構造**
  ```json
  {
    "items": [
      {
        "id": 1,
        "title": "タスクタイトル",
        "description": "説明",
        "status": "未着手",
        "priority": "中",
        "dueDate": "2024-01-01T00:00:00Z",
        "createdAt": "2024-01-01T00:00:00Z",
        "updatedAt": "2024-01-01T00:00:00Z",
        "isCompleted": false
      }
    ]
  }
  ```
- **読み込み**
  - 起動時に`data/project.json`から読み込み
  - ファイルが存在しない場合は空のプロジェクトを作成

#### 7. UI要件

- **ツールバー**
  - アイテム追加ボタン
  - 一括削除ボタン（選択時のみ有効）
  - フィルタボタン
  - エクスポート/インポートボタン
- **キーボードショートカット**
  - `Ctrl+N`: 新規追加
  - `Ctrl+F`: フィルタフォーカス
  - `Ctrl+S`: 保存
  - `Delete`: 選択行削除
  - `Esc`: ダイアログ閉じる
- **視覚的フィードバック**
  - 選択行のハイライト
  - 選択件数の表示
  - 行ホバー時のハイライト

#### 8. パフォーマンス要件

- 仮想化スクロール（1000件以上でもスムーズに動作）
- 大量データでのフィルタリング・ソートの高速化

#### 9. 実装上の注意事項

- 各フレームワークのベストプラクティスに従う
- フレームワーク標準コンポーネントを可能な限り使用
- すべてのフレームワークで同じデータ形式（JSON）を使用
- IDは連番（1から開始）で、削除後も再利用しない（常に最大値+1）

### パフォーマンス測定要件

#### 1. メモリ使用量の測定

- **測定タイミング**:
  - アプリ起動直後（空の状態）
  - Todoアイテム10件追加後
  - Todoアイテム100件追加後
  - Todoアイテム1000件追加後
- **測定指標**:
  - プライベートメモリ使用量（MB）
  - 作業セットサイズ（MB）
  - ピークメモリ使用量（MB）

#### 2. CPU使用率の測定

- **測定タイミング**:
  - アイドル状態（操作なし）
  - Todoアイテム追加時
  - Todoアイテム一覧スクロール時（100件以上）
  - フィルタリング操作時
- **測定指標**:
  - 平均CPU使用率（%）
  - ピークCPU使用率（%）

#### 3. パフォーマンステストシナリオ

- アプリ起動時間（冷起動）
- Todoアイテム1000件の一覧表示時間
- 大量データ（1000件）でのスクロールパフォーマンス
- フィルタリング操作の応答時間

### 実装要件

#### 1. コード品質

- 各フレームワークのベストプラクティスに従う
- 可能な限りシンプルな実装（過度な最適化を避ける）
- フレームワーク標準の状態管理方法を使用

#### 2. 環境統一

- 同じ開発環境（OS、ハードウェア）で測定
- 同じバージョンのランタイム/依存関係を使用
- 測定時に他のアプリケーションを最小限に実行

#### 3. データ統一

- すべてのフレームワークで同じテストデータセットを使用
- 比較用の標準的なTodoアイテムセットを定義
- テストデータセットは `data/` ディレクトリに配置
  - `todos_10.json`: 10件のサンプルデータ
  - `todos_100.json`: 100件のサンプルデータ
  - `todos_1000.json`: 1000件のサンプルデータ

**データ形式**:
テストデータはJSON形式で、以下の構造を持ちます：

```json
[
  {
    "id": 1,
    "title": "タスクタイトル（最大200文字）",
    "description": "タスクの説明（最大500文字、任意）",
    "createdAt": "ISO 8601形式の日時文字列",
    "isCompleted": false
  }
]
```

**注意事項**:
- `id`フィールドは数値（連番、1から開始）
- アイテム追加時に自動的に次の連番が割り当てられます
- 削除後もIDは再利用されず、常に最大値+1が次のIDとなります
- 各フレームワークの実装では、このデータ形式を読み込んで使用してください

#### 4. 計測環境

測定は以下の環境で実施する：

##### ハードウェア

- **CPU**: 13th Gen Intel Core i5-13400F（10コア / 16スレッド）
- **メモリ**: 48 GB（DDR4-3200）
- **ストレージ**: NVMe SSD 1TB × 2（INTEL SSDPEKNU010TZ / WD Blue SN580）
- **GPU**: NVIDIA GeForce RTX 3060 Ti

##### ソフトウェア

- **OS**: Windows 11 Home（64bit, Build 26200）
- **.NET Runtime**: .NET 8 / 9 / 10（SDK 10.0.302）
- **Node.js**: v22.11.0（npm 11.0.0）
- **Python**: 3.10.6 / 3.11.3
- **その他**: Flutter 3.38.5（Dart 3.10.4）、Rust 1.91.1（cargo 1.91.1）、Electron 41.7.1

##### 測定ツール

- **メモリ測定**: Windows Task Manager / Process Explorer / PerfView
- **CPU測定**: Windows Task Manager / Process Explorer / PerfView
- **起動時間測定**: ストップウォッチ / カスタムスクリプト
- **その他**: PowerShell（`Get-Process` / `Measure-Command`）、Windows Performance Recorder（WPR）

##### 測定条件

- 測定前にシステムを再起動
- バックグラウンドアプリケーションを最小限に実行
- 同じテストデータセットを使用（`data/` ディレクトリ参照）
- 各測定は3回実施し、平均値を記録
- 測定時のウィンドウサイズ: デフォルトサイズ

### 比較項目

#### 実装面

- コード行数（LOC）
- 依存関係の数とサイズ
- ビルド時間
- バンドルサイズ（実行ファイルサイズ）

#### 実行時パフォーマンス

- メモリ使用量（各種状態での測定値）
- CPU使用率（各種操作での測定値）
- 起動時間
- UI応答性

#### 開発体験

- 開発の容易さ
- ドキュメントの充実度
- コミュニティサポート
- テスタビリティ（UIテストの容易さ、自動化のしやすさなど）

## 比較結果

### WinForms除外理由

WinFormsは比較対象から除外しています。理由は以下の通りです：

- **デザインの自由度不足**: WinFormsでは、一般的なモダンUIを作成することができない。フレームワーク自体が持つ制約により、テーブルヘッダーへのフィルタアイコンの配置や、モダンなUIパターンの実装が困難または不可能である

上記の理由により、WinFormsは実用的な比較対象として不適切と判断し、除外しています。

### React Native for Desktop除外理由

React Native for Desktopは比較対象から除外しています。理由は以下の通りです：

- **情報不足**: React Native for Desktopに関する情報がほとんどなく、実用的な開発を行うことができない。ドキュメントやコミュニティサポートが不十分であり、本プロジェクトの要件を満たす実装が困難である

上記の理由により、React Native for Desktopは実用的な比較対象として不適切と判断し、除外しています。

### MAUI除外理由

MAUIは比較対象から除外しています。理由は以下の通りです：

- **未実装**: 本リポジトリに MAUI 実装がなく、比較用アプリが存在しない
- **WinUI3との重複**: Windows 上では WinUI3 のラッパーに近く、WinUI3 を比較対象に含めているため、別途比較する優先度が低い
- **標準コンポーネントの不足**: 標準で DataGrid が提供されておらず、CommunityToolkit 等の追加導入が必要となる

上記の理由により、MAUIは実用的な比較対象として不適切と判断し、除外しています。

### GPUI除外理由

GPUIは比較対象から除外しています。理由は以下の通りです：

- **ファイルピッカーなどの機能不足**: GPUIにはファイルピッカーなどの標準的なプラットフォーム統合機能が提供されていない。本プロジェクトの要件であるエクスポート/インポート機能や、データファイルの読み込みに必要なファイル選択機能を実装することが困難である

上記の理由により、GPUIは実用的な比較対象として不適切と判断し、除外しています。

### 機能面の比較

#### 基本機能

| Framework             | CRUD | Filtering | Persistence | Search | Sort | Notes                        |
| --------------------- | ---- | --------- | ----------- | ------ | ---- | ---------------------------- |
| Avalonia              | Y    | Y         | Y           | Y      | Y    |                              |
| Compose Multiplatform | Y    | Y         | Y           | Y      | Y    |                              |
| Electron              | Y    | Y         | Y           | Y      | Y    |                              |
| Flutter               | Y    | Y         | Y           | Y      | Y    |                              |
| GPUI                  | -    | -         | -           | -      | -    | Excluded (no file picker)    |
| MAUI                  | -    | -         | -           | -      | -    | Excluded (WinUI3 wrapper)    |
| React Native          | -    | -         | -           | -      | -    | Excluded (insufficient info) |
| Tauri                 | Y    | Y         | Y           | Y      | Y    |                              |
| WinForms              | -    | -         | -           | -      | -    | Excluded (limited UI)        |
| WPF                   | Y    | Y         | Y           | Y      | Y    |                              |
| WinUI3                | Y    | Y         | Y           | Y      | Y    |                              |
| wxWidgets             | Y    | Y         | Y           | Y      | Y    |                              |

**ヘッダー**:
- Framework: フレームワーク
- CRUD: 作成・読取・更新・削除
- Filtering: フィルタリング
- Persistence: データ永続化
- Search: 検索
- Sort: ソート
- Notes: 備考

**凡例**: `Y` 実装済み / `N` 未実装 / `P` 部分的 / `-` 未確認（除外は Notes 参照）

**Notes**:
- Filtering: ステータス / 優先度フィルタは実装済み。日付範囲フィルタ UI は各アプリとも未実装
- Search: FilterBar によるタイトル / 説明のテキスト検索
- WinUI3: CommunityToolkit DataGrid を使用してテーブル UI を実装

#### ウィンドウ機能

| Framework             | Multi-window | Resize | Position memory | Drag & drop | Transparency | Notes                                                 |
| --------------------- | ------------ | ------ | --------------- | ----------- | ------------ | ----------------------------------------------------- |
| Avalonia              | Y            | Y      | Y               | Y           | Y            |                                                       |
| Compose Multiplatform | Y            | Y      | Y               | Y           | P            | FW: limited window transparency on Compose Desktop    |
| Electron              | Y            | Y      | Y               | Y           | Y            |                                                       |
| Flutter               | N            | Y      | Y               | Y           | P            | FW: no first-class multi-window; transparency limited |
| GPUI                  | -            | -      | -               | -           | -            | Excluded (no file picker)                             |
| MAUI                  | -            | -      | -               | -           | -            | Excluded (WinUI3 wrapper)                             |
| React Native          | -            | -      | -               | -           | -            | Excluded (insufficient info)                          |
| Tauri                 | Y            | Y      | Y               | Y           | Y            |                                                       |
| WinForms              | -            | -      | -               | -           | -            | Excluded (limited UI)                                 |
| WPF                   | Y            | Y      | Y               | Y           | Y            |                                                       |
| WinUI3                | Y            | Y      | Y               | Y           | Y            | MicaBackdrop                                          |
| wxWidgets             | Y            | Y      | Y               | Y           | Y            |                                                       |

**ヘッダー**:
- Framework: フレームワーク
- Multi-window: マルチウィンドウ
- Resize: ウィンドウサイズ変更
- Position memory: ウィンドウ位置・サイズの記憶
- Drag & drop: ドラッグ&ドロップ
- Transparency: ウィンドウ透過
- Notes: 備考

**凡例**: `Y` 実装済み / `N` 未実装 / `P` 部分的 / `-` 未確認（除外は Notes 参照）

**Notes**:
- 各アプリ実装に基づく判定。`N`/`P` で `FW:` とあるものはフレームワーク制約
- Multi-window: 選択1件を独立ウィンドウで開く（モーダルは含めない）。Flutter は第一級 API がなく未実装
- Position memory: `window.json` に位置・サイズを保存し起動時復元
- Drag & drop: `.json` ドロップでインポート（既存 Import とパース共有）
- Transparency: Opacity 約 0.95 またはシステム背景（WinUI3 は Mica）。Compose / Flutter は OS・装飾付き窓での透過が限定的なため `P`

#### UI機能

| Framework             | Theme switch | Dark mode | Custom style | Animation | Responsive layout | Notes                                 |
| --------------------- | ------------ | --------- | ------------ | --------- | ----------------- | ------------------------------------- |
| Avalonia              | Y            | Y         | Y            | Y         | Y                 |                                       |
| Compose Multiplatform | Y            | Y         | Y            | Y         | Y                 |                                       |
| Electron              | Y            | Y         | Y            | Y         | Y                 |                                       |
| Flutter               | Y            | Y         | Y            | Y         | Y                 |                                       |
| GPUI                  | -            | -         | -            | -         | -                 | Excluded (no file picker)             |
| MAUI                  | -            | -         | -            | -         | -                 | Excluded (WinUI3 wrapper)             |
| React Native          | -            | -         | -            | -         | -                 | Excluded (insufficient info)          |
| Tauri                 | Y            | Y         | Y            | Y         | Y                 |                                       |
| WinForms              | -            | -         | -            | -         | -                 | Excluded (limited UI)                 |
| WPF                   | Y            | Y         | Y            | Y         | Y                 |                                       |
| WinUI3                | Y            | Y         | Y            | Y         | Y                 | App RequestedTheme (not system-only)  |
| wxWidgets             | Y            | Y         | Y            | P         | Y                 | FW: limited UI animation on wxPython  |

**ヘッダー**:
- Framework: フレームワーク
- Theme switch: テーマ切り替え
- Dark mode: ダークモード
- Custom style: カスタムスタイル
- Animation: アニメーション
- Responsive layout: レスポンシブレイアウト
- Notes: 備考

**凡例**: `Y` 実装済み / `N` 未実装 / `P` 部分的 / `-` 未確認（除外は Notes 参照）

**Notes**:
- 各アプリ実装に基づく判定
- Theme switch / Dark mode: ツールバーからライト/ダーク切替。`theme.json` に永続化
- Custom style: ブランド色（青 `#2563EB` / 赤 `#DC2626` / 灰背景系）をライト・ダーク双方に適用
- Animation: ダイアログ表示などの短いトランジション（約 150–200ms）。wx は `ShowWithEffect` の best-effort のため `P`
- Responsive layout: Toolbar / FilterBar の折り返し（Wrap / FlowRow / flex-wrap）

#### プラットフォーム統合機能

| Framework             | Notification | System tray | File association | Native dialog | Clipboard | Notes                        |
| --------------------- | ------------ | ----------- | ---------------- | ------------- | --------- | ---------------------------- |
| Avalonia              | -            | -           | -                | -             | -         | Pending                      |
| Compose Multiplatform | -            | -           | -                | -             | -         | Pending                      |
| Electron              | -            | -           | -                | -             | -         | Pending                      |
| Flutter               | -            | -           | -                | -             | -         | Pending                      |
| GPUI                  | -            | -           | -                | -             | -         | Excluded (no file picker)    |
| MAUI                  | -            | -           | -                | -             | -         | Excluded (WinUI3 wrapper)    |
| React Native          | -            | -           | -                | -             | -         | Excluded (insufficient info) |
| Tauri                 | -            | -           | -                | -             | -         | Pending                      |
| WinForms              | -            | -           | -                | -             | -         | Excluded (limited UI)        |
| WPF                   | -            | -           | -                | -             | -         | Pending                      |
| WinUI3                | -            | -           | -                | -             | -         | Pending                      |
| wxWidgets             | -            | -           | -                | -             | -         | Pending                      |

**ヘッダー**:
- Framework: フレームワーク
- Notification: システム通知
- System tray: システムトレイ
- File association: ファイル関連付け
- Native dialog: ネイティブダイアログ
- Clipboard: クリップボード
- Notes: 備考

**凡例**: `Y` 実装済み / `N` 未実装 / `P` 部分的 / `-` 未確認（除外は Notes 参照）

#### パフォーマンス機能

| Framework             | Virtual list | Lazy load | Background work | Async | Memory opt. | Notes                        |
| --------------------- | ------------ | --------- | --------------- | ----- | ----------- | ---------------------------- |
| Avalonia              | -            | -         | -               | -     | -           | Pending                      |
| Compose Multiplatform | -            | -         | -               | -     | -           | Pending                      |
| Electron              | -            | -         | -               | -     | -           | Pending                      |
| Flutter               | -            | -         | -               | -     | -           | Pending                      |
| GPUI                  | -            | -         | -               | -     | -           | Excluded (no file picker)    |
| MAUI                  | -            | -         | -               | -     | -           | Excluded (WinUI3 wrapper)    |
| React Native          | -            | -         | -               | -     | -           | Excluded (insufficient info) |
| Tauri                 | -            | -         | -               | -     | -           | Pending                      |
| WinForms              | -            | -         | -               | -     | -           | Excluded (limited UI)        |
| WPF                   | -            | -         | -               | -     | -           | Pending                      |
| WinUI3                | -            | -         | -               | -     | -           | Pending                      |
| wxWidgets             | -            | -         | -               | -     | -           | Pending                      |

**ヘッダー**:
- Framework: フレームワーク
- Virtual list: 仮想化リスト
- Lazy load: 遅延読み込み
- Background work: バックグラウンド処理
- Async: 非同期処理
- Memory opt.: メモリ最適化
- Notes: 備考

**凡例**: `Y` 実装済み / `N` 未実装 / `P` 部分的 / `-` 未確認（除外は Notes 参照）

### 実装面の比較

| Framework             | LOC | Dependencies | Build time (s) | Bundle size (MB) | Notes                        |
| --------------------- | --- | ------------ | -------------- | ---------------- | ---------------------------- |
| Avalonia              | -   | -            | -              | -                | Pending                      |
| Compose Multiplatform | -   | -            | -              | -                | Pending                      |
| Electron              | -   | -            | -              | -                | Pending                      |
| Flutter               | -   | -            | -              | -                | Pending                      |
| GPUI                  | -   | -            | -              | -                | Pending                      |
| MAUI                  | -   | -            | -              | -                | Excluded (WinUI3 wrapper)    |
| React Native          | -   | -            | -              | -                | Excluded (insufficient info) |
| Tauri                 | -   | -            | -              | -                | Pending                      |
| WinForms              | -   | -            | -              | -                | Excluded (limited UI)        |
| WPF                   | -   | -            | -              | -                | Pending                      |
| WinUI3                | -   | -            | -              | -                | Pending                      |
| wxWidgets             | -   | -            | -              | -                | Pending                      |

**ヘッダー**:
- Framework: フレームワーク
- LOC: コード行数
- Dependencies: 依存関係数
- Build time (s): ビルド時間（秒）
- Bundle size (MB): バンドルサイズ（MB）
- Notes: 備考

### 実行時パフォーマンスの比較

#### メモリ使用量（MB）

| Framework             | Startup (empty) | After 10 | After 100 | After 1000 | Peak                         |
| --------------------- | --------------- | -------- | --------- | ---------- | ---------------------------- |
| Avalonia              | -               | -        | -         | -          | -                            |
| Compose Multiplatform | -               | -        | -         | -          | -                            |
| Electron              | -               | -        | -         | -          | -                            |
| Flutter               | -               | -        | -         | -          | -                            |
| GPUI                  | -               | -        | -         | -          | -                            |
| MAUI                  | -               | -        | -         | -          | Excluded (WinUI3 wrapper)    |
| React Native          | -               | -        | -         | -          | Excluded (insufficient info) |
| Tauri                 | -               | -        | -         | -          | -                            |
| WinForms              | -               | -        | -         | -          | Excluded (limited UI)        |
| WPF                   | -               | -        | -         | -          | -                            |
| WinUI3                | -               | -        | -         | -          | Pending                      |
| wxWidgets             | -               | -        | -         | -          | -                            |

**ヘッダー**:
- Framework: フレームワーク
- Startup (empty): 起動直後（空）
- After 10: 10件追加後
- After 100: 100件追加後
- After 1000: 1000件追加後
- Peak: ピーク使用量

#### CPU使用率（%）

| Framework             | Idle | Add | Scroll | Filtering | Peak                         |
| --------------------- | ---- | --- | ------ | --------- | ---------------------------- |
| Avalonia              | -    | -   | -      | -         | -                            |
| Compose Multiplatform | -    | -   | -      | -         | -                            |
| Electron              | -    | -   | -      | -         | -                            |
| Flutter               | -    | -   | -      | -         | -                            |
| GPUI                  | -    | -   | -      | -         | -                            |
| MAUI                  | -    | -   | -      | -         | Excluded (WinUI3 wrapper)    |
| React Native          | -    | -   | -      | -         | Excluded (insufficient info) |
| Tauri                 | -    | -   | -      | -         | -                            |
| WinForms              | -    | -   | -      | -         | Excluded (limited UI)        |
| WPF                   | -    | -   | -      | -         | -                            |
| WinUI3                | -    | -   | -      | -         | Pending                      |
| wxWidgets             | -    | -   | -      | -         | -                            |

**ヘッダー**:
- Framework: フレームワーク
- Idle: アイドル時
- Add: 追加操作時
- Scroll: スクロール時
- Filtering: フィルタリング時
- Peak: ピーク使用率

#### 起動時間とUI応答性

| Framework             | Startup (s) | Render 1000 (s) | Scroll FPS | Filter response (ms)         |
| --------------------- | ----------- | --------------- | ---------- | ---------------------------- |
| Avalonia              | -           | -               | -          | -                            |
| Compose Multiplatform | -           | -               | -          | -                            |
| Electron              | -           | -               | -          | -                            |
| Flutter               | -           | -               | -          | -                            |
| GPUI                  | -           | -               | -          | -                            |
| MAUI                  | -           | -               | -          | Excluded (WinUI3 wrapper)    |
| React Native          | -           | -               | -          | Excluded (insufficient info) |
| Tauri                 | -           | -               | -          | -                            |
| WinForms              | -           | -               | -          | Excluded (limited UI)        |
| WPF                   | -           | -               | -          | -                            |
| WinUI3                | -           | -               | -          | Pending                      |
| wxWidgets             | -           | -               | -          | -                            |

**ヘッダー**:
- Framework: フレームワーク
- Startup (s): 起動時間（秒）
- Render 1000 (s): 1000件表示時間（秒）
- Scroll FPS: スクロール時 FPS
- Filter response (ms): フィルタリング応答時間（ms）

### 開発体験の評価

| Framework             | Ease of use | Docs | Community | Testability | Overall                      |
| --------------------- | ----------- | ---- | --------- | ----------- | ---------------------------- |
| Avalonia              | -           | -    | -         | -           | -                            |
| Compose Multiplatform | -           | -    | -         | -           | -                            |
| Electron              | -           | -    | -         | -           | -                            |
| Flutter               | -           | -    | -         | -           | -                            |
| GPUI                  | -           | -    | -         | -           | -                            |
| MAUI                  | -           | -    | -         | -           | Excluded (WinUI3 wrapper)    |
| React Native          | -           | -    | -         | -           | Excluded (insufficient info) |
| Tauri                 | -           | -    | -         | -           | -                            |
| WinForms              | -           | -    | -         | -           | Excluded (limited UI)        |
| WPF                   | -           | -    | -         | -           | -                            |
| WinUI3                | -           | -    | -         | -           | Pending                      |
| wxWidgets             | -           | -    | -         | -           | -                            |

**ヘッダー**:
- Framework: フレームワーク
- Ease of use: 開発の容易さ
- Docs: ドキュメント
- Community: コミュニティ
- Testability: テスタビリティ
- Overall: 総合評価

**評価基準**:

- Ease of use: `*****`（5段階）
- Docs: `*****`（5段階）
- Community: `*****`（5段階）
- Testability: `*****`（5段階） - UIテスト自動化のしやすさ、ツール成熟度、テスト用フック（AutomationId / semantics など）
- Overall: `*****`（5段階）
