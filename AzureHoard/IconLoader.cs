using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AzureHoard
{
    /// <summary>Saca el icono que Windows muestra para un archivo, carpeta o acceso directo.</summary>
    static class IconLoader
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SHGetFileInfo(string path, uint attributes, ref SHFILEINFO info, uint size, uint flags);

        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr hIcon);

        const uint SHGFI_ICON = 0x100;
        const uint SHGFI_LARGEICON = 0x0;

        static readonly Dictionary<string, ImageSource> Cache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        public static ImageSource Get(string path)
        {
            ImageSource cached;
            if (Cache.TryGetValue(path, out cached)) return cached;

            ImageSource result = null;
            try
            {
                var info = new SHFILEINFO();
                SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf(info), SHGFI_ICON | SHGFI_LARGEICON);
                if (info.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                        result = src;
                    }
                    finally { DestroyIcon(info.hIcon); }
                }
            }
            catch (Exception ex) { Store.Log(ex); }

            Cache[path] = result;
            return result;
        }
    }
}
