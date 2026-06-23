#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Core
{
    /// <summary>
    /// Buffers Unity log messages via the supported <see cref="Application.logMessageReceivedThreaded"/>
    /// callback (no internal/reflection access), so the AI can read recent Console output.
    /// Captures script/runtime logs (Log/Warning/Error/Exception/Assert). Compiler errors go through a
    /// different path — read Editor.log for those.
    /// </summary>
    public static class LogCollector
    {
        public struct Item
        {
            public string type;
            public string message;
            public string stackTrace;
            public DateTime time;
        }

        const int Capacity = 500;
        static readonly LinkedList<Item> _buffer = new();
        static readonly object _lock = new();

        [InitializeOnLoadMethod]
        static void Subscribe()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
        }

        static void OnLog(string message, string stackTrace, LogType type)
        {
            lock (_lock)
            {
                _buffer.AddLast(new Item
                {
                    type = type.ToString(),
                    message = message,
                    stackTrace = stackTrace,
                    time = DateTime.Now
                });
                while (_buffer.Count > Capacity)
                    _buffer.RemoveFirst();
            }
        }

        public static Item[] Snapshot()
        {
            lock (_lock)
            {
                var arr = new Item[_buffer.Count];
                _buffer.CopyTo(arr, 0);
                return arr;
            }
        }

        public static void Clear()
        {
            lock (_lock) { _buffer.Clear(); }
        }
    }
}
