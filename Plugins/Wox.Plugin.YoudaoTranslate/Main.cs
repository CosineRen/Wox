using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Wox.Infrastructure.Http;
using Newtonsoft.Json;

namespace Wox.Plugin.YoudaoTranslate
{
    public class TranslateResult
    {
        public int errorCode { get; set; }
        public List<string> translation { get; set; }
        public BasicTranslation basic { get; set; }
        public List<WebTranslation> web { get; set; }

    }

    public class BasicTranslation
    {
        public string phonetic { get; set; }
        public List<string> explains { get; set; }
    }

    public class WebTranslation
    {
        public string key { get; set; }
        public List<string> value { get; set; }
    }

    public class RelatedPhrase
    {

    }
    public class Main : IPlugin
    {
        public const string appKey = "316a2c09943b1fca";
        public const string privateKey = "JKR9cxRnN0X4M1Jg8oYKoiVHbgiBFoUw";
        private PluginInitContext context;
        public List<Result> Query(Query query)
        {
            var search = query.Search;
            var results = new List<Result>();
            const string ico = "Images\\youdao.jpg";
            if (string.IsNullOrWhiteSpace(search))
            {
                results.Add(ResultForListCommandAutoComplete(query));
                return results;
            }
            var salt = new Random().Next(1, 10);
            var sign = Encode(appKey + search + salt + privateKey);
            var transUrl =
               $"http://openapi.youdao.com/api?q={search}&from=auto&to=auto&appKey={appKey}&salt={salt}&sign={sign}";
            var json = Http.Get(transUrl).Result;
            TranslateResult o = JsonConvert.DeserializeObject<TranslateResult>(json);
            if (o.errorCode == 0)
            {
                if (o.translation != null)
                {
                    var translation = string.Join(", ", o.translation.ToArray());
                    var title = translation;
                    if (o.basic?.phonetic != null)
                    {
                        title += " [" + o.basic.phonetic + "]";
                    }
                    results.Add(new Result
                    {
                        Title = title,
                        SubTitle = "翻译结果",
                        IcoPath = ico,
                        Action = this.copyToClipboardFunc(translation)
                    });
                }
                if (o.basic?.explains != null)
                {
                    var explantion = string.Join(",", o.basic.explains.ToArray());
                    results.Add(new Result
                    {
                        Title = explantion,
                        SubTitle = "简明释义",
                        IcoPath = ico,
                        Action = this.copyToClipboardFunc(explantion)
                    });
                }
                if (o.web != null)
                {
                    foreach (WebTranslation t in o.web)
                    {
                        var translation = string.Join(",", t.value.ToArray());
                        results.Add(new Result
                        {
                            Title = translation,
                            SubTitle = "网络释义：" + t.key,
                            IcoPath = ico,
                            Action = this.copyToClipboardFunc(translation)
                        });
                    }
                }
            }
            else
            {
                string error = string.Empty;
                switch (o.errorCode)
                {
                    case 20:
                        error = "要翻译的文本过长";
                        break;
                    case 30:
                        error = "无法进行有效的翻译";
                        break;
                    case 40:
                        error = "不支持的语言类型";
                        break;
                    case 50:
                        error = "无效的key";
                        break;
                }
                results.Add(new Result
                {
                    Title = error,
                    IcoPath = ico
                });
            }
            return results;
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
        }


        private Result ResultForListCommandAutoComplete(Query query)
        {
            string title = "有道翻译";
            string subtitle = "有道云智能翻译";
            return ResultForCommand(query, "有道翻译", title, subtitle);
        }

        private Result ResultForCommand(Query query, string command, string title, string subtitle)
        {
            const string seperater = Plugin.Query.TermSeperater;
            var result = new Result
            {
                Title = title,
                IcoPath = "Images\\youdao.jpg",
                SubTitle = subtitle,
                Action = e =>
                {
                    context.API.ChangeQuery($"{query.ActionKeyword}{seperater}{command}{seperater}");
                    return false;
                }
            };
            return result;
        }

        private System.Func<ActionContext, bool> copyToClipboardFunc(string text)
        {
            return c =>
            {
                if (this.copyToClipboard(text))
                {
                    context.API.ShowMsg("翻译已被存入剪贴板");
                }
                else
                {
                    context.API.ShowMsg("剪贴板打开失败，请稍后再试");
                }
                return false;
            };
        }



        private bool copyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (System.Exception e)
            {
                return false;
            }
            return true;    
        }

        private static string Encode(string source)
        {
            return EncryptWithMD5(source);
        }

        private static string EncryptWithMD5(string source)
        {
            byte[] sor = Encoding.UTF8.GetBytes(source);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(sor);
            StringBuilder strbul = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                strbul.Append(result[i].ToString("x2"));//加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位

            }
            return strbul.ToString();
        }
    }
}
