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
- **CPU**: （未設定）
- **メモリ**: （未設定）
- **ストレージ**: （未設定）
- **GPU**: （未設定）

##### ソフトウェア
- **OS**: Windows 10/11（64bit）
- **.NET Runtime**: （未設定）
- **Node.js**: （未設定）
- **Python**: （未設定）
- **その他**: （未設定）

##### 測定ツール
- **メモリ測定**: Windows Task Manager / Process Explorer / PerfView
- **CPU測定**: Windows Task Manager / Process Explorer / PerfView
- **起動時間測定**: ストップウォッチ / カスタムスクリプト
- **その他**: （未設定）

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

### WinUI3除外理由

WinUI3は比較対象から除外しています。理由は以下の通りです：

- **情報不足**: WinUI3に関する情報が少なく、実用的な開発を行うことが困難である。ドキュメントやコミュニティサポートが不十分である
- **標準コンポーネントの不足**: WinUI3には標準でDataGridコンポーネントが提供されていないため、本プロジェクトの要件であるテーブル形式のUIを実装するには、カスタムコンポーネントの実装が必要となる
- **フレームワークの成熟度**: WinUI3はまだ準備が整っていない（unreadyな）フレームワークであり、本プロジェクトの要件を満たす実装が困難である

上記の理由により、WinUI3は実用的な比較対象として不適切と判断し、除外しています。

### MAUI除外理由

MAUIは比較対象から除外しています。理由は以下の通りです：

- **WinUI3のラッパー**: MAUIはWinUI3のラッパーであり、WinUI3と同様の問題を抱えている。WinUI3が持つ制約がそのままMAUIにも影響する
- **標準コンポーネントの不足**: WinUI3ベースのため、標準でDataGridコンポーネントが提供されていない。本プロジェクトの要件であるテーブル形式のUIを実装するには、カスタムコンポーネントの実装が必要となる
- **フレームワークの成熟度**: WinUI3と同様に、まだ準備が整っていない（unreadyな）フレームワークであり、本プロジェクトの要件を満たす実装が困難である

上記の理由により、MAUIは実用的な比較対象として不適切と判断し、除外しています。

### GPUI除外理由

GPUIは比較対象から除外しています。理由は以下の通りです：

- **ファイルピッカーなどの機能不足**: GPUIにはファイルピッカーなどの標準的なプラットフォーム統合機能が提供されていない。本プロジェクトの要件であるエクスポート/インポート機能や、データファイルの読み込みに必要なファイル選択機能を実装することが困難である

上記の理由により、GPUIは実用的な比較対象として不適切と判断し、除外しています。

### 機能面の比較

#### 基本機能

| Framework             | CRUD | Filtering | Persistence | Search | Sort | Notes                        |
| --------------------- | ---- | --------- | ----------- | ------ | ---- | ---------------------------- |
| Avalonia              | -    | -         | -           | -      | -    | Pending                      |
| Compose Multiplatform | -    | -         | -           | -      | -    | Pending                      |
| Electron              | -    | -         | -           | -      | -    | Pending                      |
| Flutter               | -    | -         | -           | -      | -    | Pending                      |
| GPUI                  | -    | -         | -           | -      | -    | Excluded (no file picker)    |
| MAUI                  | -    | -         | -           | -      | -    | Excluded (WinUI3 wrapper)    |
| React Native          | -    | -         | -           | -      | -    | Excluded (insufficient info) |
| Tauri                 | -    | -         | -           | -      | -    | Pending                      |
| WinForms              | -    | -         | -           | -      | -    | Excluded (limited UI)        |
| WPF                   | -    | -         | -           | -      | -    | Pending                      |
| WinUI3                | -    | -         | -           | -      | -    | Excluded (no DataGrid)       |
| wxWidgets             | -    | -         | -           | -      | -    | Pending                      |

**Legend**: ✅ Implemented / ❌ Not implemented / ⚠️ Partial / - Not checked

#### ウィンドウ機能

| Framework             | Multi-window | Resize | Position memory | Drag & drop | Transparency | Notes                        |
| --------------------- | ------------ | ------ | --------------- | ----------- | ------------ | ---------------------------- |
| Avalonia              | -            | -      | -               | -           | -            | Pending                      |
| Compose Multiplatform | -            | -      | -               | -           | -            | Pending                      |
| Electron              | -            | -      | -               | -           | -            | Pending                      |
| Flutter               | -            | -      | -               | -           | -            | Pending                      |
| GPUI                  | -            | -      | -               | -           | -            | Excluded (no file picker)    |
| MAUI                  | -            | -      | -               | -           | -            | Excluded (WinUI3 wrapper)    |
| React Native          | -            | -      | -               | -           | -            | Excluded (insufficient info) |
| Tauri                 | -            | -      | -               | -           | -            | Pending                      |
| WinForms              | -            | -      | -               | -           | -            | Excluded (limited UI)        |
| WPF                   | -            | -      | -               | -           | -            | Pending                      |
| WinUI3                | -            | -      | -               | -           | -            | Excluded (no DataGrid)       |
| wxWidgets             | -            | -      | -               | -           | -            | Pending                      |

**Legend**: ✅ Supported / ❌ Not supported / ⚠️ Partial / - Not checked

#### UI機能

| Framework             | Theme switch | Dark mode | Custom style | Animation | Responsive layout | Notes                        |
| --------------------- | ------------ | --------- | ------------ | --------- | ----------------- | ---------------------------- |
| Avalonia              | -            | -         | -            | -         | -                 | Pending                      |
| Compose Multiplatform | -            | -         | -            | -         | -                 | Pending                      |
| Electron              | -            | -         | -            | -         | -                 | Pending                      |
| Flutter               | -            | -         | -            | -         | -                 | Pending                      |
| GPUI                  | -            | -         | -            | -         | -                 | Excluded (no file picker)    |
| MAUI                  | -            | -         | -            | -         | -                 | Excluded (WinUI3 wrapper)    |
| React Native          | -            | -         | -            | -         | -                 | Excluded (insufficient info) |
| Tauri                 | -            | -         | -            | -         | -                 | Pending                      |
| WinForms              | -            | -         | -            | -         | -                 | Excluded (limited UI)        |
| WPF                   | -            | -         | -            | -         | -                 | Pending                      |
| WinUI3                | -            | -         | -            | -         | -                 | Excluded (no DataGrid)       |
| wxWidgets             | -            | -         | -            | -         | -                 | Pending                      |

**Legend**: ✅ Supported / ❌ Not supported / ⚠️ Partial / - Not checked

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
| WinUI3                | -            | -           | -                | -             | -         | Excluded (no DataGrid)       |
| wxWidgets             | -            | -           | -                | -             | -         | Pending                      |

**Legend**: ✅ Supported / ❌ Not supported / ⚠️ Partial / - Not checked

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
| WinUI3                | -            | -         | -               | -     | -           | Excluded (no DataGrid)       |
| wxWidgets             | -            | -         | -               | -     | -           | Pending                      |

**Legend**: ✅ Supported / ❌ Not supported / ⚠️ Partial / - Not checked

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
| WinUI3                | -   | -            | -              | -                | Excluded (no DataGrid)       |
| wxWidgets             | -   | -            | -              | -                | Pending                      |

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
| WinUI3                | -               | -        | -         | -          | Excluded (no DataGrid)       |
| wxWidgets             | -               | -        | -         | -          | -                            |

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
| WinUI3                | -    | -   | -      | -         | Excluded (no DataGrid)       |
| wxWidgets             | -    | -   | -      | -         | -                            |

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
| WinUI3                | -           | -               | -          | Excluded (no DataGrid)       |
| wxWidgets             | -           | -               | -          | -                            |

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
| WinUI3                | -           | -    | -         | -           | Excluded (no DataGrid)       |
| wxWidgets             | -           | -    | -         | -           | -                            |

**Rating criteria**:
- Ease of use: ⭐⭐⭐⭐⭐ (5-point scale)
- Docs: ⭐⭐⭐⭐⭐ (5-point scale)
- Community: ⭐⭐⭐⭐⭐ (5-point scale)
- Testability: ⭐⭐⭐⭐⭐ (5-point scale) — UI test automation ease, tooling maturity, testability hooks (e.g. AutomationId / semantics)
- Overall: ⭐⭐⭐⭐⭐ (5-point scale)
