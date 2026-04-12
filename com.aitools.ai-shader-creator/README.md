# AI Shader Creator

Unity Editor上でAIに自然言語で指示するだけで、URP対応シェーダーとマテリアルを自動生成するツールです。

## 必要環境
- Unity 2022.3 LTS 以上
- Universal Render Pipeline (URP)
- インターネット接続（Claude API使用）

## インストール方法

1. Package Manager を開く（Window > Package Manager）
2. 左上の `+` > `Add package from disk...`
3. `package.json` を選択

## セットアップ

1. `Tools > AI Shader Creator > Settings` を開く
2. Claude APIキーを入力して保存
   - APIキーは https://console.anthropic.com/ で取得
3. `Tools > AI Shader Creator > Open` でメインウィンドウを開く

## 使い方

1. シーン内のGameObjectを選択（マテリアルを自動適用する場合）
2. チャット欄に作りたいシェーダーを日本語で入力
3. 「送信」ボタン（またはShift+Enter）

### 入力例
- `炎のような揺らめくエフェクトのシェーダーを作って`
- `ホログラムっぽいSFシェーダー。スキャンライン付きで`
- `グリッチエフェクト。青と紫のノイズ`
- `透明な水面シェーダー。波紋あり`

## 生成されるファイル
- シェーダー: `Assets/AIGeneratedShaders/{名前}.shader`
- マテリアル: `Assets/AIGeneratedShaders/Materials/{名前}.mat`

## コンパイルエラーの自動修正
シェーダーにコンパイルエラーがあった場合、最大3回まで自動修正を試みます。
3回失敗した場合はエラー内容がチャットに表示されます。

## モデル選択
Settings画面でモデルを選べます：
- **Opus 4.6** (デフォルト): 高品質、複雑なシェーダーに最適
- **Haiku 4.5**: 高速・低コスト、シンプルなシェーダー向け
