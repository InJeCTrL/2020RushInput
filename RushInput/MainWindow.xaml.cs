using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RushInput
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public class UserInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public string UID { get; set; }
            public string Pwd { get; set; }
            private string _Status;
            public string Status
            {
                get
                {
                    return _Status;
                }
                set
                {
                    _Status = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Status"));
                    }
                }
            }
            private string _Location1;
            public string Location1
            {
                get
                {
                    return _Location1;
                }
                set
                {
                    _Location1 = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Location1"));
                    }
                }
            }
            private string _Location2;
            public string Location2
            {
                get
                {
                    return _Location2;
                }
                set
                {
                    _Location2 = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Location2"));
                    }
                }
            }
            private string _SpecialPayload;
            public string SpecialPayload
            {
                get
                {
                    return _SpecialPayload;
                }
                set
                {
                    _SpecialPayload = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SpecialPayload"));
                    }
                }
            }
            private bool _UseLastLocation;
            public bool UseLastLocation
            {
                get
                {
                    return _UseLastLocation;
                }
                set
                {
                    _UseLastLocation = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("UseLastLocation"));
                    }
                }
            }
            private bool _UseDefaultPayload;
            public bool UseDefaultPayload
            {
                get
                {
                    return _UseDefaultPayload;
                }
                set
                {
                    _UseDefaultPayload = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("UseDefaultPayload"));
                    }
                }
            }
        }
        private readonly string HealthInfo = "&csld=%E6%97%A0&csldxx=&hbjc=%E6%97%A0&hbjcxx=&fyzz=%E5%81%A5%E5%BA%B7&fyzzxx=&glzt=%E5%90%A6&zxzg=%E5%90%A6";
        private ObservableCollection<UserInfo> UserList = new ObservableCollection<UserInfo>();
        private Timer t;
        public MainWindow()
        {
            InitializeComponent();
            UserGrid.DataContext = UserList;
            t = new Timer(Timedup);
            SetTime();
        }
        /// <summary>
        /// 达到预定时间
        /// </summary>
        /// <param name="obj"></param>
        private void Timedup(object obj)
        {
            RushList(DateTime.Now);
            SetTime(true);
        }
        /// <summary>
        /// 过一遍整个列表
        /// </summary>
        /// <param name="obj">时间</param>
        private void RushList(DateTime time)
        {
            Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    UserGrid.IsReadOnly = true;
                });          
                foreach (var item in UserList)
                {
                    RushOne(item, time);
                }
                this.Dispatcher.Invoke(() =>
                {
                    UserGrid.IsReadOnly = false;
                });
            });
        }
        /// <summary>
        /// 对列表中某一项操作
        /// </summary>
        /// <param name="userInfo">用户信息</param>
        /// <param name="time">执行时间</param>
        private void RushOne(UserInfo userInfo, DateTime time)
        {
            string Cookies = Login(userInfo.UID, userInfo.Pwd);
            // 登录失败: 今日填报时间已过
            if (Cookies == "当日填报结束")
            {
                userInfo.Status = time.Year.ToString() + "." +
                                  time.Month.ToString() + "." +
                                  time.Day.ToString() + "不在填报时间";
            }
            // 登录失败: 用户名或密码错误
            else if (Cookies == "用户名或密码错误")
            {
                userInfo.Status = time.Year.ToString() + "." +
                                  time.Month.ToString() + "." +
                                  time.Day.ToString() + "用户名或密码错误";
            }
            // 登录成功
            else
            {
                // 使用最后一次提交的地址
                if (userInfo.UseLastLocation)
                {
                    SetLocation(Cookies, userInfo);
                }
                // 该用户今日已提交
                if (Check(Cookies))
                {
                    userInfo.Status = time.Year.ToString() +  "." + 
                                      time.Month.ToString() + "." + 
                                      time.Day.ToString() + "已填报, 无需重复提交";
                }
                // 今日未提交
                else
                {
                    Submit(Cookies, userInfo.Location1, userInfo.Location2, userInfo.UseDefaultPayload, userInfo.SpecialPayload);
                    if (Check(Cookies))
                    {
                        userInfo.Status = time.Year.ToString() + "." +
                                          time.Month.ToString() + "." +
                                          time.Day.ToString() + "自动填报成功";
                    }
                    else
                    {
                        userInfo.Status = time.Year.ToString() + "." +
                                          time.Month.ToString() + "." +
                                          time.Day.ToString() + "!!!自动填表失败!!!";
                    }
                }
            }
        }
        /// <summary>
        /// 尝试登录并获取Cookies
        /// </summary>
        /// <param name="UID">账号</param>
        /// <param name="Pwd">密码</param>
        /// <returns>非填报时间返回"当日填报结束", 否则返回Cookies</returns>
        private string Login(string UID, string Pwd)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://tjxx.lnu.edu.cn/login_do.asp");
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            // 禁止自动跳转
            webRequest.AllowAutoRedirect = false;
            Stream reqstream = webRequest.GetRequestStream();
            byte[] LoginPayload = Encoding.UTF8.GetBytes("userid=" + Uri.EscapeDataString(UID ?? "") + "&userpwd=" + Uri.EscapeDataString(Pwd ?? ""));
            reqstream.Write(LoginPayload, 0, LoginPayload.Length);
            reqstream.Close();
            WebResponse webResponse = webRequest.GetResponse();
            string cookies = webResponse.Headers.Get("Set-Cookie");
            Stream respstream = webResponse.GetResponseStream();
            StreamReader streamReader = new StreamReader(respstream);
            string ret = streamReader.ReadToEnd();
            webResponse.Close();
            Console.WriteLine(cookies);
            if (ret.IndexOf("当日填报结束") != -1)
            {
                return "当日填报结束";
            }
            else if (ret.IndexOf("title: '登录失败'") != -1)
            {
                return "用户名或密码错误";
            }
            else
            {
                return cookies;
            }
        }
        /// <summary>
        /// 提交填报信息
        /// </summary>
        /// <param name="Cookies">登录Cookies</param>
        /// <param name="Location1">行政区划</param>
        /// <param name="Location2">详细住址</param>
        /// <param name="UseDefaultPayload">是否使用默认载荷</param>
        /// <param name="SpecialPayload">自定义的特殊载荷</param>
        private void Submit(string Cookies, string Location1, string Location2, bool UseDefaultPayload, string SpecialPayload)
        {
            WebRequest webRequest = WebRequest.Create("http://tjxx.lnu.edu.cn/inputExt_do.asp");
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
            Stream reqstream = webRequest.GetRequestStream();
            byte[] Payload = Encoding.UTF8.GetBytes("xszd=" + Uri.EscapeDataString(Location1 ?? "") + "&xxdz=" + Uri.EscapeDataString(Location2 ?? "") + (UseDefaultPayload == true ? HealthInfo : SpecialPayload));
            reqstream.Write(Payload, 0, Payload.Length);
            reqstream.Close();
            WebResponse webResponse = webRequest.GetResponse();
            webResponse.Close();
        }
        /// <summary>
        /// 检查网页是否标记已填报
        /// </summary>
        /// <param name="Cookies">登录Cookies</param>
        /// <returns>已填报true, 未填报false</returns>
        private bool Check(string Cookies)
        {
            bool flag = false;
            WebRequest webRequest = WebRequest.Create("http://tjxx.lnu.edu.cn/inputExt.asp");
            webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
            WebResponse webResponse = webRequest.GetResponse();
            Stream stream = webResponse.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string ret = reader.ReadToEnd();
            if (ret.IndexOf("已填报") != -1)
            {
                flag = true;
            }
            webResponse.Close();
            return flag;
        }
        /// <summary>
        /// 设置填报所在地址
        /// </summary>
        /// <param name="Cookies">登录Cookies</param>
        /// <param name="userInfo">用户信息</param>
        private void SetLocation(string Cookies, UserInfo userInfo)
        {
            WebRequest webRequest = WebRequest.Create("http://tjxx.lnu.edu.cn/inputExt.asp");
            webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
            WebResponse webResponse = webRequest.GetResponse();
            Stream stream = webResponse.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string ret = reader.ReadToEnd();
            webResponse.Close();
            string Loca1 = ret.Substring(ret.IndexOf("value=\"") + 7);
            string Loca2 = Loca1.Substring(Loca1.IndexOf("value=\"") + 7);
            Loca1 = Loca1.Substring(0, Loca1.IndexOf('"'));
            Loca2 = Loca2.Substring(0, Loca2.IndexOf('"'));
            userInfo.Location1 = Loca1;
            userInfo.Location2 = Loca2;
        }
        /// <summary>
        /// 设定Rush时间
        /// </summary>
        /// <param name="TodayComplete">是否当日已Rush</param>
        private void SetTime(bool TodayComplete = false)
        {
            DateTime Today = DateTime.Now;
            // 当日12点后或当日Rush过, 设定次日7点05填报
            if (Today.Hour > 12 || TodayComplete)
            {
                DateTime Tomorrow = Today.AddDays(1);
                t.Change((int)(new DateTime(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day,
                                    7, 5, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
            // 在当天填报时间范围内, 立刻填报
            else if (Today.Hour >= 7 && Today.Hour <= 12)
            {
                t.Change(0, Timeout.Infinite);
            }
            // 当日7点前, 设定当日7点05填报
            else
            {
                t.Change((int)(new DateTime(Today.Year, Today.Month, Today.Day,
                    7, 5, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
        }
        /// <summary>
        /// 添加新用户
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewUser_Click(object sender, RoutedEventArgs e)
        {
            UserList.Add(new UserInfo
            {
                UID = txtUID.Text,
                Pwd = txtPwd.Text,
                Status = "",
                Location1 = txtLoca1.Text,
                Location2 = txtLoca2.Text,
                SpecialPayload = txtSpecPL.Text,
                UseLastLocation = ForgetLoca.IsChecked.Value,
                UseDefaultPayload = UseDefaultPL.IsChecked.Value
            });
            txtUID.Clear();
            txtPwd.Clear();
            txtLoca1.Clear();
            txtLoca2.Clear();
            txtSpecPL.Clear();
            ForgetLoca.IsChecked = false;
            UseDefaultPL.IsChecked = false;
        }
        /// <summary>
        /// 手动进行一次列表Rush
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RushOne_Click(object sender, RoutedEventArgs e)
        {
            RushList(DateTime.Now);
        }
        /// <summary>
        /// 自动获取地址
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ForgetLoca_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Q:什么是自动获取地址？\n\nA:因若干原因而无法确定行政区划与详细地址（如：不在填报时间无法即时查看到先前填写的地址）可勾选此选项，程序将在每天填报前自动获取行政区划和详细住址。", "Q&A");
        }
        /// <summary>
        /// 使用默认载荷
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UseDefaultPL_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Q:什么是默认载荷？\n\nA:无城市间流动经历\n无湖北人员接触史\n身体状况健康\n非隔离观察\n非在校在岗", "Q&A");
        }
    }
}
