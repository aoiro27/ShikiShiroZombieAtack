# 式城ゾンビアタック (ShikiShiro Zombie Attack)

iPad 横画面向けの、サードパーソン・ゾンビシューティングです。Unity エディタで開くと、夜の封鎖街区が実行時に組み立てられ、ウェーブ式の生存戦が始まります。

このマシン上には Unity が入っていなかったため、**Unity 2022.3 LTS で開いて Play** するとプレイできます。見た目は Kenney の 3D アセット（Unity Asset Store でも配信されている CC0 パック）を `Resources` から読み込みます。アセットがまだインポート前の場合は Primitive にフォールバックします。

## 必要環境

- Unity **2022.3 LTS**（推奨 2022.3.50f1。近くの 2022.3 でも可）
- モジュール: **iOS Build Support**（実機へ出す場合）
- macOS で iPad 実機ビルドする場合は Xcode

Unity Hub で「Add project from disk」し、このフォルダを指定してください。初回インポート後、`Assets/Scenes/Main.unity` を開いて Play します。

## 操作

### iPad（ランドスケープ）

| 操作 | 内容 |
| --- | --- |
| 左スティック | 移動 |
| 右スティック | 視点 |
| FIRE | 射撃（長押し連射） |
| RELOAD | リロード |
| WEAPON | 武器切替（ハンドガン / SMG / ショットガン） |
| SPRINT | ダッシュ |
| II | 一時停止。ゲームオーバー後は再開 |

セーフエリアを考慮した HUD です。

### エディタ（PC）

- **WASD** 移動
- **マウス** 視点
- **左クリック / Space** 射撃
- **R** リロード
- **Q** 武器切替
- **Shift** ダッシュ
- **Esc** ポーズ

## ゲーム内容

- ウォーカー / ランナー / ブルートの 3 種族
- ヘッドショット（上半身上部）で高ダメージ・高スコア
- ウェーブごとに出現数と種族比率が上がる
- 弾薬箱・メディキットのドロップ
- コンボ、ハイスコア（`PlayerPrefs` 保存）
- オブジェクトプール（ゾンビ最大同時 36、プール 48）

## iPad 向けビルド

1. File → Build Settings → iOS → Switch Platform  
2. Player Settings は初期値で次を設定済みです  
   - Bundle ID: `com.shikishiro.zombieattack`  
   - Target Device: **iPad Only**  
   - Orientation: Landscape Left / Right のみ  
   - Target minimum iOS: 13.0  
   - Automatic Signing オン（Team ID は自分の Apple Developer を入れてください）  
3. Build して Xcode から iPad にインストール  

実機では 60fps 目標、Metal、IL2CPP です。重い場合は Quality を Medium に、同時ゾンビ数を `GameConfig.MaxAliveZombies` で下げてください。

## コード構成

すべて名前空間 `ShikiShiro` です。シーン上の `GameRoot` が `Bootstrap` を持ち、アリーナ・プレイヤー・カメラ・HUD・ホードを実行時生成します。

```
Assets/Scripts/
  Core/        起動・入力・セッション
  Player/      移動・体力・TPSカメラ
  Combat/      武器・ヒットスキャン・ドロップ
  Enemies/     ゾンビAI・ウェーブ
  World/       街区生成・Kenneyアセット読込
  UI/          iPadタッチHUD
  Audio/       手続き型SE
```

見た目のモデルは `Assets/Resources/Kenney/` にあります（Characters / Buildings / City / Weapons）。出典は `Assets/Art/THIRD_PARTY.txt`。

数値調整は `GameConfig` と `WeaponStats` が中心です。

## これから足すと「本格」が一段上がるもの

- 人型アニメーション（Kenney の idle/run クリップを Animator に接続）
- NavMesh のベイク（建物裏への回り込み）
- URP + ポストプロセス（ブルーム、色収差）
- ストーリー・セーフルーム・武器拾い
- Game Center ランキング

Primitive フォールバック付きなので、Unity で開いてインポートが終われば Kenney モデルで即プレイできます。
