using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoVS
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowViewModel vm;
        private bool isConfigChanged = false;

        public MainWindow()
        {
            Config.Load().Wait();

            InitializeComponent();

            vm = new WindowViewModel();
            DataContext = vm;

            /*
            Closing += (s, e) =>
            {
                vm.Dispose();
                Config.Save().Wait();
            };
            */
            Closed += (s, e) =>
            {
                vm.Dispose();
                if (isConfigChanged)
                {
                    Config.Save().Wait();
                }
            };
        }

        private async void Button_ConnectVs_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnClickConnectVs();
        }

        private void TextBox_SlnFile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // ドロップしたファイル名を全部取得する。
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                vm.OnDropSlnFilePath(filenames);
            }
        }

        private void TextBox_SlnFile_DragOver(object sender, DragEventArgs e)
        {
            // マウスポインタを変更する。
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async void Button_VsOpeStatusBar_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnClickVsOpe();
        }

        private void TextBox_VsInfoExePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            isConfigChanged = true;
        }

        private void TextBox_OpeItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // ドロップしたファイル名を全部取得する。
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                vm.OnDropOpeItem(filenames);
            }
        }

        private void TextBox_OpeItem_PreviewDragOver(object sender, DragEventArgs e)
        {
            // マウスポインタを変更する。
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Button_OpeAdd_Click(object sender, RoutedEventArgs e)
        {
            vm.OnClickOpeAdd();
        }
    }
}
