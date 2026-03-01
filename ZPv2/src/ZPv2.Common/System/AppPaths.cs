namespace ZPv2.Common.System;

public static class AppPaths
{
    public static string ProgramDataRoot()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(root, "ZPv2");
    }

    public static string LocalAppDataRoot()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "ZPv2");
    }

    public static string ConfigDir(bool localFallback = false)
    {
        return Path.Combine(localFallback ? LocalAppDataRoot() : ProgramDataRoot(), "config");
    }

    public static string LogDir(bool localFallback = false)
    {
        return Path.Combine(localFallback ? LocalAppDataRoot() : ProgramDataRoot(), "log");
    }

    public static string ConfigFile(bool localFallback = false)
    {
        return Path.Combine(ConfigDir(localFallback), "config.json");
    }

    public static bool EnsureWritableDirectory(string dir)
    {
        Directory.CreateDirectory(dir);
        var probe = Path.Combine(dir, ".probe-" + Guid.NewGuid().ToString("N") + ".tmp");
        using (var fs = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 8, FileOptions.DeleteOnClose))
        {
            fs.WriteByte(0x5A);
            fs.Flush();
        }
        if (File.Exists(probe)) File.Delete(probe);
        return true;
    }

    public static string ResolveWritableLogDir()
    {
        try
        {
            var pd = LogDir(false);
            EnsureWritableDirectory(pd);
            return pd;
        }
        catch
        {
            var local = LogDir(true);
            EnsureWritableDirectory(local);
            return local;
        }
    }

    public static string ResolveWritableConfigFilePath()
    {
        try
        {
            var dir = ConfigDir(false);
            EnsureWritableDirectory(dir);
            return ConfigFile(false);
        }
        catch
        {
            var dir = ConfigDir(true);
            EnsureWritableDirectory(dir);
            return ConfigFile(true);
        }
    }
}
