using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;

namespace AIShaderCreator.Editor
{
    public static class EditorCoroutineRunner
    {
        public static void Run(IEnumerator coroutine)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(coroutine);
        }

        public static Task<T> RunAsTask<T>(Func<Action<T>, Action<string>, IEnumerator> coroutineFactory)
        {
            var tcs = new TaskCompletionSource<T>();
            Run(coroutineFactory(
                result => tcs.TrySetResult(result),
                error => tcs.TrySetException(new Exception(error))
            ));
            return tcs.Task;
        }
    }
}
