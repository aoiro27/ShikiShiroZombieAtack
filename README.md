# 式城ゾンビアタック (ShikiShiro Zombie Attack)

iPad 横画面向けのファーストパーソン・ゾンビシューティングです。Unity で開いて Play すると、夜の封鎖街区が生成され、ウェーブ生存戦が始まります。

見た目は KayKit / ストアのゾンビと一人称武器、システムはヒットスキャン射撃・ホードAI・武器3種・ドロップ・ハイスコアまで一通り入っています。

## 必要環境

- Unity **2022.3 LTS**（推奨 2022.3.50f1）
- iPad 実機: **iOS Build Support** と Xcode

Unity Hub → Add project from disk でこのフォルダを指定。初回インポート後、メニュー「式城ゾンビアタック / Main シーンを開く」か `Assets/Scenes/Main.unity` を Play。

## 操作

### iPad（ランドスケープ固定）

| 操作 | 内容 |
| --- | --- |
| 左スティック | 移動 |
| 右スティック | 視点 |
| 右下の赤い「撃つ」ボタン | 射撃（画像ボタン、長押し連射） |
| WEAPON | ハンドガン / SMG / ショットガン |
| SPRINT | ダッシュ |
| II | ポーズ。ゲームオーバー後は再開 |

### エディタ

WASD 移動、マウス視点、左クリック/Space 射撃、R リロード、Q 武器切替、Shift ダッシュ、Esc ポーズ。

## 中身

- ウォーカー / ランナー / ブルート
- ヘッドショット補正、リコイル、マズルフラッシュ
- ウェーブ進行、弾薬箱・メディキット
- コンボとハイスコア（端末に保存）
- ゾンビとエフェクトはオブジェクトプール
- BGM / 銃声 / ゾンビのうめき・死亡 / ヒットSE

調整は `GameConfig` と `WeaponStats`。モデルパスは `GameAssets`。

## iPad ビルド

Build Settings → iOS。出力フォルダは毎回同じ場所にしてください。

署名を Xcode でやり直さなくて済む手順:

1. Unity メニュー **式城ゾンビアタック / iOS 署名 (Team ID)** を開き、Xcode の Signing & Capabilities に出ている 10 文字の Team ID を保存する（この Mac にだけ残ります）
2. Player Settings は Automatic Signing・Bundle ID `com.shikishiro.zombieattack`
3. 以降は Unity からビルドするだけで、生成された Xcode プロジェクトに同じチームが入る

Xcode 側で毎回 Signing を触る必要はありません。実機へは Xcode で Run するだけです。

## クレジット

`Assets/Art/THIRD_PARTY.txt` を参照。
