using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Setup.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace AutoVS
{
    class VSInfo
    {
        public string Name { get; set; }
        public string DteDisplayName { get; set; }
        public string ExePath { get; set; }
    }

    class VsDte : IDisposable
    {
        private EnvDTE.DTE dte = null;
        private Process proc = null;

        public VsDte()
        {
            // https://docs.microsoft.com/ja-jp/visualstudio/extensibility/launch-visual-studio-dte?view=vs-2019
            // https://qiita.com/tafuji/items/fd36c373f770d1ef4707

            // VisualStudio固有設定を初期化
            // "WDExpress.DTE.15.0:{ProcessID}" : VisualStudio2017 Express
            vsinfo = new ObservableCollection<VSInfo>();
            foreach (var info in Config.VSInfo)
            {
                vsinfo.Add(info);
            }
        }

        private ObservableCollection<VSInfo> vsinfo;
        public ObservableCollection<VSInfo> VsInfo
        {
            get { return vsinfo; }
        }
        public int SelectIndexVsInfo { get; set; }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出する

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Quiteすると対象のVisualStudioが終了する
                    //dte?.Quit();
                    // 自分でプロセスを立ち上げた場合は処理終了時にプロセス終了する？
                    //proc?.Dispose();
                }

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion

        public bool StatusBar(string msg)
        {
            if (dte == null) return false;

            try
            {
                dte.StatusBar.Text = msg;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                // なんらかの要因でDTEの接続が切れていると見なす
                dte = null;
                return false;
            }

            return true;
        }

        public bool Connect(string slnpath)
        {
            // 最初に起動中のプロセスから検索
            dte = GetVsDte(slnpath);
            // 見つからなかったら手動で起動する
            if (dte == null)
            {
                // VisualStudio起動
                LaunchVs(slnpath);
                // 起動したら改めて取得
                dte = TryGetVsDte(1000, slnpath);
            }

            return (dte != null);
        }

        private void LaunchVs(string slnpath)
        {
            var app = new ProcessStartInfo();
            app.FileName = vsinfo[SelectIndexVsInfo].ExePath;
            app.Arguments = slnpath;
            //app.UseShellExecute = true;   // プロセス起動に失敗する場合は有効にする
            proc = Process.Start(app);
            proc.WaitForInputIdle();
        }

        private EnvDTE.DTE TryGetVsDte(int wait, string sln)
        {
            EnvDTE.DTE tempDte = null;

            // 60回DTE取得をトライ
            for (int i=0; i<60; i++)
            {
                // DTE取得トライ
                tempDte = GetVsDte(sln);
                if (tempDte != null) return tempDte;
                // 失敗したら少し待つ
                Thread.Sleep(millisecondsTimeout: wait);
            }

            return null;
        }

        private EnvDTE.DTE GetVsDte(string sln)
        {
            EnvDTE.DTE tempDte = null;

            // プロセス一覧を取得
            IEnumerable<string> runningObjectDisplayNames = null;
            GetRunningObject("dummy", out runningObjectDisplayNames);
            // 指定されたプロセスを検索
            var re = new Regex($@"{vsinfo[SelectIndexVsInfo].DteDisplayName}\.DTE");
            foreach (var obj in runningObjectDisplayNames)
            {
                if (re.IsMatch(obj))
                {
                    // VisualStudioのプロセスを検出したら、指定されたソリューションかチェック
                    tempDte = GetRunningObject(obj, out runningObjectDisplayNames) as EnvDTE.DTE;
                    try
                    {
                        if (tempDte.Solution.FileName.Equals(sln))
                        {
                            return tempDte;
                        }
                    }
                    catch
                    {
                        // COM操作に失敗することがある
                        // その時はスルーして終了
                    }
                }
            }

            return null;
        }

        private static object GetRunningObject(string displayName, out IEnumerable<string> runningObjectDisplayNames)
        {
            IBindCtx bindContext = null;
            NativeMethods.CreateBindCtx(0, out bindContext);

            IRunningObjectTable runningObjectTable = null;
            bindContext.GetRunningObjectTable(out runningObjectTable);

            IEnumMoniker monikerEnumerator = null;
            runningObjectTable.EnumRunning(out monikerEnumerator);

            object runningObject = null;
            List<string> runningObjectDisplayNameList = new List<string>();
            IMoniker[] monikers = new IMoniker[1];
            IntPtr numberFetched = IntPtr.Zero;
            while (monikerEnumerator.Next(1, monikers, numberFetched) == 0)
            {
                IMoniker moniker = monikers[0];

                string objectDisplayName = null;
                try
                {
                    moniker.GetDisplayName(bindContext, null, out objectDisplayName);
                }
                catch (UnauthorizedAccessException)
                {
                    // Some ROT objects require elevated permissions.
                }

                if (!string.IsNullOrWhiteSpace(objectDisplayName))
                {
                    runningObjectDisplayNameList.Add(objectDisplayName);
                    if (objectDisplayName.EndsWith(displayName, StringComparison.Ordinal))
                    {
                        runningObjectTable.GetObject(moniker, out runningObject);
                        if (runningObject == null)
                        {
                            throw new InvalidOperationException($"Failed to get running object with display name {displayName}");
                        }
                    }
                }
            }

            runningObjectDisplayNames = runningObjectDisplayNameList;
            return runningObject;
        }

        private static ISetupInstance GetSetupInstance(bool isPreRelease)
        {
            return GetSetupInstances().First(i => IsPreRelease(i) == isPreRelease);
        }

        private static IEnumerable<ISetupInstance> GetSetupInstances()
        {
            ISetupConfiguration setupConfiguration = new SetupConfiguration();
            IEnumSetupInstances enumerator = setupConfiguration.EnumInstances();

            int count;
            do
            {
                ISetupInstance[] setupInstances = new ISetupInstance[1];
                enumerator.Next(1, setupInstances, out count);
                if (count == 1 && setupInstances[0] != null)
                {
                    yield return setupInstances[0];
                }
            }
            while (count == 1);
        }

        private static bool IsPreRelease(ISetupInstance setupInstance)
        {
            ISetupInstanceCatalog setupInstanceCatalog = (ISetupInstanceCatalog)setupInstance;
            return setupInstanceCatalog.IsPrerelease();
        }

        private static class NativeMethods
        {
            [DllImport("ole32.dll")]
            public static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);
        }

    }

}
