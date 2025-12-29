# Flutter アプリ公開ガイド

このドキュメントでは、Flutterアプリを各プラットフォームに公開する方法を説明します。

## 目次
1. [共通の準備](#共通の準備)
2. [Android (Google Play Store)](#android-google-play-store)
3. [iOS (Apple App Store)](#ios-apple-app-store)
4. [Windows](#windows)
5. [macOS](#macos)
6. [Linux](#linux)
7. [Web](#web)

---

## 共通の準備

### 1. バージョン情報の更新

`pubspec.yaml`でバージョンを更新します：

```yaml
version: 1.0.0+1  # 1.0.0はバージョン名、+1はビルド番号
```

### 2. アプリ情報の設定

- アプリ名
- アイコン
- 説明文
- ライセンス情報

---

## Android (Google Play Store)

### 1. 署名キーの作成

```bash
keytool -genkey -v -keystore ~/upload-keystore.jks -keyalg RSA -keysize 2048 -validity 10000 -alias upload
```

### 2. キーストア設定ファイルの作成

`android/key.properties`を作成：

```properties
storePassword=<パスワード>
keyPassword=<パスワード>
keyAlias=upload
storeFile=<キーストアファイルのパス>
```

### 3. build.gradle.ktsの更新

`android/app/build.gradle.kts`を更新して署名設定を追加します。

### 4. リリースビルド

```bash
flutter build appbundle
```

出力: `build/app/outputs/bundle/release/app-release.aab`

### 5. Google Play Consoleにアップロード

1. [Google Play Console](https://play.google.com/console)にアクセス
2. アプリを作成
3. アプリバンドル（.aab）をアップロード
4. ストア情報を入力
5. 審査を提出

### 参考リンク
- [Flutter公式: Android リリース](https://docs.flutter.dev/deployment/android)

---

## iOS (Apple App Store)

### 1. Xcodeでの設定

```bash
open ios/Runner.xcworkspace
```

1. **Signing & Capabilities**で署名設定
2. **Bundle Identifier**を一意の値に変更
3. **Version**と**Build**番号を設定

### 2. リリースビルド

```bash
flutter build ipa
```

または、Xcodeから：
1. Product → Archive
2. Distribute App
3. App Store Connect
4. アップロード

### 3. App Store Connectでの設定

1. [App Store Connect](https://appstoreconnect.apple.com/)にアクセス
2. アプリを作成
3. アプリ情報を入力
4. ビルドを選択
5. 審査を提出

### 参考リンク
- [Flutter公式: iOS リリース](https://docs.flutter.dev/deployment/ios)

---

## Windows

### 1. リリースビルド

```bash
flutter build windows --release
```

出力: `build/windows/x64/runner/Release/` フォルダ

### 2. 配布方法

#### 方法A: Microsoft Store

1. [Microsoft Partner Center](https://partner.microsoft.com/)でアプリを作成
2. `.msix`パッケージを作成（`flutter build windows --release`後、手動でパッケージ化）
3. ストアに提出

#### 方法B: 直接配布

- インストーラーを作成（Inno Setup、NSISなど）
- ZIPファイルとして配布

### 参考リンク
- [Flutter公式: Windows リリース](https://docs.flutter.dev/deployment/windows)

---

## macOS

### 1. 署名と公証の設定

`macos/Runner.xcodeproj`を開いて署名設定を行います。

### 2. リリースビルド

```bash
flutter build macos --release
```

出力: `build/macos/Build/Products/Release/` フォルダ

### 3. 配布方法

#### 方法A: Mac App Store

1. XcodeでArchiveを作成
2. App Store Connectにアップロード
3. 審査を提出

#### 方法B: 直接配布

- `.dmg`ファイルを作成
- または`.zip`として配布

### 参考リンク
- [Flutter公式: macOS リリース](https://docs.flutter.dev/deployment/macos)

---

## Linux

### 1. リリースビルド

```bash
flutter build linux --release
```

出力: `build/linux/x64/release/bundle/` フォルダ

### 2. 配布方法

#### 方法A: Snap Store

```bash
# snapcraft.yamlを作成してから
snapcraft
snapcraft upload *.snap
```

#### 方法B: AppImage

```bash
# AppImageを作成
appimagetool build/linux/x64/release/bundle
```

#### 方法C: 直接配布

- `.tar.gz`や`.deb`、`.rpm`パッケージとして配布

### 参考リンク
- [Flutter公式: Linux リリース](https://docs.flutter.dev/deployment/linux)

---

## Web

### 1. リリースビルド

```bash
flutter build web --release
```

出力: `build/web/` フォルダ

### 2. デプロイ方法

#### 方法A: Firebase Hosting

```bash
# Firebase CLIをインストール後
firebase init hosting
firebase deploy --only hosting
```

#### 方法B: GitHub Pages

```bash
# gh-pagesブランチにビルド結果をプッシュ
```

#### 方法C: その他のホスティング

- Netlify
- Vercel
- AWS S3 + CloudFront
- 任意のWebサーバー

### 参考リンク
- [Flutter公式: Web リリース](https://docs.flutter.dev/deployment/web)

---

## 便利なコマンド

### 全プラットフォームのビルド

```bash
# Android
flutter build appbundle

# iOS
flutter build ipa

# Windows
flutter build windows --release

# macOS
flutter build macos --release

# Linux
flutter build linux --release

# Web
flutter build web --release
```

### ビルド前のチェック

```bash
flutter analyze
flutter test
flutter doctor
```

---

## トラブルシューティング

### ビルドエラーが発生した場合

1. `flutter clean`を実行
2. `flutter pub get`を実行
3. `flutter doctor`で環境を確認
4. プラットフォーム固有の設定を確認

### サイズが大きすぎる場合

```bash
# リリースビルドで最適化
flutter build <platform> --release --split-debug-info=<directory>
```

---

## 参考資料

- [Flutter公式: デプロイメント](https://docs.flutter.dev/deployment)
- [Flutter公式: パフォーマンス最適化](https://docs.flutter.dev/perf)

