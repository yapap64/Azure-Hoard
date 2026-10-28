using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace AzureHoard
{
    public partial class App : Application
    {
        public static HoardLayout Data;
        public static readonly List<FenceWindow> Fences = new List<FenceWindow>();

        static DispatcherTimer _refreshTimer;
        static readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        static bool _fencesVisible = true;

        Mutex _mutex;
        bool _owner;
        System.Windows.Forms.NotifyIcon _tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Salida de emergencia:  AzureHoard.exe --restore   (vuelve a mostrar los iconos del escritorio)
            if (e.Args.Any(a => string.Equals(a, "--restore", StringComparison.OrdinalIgnoreCase)))
            {
                Desktop.SetIconsVisible(true);
                Shutdown();
                return;
            }

            bool created;
            _mutex = new Mutex(true, "AzureHoard.SingleInstance", out created);
            if (!created) { Shutdown(); return; }
            _owner = true;

            // Pase lo que pase, los iconos reales del escritorio tienen que volver.
            DispatcherUnhandledException += (s, a) => { Store.Log(a.Exception); Desktop.SetIconsVisible(true); };
            AppDomain.CurrentDomain.UnhandledException += (s, a) => Desktop.SetIconsVisible(true);
            SessionEnding += (s, a) => Desktop.SetIconsVisible(true);

            Desktop.SetIconsVisible(true);
            Data = Store.Load();
            L.Lang = string.IsNullOrEmpty(Data.Lang) ? "en" : Data.Lang;

            if (!Data.Fences.Any(f => f.IsDesktop))
                Data.Fences.Insert(0, new FenceData { IsDesktop = true, X = 60, Y = 60, W = 540, H = 380 });

            foreach (var d in Data.Fences.ToList()) OpenFence(d);

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _refreshTimer.Tick += (s, a) => { _refreshTimer.Stop(); RefreshAll(); };

            RefreshAll();
            Desktop.SetIconsVisible(false);
            WatchDesktop();
            BuildTray();
            SaveLayout();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_owner)
            {
                try { Desktop.SetIconsVisible(true); } catch { }
                if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
            }
            base.OnExit(e);
        }

        // ---------- fences ----------

        public static void SaveLayout()
        {
            if (Data != null) Store.Save(Data);
        }

        static void OpenFence(FenceData d)
        {
            var w = new FenceWindow(d);
            Fences.Add(w);
            w.Show();
        }

        public static void NewFence()
        {
            int n = Fences.Count;
            var d = new FenceData
            {
                Title = L.T("New fence"),
                X = 140 + (n * 36) % 320,
                Y = 140 + (n * 36) % 220,
                W = 320,
                H = 220
            };
            Data.Fences.Add(d);
            OpenFence(d);
            SaveLayout();
        }

        public static void DeleteFence(FenceWindow w)
        {
            if (w.Data.IsDesktop) return;
            Data.Fences.Remove(w.Data);
            Fences.Remove(w);
            w.Close();
            SaveLayout();
            RefreshAll();
        }

        /// <summary>Pone un archivo dentro de una fence. Un archivo solo puede estar en una fence a la vez.</summary>
        public static void AddItem(FenceWindow target, string path)
        {
            if (string.IsNullOrEmpty(path) || !(File.Exists(path) || Directory.Exists(path))) return;
            foreach (var f in Fences)
                f.Data.Items.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            if (!target.Data.IsDesktop) target.Data.Items.Add(path);
            SaveLayout();
            RefreshAll();
        }

        public static void RemoveItem(FenceWindow w, string path)
        {
            w.Data.Items.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            SaveLayout();
            RefreshAll();
        }

        /// <summary>Vuelve a pintar el contenido de todas las fences. La fence "Escritorio" muestra lo que no está en ninguna otra.</summary>
        public static void RefreshAll()
        {
            var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Fences.Where(x => !x.Data.IsDesktop))
            {
                f.Data.Items.RemoveAll(p => !(File.Exists(p) || Directory.Exists(p)));
                foreach (var p in f.Data.Items) assigned.Add(p);
                f.SetItems(f.Data.Items);
            }
            var rest = Desktop.GetItems().Where(p => !assigned.Contains(p)).ToList();
            foreach (var f in Fences.Where(x => x.Data.IsDesktop)) f.SetItems(rest);
        }

        // ---------- vigilar el escritorio ----------

        static void WatchDesktop()
        {
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
                    var w = new FileSystemWatcher(dir)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                        EnableRaisingEvents = true
                    };
                    FileSystemEventHandler h = (s, e) => ScheduleRefresh();
                    w.Created += h;
                    w.Deleted += h;
                    w.Renamed += (s, e) => ScheduleRefresh();
                    _watchers.Add(w);
                }
                catch (Exception ex) { Store.Log(ex); }
            }
        }

        static void ScheduleRefresh()
        {
            Current.Dispatcher.BeginInvoke(new Action(() => { _refreshTimer.Stop(); _refreshTimer.Start(); }));
        }

        // ---------- icono de la bandeja ----------

        static void ToggleFences()
        {
            _fencesVisible = !_fencesVisible;
            foreach (var f in Fences)
            {
                if (_fencesVisible) f.Show(); else f.Hide();
            }
            Desktop.SetIconsVisible(!_fencesVisible);
        }

        void ExitApp()
        {
            Desktop.SetIconsVisible(true);
            foreach (var f in Fences.ToList()) f.Close();
            Shutdown();
        }

        void BuildTray()
        {
            _tray = new System.Windows.Forms.NotifyIcon { Text = "Azure Hoard", Visible = true };
            try { _tray.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath); }
            catch { _tray.Icon = System.Drawing.SystemIcons.Application; }
            _tray.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            _tray.ContextMenuStrip.Opening += (s, e) => FillTrayMenu();
            _tray.DoubleClick += (s, e) => ToggleFences();
        }

        void FillTrayMenu()
        {
            var items = _tray.ContextMenuStrip.Items;
            items.Clear();
            items.Add(L.T("New fence"), null, (s, e) => NewFence());
            items.Add(L.T("Show or hide fences"), null, (s, e) => ToggleFences());

            var lang = new System.Windows.Forms.ToolStripMenuItem(L.T("Language"));
            foreach (var kv in new[] { ("en", "English"), ("es", "Español"), ("gl", "Galego"), ("pt", "Português") })
            {
                string code = kv.Item1;
                var it = new System.Windows.Forms.ToolStripMenuItem(kv.Item2) { Checked = L.Lang == code };
                it.Click += (s, e) =>
                {
                    L.Lang = code;
                    Data.Lang = code;
                    SaveLayout();
                    foreach (var f in Fences) f.RefreshTexts();
                };
                lang.DropDownItems.Add(it);
            }
            items.Add(lang);

            var auto = new System.Windows.Forms.ToolStripMenuItem(L.T("Start with Windows")) { Checked = Autostart.IsOn() };
            auto.Click += (s, e) => Autostart.Set(!Autostart.IsOn());
            items.Add(auto);

            items.Add(L.T("Open data folder"), null, (s, e) =>
            {
                try
                {
                    Directory.CreateDirectory(Store.Dir);
                    Process.Start(new ProcessStartInfo(Store.Dir) { UseShellExecute = true });
                }
                catch (Exception ex) { Store.Log(ex); }
            });
            items.Add(new System.Windows.Forms.ToolStripSeparator());
            items.Add(L.T("Exit"), null, (s, e) => ExitApp());
        }
    }

    /// <summary>Arrancar con Windows (clave Run del usuario, sin permisos de administrador).</summary>
    static class Autostart
    {
        const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Name = "AzureHoard";

        public static bool IsOn()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(KeyPath))
                return k != null && k.GetValue(Name) != null;
        }

        public static void Set(bool on)
        {
            using (var k = Registry.CurrentUser.OpenSubKey(KeyPath, true))
            {
                if (k == null) return;
                if (on) k.SetValue(Name, "\"" + Environment.ProcessPath + "\"");
                else k.DeleteValue(Name, false);
            }
        }
    }
}
