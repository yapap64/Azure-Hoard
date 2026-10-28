using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AzureHoard
{
    /// <summary>Todo lo que toca el escritorio real de Windows: esconder sus iconos y colocar las fences encima del fondo.</summary>
    static class Desktop
    {
        delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] static extern int SetWindowLong(IntPtr hWnd, int index, int value);

        static IntPtr _host = IntPtr.Zero;   // ventana del escritorio que contiene la lista de iconos
        static IntPtr _list = IntPtr.Zero;   // la lista de iconos del escritorio (SysListView32)
        static bool _found;

        static void Find()
        {
            _host = IntPtr.Zero; _list = IntPtr.Zero; _found = false;
            IntPtr host = IntPtr.Zero, view = IntPtr.Zero;

            IntPtr prog = FindWindow("Progman", null);
            if (prog != IntPtr.Zero)
            {
                view = FindWindowEx(prog, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (view != IntPtr.Zero) host = prog;
            }
            if (view == IntPtr.Zero)
            {
                IntPtr h2 = IntPtr.Zero, v2 = IntPtr.Zero;
                EnumWindows((w, l) =>
                {
                    IntPtr v = FindWindowEx(w, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (v != IntPtr.Zero) { h2 = w; v2 = v; return false; }
                    return true;
                }, IntPtr.Zero);
                host = h2; view = v2;
            }
            if (view != IntPtr.Zero)
            {
                _list = FindWindowEx(view, IntPtr.Zero, "SysListView32", null);
                _host = host;
                _found = _host != IntPtr.Zero;
            }
        }

        public static IntPtr Host
        {
            get { if (!_found) Find(); return _host; }
        }

        /// <summary>Esconde (false) o muestra (true) los iconos reales del escritorio.</summary>
        public static void SetIconsVisible(bool visible)
        {
            try
            {
                Find();
                if (_list != IntPtr.Zero) ShowWindow(_list, visible ? 5 : 0);
            }
            catch (Exception ex) { Store.Log(ex); }
        }

        /// <summary>Quita la ventana de Alt+Tab.</summary>
        public static void MakeToolWindow(IntPtr hwnd)
        {
            int ex = GetWindowLong(hwnd, -20);
            SetWindowLong(hwnd, -20, ex | 0x80);
        }

        /// <summary>Coloca la ventana justo encima del escritorio y por debajo de las demás aplicaciones.</summary>
        public static void PinToDesktop(IntPtr hwnd)
        {
            IntPtr host = Host;
            if (host == IntPtr.Zero) return;
            SetWindowPos(hwnd, host, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010); // NOSIZE | NOMOVE | NOACTIVATE
        }

        /// <summary>Archivos y carpetas visibles del escritorio (el del usuario y el público).</summary>
        public static List<string> GetItems()
        {
            var list = new List<string>();
            var dirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
            };
            foreach (var dir in dirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                try
                {
                    foreach (var p in Directory.EnumerateFileSystemEntries(dir))
                    {
                        try
                        {
                            var a = File.GetAttributes(p);
                            if ((a & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;
                            list.Add(p);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return list.OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
    }
}
