# Todo App

モダンなデスクトップ向けフレームワークで、同じ Todo（テーブル型プロジェクト管理）アプリを実装し、機能・実装コスト・実行時パフォーマンス・開発体験を比較するリポジトリです。

## 総評

同一機能を **8 フレームワーク**（Avalonia / Compose Multiplatform / Electron / Flutter / Tauri / WPF / WinUI3 / wxWidgets）で実装し、Windows 上で計測した（2026-08）。GPUI / MAUI / React Native / WinForms は要件または重複のため除外。

読み手が先に知りたい結論だけをここに置く。詳細な表・測定条件・除外理由は後続セクションを参照。

### 総合ランキング

実装 8 FW を、次の **5 軸を等重み**（各 1〜5・高いほど良い・相対正規化）で平均し順位付けした。

| Axis           | What it covers                                                       |
| -------------- | -------------------------------------------------------------------- |
| Runtime        | Memory peak + CPU peak (lower is better → higher score)              |
| Responsiveness | Startup, Render 1000, Scroll FPS, Filter (faster/smoother → higher)  |
| Footprint      | Bundle size (smaller → higher; Tauri excludes WebView2 runtime)      |
| DX             | Developer-experience Overall (relative within this repo)             |
| Features       | Desktop capability gaps vs this app's requirements (multi-window, …) |

| Rank | Framework             | Score | Runtime | Resp. | Footprint | DX  | Feat. | Blurb (EN)                                      |
| ---- | --------------------- | ----- | ------- | ----- | --------- | --- | ----- | ----------------------------------------------- |
| 1    | Tauri                 | 4.3   | 3.5     | 5     | 5         | 3   | 5     | Best overall after weighting desktop features   |
| 2    | wxWidgets             | 3.8   | 5       | 4     | 4.5       | 1   | 4.5   | Lightest runtime; full windows; weak DX         |
| 3    | Flutter               | 3.8   | 3.5     | 5     | 4.5       | 4   | 2     | Strong elsewhere; multi-window `N` is a big hit |
| 4    | Electron              | 3.7   | 2.5     | 5     | 1         | 5   | 5     | DX/UI ceiling; complete desktop features        |
| 5    | Avalonia              | 3.7   | 4       | 3.5   | 3.5       | 3   | 4.5   | Steady .NET cross-platform middle ground        |
| 6    | WinUI3                | 3.3   | 4       | 2.5   | 4         | 1   | 5     | Lean memory + full features; Appx DX hurts      |
| 7    | WPF                   | 3.1   | 4       | 2     | 2         | 3   | 4.5   | Mature; weak scroll/filter latency              |
| 8    | Compose Multiplatform | 2.6   | 1       | 3     | 3         | 2   | 4     | Nice UI; heavy RAM and desktop friction         |

**軸の解説**:
- **Runtime**: メモリ Peak・CPU Peak。軽い／低いほど高得点
- **Responsiveness**: Startup・Render 1000・Scroll FPS・Filter。速い／滑らかほど高得点
- **Footprint**: Bundle size。小さいほど高得点（Tauri は WebView2 ランタイム別配布）
- **DX**: 開発体験 Overall（本リポ相対評価）
- **Features**: 本アプリ要件に対するデスクトップ機能の欠落。マルチウィンドウ `N`、透過 `P`、ファイル関連付け `P` などを減点（Flutter の multi-window 非対応はここが大きく効く）

**順位の解説**:
- **1 Tauri**: Features を入れたあと総合首位。配布・CPU・デスクトップ機能が揃う
- **2 wxWidgets**: 実行時は最軽量級でウィンドウ機能も充足。DX が足を引っ張る（同点 Flutter より Features で上位）
- **3 Flutter**: 起動・UI・DX・サイズは強いが、**第一級マルチウィンドウ非対応**で Features を大きく減点
- **4 Electron**: DX・UI・機能は天井。バンドルが重い
- **5 Avalonia**: .NET クロスプラットフォームの堅実枠
- **6 WinUI3**: メモリと機能は良いが Appx DX と Render が弱い
- **7 WPF**: 成熟だがスクロール／フィルタ応答が弱い
- **8 Compose Multiplatform**: UI は良いがメモリと desktop 摩擦が大きい

**読み方**:
- 「どれか一つ」なら **Tauri**（機能込みの総合）。マルチウィンドウが不要で DX を取るなら **Flutter** / **Electron**
- **Electron** は総合 4 位でも DX 最優先なら実質の本命になりうる（用途別表を参照）
- スコアは本リポ条件での相対値であり、絶対的な業界順位ではない

### ひと目で見る結果

| Framework             | Mem peak (MB) | CPU peak (%) | Startup (s) | Bundle (MB) | DX    | Snapshot (EN)                           |
| --------------------- | ------------- | ------------ | ----------- | ----------- | ----- | --------------------------------------- |
| Avalonia              | 118           | 8.8          | 1.21        | 37          | ***   | Solid .NET cross-platform; mid DX       |
| Compose Multiplatform | 399           | 10.7         | 1.34        | 45          | **    | Smooth UI; heavy RAM / desktop friction |
| Electron              | 252           | 8.0          | 0.48        | 293         | ***** | Best DX; large bundle / mid memory      |
| Flutter               | 124           | 13.4         | 0.26        | 27          | ****  | Fast start; no first-class multi-window |
| Tauri                 | 189           | 4.9          | 0.44        | 11          | ***   | Smallest bundle; low CPU; Rust cost     |
| WPF                   | 114           | 8.0          | 0.93        | 155         | ***   | Mature .NET; weak scroll/filter latency |
| WinUI3                | 97            | 7.0          | 0.60        | 30          | *     | Lean memory; Appx DX is heaviest        |
| wxWidgets             | 23            | 5.8          | 0.22        | 24          | *     | Lightest RAM / start; weak ecosystem    |

（数値は詳細表の Peak / Startup / Bundle / DX Overall。DX は実装 8 FW 内の相対評価。）

### 文章でのまとめ

- **メモリ**: wx が突出して軽い（Peak 23 MB）。続けて WinUI / WPF / Avalonia / Flutter が ~100 MB 帯。Compose が最も重い（~400 MB）。Electron / Tauri は WebView 込みで中〜大。
- **CPU**: アイドルはいずれも低い。負荷ピークは Flutter の Add/Filter がやや高く、Tauri / wx は抑えめ。差は「どの操作が重いか」の違いが大きい。
- **起動・UI**: 起動は wx / Flutter が速い。1000 件描画は Web 系と Compose / Flutter が速い一方、WinUI DataGrid は遅め、WPF はスクロール FPS・フィルタ応答が弱い。
- **配布サイズ**: Tauri が最小（11 MB、WebView2 ランタイム別）。Electron が最大（~293 MB）。.NET self-contained はトリム有無で差が出る。
- **開発体験**: Electron > Flutter ≫ Avalonia / Tauri / WPF > Compose ≫ WinUI / wx。Web スキル流用と公式エコシステムが効く。WinUI は Appx、Compose は Gradle/配布、wx は情報量・テスト基盤がボトルネック。
- **機能面**: 実装 8 FW は基本 CRUD 等を概ね同水準。**Flutter のみマルチウィンドウがフレームワーク制約で `N`**（デスクトップ用途では大きい欠落）。ほか透過 `P` やファイル関連付け `P` なども差が出る

### 用途別の目安

| Priority                        | Picks              | Why (EN)                                         |
| ------------------------------- | ------------------ | ------------------------------------------------ |
| Fast hiring / shipping velocity | Electron, Flutter  | Strong DX, docs, and UI-test tooling             |
| Need multi-window desktop UX    | Tauri, Electron, … | Avoid Flutter here (no first-class multi-window) |
| Minimize memory                 | wxWidgets, WinUI3  | Lowest peaks (wx lightest)                       |
| Minimize install size           | Tauri              | Smallest exe (WebView2 runtime separate)         |
| Reuse .NET / XAML skills        | Avalonia, WPF      | Easy MVVM transfer; watch WPF UI latency         |
| Balance size + CPU              | Tauri, Flutter     | Small–mid bundle; relatively fast startup        |
| Native Windows look             | WinUI3             | Good memory; packaging DX is heavy               |

**用途別の解説**:
- **開発速度・人材調達**: Electron / Flutter。DX・ドキュメント・UI テストが強い（マルチウィンドウ必須なら Flutter は避ける）
- **マルチウィンドウ必須**: Tauri / Electron ほか（Flutter は第一級 API がなく非対応）
- **メモリ最小化**: wxWidgets / WinUI3。Peak が小さい（wx が最軽量）
- **配布サイズ最小化**: Tauri。exe が最小（ランタイム前提に注意）
- **.NET / XAML 資産の活用**: Avalonia / WPF。MVVM 転用しやすい。WPF は UI 応答に注意
- **バランス（サイズ+CPU）**: Tauri / Flutter。小〜中バンドル、起動も比較的速い
- **Windows ネイティブ見た目**: WinUI3。メモリは良いがパッケージ開発体験は重い

**注意**: 総評は「同じ Todo をこのリポで実装・計測した結果」に限定される。アプリ規模・チームスキル・ターゲット OS が変われば最適解も変わる。

### この README の読み方

1. **総評**（本節）→ 総合ランキングと用途別の当たりをつける
2. **[実行バイナリの場所](#実行バイナリの場所windows)** → 各アプリの起動パス
3. **[比較結果](#比較結果)** → 機能・LOC・メモリ・CPU・UI・DX の詳細表
4. **[内容](#内容)** → 要件・測定シナリオ・比較項目の定義（実装前スペック）

### 実行バイナリの場所（Windows）

計測・手元確認で使う Release 成果物のパス（リポジトリルート相対）。無い場合は各プロジェクトで Release ビルドしてから起動する。

- **Avalonia**: `ToDoApp.Avalonia/bin/Release/net9.0/win-x64/ToDoApp.Avalonia.exe`
- **Compose Multiplatform**: `ToDoApp.KotlinMultiplatform/composeApp/build/compose/binaries/main-release/app/com.example.todoappkotlinmultiplatform/com.example.todoappkotlinmultiplatform.exe`
- **Electron**: `ToDoApp.Electron/release-measure/win-unpacked/Todo App.exe`
- **Flutter**: `todoapp_flutter/build/windows/x64/runner/Release/todoapp_flutter.exe`
- **Tauri**: `ToDoApp.Tauri/src-tauri/target/release/todoapp-tauri.exe`
- **WPF**: `ToDoApp.Wpf/bin/Release/net8.0-windows/win-x64/ToDoApp.Wpf.exe`
- **WinUI3**: `ToDoApp.WinUI/bin/x64/Release/net8.0-windows10.0.19041.0/win-x64/`（`ToDoApp.WinUI.exe` 直起動は不可。下記参照）
- **wxWidgets**: `ToDoApp.wxWidgets/dist/main.dist/TodoApp.exe`（フォルダごと必要。onefile 単体は Defender 誤検知しやすい）

**起動時の注意（日本語）**:
- **Flutter**: 上記 exe をダブルクリック、または `cd todoapp_flutter` → `flutter build windows --release` 後に同じパス。開発中は `flutter run -d windows`
- **Tauri**: 上記 exe。開発中は `cd ToDoApp.Tauri` → `npm run tauri dev`（WebView2 必須）
- **WinUI3**: exe 直起動は `0xC000027B` で落ちるため `.\scripts\register_winui.ps1 -Launch`（要: 先に Release ビルドで `AppxManifest.xml` があること）
- **wxWidgets**: standalone は `dist/main.dist/TodoApp.exe`。ソース確認は `cd ToDoApp.wxWidgets` → `uv run python main.py`（UI/CPU 計測と同系統）
- **Electron**: `release-measure/win-unpacked` は計測用出力。無い場合はプロジェクト側で `electron-builder` 等の Release を再生成
- **Compose**: Gradle の `createReleaseDistributable` / `packageReleaseDistributionForCurrentOS` 後の上記 exe

---

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

| Framework             | Theme switch | Dark mode | Custom style | Animation | Responsive layout | Notes                                |
| --------------------- | ------------ | --------- | ------------ | --------- | ----------------- | ------------------------------------ |
| Avalonia              | Y            | Y         | Y            | Y         | Y                 |                                      |
| Compose Multiplatform | Y            | Y         | Y            | Y         | Y                 |                                      |
| Electron              | Y            | Y         | Y            | Y         | Y                 |                                      |
| Flutter               | Y            | Y         | Y            | Y         | Y                 |                                      |
| GPUI                  | -            | -         | -            | -         | -                 | Excluded (no file picker)            |
| MAUI                  | -            | -         | -            | -         | -                 | Excluded (WinUI3 wrapper)            |
| React Native          | -            | -         | -            | -         | -                 | Excluded (insufficient info)         |
| Tauri                 | Y            | Y         | Y            | Y         | Y                 |                                      |
| WinForms              | -            | -         | -            | -         | -                 | Excluded (limited UI)                |
| WPF                   | Y            | Y         | Y            | Y         | Y                 |                                      |
| WinUI3                | Y            | Y         | Y            | Y         | Y                 | App RequestedTheme (not system-only) |
| wxWidgets             | Y            | Y         | Y            | P         | Y                 | FW: limited UI animation on wxPython |

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

| Framework             | Notification | System tray | File association | Native dialog | Clipboard | Notes                                               |
| --------------------- | ------------ | ----------- | ---------------- | ------------- | --------- | --------------------------------------------------- |
| Avalonia              | Y            | Y           | P                | Y             | Y         | P: argv open only; no installer registration        |
| Compose Multiplatform | Y            | Y           | P                | Y             | Y         | P: argv open only; no installer registration        |
| Electron              | Y            | Y           | Y                | Y             | Y         | fileAssociations + open-file / second-instance      |
| Flutter               | Y            | Y           | P                | Y             | Y         | P: argv open only; no installer registration        |
| GPUI                  | -            | -           | -                | -             | -         | Excluded (no file picker)                           |
| MAUI                  | -            | -           | -                | -             | -         | Excluded (WinUI3 wrapper)                           |
| React Native          | -            | -           | -                | -             | -         | Excluded (insufficient info)                        |
| Tauri                 | Y            | Y           | Y                | Y             | Y         | fileAssociations + CLI / open-file                  |
| WinForms              | -            | -           | -                | -             | -         | Excluded (limited UI)                               |
| WPF                   | Y            | Y           | P                | Y             | Y         | P: argv open only; no installer registration        |
| WinUI3                | Y            | Y           | Y                | Y             | Y         | Package FileTypeAssociation; Win32 tray best-effort |
| wxWidgets             | Y            | Y           | P                | Y             | Y         | P: argv open only; no installer registration        |

**ヘッダー**:
- Framework: フレームワーク
- Notification: システム通知
- System tray: システムトレイ
- File association: ファイル関連付け
- Native dialog: ネイティブダイアログ
- Clipboard: クリップボード
- Notes: 備考

**凡例**: `Y` 実装済み / `N` 未実装 / `P` 部分的 / `-` 未確認（除外は Notes 参照）

**Notes**:
- 各アプリ実装に基づく判定
- Notification: 手動保存・インポート完了時の OS トースト／バルーン
- System tray: 閉じるでトレイ退避、「表示」「終了」メニュー
- Clipboard: ツールバー「コピー」で選択アイテムを JSON テキスト化
- Native dialog: import/export の OS ファイルピッカー（従来どおり）
- File association: Electron / Tauri / WinUI はパッケージ関連付け `Y`。他は起動引数の `.json` を ImportFromPath する `P`（インストーラ登録なし）

#### パフォーマンス機能

| Framework             | Virtual list | Lazy load | Background work | Async | Memory opt. | Notes                        |
| --------------------- | ------------ | --------- | --------------- | ----- | ----------- | ---------------------------- |
| Avalonia              | Y            | Y         | Y               | Y     | P           |                              |
| Compose Multiplatform | Y            | Y         | Y               | Y     | P           |                              |
| Electron              | Y            | Y         | Y               | Y     | P           |                              |
| Flutter               | Y            | Y         | Y               | Y     | P           |                              |
| GPUI                  | -            | -         | -               | -     | -           | Excluded (no file picker)    |
| MAUI                  | -            | -         | -               | -     | -           | Excluded (WinUI3 wrapper)    |
| React Native          | -            | -         | -               | -     | -           | Excluded (insufficient info) |
| Tauri                 | Y            | Y         | Y               | Y     | P           |                              |
| WinForms              | -            | -         | -               | -     | -           | Excluded (limited UI)        |
| WPF                   | Y            | Y         | Y               | Y     | P           |                              |
| WinUI3                | Y            | Y         | Y               | Y     | P           |                              |
| wxWidgets             | Y            | Y         | Y               | Y     | P           |                              |

**ヘッダー**:
- Framework: フレームワーク
- Virtual list: 仮想化リスト
- Lazy load: 遅延読み込み
- Background work: バックグラウンド処理
- Async: 非同期処理
- Memory opt.: メモリ最適化
- Notes: 備考

**凡例**: `Y` 実装済み / `N` 未実装 / `P` 部分的 / `-` 未確認（除外は Notes 参照）

**Notes**:
- 各アプリ実装に基づく判定
- Virtual list: DataGrid / LazyColumn / ListView.builder / tanstack-virtual / wx.LC_VIRTUAL 等
- Lazy load: 全件はメモリ保持。表示は初期 100 件、末尾スクロールで +100。フィルタ／ソート変更時はリセット
- Background work: フィルタ／ソート（および保存）を UI スレッド外へ（Task.Run / Isolate / Dispatchers / Thread + CallAfter / scheduleWork）
- Async: ファイル I/O の非同期化（wx も worker + CallAfter）
- Memory opt.: 仮想化に加え Dispose／購読解除などの軽い最適化のみのため `P`（オブジェクトプール等は未実装）

### 実装面の比較

| Framework             | LOC  | Dependencies | Build time (s) | Bundle size (MB) | Notes                                          |
| --------------------- | ---- | ------------ | -------------- | ---------------- | ---------------------------------------------- |
| Avalonia              | 2616 | 7            | 12             | 37               | Self-contained + trimmed (single-file publish) |
| Compose Multiplatform | 1857 | 11           | 98             | 45               | MSI (`packageReleaseDistributionForCurrentOS`) |
| Electron              | 2424 | 12           | 26             | 293              | `win-unpacked` (`dist` + `dist-electron` only) |
| Flutter               | 2390 | 17           | 54             | 27               | `flutter build windows --release`              |
| GPUI                  | -    | -            | -              | -                | Excluded (no file picker)                      |
| MAUI                  | -    | -            | -              | -                | Excluded (unimplemented / WinUI3 overlap)      |
| React Native          | -    | -            | -              | -                | Excluded (insufficient info)                   |
| Tauri                 | 2266 | 25           | 146            | 11               | exe only (WebView2 runtime not included)       |
| WinForms              | -    | -            | -              | -                | Excluded (limited UI)                          |
| WPF                   | 2293 | 3            | 5              | 155              | Self-contained single-file (not trimmed)       |
| WinUI3                | 2303 | 4            | 23             | 30               | Self-contained + trimmed publish               |
| wxWidgets             | 2473 | 1            | 456            | 24               | Nuitka onefile (`TodoApp.exe`)                 |

**ヘッダー**:
- Framework: フレームワーク
- LOC: コード行数
- Dependencies: 依存関係数
- Build time (s): ビルド時間（秒）
- Bundle size (MB): バンドルサイズ（MB）
- Notes: 備考

**測定メモ**（2026-08-08、上記計測環境）:
- LOC: アプリソースのみ（`.cs` / `.xaml` / `.axaml` / `.kt` / `.ts(x)` / `.js` / `.dart` / `.rs` / `.py` 等）。`node_modules` / `bin` / `obj` / `.venv` / Flutter の `windows` 等生成ツリーは除外
- Dependencies: マニフェスト上の直接依存（.NET = `PackageReference`、Compose = `implementation`、Electron/Flutter = `dependencies`、Tauri = npm `dependencies` + Cargo `[dependencies]`、wx = `pyproject` の runtime 依存）。dev 専用は未計上
- Build time / Bundle size: **プロジェクト成果物を毎回完全クリーンしたうえで Release 配布ビルドを 3 回実行し、平均値**（秒・MBは四捨五入）。測定スクリプト: `scripts/measure_impl.ps1`
  - 共通前提: グローバルなパッケージキャッシュは事前リストア済み（NuGet / npm / cargo registry / pub / Gradle wrapper）。ネットワーク取得時間は計測外
  - 各 run 前に `bin`/`obj`/`dist`/`target`/`build` 等の成果物を削除。Gradle は `--no-build-cache --no-configuration-cache`、Nuitka は `--disable-ccache`
  - コマンド: `dotnet publish` / Gradle `packageReleaseDistributionForCurrentOS` / `tsc`+`vite`+`electron-builder` / `flutter build windows --release` / `vite`+`cargo build --release` / Nuitka onefile
- Bundle size: 実行可能な配布物フォルダ、または単一 exe / MSI（Notes 参照）。Electron は `files` を `dist`/`dist-electron` に限定し、旧成果物の混入を排除

### 実行時パフォーマンスの比較

#### メモリ使用量（MB）

| Framework             | Startup (empty) | After 10 | After 100 | After 1000 | Peak | Notes                        |
| --------------------- | --------------- | -------- | --------- | ---------- | ---- | ---------------------------- |
| Avalonia              | 117             | 118      | 118       | 118        | 118  | -                            |
| Compose Multiplatform | 380             | 389      | 389       | 399        | 399  | -                            |
| Electron              | 226             | 235      | 244       | 252        | 252  | -                            |
| Flutter               | 111             | 118      | 117       | 124        | 124  | -                            |
| GPUI                  | -               | -        | -         | -          | -    | Excluded (no file picker)    |
| MAUI                  | -               | -        | -         | -          | -    | Excluded (unimplemented)     |
| React Native          | -               | -        | -         | -          | -    | Excluded (insufficient info) |
| Tauri                 | 189             | 189      | 189       | 189        | 189  | -                            |
| WinForms              | -               | -        | -         | -          | -    | Excluded (limited UI)        |
| WPF                   | 95              | 106      | 112       | 114        | 114  | -                            |
| WinUI3                | 94              | 95       | 95        | 97         | 97   | Packaged (Appx register)     |
| wxWidgets             | 20              | 20       | 22        | 23         | 23   | -                            |

**ヘッダー**:
- Framework: フレームワーク
- Startup (empty): 起動直後（空）
- After 10: 10件追加後
- After 100: 100件追加後
- After 1000: 1000件追加後
- Peak: ピーク使用量
- Notes: 備考

**測定メモ**（2026-08-08、上記計測環境）:
- 指標: プライベートメモリ（`PrivateMemorySize64`、MB）。Peak は当該アプリの 4 状態平均のうち最大値
- 各状態×3 回の平均。起動後約 8〜12 秒待機してから計測。`data/project_{0,10,100,1000}.json` を CLI 引数（または永続ファイルへコピー）で投入
- Electron: 関連プロセス（同名）の Private 合計。Tauri: 本体 + WebView2 子プロセスの Private 合計。wxWidgets: venv 経由の GUI プロセス（onefile スタブは不使用）
- WinUI3: exe 直起動は `0xC000027B` で失敗するため、`AppxManifest.xml` を `Add-AppxPackage -Register` したうえで `shell:AppsFolder\...!App` 起動。データは `%LocalAppData%\Packages\<PFN>\LocalState\Data\project.json` へコピー
- スクリプト: `scripts/measure_memory.ps1` / `scripts/measure_memory_partial.ps1`

#### CPU使用率（%）

| Framework             | Idle | Add  | Scroll | Filtering | Peak | Notes                        |
| --------------------- | ---- | ---- | ------ | --------- | ---- | ---------------------------- |
| Avalonia              | 0.5  | 8.8  | 7.4    | 8.1       | 8.8  | -                            |
| Compose Multiplatform | 1.0  | 2.7  | 2.2    | 10.7      | 10.7 | -                            |
| Electron              | 0.4  | 6.4  | 8.0    | 6.5       | 8.0  | -                            |
| Flutter               | 0.1  | 12.8 | 1.6    | 13.4      | 13.4 | -                            |
| GPUI                  | -    | -    | -      | -         | -    | Excluded (no file picker)    |
| MAUI                  | -    | -    | -      | -         | -    | Excluded (WinUI3 wrapper)    |
| React Native          | -    | -    | -      | -         | -    | Excluded (insufficient info) |
| Tauri                 | 1.0  | 4.8  | 4.9    | 3.9       | 4.9  | app + WebView2 process tree  |
| WinForms              | -    | -    | -      | -         | -    | Excluded (limited UI)        |
| WPF                   | 0.6  | 8.0  | 6.5    | 6.1       | 8.0  | -                            |
| WinUI3                | 0.3  | 7.0  | 6.2    | 6.5       | 7.0  | Packaged (Appx register)     |
| wxWidgets             | 0.2  | 4.8  | 5.8    | 5.2       | 5.8  | uv run (source)              |

**ヘッダー**:
- Framework: フレームワーク
- Idle: アイドル時
- Add: 追加操作時
- Scroll: スクロール時
- Filtering: フィルタリング時
- Peak: ピーク使用率
- Notes: 備考

**測定メモ**（2026-08-08、上記計測環境）:
- 指標: 全論理プロセッサに対するプロセス CPU%（`TotalProcessorTime` 差分 / 経過時間 / 論理コア数 × 100）。Peak は Idle/Add/Scroll/Filtering の平均のうち最大
- 各アプリに `--cpu-bench` を実装。フェーズファイルへ `idle` → `add` → `scroll` → `filter` → `done` を書き、外部スクリプトがフェーズ中をサンプリング（各 5 秒、3 回平均）
- データ: `data/project_1000.json`。Add 中の都度ディスク保存はスキップ（UI/ロジック負荷を測る）
- Electron: 同名プロセス合算。Tauri: 本体 + プロセスツリー上の WebView2。WinUI3: Appx 登録起動 + LocalState リクエストファイル。wxWidgets: `uv run python main.py`（Nuitka 再ビルド不要）。Compose: `createReleaseDistributable` の exe
- スクリプト: `scripts/measure_cpu.ps1` / 終了後 `cleanup_local_leftovers.ps1`
- Tauri Release は `custom-protocol` feature 必須（未設定だと cfg(dev) になり localhost を見に行く）

#### 起動時間とUI応答性

| Framework             | Startup (s) | Render 1000 (s) | Scroll FPS | Filter response (ms) | Notes                              |
| --------------------- | ----------- | --------------- | ---------- | -------------------- | ---------------------------------- |
| Avalonia              | 1.21        | 0.14            | 60.2       | 18.9                 | -                                  |
| Compose Multiplatform | 1.34        | 0.04            | 31.4       | 24.8                 | Scroll paced at 16ms ticks         |
| Electron              | 0.48        | 0.01            | 60.0       | 17.6                 | -                                  |
| Flutter               | 0.26        | 0.03            | 58.8       | 18.7                 | Sync filter for typical list sizes |
| GPUI                  | -           | -               | -          | -                    | -                                  |
| MAUI                  | -           | -               | -          | -                    | Excluded (WinUI3 wrapper)          |
| React Native          | -           | -               | -          | -                    | Excluded (insufficient info)       |
| Tauri                 | 0.44        | 0.04            | 60.4       | 17.3                 | -                                  |
| WinForms              | -           | -               | -          | -                    | Excluded (limited UI)              |
| WPF                   | 0.93        | 0.46            | 22.3       | 143.5                | -                                  |
| WinUI3                | 0.60        | 1.28            | 60.2       | 15.2                 | Packaged LocalState import path    |
| wxWidgets             | 0.22        | 0.01            | 25.9       | 21.0                 | `uv run` (not Nuitka)              |

**ヘッダー**:
- Framework: フレームワーク
- Startup (s): 起動時間（秒）
- Render 1000 (s): 1000件表示時間（秒）
- Scroll FPS: スクロール時 FPS
- Filter response (ms): フィルタリング応答時間（ms）
- Notes: 備考

**測定メモ**（2026-08-08、上記計測環境）:
- 各アプリに `--ui-bench` を実装。結果 JSON（`startup_s` / `render_1000_s` / `scroll_fps` / `filter_response_ms`）を書き出し、3 回平均
- **Startup（公平化）**: OS プロセス作成時刻 → 空 UI の初回フレーム相当。`.NET` / WinUI は `Process.StartTime`、Flutter は runner が `GetProcessTimes` を注入、Compose は `ProcessHandle.startInstant`、Electron は `process.uptime` 逆算、Tauri / wx は `GetProcessTimes`
- Render 1000: `data/project_1000.json` のインポート開始 → リスト反映（フィルタ適用完了まで待機）。WinUI3 は Appx LocalState 経由 + DataGrid
- Scroll FPS: 約 3 秒間の programmatic load-more / reset 中のフレーム数。Compose はフレーム時計がベンチ経路で不安定なため 16ms ペースのティック換算
- Filter: フィルタ ON/OFF を 10 回平均。Flutter は典型的なリスト規模では UI isolate 同期フィルタ（`compute` は 5000 件超）。WinUI は `Task.Run` + UI 反映（WPF/Avalonia と同型）
- スクリプト: `scripts/measure_ui.ps1` / 終了後 `cleanup_local_leftovers.ps1`

### 開発体験の評価

| Framework             | Ease of use | Docs  | Community | Testability | Overall | Notes                                |
| --------------------- | ----------- | ----- | --------- | ----------- | ------- | ------------------------------------ |
| Avalonia              | ****        | ***   | **        | ****        | ***     | XAML/MVVM; smaller desktop community |
| Compose Multiplatform | **          | **    | **        | **          | **      | UI smooth; desktop/Gradle friction   |
| Electron              | *****       | ***** | *****     | *****       | *****   | Web stack; Playwright ceiling        |
| Flutter               | ****        | ***** | *****     | *****       | ****    | Strong docs / integration_test       |
| GPUI                  | -           | -     | -         | -           | -       | Excluded (no file picker)            |
| MAUI                  | -           | -     | -         | -           | -       | Excluded (WinUI3 wrapper)            |
| React Native          | -           | -     | -         | -           | -       | Excluded (insufficient info)         |
| Tauri                 | ***         | ***   | ***       | ****        | ***     | Easy front; Rust/WebView2 cost       |
| WinForms              | -           | -     | -         | -           | -       | Excluded (limited UI)                |
| WPF                   | ***         | ****  | ****      | ***         | ***     | Mature docs; verbose XAML/DataGrid   |
| WinUI3                | *           | **    | **        | **          | *       | Appx/packaging heaviest DX cost      |
| wxWidgets             | **          | **    | *         | *           | *       | Simple code; weak UI test ecosystem  |

**ヘッダー**:
- Framework: フレームワーク
- Ease of use: 開発の容易さ
- Docs: ドキュメント
- Community: コミュニティ
- Testability: テスタビリティ
- Overall: 総合評価
- Notes: 備考（英語・短い要約）

**評価基準**:

- Ease of use: `*****`（5段階）
- Docs: `*****`（5段階）
- Community: `*****`（5段階）
- Testability: `*****`（5段階） - UIテスト自動化のしやすさ、ツール成熟度、テスト用フック（AutomationId / semantics など）
- Overall: `*****`（5段階）

**各フレームワークの解説**:
- **Avalonia**: XAML+MVVM が WPF 知識で転用しやすい。Headless/UI テストはあるが、デスクトップ専業でコミュニティは小さめ
- **Compose Multiplatform**: Compose UI 自体は快適。desktop / Gradle / 配布まわりの摩擦が大きく、desktop 向け情報も少なめ
- **Electron**: Web スタックをそのまま使える。ドキュメント・コミュニティ・Playwright 等の UI テストがこの比較では天井
- **Flutter**: ウィジェット開発は速い。公式 docs と `integration_test` が強い。Windows 固有はたまに調査コスト
- **Tauri**: フロントは Electron に近い。Rust 側と WebView2 / ビルドで学習コストがある
- **WPF**: 成熟して情報は豊富。XAML / DataGrid の記述量は多く、UI Automation / FlaUI 系が使える
- **WinUI3**: 機能は揃うが、Appx 登録・パッケージ制約が本リポで最大の DX 負荷。UIA はあるが環境構築コストが高い
- **wxWidgets**（wxPython）: 実装は素直。Python+wx のデスクトップ情報と UI 自動テストのエコシステムは弱い
- **GPUI / MAUI / React Native / WinForms**: 他表と同じ理由で除外（Notes 参照）

**評価メモ**（2026-08、本リポ実装体験ベース）:
- Ease は同一 Todo 実装時のつまずき・ビルド・デバッグのしやすさ。Docs / Community は公式資料とエコシステムの成熟度も加味
- Testability は本リポで UI 自動テスト未導入のため、フック／ツール成熟度の見込み評価
- 星は実装 8 FW 内の**相対正規化**（各軸で概ね 1〜5 を使う）。絶対的な「業界合格点」ではなく差別化のための尺度
- Overall は 4 軸の概ね平均（端数は体感で丸め）。天井 Electron / 床 WinUI・wx
