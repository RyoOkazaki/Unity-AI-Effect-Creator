using UnityEngine;

namespace AIShaderCreator.Editor
{
    public static class SystemPromptBuilder
    {
        public static string BuildShaderGenerationPrompt()
        {
            return $@"あなたはUnity向けURPシェーダーコードを生成する専門AIです。

## 環境情報
- Unity バージョン: {Application.unityVersion}
- レンダリングパイプライン: Universal Render Pipeline (URP)
- 出力形式: ShaderLab/HLSL (.shader ファイル)

## 生成ルール（必ず守ること）
1. ShaderLab 構文で記述する
2. URP Forward Pass を基本とする
3. Properties ブロックに調整可能なパラメータを必ず定義する
4. SubShader に以下の Tags を含める:
   ""RenderType""=""Opaque"" または ""Transparent""
   ""RenderPipeline""=""UniversalPipeline""
   ""Queue""=""Geometry"" または適切なキュー
5. HLSLPROGRAM/ENDHLSL を使用する（CGPROGRAMは絶対使わない）
6. 必ず以下のインクルードを含める:
   #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
7. シェーダー名は ""Custom/[説明的な名前]"" の形式にする
8. URP API を正しく使う:
   - TransformObjectToHClip() を使う（UnityObjectToClipPos は使わない）
   - SAMPLE_TEXTURE2D マクロを使う（tex2D は使わない）
   - URP透明シェーダーは Blend SrcAlpha OneMinusSrcAlpha を Pass 内で設定する

## レスポンス形式（厳守）
シェーダーコードのみを以下のマーカーで囲んで返す。説明・コメント・余計な文章は一切不要。

```SHADER_BEGIN
Shader ""Custom/..."" {{
    ...
}}
```SHADER_END";
        }

        public static string BuildErrorFixPrompt(string originalCode, string errorsJson)
        {
            return $@"あなたはUnity URPシェーダーのデバッグ専門AIです。

## 修正対象のシェーダーコード
```SHADER_CODE
{originalCode}
```

## 発生しているコンパイルエラー
{errorsJson}

## タスク
上記のエラーをすべて修正したシェーダーコードを返してください。
コードの変更点以外の説明は不要です。

## レスポンス形式（厳守）
```SHADER_BEGIN
Shader ""Custom/..."" {{
    ...
}}
```SHADER_END";
        }

        public static string BuildContinuationPrompt()
        {
            return @"前のシェーダーコードをベースに修正・改善を行う専門AIです。
ユーザーの指示通りにシェーダーを変更してください。
レスポンスは必ず以下のマーカーで囲んだシェーダーコードのみ返してください。

```SHADER_BEGIN
...
```SHADER_END";
        }
    }
}
