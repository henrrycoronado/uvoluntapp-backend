using System.Text.Json;
using System.Text.Json.Nodes;

namespace Uvoluntapp.Backend.Domain.Audit;

public static class AuditDetails
{
    private static readonly string[] _sensitiveFragments =
    [
        "password",
        "contrasena",
        "contraseña",
        "token",
        "secret",
        "otp",
        "hash",
        "credential",
    ];

    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static string Serialize(IReadOnlyDictionary<string, object?>? details)
    {
        if (details is null || details.Count == 0)
        {
            return "{}";
        }

        var node = JsonSerializer.SerializeToNode(details, _options);
        Redact(node);
        return node?.ToJsonString(_options) ?? "{}";
    }

    public static bool IsSensitiveKey(string key) =>
        _sensitiveFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var name in jsonObject.Select(property => property.Key).ToList())
                {
                    if (IsSensitiveKey(name))
                    {
                        jsonObject.Remove(name);
                    }
                    else
                    {
                        Redact(jsonObject[name]);
                    }
                }

                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    Redact(item);
                }

                break;
        }
    }
}
