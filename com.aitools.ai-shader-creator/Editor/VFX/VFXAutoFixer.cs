using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    /// <summary>
    /// VFX 生成スクリプトのコンパイルエラーを自動検出・修正する。
    /// [InitializeOnLoad] によりドメインリロード後も動作を継続する。
    /// </summary>
    [InitializeOnLoad]
    public static class VFXAutoFixer
    {
        // SessionState keys（ドメインリロードを跨いで状態を保持）
        private const string KeyCode         = "VFXAutoFixer_Code";
        private const string KeyName         = "VFXAutoFixer_ClassName";
        private const string KeyAttempt      = "VFXAutoFixer_Attempt";
        private const string KeyLastPrefab   = "VFXAutoFixer_LastPrefabPath";
        private const int    MaxAttempts = 3;

        /// <summary>直近に生成されたプレハブのパスを返す。</summary>
        public static string GetLastPrefabPath() => SessionState.GetString(KeyLastPrefab, null);

        /// <summary>プレハブパスを記録する（VFXCodeWriter から呼ぶ）。</summary>
        public static void SetExpectedPrefabPath(string path) =>
            SessionState.SetString(KeyLastPrefab, path);

        static VFXAutoFixer()
        {
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
        }

        /// <summary>スクリプト書き込み後に呼び出して監視を開始する。</summary>
        public static void Register(string code, string className)
        {
            SessionState.SetString(KeyCode,    code);
            SessionState.SetString(KeyName,    className);
            SessionState.SetInt(KeyAttempt, 1);
        }

        private static void OnAssemblyCompiled(string assemblyPath, CompilerMessage[] messages)
        {
            var className = SessionState.GetString(KeyName, null);
            if (string.IsNullOrEmpty(className)) return;

            // 生成スクリプトに関係するエラーのみ抽出
            var errors = messages
                .Where(m => m.type == CompilerMessageType.Error
                         && m.file != null
                         && m.file.Replace("\\", "/").Contains(className))
                .ToArray();

            if (errors.Length == 0)
            {
                Debug.Log("[AI Effect Creator] コンパイル成功。エフェクトを生成しています...");
                ClearState();
                return;
            }

            var attempt = SessionState.GetInt(KeyAttempt, 1);
            var code    = SessionState.GetString(KeyCode, null);

            if (attempt > MaxAttempts || string.IsNullOrEmpty(code))
            {
                Debug.LogError(
                    $"[AI Effect Creator] 自動修正に失敗しました（{MaxAttempts}回試行）。\n" +
                    string.Join("\n", errors.Select(e => $"  Line {e.line}: {e.message}")));
                ClearState();
                return;
            }

            var errorSummary = string.Join("\n", errors.Select(e => $"Line {e.line}: {e.message}"));
            Debug.Log($"[AI Effect Creator] コンパイルエラー {errors.Length} 件を検出。自動修正中... ({attempt}/{MaxAttempts})");

            SessionState.SetInt(KeyAttempt, attempt + 1);
            EditorCoroutineRunner.Run(AutoFixCoroutine(code, errorSummary));
        }

        private static IEnumerator AutoFixCoroutine(string originalCode, string errorText)
        {
            var service = (AIService)EditorPrefs.GetInt("AIShaderCreator_Service", 0);
            if (!AIServiceFactory.HasKey(service))
            {
                Debug.LogError("[AI Effect Creator] APIキーが設定されていないため自動修正できません。");
                ClearState();
                yield break;
            }

            var model  = EditorPrefs.GetString(
                AIServiceFactory.GetModelPrefsKey(service),
                AIServiceFactory.GetDefaultModel(service));
            var client = AIServiceFactory.Create(service, model);

            var fixPrompt = BuildFixPrompt(originalCode, errorText);
            var messages  = new[] { new ChatMessage("user", fixPrompt) };

            string fixedResponse = null;
            string apiError      = null;

            yield return client.SendMessageCoroutine(
                VFXCodeSystemPromptBuilder.Build(), messages, 8192,
                r => fixedResponse = r,
                e => apiError = e
            );

            if (apiError != null || fixedResponse == null)
            {
                Debug.LogError($"[AI Effect Creator] 修正リクエスト失敗: {apiError}");
                ClearState();
                yield break;
            }

            if (!TryExtractCode(fixedResponse, out var fixedCode))
            {
                Debug.LogError("[AI Effect Creator] 修正コードを抽出できませんでした。");
                ClearState();
                yield break;
            }

            // 修正済みコードで更新して再コンパイル
            SessionState.SetString(KeyCode, fixedCode);
            VFXCodeWriter.Write(fixedCode, out _);
        }

        private static string BuildFixPrompt(string code, string errors) => $@"以下のC#コードにコンパイルエラーがあります。すべて修正して完全なコードを返してください。

## 元のコード
{code}

## コンパイルエラー
{errors}

修正後のコードをVFX_CODE_BEGIN〜VFX_CODE_ENDで囲んで返してください。";

        private static bool TryExtractCode(string response, out string code)
        {
            code = null;
            const string begin = "VFX_CODE_BEGIN";
            const string end   = "VFX_CODE_END";
            var b = response.IndexOf(begin);
            var e = response.IndexOf(end);
            if (b < 0 || e < 0 || e <= b) return false;
            code = response.Substring(b + begin.Length, e - b - begin.Length).Trim();
            if (code.StartsWith("```")) code = code.Substring(code.IndexOf('\n') + 1);
            if (code.EndsWith("```"))   code = code.Substring(0, code.LastIndexOf("```")).TrimEnd();
            return !string.IsNullOrWhiteSpace(code);
        }

        private static void ClearState()
        {
            SessionState.EraseString(KeyCode);
            SessionState.EraseString(KeyName);
            SessionState.EraseInt(KeyAttempt);
        }
    }
}
