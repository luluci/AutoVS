using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoVS
{
    class WindowViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            var e = new PropertyChangedEventArgs(PropertyName);
            PropertyChanged?.Invoke(this, e);
        }

        private string updateTgtFolder;
        private VsDte dtevs;

        public WindowViewModel()
        {
            dtevs = new VsDte();

            // GUI設定
            VsDteConnectState = "接続";
            OpeStatus = "ツール起動しました";
            SlnFilePath = Config.PrevSlnFilePath;

            //
            updateTgtFolder = "test_dir1";
        }

        private void UpdateConfig()
        {
            Config.VSInfo.Clear();
            foreach (var item in dtevs.VsInfo)
            {
                Config.VSInfo.Add(item);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Config設定を更新
                    UpdateConfig();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        private string opeStatus;
        public string OpeStatus
        {
            get { return opeStatus; }
            set
            {
                opeStatus = value;
                NotifyPropertyChanged(nameof(OpeStatus));
            }
        }

        public ObservableCollection<VSInfo> VsInfo
        {
            get { return dtevs.VsInfo; }
        }
        private int selectIndexVsInfo = 0;
        public int SelectIndexVsInfo
        {
            get { return selectIndexVsInfo; }
            set
            {
                //
                selectIndexVsInfo = value;
                dtevs.SelectIndexVsInfo = value;
                NotifyPropertyChanged(nameof(SelectIndexVsInfo));
            }
        }

        private bool vsDteConnectEnable = true;
        public bool VsDteConnectEnable
        {
            get { return vsDteConnectEnable; }
            set
            {
                vsDteConnectEnable = value;
                NotifyPropertyChanged(nameof(VsDteConnectEnable));
            }
        }

        private string vsDteConnectState = "";
        public string VsDteConnectState
        {
            get { return vsDteConnectState; }
            set
            {
                vsDteConnectState = value;
                NotifyPropertyChanged(nameof(VsDteConnectState));
            }
        }

        public string VsSlnName
        {
            get { return dtevs.VsSlnName; }
        }

        public ObservableCollection<VSProjectInfo> VsSlnInfo
        {
            get { return dtevs.VsSlnInfo; }
        }
        private int selectIndexVsSlnInfo = 0;
        public int SelectIndexVsSlnInfo
        {
            get { return selectIndexVsSlnInfo; }
            set
            {
                selectIndexVsSlnInfo = value;
                dtevs.SelectIndexVsSlnInfo = value;
                NotifyPropertyChanged(nameof(SelectIndexVsSlnInfo));

                //dtevs.GetDir();
                // 選択したプロジェクトの情報をチェック
                if (dtevs.HasFolderInSelectProject(updateTgtFolder))
                {
                    IsEnableUpdateFolder = true;
                }
                else
                {
                    IsEnableUpdateFolder = false;
                }
            }
        }

        public void UpdateVsSlnInfo()
        {
            NotifyPropertyChanged(nameof(VsSlnName));
            NotifyPropertyChanged(nameof(VsSlnInfo));
            SelectIndexVsSlnInfo = 0;
        }

        public async Task OnClickConnectVs()
        {
            if (SlnFilePath.Length == 0)
            {
                OpeStatus = "ソリューションファイルを指定してください。";
                return;
            }

            VsDteConnectEnable = false;
            OpeStatus = "VisualStudioに接続しています…";
            VsDteConnectState = "接続中…";
            //var dte = GetVsDte("WDExpress", @"D:\home\csharp\TaskTimer\TaskTimer.sln");
            //Connect("WDExpress", @"D:\home\csharp\TaskTimer\TaskTimer.sln");
            var result = await Task.Run(() =>
            {
                return dtevs.Connect(SlnFilePath);
            });
            if (result)
            {
                // ソリューション情報ロード
                dtevs.LoadSolutionInfo();
                UpdateVsSlnInfo();
                // GUI更新
                OpeStatus = "VisualStudioに接続しました。";
                VsDteConnectState = "切断";
            }
            else
            {
                OpeStatus = "VisualStudioへの接続に失敗しました。";
                VsDteConnectState = "接続";
            }
            VsDteConnectEnable = true;
        }

        private string slnFilePath = "";
        public string SlnFilePath
        {
            get { return slnFilePath; }
            set
            {
                slnFilePath = value;
                NotifyPropertyChanged(nameof(SlnFilePath));
            }
        }

        public void OnDropSlnFilePath(string[] filenames)
        {
            SlnFilePath = filenames[0];
            Config.PrevSlnFilePath = filenames[0];
        }

        private string vsOpeStatusBarText = "";
        public string VsOpeStatusBarText
        {
            get { return vsOpeStatusBarText; }
            set
            {
                vsOpeStatusBarText = value;
                NotifyPropertyChanged(nameof(VsOpeStatusBarText));
            }
        }

        public void FailedVsOpe()
        {
            // 
            OpeStatus = "VisualStudioの操作に失敗しました。おそらく切断されています。最初からやり直してください";
            VsDteConnectState = "接続";
        }

        public async Task OnClickVsOpe()
        {
            var result = await Task.Run(() =>
            {
                return dtevs.StatusBar(vsOpeStatusBarText);
            });
            if (!result)
            {
                FailedVsOpe();
            }
        }


        private string opeItem = "";
        public string OpeItem
        {
            get { return opeItem; }
            set
            {
                opeItem = value;
                NotifyPropertyChanged(nameof(OpeItem));
            }
        }

        public void OnDropOpeItem(string[] filenames)
        {
            OpeItem = filenames[0];
        }

        public void OnClickOpeAdd()
        {
            dtevs.AddFile("");
        }


        private bool isEnableUpdateFolder = false;
        public bool IsEnableUpdateFolder
        {
            get
            {
                return isEnableUpdateFolder;
            }
            set
            {
                isEnableUpdateFolder = value;
                NotifyPropertyChanged(nameof(IsEnableUpdateFolder));
            }
        }

        public async Task OnClickUpdateFolder()
        {
            IsEnableUpdateFolder = false;
            await Task.Run(() =>
            {
                dtevs.UpdateFolder(updateTgtFolder);
            });
            IsEnableUpdateFolder = true;
        }
    }
}
