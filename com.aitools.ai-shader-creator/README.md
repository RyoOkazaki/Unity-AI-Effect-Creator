# AI Shader Creator

Unity Editor上でAIに自然言語で指示するだけで、URP対応シェーダーとマテリアルを自動生成するツールです。

## 必要環境
- Unity 2022.3 LTS 以上
- Universal Render Pipeline (URP)
- インターネット接続（AI API使用）

## インストール方法

1. Package Manager を開く（Window > Package Manager）
2. 左上の `+` > `Add package from disk...`
3. `package.json` を選択

または Git URL でインストール：
```
https://github.com/RyoOkazaki/Claude-Company.git?path=unity-ai-shader-creator/com.aitools.ai-shader-creator
```

## セットアップ

1. `Tools > AI Shader Creator > Settings` を開く
2. 使用するサービスのAPIキーを入力して保存
   - **Claude**: https://console.anthropic.com/
   - **OpenAI**: https://platform.openai.com/api-keys
   - **Gemini**: https://aistudio.google.com/app/apikey
3. `Tools > AI Shader Creator > Open` でメインウィンドウを開く

## 使い方

1. シーン内のGameObjectを選択（マテリアルを自動適用する場合）
2. ウィンドウ上部でAIサービスを選択（Claude / OpenAI / Gemini）
3. チャット欄に作りたいシェーダーを日本語で入力
4. 「送信」ボタン（またはShift+Enter）

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

## 対応AIサービスとモデル

### Claude（推奨）
- **Opus 4.6** (デフォルト): 高品質、複雑なシェーダーに最適
- **Haiku 4.5**: 高速・低コスト、シンプルなシェーダー向け

### OpenAI
- **GPT-4o** (デフォルト): 高品質
- **GPT-4o mini**: 高速・低コスト

### Google Gemini
- **Gemini 2.0 Flash** (デフォルト): 高速
- **Gemini 1.5 Pro**: 高品質

## APIキーのセキュリティ
- APIキーはお使いのPCのEditorPrefsに難読化して保存されます
- キーはそれぞれの公式AIサービスにのみ送信されます
- コードにキーをハードコードする必要はありません
