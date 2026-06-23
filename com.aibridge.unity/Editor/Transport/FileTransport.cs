#nullable enable
using System;
using System.IO;
using System.Linq;
using AIBridge.Editor.Core;
using UnityEngine;

namespace AIBridge.Editor.Transport
{
    /// <summary>
    /// Watches &lt;channel&gt;/in for *.json requests, dispatches them, and writes responses to &lt;channel&gt;/out.
    /// Uses polling (not FileSystemWatcher) so everything stays on the main thread and survives domain reloads.
    /// </summary>
    public class FileTransport : ITransport
    {
        readonly string _inDir;
        readonly string _outDir;

        public FileTransport(string channelRoot)
        {
            _inDir = Path.Combine(channelRoot, "in");
            _outDir = Path.Combine(channelRoot, "out");
            Directory.CreateDirectory(_inDir);
            Directory.CreateDirectory(_outDir);
        }

        public void Poll(CommandDispatcher dispatcher)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(_inDir, "*.json");
            }
            catch
            {
                return;
            }

            if (files.Length == 0)
                return;

            foreach (var file in files.OrderBy(f => f))
            {
                string raw;
                try
                {
                    raw = File.ReadAllText(file);
                }
                catch
                {
                    continue; // likely still being written; retry next poll
                }

                // Remove the request first so a handler that throws can't cause an infinite reprocess loop.
                try { File.Delete(file); } catch { /* ignore */ }

                var response = dispatcher.Handle(raw);
                WriteResponse(response);
            }
        }

        void WriteResponse(string responseJson)
        {
            // The response echoes the request id; reuse it as the output filename.
            RequestEnvelope env;
            try { env = JsonUtility.FromJson<RequestEnvelope>(responseJson) ?? new RequestEnvelope(); }
            catch { env = new RequestEnvelope(); }

            var id = string.IsNullOrEmpty(env.id) ? Guid.NewGuid().ToString("N") : env.id;
            var dest = Path.Combine(_outDir, id + ".json");
            var tmp = dest + ".tmp";

            // Write to a temp file then swap, so a reader never sees a half-written response.
            File.WriteAllText(tmp, responseJson);
            if (File.Exists(dest))
                File.Delete(dest);
            File.Move(tmp, dest);
        }
    }
}
