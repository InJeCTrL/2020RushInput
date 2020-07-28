using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;

namespace RushInput
{
    public class User: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public static string[] opt_csld
        {
            get
            {
                return new[]
                {
                    "无",
                    "有（省内）",
                    "有（省外、境外）"
                };
            }
        }
        public static string[] opt_hbjc
        {
            get
            {
                return new[]
                {
                    "无",
                    "有"
                };
            }
        }
        public static string[] opt_fyzz
        {
            get
            {
                return new[]
                {
                    "健康",
                    "发热",
                    "其它"
                };
            }
        }
        public static string[] opt_glzt
        {
            get
            {
                return new[]
                {
                    "否",
                    "居家隔离",
                    "集中隔离",
                    "医学隔离"
                };
            }
        }
        public static string[] opt_zxzg
        {
            get
            {
                return new[]
                {
                    "否",
                    "是（蒲河校区）",
                    "是（崇山校区）",
                    "是（武圣校区）"
                };
            }
        }
        public User()
        {
            userid = "学号";
            userpwd = "密码";
            Cookies = "";
            UseLatestLoc = true;
            IPLocFolowLoc = true;
            xszd = "";
            xszq = "";
            xszjd = "";
            xszsq = "";
            jtdz = "";
            csld = opt_csld[0];
            csldxx = "";
            hbjc = opt_hbjc[0];
            hbjcxx = "";
            fyzz = opt_fyzz[0];
            fyzzxx = "";
            glzt = opt_glzt[0];
            zxzg = opt_zxzg[0];
            gdipszd = "";
            bdipszd = "";
            txipszd = "";
            LatestResult = "";
        }
        /// <summary>
        /// 学号
        /// </summary>
        public string userid { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string userpwd { get; set; }
        /// <summary>
        /// 成功登录Cookies
        /// </summary>
        public string Cookies { get; set; }
        /// <summary>
        /// 填报页面HTML
        /// </summary>
        private string page_Input = "";
        /// <summary>
        /// 验证网站、账号并尝试获取Cookies
        /// </summary>
        /// <param name="DayTime">执行时间</param>
        /// <returns>
        /// 登录成功: true, 并设置Cookies;
        /// 登录失败: false, 并设置LatestResult
        /// </returns>
        private bool Verify(string DayTime)
        {
            bool LoginResult = false;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://tjxx.lnu.edu.cn/login_do.asp");
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            // 禁止自动跳转
            webRequest.AllowAutoRedirect = false;
            try
            {
                using (Stream reqstream = webRequest.GetRequestStream())
                {
                    byte[] LoginPayload = Encoding.UTF8.GetBytes("userid=" + userid + "&userpwd=" + userpwd);
                    reqstream.Write(LoginPayload, 0, LoginPayload.Length);
                    reqstream.Flush();
                    using (WebResponse webResponse = webRequest.GetResponse())
                    {
                        string cookies = webResponse.Headers.Get("Set-Cookie");
                        using (Stream respstream = webResponse.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(respstream))
                            {
                                string ret = streamReader.ReadToEnd();
                                // Console.WriteLine(cookies);
                                if (ret.IndexOf("当日填报结束") != -1)
                                {
                                    Cookies = "";
                                    LatestResult = DayTime + "当日填报结束";
                                }
                                else if (ret.IndexOf("title: '登录失败'") != -1)
                                {
                                    Cookies = "";
                                    LatestResult = DayTime + "用户名或密码错误";
                                }
                                else
                                {
                                    Cookies = cookies;
                                    LoginResult = true;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Cookies = "";
                LatestResult = DayTime + "网站异常";
            }
            finally
            {
                if (webRequest != null)
                {
                    webRequest.Abort();
                }
            }
            return LoginResult;
        }
        /// <summary>
        /// 检查当日是否已填报
        /// </summary>
        /// <returns>
        /// 已填报: true;
        /// 未填报: false
        /// </returns>
        private bool CheckSubmitted()
        {
            bool flag = false;
            WebRequest webRequest = WebRequest.Create("http://tjxx.lnu.edu.cn/inputExt.asp");
            webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream stream = webResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        page_Input = reader.ReadToEnd();
                        if (page_Input.IndexOf("已填报") != -1)
                        {
                            flag = true;
                        }
                    }

                }
            }
            if (webRequest != null)
            {
                webRequest.Abort();
            }
            return flag;
        }
        /// <summary>
        /// 更新所在地址
        /// </summary>
        private void UpdateLocation()
        {
            string Loca1 = page_Input.Substring(page_Input.IndexOf("value=\"") + 7);
            string Loca2 = Loca1.Substring(Loca1.IndexOf("value=\"") + 7);
            string Loca3 = Loca2.Substring(Loca2.IndexOf("value=\"") + 7);
            string Loca4 = Loca3.Substring(Loca3.IndexOf("value=\"") + 7);
            string Loca5 = Loca4.Substring(Loca4.IndexOf("value=\"") + 7);
            Loca1 = Loca1.Substring(0, Loca1.IndexOf('"'));
            Loca2 = Loca2.Substring(0, Loca2.IndexOf('"'));
            Loca3 = Loca3.Substring(0, Loca3.IndexOf('"'));
            Loca4 = Loca4.Substring(0, Loca4.IndexOf('"'));
            Loca5 = Loca5.Substring(0, Loca5.IndexOf('"'));
            xszd = Loca1;
            xszq = Loca2;
            xszjd = Loca3;
            xszsq = Loca4;
            jtdz = Loca5;
        }
        /// <summary>
        /// 更新IP所在地址
        /// </summary>
        private void UpdateIPLocation()
        {
            gdipszd = xszd;
        }
        /// <summary>
        /// 提交填报信息
        /// </summary>
        /// <param name="time">执行时间</param>
        public void Submit(DateTime time)
        {
            string DayTime = time.Year.ToString() + "." +
                                time.Month.ToString() + "." +
                                time.Day.ToString() + " ";
            if (Verify(DayTime))
            {
                if (CheckSubmitted() == false)
                {
                    if (UseLatestLoc)
                    {
                        UpdateLocation();
                    }
                    if (IPLocFolowLoc)
                    {
                        UpdateIPLocation();
                    }
                    WebRequest webRequest = WebRequest.Create("http://tjxx.lnu.edu.cn/inputExt_do.asp");
                    webRequest.Method = "POST";
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
                    using (Stream reqstream = webRequest.GetRequestStream())
                    {
                        byte[] Payload = Encoding.UTF8.GetBytes(
                            "xszd=" + xszd +
                            "&xszq=" + xszq +
                            "&xszjd=" + xszjd +
                            "&xszsq=" + xszsq +
                            "&jtdz=" + jtdz +
                            "&csld=" + csld +
                            "&csldxx=" + csldxx +
                            "&hbjc=" + hbjc +
                            "&hbjcxx=" + hbjcxx +
                            "&fyzz=" + fyzz +
                            "&fyzzxx=" + fyzzxx +
                            "&glzt=" + glzt +
                            "&zxzg=" + zxzg +
                            "&gdipszd=" + gdipszd +
                            "&bdipszd=" + bdipszd +
                            "&txipszd=" + txipszd
                            );
                        reqstream.Write(Payload, 0, Payload.Length);
                        reqstream.Flush();
                        WebResponse webResponse = webRequest.GetResponse();
                        webResponse.Dispose();
                        webResponse.Close();
                    }
                    if (webRequest != null)
                    {
                        webRequest.Abort();
                    }
                    if (CheckSubmitted())
                    {
                        LatestResult = DayTime + "自动填报成功";
                    }
                    else
                    {
                        LatestResult = DayTime + "!!!自动填报失败!!!";
                    }
                }
                else
                {
                    if (UseLatestLoc)
                    {
                        UpdateLocation();
                    }
                    if (IPLocFolowLoc)
                    {
                        UpdateIPLocation();
                    }
                    LatestResult = DayTime + "已填报, 无需重复提交";
                }
            }
        }
        /// <summary>
        /// 是否使用最近一次所在地信息
        /// </summary>
        public bool UseLatestLoc { get; set; }
        /// <summary>
        /// IP定位地址是否跟随所在地信息
        /// </summary>
        public bool IPLocFolowLoc { get; set; }
        /// <summary>
        /// 学生所在地省市
        /// </summary>
        private string _xszd;
        public string xszd
        {
            get
            {
                return _xszd;
            }
            set
            {
                _xszd = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("xszd"));
                }
            }
        }
        /// <summary>
        /// 学生所在地县（区）
        /// </summary>
        private string _xszq;
        public string xszq
        {
            get
            {
                return _xszq;
            }
            set
            {
                _xszq = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("xszq"));
                }
            }
        }
        /// <summary>
        /// 学生所在地街道（乡镇）
        /// </summary>
        private string _xszjd;
        public string xszjd
        {
            get
            {
                return _xszjd;
            }
            set
            {
                _xszjd = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("xszjd"));
                }
            }
        }
        /// <summary>
        /// 学生所在地社区（村）
        /// </summary>
        private string _xszsq;
        public string xszsq
        {
            get
            {
                return _xszsq;
            }
            set
            {
                _xszsq = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("xszsq"));
                }
            }
        }
        /// <summary>
        /// 详细地址
        /// </summary>
        private string _jtdz;
        public string jtdz
        {
            get
            {
                return _jtdz;
            }
            set
            {
                _jtdz = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("jtdz"));
                }
            }
        }
        /// <summary>
        /// 城市间流动经历选项
        /// </summary>
        public string csld { get; set; }
        /// <summary>
        /// 城市间流动经历信息
        /// </summary>
        public string csldxx { get; set; }
        /// <summary>
        /// 人员接触选项
        /// </summary>
        public string hbjc { get; set; }
        /// <summary>
        /// 人员接触信息
        /// </summary>
        public string hbjcxx { get; set; }
        /// <summary>
        /// 当日身体症状选项
        /// </summary>
        public string fyzz { get; set; }
        /// <summary>
        /// 当日身体症状信息
        /// </summary>
        public string fyzzxx { get; set; }
        /// <summary>
        /// 隔离状态
        /// </summary>
        public string glzt { get; set; }
        /// <summary>
        /// 在校在岗
        /// </summary>
        public string zxzg { get; set; }
        /// <summary>
        /// 高德IP定位所在地
        /// </summary>
        private string _gdipszd;
        public string gdipszd
        {
            get
            {
                return _gdipszd;
            }
            set
            {
                _gdipszd = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("gdipszd"));
                }
            }
        }
        /// <summary>
        /// 百度IP定位所在地
        /// </summary>
        public string bdipszd { get; set; }
        /// <summary>
        /// 腾讯IP定位所在地
        /// </summary>
        public string txipszd { get; set; }
        /// <summary>
        /// 最近一次执行结果
        /// </summary>
        private string _LatestResult;
        public string LatestResult
        {
            get
            {
                return _LatestResult;
            }
            set
            {
                _LatestResult = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LatestResult"));
                }
            }
        }
    }
}
