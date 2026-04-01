# AudioGuard

Windows タスクトレイに常駐し、新しいプロセスがオーディオミキサーを使用した際に、自動で音量を調整（またはミュート）する常駐アプリです。

## 機能

- **自動音量調整** — 未登録の exe が音を出したら、既定の音量レベルに自動設定
- **ミュートモード** — 設定により新規プロセスをミュートにすることも可能
- **再適用なし** — 一度適用した exe は JSON に記録し、次回以降は再適用しない
- **除外リスト** — 特定のプロセスを音量調整の対象外に設定可能
- **自動起動** — Windows 起動時の自動実行に対応
- **タスクトレイ常駐** — バルーン通知で適用結果を表示

## 動作環境

- Windows 10 / 11 (64bit)
- .NET ランタイム不要（自己完結型ビルド）

## ダウンロード

[Releases](../../releases) から `AudioGuard.exe` をダウンロードして実行してください。

## 使い方

1. `AudioGuard.exe` を実行するとタスクトレイに常駐します
2. トレイアイコンをダブルクリックで設定画面を開きます
3. 既定音量・ミュート設定・除外リストを変更して「保存」

### 設定項目

| 項目 | 説明 |
|------|------|
| 既定音量 | 新規プロセスに適用する音量 (0-100%) |
| ミュートモード | 音量調整の代わりにミュートにする |
| 除外リスト | 音量調整しない exe 名 |
| 自動起動 | Windows 起動時に自動実行 |

設定は `%AppData%\AudioGuard\settings.json` に保存されます。

## ビルド方法

[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) が必要です。

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./dist
```

または `publish.bat` を実行してください。

## 技術仕様

- .NET 8 / WPF
- Windows Core Audio API (COM interop)
- `IAudioSessionManager2.RegisterSessionNotification` によるリアルタイム監視

## ライセンス

MIT License
