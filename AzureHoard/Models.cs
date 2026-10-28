using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace AzureHoard
{
    /// <summary>Una fence guardada en disco.</summary>
    public class FenceData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "";
        public double X { get; set; } = 60;
        public double Y { get; set; } = 60;
        public double W { get; set; } = 340;
        public double H { get; set; } = 240;
        public bool Rolled { get; set; }
        public bool IsDesktop { get; set; }
        public List<string> Items { get; set; } = new List<string>();
    }

    /// <summary>Todo lo que se guarda en layout.json.</summary>
    public class HoardLayout
    {
        public string Lang { get; set; } = "en";
        public List<FenceData> Fences { get; set; } = new List<FenceData>();
    }

    /// <summary>Un archivo, carpeta o acceso directo mostrado dentro de una fence.</summary>
    public class ItemVm
    {
        public string Path { get; }
        public string Name { get; }
        public ImageSource Icon { get; }

        public ItemVm(string path)
        {
            Path = path;
            string n = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Name = Directory.Exists(path) ? n : System.IO.Path.GetFileNameWithoutExtension(n);
            Icon = IconLoader.Get(path);
        }
    }
}
