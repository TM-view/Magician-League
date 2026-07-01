using System.Collections.Generic;
using System.Text;

public static class FirestoreHelper
{
    public static string ToFirestoreJson(Dictionary<string, object> fields)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"fields\":{");

        bool first = true;
        foreach (var kvp in fields)
        {
            if (!first)
                sb.Append(",");
            first = false;

            string valueJson = kvp.Value switch
            {
                string s => $"\"stringValue\":\"{s}\"",
                int i => $"\"integerValue\":{i}",
                float f => $"\"doubleValue\":{f}",
                bool b => $"\"booleanValue\":{b.ToString().ToLower()}",
                _ => $"\"stringValue\":\"{kvp.Value}\"",
            };

            sb.Append($"\"{kvp.Key}\":{{{valueJson}}}");
        }

        sb.Append("}}");
        return sb.ToString();
    }

    public static string GetStringField(string json, string fieldName)
    {
        string search = $"\"{fieldName}\"";
        int idx = json.IndexOf(search);
        if (idx < 0)
            return null;

        int valIdx = json.IndexOf("stringValue", idx);
        if (valIdx < 0)
            return null;

        int start = json.IndexOf("\"", valIdx + 13) + 1;
        int end = json.IndexOf("\"", start);

        return json.Substring(start, end - start);
    }
}
