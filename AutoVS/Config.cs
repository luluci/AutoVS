using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace AutoVS
{
    static class Config
    {
        public static ConfigImpl config = new ConfigImpl();

        public static string PrevSlnFilePath
        {
            get { return config.json.PrevSlnFilePath; }
            set
            {
                config.json.PrevSlnFilePath = value;
            }
        }

        public static List<VSInfo> VSInfo
        {
            get { return config.json.VSInfo; }
            set
            {
                config.json.VSInfo = value;
            }
        }

        public static async Task Load()
        {
            await config.Load().ConfigureAwait(false);
        }
        public static async Task Save()
        {
            await config.Save().ConfigureAwait(false);
        }
    }

    class ConfigImpl
    {
        private string configFilePath;
        public JsonItem json;

        public ConfigImpl()
        {
            // パス設定
            configFilePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + @".json";
        }

        /** 初回起動用に同期的に動作する
         * 
         */
        public async Task Load()
        {
            // 設定ロード
            if (File.Exists(configFilePath))
            {
                // ファイルが存在する
                //
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                    //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                };
                //
                using (var stream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read))
                {
                    // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                    json = await JsonSerializer.DeserializeAsync<JsonItem>(stream, options).ConfigureAwait(false);
                }
            }
            else
            {
                // ファイルが存在しない
                json = new JsonItem
                {
                    PrevSlnFilePath = "",
                    VSInfo = new List<VSInfo>()
                    {
                        new VSInfo()
                        {
                            Name = "Visual Studio 2017 Express",
                            DteDisplayName = "WDExpress.DTE",
                            ExePath = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\WDExpress\Common7\IDE\wdexpress.exe",
                        }
                    },
                };
            }
        }
        
        public async Task Save()
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
            //
            string jsonStr = JsonSerializer.Serialize(json, options);
            //
            /*
            using (var stream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write))
            using (var writer = new Utf8JsonWriter(stream))
            {
                JsonSerializer.Serialize(writer, json, options);
            }
            */
            using (var stream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write))
            {
                // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                await JsonSerializer.SerializeAsync(stream, json, options).ConfigureAwait(false);
            }
        }
    }

    class JsonItem
    {
        [JsonPropertyName("prev_sln_file_path")]
        public string PrevSlnFilePath { get; set; }

        [JsonPropertyName("visual_studio_info")]
        public List<VSInfo> VSInfo { get; set; } = new List<VSInfo>();

    }
}
