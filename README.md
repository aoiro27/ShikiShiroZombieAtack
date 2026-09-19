# 式城ゾンビアタック (ShikiShiro Zombie Attack)

iPad 横画面向けのファーストパーソン・ゾンビシューティングです。Unity で開いて Play すると、夜の封鎖街区が生成され、ウェーブ生存戦が始まります。

見た目は CC0 の KayKit / Quaternius / Kenney モデル、システムはヒットスキャン射撃・ホードAI・武器3種・ドロップ・ハイスコアまで一通り入っています。

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
| RELOAD | リロード |
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

Build Settings → iOS。Player Settings は初期値で iPad Only・横画面・Bundle ID `com.shikishiro.zombieattack`・Automatic Signing。Team ID だけ自分の Apple Developer を入れてください。

## クレジット

`Assets/Art/THIRD_PARTY.txt` を参照（いずれも CC0）。
