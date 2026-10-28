using System;
using System.IO;
using System.Text.Json;

namespace AzureHoard
{
    public static class Store
    {
        public static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AzureHoard");
        static readonly string FilePath = Path.Combine(Dir, "layout.json");
        static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { WriteIndented = true };

        public static HoardLayout Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<HoardLayout>(File.ReadAllText(FilePath)) ?? new HoardLayout();
            }
            catch (Exception ex) { Log(ex); }
            return new HoardLayout();
        }

        public static void Save(HoardLayout l)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(l, Opt));
                File.Move(tmp, FilePath, true);
            }
            catch (Exception ex) { Log(ex); }
        }

        public static void Log(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.AppendAllText(Path.Combine(Dir, "log.txt"), DateTime.Now.ToString("s") + " " + ex + Environment.NewLine);
            }
            catch { }
        }
    }
}
