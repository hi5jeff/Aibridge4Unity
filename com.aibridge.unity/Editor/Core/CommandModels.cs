#nullable enable
using System.Text;

namespace AIBridge.Editor.Core
{
    /// <summary>Minimal shape every request shares. Handlers parse their own typed fields from the raw JSON.</summary>
    [System.Serializable]
    public class RequestEnvelope
    {
        public string id = "";
        public string command = "";
    }

    /// <summary>
    /// What a handler returns. <see cref="ResultJson"/> is already-serialized JSON (object or array), or null.
    /// </summary>
    public readonly struct CommandResult
    {
        public readonly bool Ok;
        public readonly string? Error;
        public readonly string? ResultJson;

        CommandResult(bool ok, string? error, string? resultJson)
        {
            Ok = ok;
            Error = error;
            ResultJson = resultJson;
        }

        public static CommandResult Success(string resultJson) => new(true, null, resultJson);
        public static CommandResult Failure(string error) => new(false, error, null);
    }

    /// <summary>Tiny JSON string helpers — we only hand-build the response envelope; payloads use JsonUtility.</summary>
    public static class Json
    {
        public static string Escape(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            var sb = new StringBuilder(s!.Length + 8);
            foreach (var c in s!)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
