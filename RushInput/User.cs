﻿using System;
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
                    "有（省内乘私家车）",
                    "有（其它情况）"
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
            xxdz = "";
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
                    WebResponse webResponse = webRequest.GetResponse();
                    string cookies = webResponse.Headers.Get("Set-Cookie");
                    Stream respstream = webResponse.GetResponseStream();
                    StreamReader streamReader = new StreamReader(respstream);
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
            catch
            {
                Cookies = "";
                LatestResult = DayTime + "网站异常";
            }
            return LoginResult;
        }
        /// <summary>
        /// 检查当日是否已填报
        /// </summary>
        /// <param name="DayTime">执行时间</param>
        /// <returns>
        /// 已填报: true;
        /// 未填报: false
        /// </returns>
        private bool CheckSubmitted(string DayTime)
        {
            bool flag = false;
            WebRequest webRequest = WebRequest.Create("http://tjxx.lnu.edu.cn/inputExt.asp");
            webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                Stream stream = webResponse.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string ret = reader.ReadToEnd();
                if (ret.IndexOf("已填报") != -1)
                {
                    flag = true;
                }
            }
            return flag;
        }
        /// <summary>
        /// 更新所在地址
        /// </summary>
        private void UpdateLocation()
        {
            WebRequest webRequest = WebRequest.Create("http://tjxx.lnu.edu.cn/inputExt.asp");
            webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                Stream stream = webResponse.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string ret = reader.ReadToEnd();
                string Loca1 = ret.Substring(ret.IndexOf("value=\"") + 7);
                string Loca2 = Loca1.Substring(Loca1.IndexOf("value=\"") + 7);
                Loca1 = Loca1.Substring(0, Loca1.IndexOf('"'));
                Loca2 = Loca2.Substring(0, Loca2.IndexOf('"'));
                xszd = Loca1;
                xxdz = Loca2;
            }
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
                if (CheckSubmitted(DayTime) == false)
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
                            "&xxdz=" + xxdz +
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
                        webRequest.GetResponse();
                    }
                    if (CheckSubmitted(DayTime))
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
        /// 学生所在地省市县
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
        /// 详细地址
        /// </summary>
        private string _xxdz;
        public string xxdz
        {
            get
            {
                return _xxdz;
            }
            set
            {
                _xxdz = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("xxdz"));
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
        public string gdipszd { get; set; }
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