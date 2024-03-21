using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Xml;
using static MyApp.TranslateTool;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;

namespace MyApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        List<Dictionary<string, string>> lists = new List<Dictionary<string, string>>();
        private static DateTime expirationDate = new DateTime(2024, 12, 30); // 假设到期日期为2023年1月1日
        private bool CheckFlag = false;

        public MainWindow()
        {
            if (DateTime.Now >= expirationDate)
            {
                MessageBox.Show("此程序已过期，无法使用。", "程序过期", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(); // 如果到期，关闭程序
            }
            InitializeComponent();
            stMeta.Text = "";

        }

        private void extract(object sender, RoutedEventArgs e)
        {
            trans.Text = string.Empty;
            lists.Clear();
            string[] lines = stMeta.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    MatchString(line.Trim(), lists);
                }
            }

            trans.Text = string.Join("\n", lists.Select(d => d["ChineseContent"]));
        }




        private void MatchString(string input, List<Dictionary<string, string>> lists)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (lists == null)
            {
                throw new ArgumentNullException(nameof(lists));
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            // 预处理
            input = Regex.Replace(input, @"(/+)$", "");

            string pattern = GetPatternBasedOnCheckFlag();
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                string group2Value = match.Groups[2].Value.Trim();
                if (!string.IsNullOrWhiteSpace(group2Value))
                {
                    lists.Add(new Dictionary<string, string>
            {
                {"ChineseContent", group2Value}
            });
                }
            }
        }
        private string GetPatternBasedOnCheckFlag()
        {
            return CheckFlag ? @"(^\d+)\s*:\s*//\s*(.*)" : @"(.*)//\s*(.*)";
        }

        /// <summary>
        /// 翻译按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Translate(object sender, RoutedEventArgs e)
        {
            trans.Text = string.Empty;
            // 翻译内容为空
            if (!lists.Any())
            {
                throw new ArgumentNullException("集合内容为空");
            }

            foreach (var item in lists)
            {
                // 异步调用百度翻译api
                TransJson_Result res = await TranslateTool.GetTranslation(item["ChineseContent"]);

                if (res != null)
                {
                    item["EnglishContent"] = res.trans_result[0].dst;
                    trans.Text += item["ChineseContent"] + ">>" + item["EnglishContent"] + "\r\n";
                }

            }
            MessageBox.Show("翻译完成！");


        }

        private void ReplceContent(object sender, RoutedEventArgs e)
        {
            ResultBox.Text = string.Empty;
            if (trans.Text == string.Empty) return;
            String TempLines = string.Empty;
            string[] lines = stMeta.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            List<Dictionary<string, string>> listTranslate = new List<Dictionary<string, string>>();
            listTranslate = TextBoxTextToList(trans);
            foreach (var line in lines)
            {
                String Temp = string.Empty;
                listTranslate.ForEach((d) =>
                {
                    // 指定key
                    string keyToCheck = "EnglishContent";
                    // 判断集合中是否含有指定key,并且key中有值
                    // 首先检查key是否存在  
                    if (d.ContainsKey(keyToCheck))
                    {
                        // 然后获取对应的value并检查其是否为null（对于引用类型）或有有效值（对于值类型）  
                        string value = d[keyToCheck];
                        if (value != null)
                        {
                            // 预处理
                            string newline = Regex.Replace(line, @"(/+)$", "");

                            // 行中完全匹配字符串
                            string pattern = GetPatternBasedOnCheckFlag();
                            Match matche = Regex.Match(newline.Trim(), pattern);
                            
                            if (matche.Success)
                            {
                                string str = matche.Groups[2].Value.Trim();
                                // 如果为空字符，不做替换，保留原本字符串
                                if (string.IsNullOrWhiteSpace(str))
                                {
                                    Temp = newline;
                                    return;
                                }
                                if (str.Equals(d["ChineseContent"].Trim()))
                                {
                                    Temp = newline.Replace(d["ChineseContent"], d                           ["ChineseContent"] + "\t" + value);
                                    return;
                                }
                            }
                            else
                            {
                                Temp = newline;
                            }

                        }
                        else
                        {
                            throw new Exception("没有翻译内容");
                        }
                    }
                    else
                    {
                        throw new Exception("还没有翻译");
                    }
                });
                TempLines += Temp + "\r\n";
            }


            ResultBox.Text = TempLines;

        }


        public List<Dictionary<string, string>> TextBoxTextToList(TextBox textBox)
        {
            if (trans.Text == string.Empty)
            {
                throw new Exception("翻译文本框为空");
            };

            List<Dictionary<string, string>> li = new List<Dictionary<string, string>>();

            string[] lines = trans.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string line in lines)
            {

                if (!string.IsNullOrEmpty(line))
                {

                    string[] res = line.Split(">>");
                    Dictionary<string, string> dict = new Dictionary<string, string>()
                    {
                        { "ChineseContent",res[0] },
                        { "EnglishContent",res[1] }
                    };
                    li.Add(dict);

                }
            }
            return li;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox check = (CheckBox)sender;
            if (check != null)
            {
                CheckFlag = (bool)check.IsChecked;
            }

        }

        private void Check_Unloaded(object sender, RoutedEventArgs e)
        {
            CheckBox check = (CheckBox)sender;
            if (check != null)
            {
                CheckFlag = (bool)check.IsChecked;
            }
        }

        private void Copy_Btn(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ResultBox.Text);
            MessageBox.Show("复制成功");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            stMeta.Clear();
        }

        private void Btn_Clear2_Click(object sender, RoutedEventArgs e)
        {
            ResultBox.Clear();
        }
    }
}
