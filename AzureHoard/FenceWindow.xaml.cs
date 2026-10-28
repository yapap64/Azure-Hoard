using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace AzureHoard
{
    /// <summary>Una fence: una ventana translúcida con título que agrupa iconos.</summary>
    public partial class FenceWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x, y, cx, cy;
            public uint flags;
        }

        public FenceData Data { get; private set; }
        public ObservableCollection<ItemVm> Items { get; } = new ObservableCollection<ItemVm>();

        double _expandedH;
        Point _dragStart;
        ItemVm _dragItem;
        readonly DispatcherTimer _saveTimer;

        public FenceWindow(FenceData d)
        {
            InitializeComponent();
            Data = d;
            ItemList.ItemsSource = Items;

            Left = d.X; Top = d.Y;
            Width = Math.Max(160, d.W);
            Height = Math.Max(80, d.H);
            var wa = SystemParameters.WorkArea;
            if (Left > wa.Right - 60 || Left < wa.Left - Width + 60) Left = wa.Left + 40;
            if (Top > wa.Bottom - 40 || Top < wa.Top - 10) Top = wa.Top + 40;
            _expandedH = Math.Max(120, d.H);

            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _saveTimer.Tick += (s, e) => { _saveTimer.Stop(); App.SaveLayout(); };

            ApplyTitle();
            ApplyRolled();

            SourceInitialized += OnSourceInitialized;
            LocationChanged += (s, e) => Changed();
            SizeChanged += (s, e) => Changed();
            // "Mostrar escritorio" (Win+D) minimiza las ventanas: las devolvemos a su sitio
            StateChanged += (s, e) => { if (WindowState != WindowState.Normal) WindowState = WindowState.Normal; };
        }

        // ---------- posición en el escritorio ----------

        void OnSourceInitialized(object sender, EventArgs e)
        {
            IntPtr h = new WindowInteropHelper(this).Handle;
            Desktop.MakeToolWindow(h);
            var src = HwndSource.FromHwnd(h);
            if (src != null) src.AddHook(Hook);
            Desktop.PinToDesktop(h);
        }

        // Cada vez que Windows intenta subir la ventana por encima de otras, la dejamos justo sobre el escritorio.
        IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0046) // WM_WINDOWPOSCHANGING
            {
                IntPtr host = Desktop.Host;
                if (host != IntPtr.Zero)
                {
                    var p = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    p.hwndInsertAfter = host;
                    p.flags &= ~0x0004u; // quitar SWP_NOZORDER
                    Marshal.StructureToPtr(p, lParam, false);
                }
            }
            return IntPtr.Zero;
        }

        void Changed()
        {
            if (!IsLoaded) return;
            Data.X = Left; Data.Y = Top; Data.W = Width;
            if (!Data.Rolled) Data.H = Height;
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        // ---------- título y enrollar ----------

        public void ApplyTitle()
        {
            TitleText.Text = Data.IsDesktop ? L.T("Desktop") : Data.Title;
        }

        public void RefreshTexts()
        {
            ApplyTitle();
        }

        void ApplyRolled()
        {
            if (Data.Rolled)
            {
                if (Height > 40) _expandedH = Height;
                Height = 34;
                ItemList.Visibility = Visibility.Collapsed;
                Grip.Visibility = Visibility.Collapsed;
            }
            else
            {
                Height = _expandedH;
                ItemList.Visibility = Visibility.Visible;
                Grip.Visibility = Visibility.Visible;
            }
        }

        void ToggleRoll()
        {
            Data.Rolled = !Data.Rolled;
            ApplyRolled();
            App.SaveLayout();
        }

        void Title_Down(object sender, MouseButtonEventArgs e)
        {
            if (TitleEdit.Visibility == Visibility.Visible) return;
            if (e.ClickCount == 2) { ToggleRoll(); return; }
            try { DragMove(); } catch { }
        }

        void Title_Right(object sender, MouseButtonEventArgs e)
        {
            var m = new ContextMenu();
            if (!Data.IsDesktop) m.Items.Add(MI(L.T("Rename"), BeginRename));
            m.Items.Add(MI(L.T("Roll up or unroll"), ToggleRoll));
            m.Items.Add(MI(L.T("New fence"), App.NewFence));
            if (!Data.IsDesktop) m.Items.Add(MI(L.T("Delete fence"), () => App.DeleteFence(this)));
            m.IsOpen = true;
        }

        static MenuItem MI(string header, Action action)
        {
            var item = new MenuItem { Header = header };
            item.Click += (s, e) => action();
            return item;
        }

        void BeginRename()
        {
            if (Data.IsDesktop) return;
            TitleEdit.Text = Data.Title;
            TitleEdit.Visibility = Visibility.Visible;
            TitleText.Visibility = Visibility.Collapsed;
            Activate();
            TitleEdit.Focus();
            TitleEdit.SelectAll();
        }

        void EndRename(bool ok)
        {
            if (TitleEdit.Visibility != Visibility.Visible) return;
            string t = TitleEdit.Text.Trim();
            if (ok && t.Length > 0) Data.Title = t;
            TitleEdit.Visibility = Visibility.Collapsed;
            TitleText.Visibility = Visibility.Visible;
            ApplyTitle();
            App.SaveLayout();
        }

        void TitleEdit_Lost(object sender, RoutedEventArgs e) { EndRename(true); }

        void TitleEdit_Key(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) EndRename(true);
            else if (e.Key == Key.Escape) EndRename(false);
        }

        // ---------- tamaño ----------

        void Grip_Delta(object sender, DragDeltaEventArgs e)
        {
            Width = Math.Max(160, Width + e.HorizontalChange);
            Height = Math.Max(100, Height + e.VerticalChange);
        }

        // ---------- iconos ----------

        public void SetItems(IEnumerable<string> paths)
        {
            Items.Clear();
            foreach (var p in paths) Items.Add(new ItemVm(p));
        }

        static ItemVm ItemAt(object source)
        {
            var d = source as DependencyObject;
            while (d != null)
            {
                var fe = d as FrameworkElement;
                if (fe != null && fe.DataContext is ItemVm) return (ItemVm)fe.DataContext;
                d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            }
            return null;
        }

        static void OpenPath(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { Store.Log(ex); }
        }

        void List_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var v = ItemAt(e.OriginalSource);
            if (v != null) OpenPath(v.Path);
        }

        void List_Right(object sender, MouseButtonEventArgs e)
        {
            var v = ItemAt(e.OriginalSource);
            if (v == null) return;
            var m = new ContextMenu();
            m.Items.Add(MI(L.T("Open"), () => OpenPath(v.Path)));
            m.Items.Add(MI(L.T("Show in folder"), () =>
            {
                try { Process.Start("explorer.exe", "/select,\"" + v.Path + "\""); }
                catch (Exception ex) { Store.Log(ex); }
            }));
            if (!Data.IsDesktop) m.Items.Add(MI(L.T("Remove from this fence"), () => App.RemoveItem(this, v.Path)));
            m.IsOpen = true;
            e.Handled = true;
        }

        // ---------- arrastrar y soltar ----------

        void List_PreviewDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            _dragItem = ItemAt(e.OriginalSource);
        }

        void List_PreviewMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragItem == null) return;
            Point p = e.GetPosition(null);
            if (Math.Abs(p.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(p.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            var item = _dragItem;
            _dragItem = null;
            // Solo un formato propio: así nunca se mueve el archivo real por accidente al soltarlo fuera.
            var data = new DataObject("AzureHoard.Item", new string[] { Data.Id, item.Path });
            DragDrop.DoDragDrop(ItemList, data, DragDropEffects.Move);
        }

        void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("AzureHoard.Item")) e.Effects = DragDropEffects.Move;
            else if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Link;
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("AzureHoard.Item"))
            {
                var a = e.Data.GetData("AzureHoard.Item") as string[];
                if (a != null && a.Length > 1) App.AddItem(this, a[1]);
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null) foreach (var f in files) App.AddItem(this, f);
            }
            e.Handled = true;
        }
    }
}
