using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyApp
{
    public class TranslateTool
    {

        static readonly HttpClient client = new HttpClient();

        private static readonly int delay;
        private static DateTime last;
        private const string API_URL = "https://fanyi-api.baidu.com/api/trans/vip/translate";




        public static async Task<TransJson_Result> GetTranslation(string source, string from = "zh", string to = "en")
        {
            Config config = new Config();
            string ID = config.ID;
            string Key = config.Key;
            try
            {
                using MD5 md5 = MD5.Create();
                byte[] md5byte = md5.ComputeHash(Encoding.UTF8.GetBytes(ID + source + last.Millisecond + Key));
                string md5string = new string(md5byte.SelectMany(s => $"{s:x2}").ToArray());
                string utf8 = System.Web.HttpUtility.UrlEncode(source, Encoding.UTF8);
                string url = $"{API_URL}?q={utf8}&from={from}&to={to}&appid={ID}&salt={last.Millisecond}&sign={md5string}";

                var time = DateTime.Now - last;
                last = DateTime.Now;
                if (time.TotalMilliseconds < delay)
                    await Task.Delay(delay - (int)time.TotalMilliseconds);

                using HttpClient http = new HttpClient();
                return await http.GetFromJsonAsync<TransJson_Result>(url);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return null;
            }
        }

        public class TransJson_Result
        {
            public string error_code { get; set; }
            /// <summary>
            /// 错误消息
            /// </summary>
            public string error_msg { get; set; }
            /// <summary>
            /// 源语种
            /// </summary>
            public string from { get; set; }
            /// <summary>
            /// 目标语种
            /// </summary> 
            public string to { get; set; }
            public Trans_Result[] trans_result { get; set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                foreach (var item in trans_result)
                {
                    yield return new KeyValuePair<string, string>(item.src, item.dst);
                }
            }
        }

        public class Trans_Result
        {
            /// <summary>
            /// 源字符串
            /// </summary>
            public string src { get; set; }
            /// <summary>
            /// 翻译后的字符串
            /// </summary>
            public string dst { get; set; }
        }


        public class Config
        {
            public readonly string ID;
            public readonly string Key;

            public Config()
            {
                // 假设文件位于与可执行文件相同的目录中  
                string relativeFilePath = "App.txt";
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory; // 更稳定地获取当前应用程序的基目录  

                string fullPath = Path.Combine(currentDirectory, relativeFilePath);
                string[] lines = File.ReadAllLines(fullPath); // 逐行读取文件，比ReadAllText更高效  

                try
                {
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            switch (key)
                            {
                                case "ID":
                                    ID = value;
                                    break;
                                case "Key":
                                    Key = value;
                                    break;
                            }
                        }
                    }

                }
                catch (FileNotFoundException ex)
                {
                    // 处理文件未找到的情况，例如记录日志或抛出异常  
                    throw new FileNotFoundException("配置文件未找到。", ex);
                }
                catch (IOException ex)
                {
                    // 处理IO异常，例如记录日志或抛出异常  
                    throw new IOException("读取配置文件时发生错误。", ex);
                }
            }
        }

    }
}
