#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;

namespace AIBridge.Editor.Core
{
    /// <summary>
    /// Tracks compiler messages (via CompilationPipeline) and exposes editor busy-state, so the AI can
    /// trigger a recompile and then poll for "done + errors" without the user touching Unity.
    /// Note: on a *successful* compile the domain reloads and this buffer resets (no errors to report);
    /// on a *failed* compile there is no reload, so the errors stay readable here.
    /// </summary>
    public static class EditorStatus
    {
        public struct CompileMsg
        {
            public string type;
            public string assembly;
            public string message;
            public string file;
            public int line;
        }

        static readonly List<CompileMsg> _messages = new();
        static readonly object _lock = new();

        public static bool LastCompileHadErrors { get; private set; }

        [InitializeOnLoadMethod]
        static void Init()
        {
            CompilationPipeline.compilationStarted -= OnStart;
            CompilationPipeline.compilationStarted += OnStart;
            CompilationPipeline.assemblyCompilationFinished -= OnAssembly;
            CompilationPipeline.assemblyCompilationFinished += OnAssembly;
        }

        static void OnStart(object ctx)
        {
            lock (_lock)
            {
                _messages.Clear();
                LastCompileHadErrors = false;
            }
        }

        static void OnAssembly(string assemblyPath, CompilerMessage[] messages)
        {
            lock (_lock)
            {
                foreach (var m in messages)
                {
                    _messages.Add(new CompileMsg
                    {
                        type = m.type.ToString(),
                        assembly = Path.GetFileNameWithoutExtension(assemblyPath),
                        message = m.message,
                        file = m.file,
                        line = m.line
                    });
                    if (m.type == CompilerMessageType.Error)
                        LastCompileHadErrors = true;
                }
            }
        }

        public static CompileMsg[] Messages()
        {
            lock (_lock) { return _messages.ToArray(); }
        }
    }
}
