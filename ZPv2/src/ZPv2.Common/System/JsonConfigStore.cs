using System.Text.Json;
using ZPv2.Common.Models;

namespace ZPv2.Common.System;

public sealed class JsonConfigStore
{
    private readonly object _sync = new();
    private readonly string _configFile;
    private readonly JsonSerializerOptions _json;

    public JsonConfigStore()
    {
        _configFile = AppPaths.ResolveWritableConfigFilePath();
        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
    }

    public string ConfigFile => _configFile;

    public ZPv2Config GetOrCreate()
    {
        lock (_sync)
        {
            var cfg = LoadUnsafe();
            if (cfg is null)
            {
                cfg = new ZPv2Config
                {
                    Token = TokenService.Generate16(),
                    ServiceUrl = "http://127.0.0.1:16262",
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                SaveUnsafe(cfg);
                return cfg;
            }

            var normalized = TokenService.NormalizeOrGenerate(cfg.Token);
            if (!string.Equals(normalized, cfg.Token, StringComparison.Ordinal))
            {
                cfg.Token = normalized;
                cfg.UpdatedAt = DateTimeOffset.UtcNow;
                SaveUnsafe(cfg);
            }

            if (string.IsNullOrWhiteSpace(cfg.ServiceUrl))
            {
                cfg.ServiceUrl = "http://127.0.0.1:16262";
                cfg.UpdatedAt = DateTimeOffset.UtcNow;
                SaveUnsafe(cfg);
            }

            return cfg;
        }
    }

    public ZPv2Config Save(ZPv2Config incoming)
    {
        lock (_sync)
        {
            var cfg = new ZPv2Config
            {
                Token = TokenService.NormalizeOrGenerate(incoming.Token),
                ServiceUrl = string.IsNullOrWhiteSpace(incoming.ServiceUrl)
                    ? "http://127.0.0.1:16262"
                    : incoming.ServiceUrl.Trim(),
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            SaveUnsafe(cfg);
            return cfg;
        }
    }

    public string RegenerateToken()
    {
        lock (_sync)
        {
            var cfg = GetOrCreate();
            cfg.Token = TokenService.Generate16();
            cfg.UpdatedAt = DateTimeOffset.UtcNow;
            SaveUnsafe(cfg);
            return cfg.Token;
        }
    }

    private ZPv2Config? LoadUnsafe()
    {
        if (!File.Exists(_configFile)) return null;
        var raw = File.ReadAllText(_configFile);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return JsonSerializer.Deserialize<ZPv2Config>(raw, _json);
    }

    private void SaveUnsafe(ZPv2Config cfg)
    {
        var dir = Path.GetDirectoryName(_configFile) ?? AppPaths.ConfigDir(false);
        Directory.CreateDirectory(dir);
        var raw = JsonSerializer.Serialize(cfg, _json);
        File.WriteAllText(_configFile, raw);
    }
}
