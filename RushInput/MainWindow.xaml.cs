using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace RushInput
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<User> UserList = new ObservableCollection<User>();
        private Timer t;
        public MainWindow()
        {
            InitializeComponent();
            ReadCfgFromFile();
            UserGrid.DataContext = UserList;
            t = new Timer(Timedup);
            SetTime();
        }
        /// <summary>
        /// 从导出文件读取配置
        /// </summary>
        private void ReadCfgFromFile()
        {
            try
            {
                using (FileStream fs = new FileStream("userlist.xml", FileMode.Open))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<User>));
                    UserList = (ObservableCollection<User>)xmlSerializer.Deserialize(fs);
                    MessageBox.Show("已导入现有配置");
                }
            }
            catch
            {
                ;
            }
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
        private void RushOne(User user, DateTime time)
        {
            user.Submit(time);
        }
        /// <summary>
        /// 设定Rush时间
        /// </summary>
        /// <param name="TodayComplete">是否当日已Rush</param>
        private void SetTime(bool TodayComplete = false)
        {
            DateTime Today = DateTime.Now;
            // 当日12点后或当日Rush过, 设定次日6点10填报
            if (Today.Hour > 12 || TodayComplete)
            {
                DateTime Tomorrow = Today.AddDays(1);
                t.Change((int)(new DateTime(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day,
                                    6, 10, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
            // 在当天填报时间范围内, 立刻填报
            else if (Today.Hour >= 6 && Today.Hour <= 12)
            {
                t.Change(0, Timeout.Infinite);
            }
            // 当日6点前, 设定当日6点10填报
            else
            {
                t.Change((int)(new DateTime(Today.Year, Today.Month, Today.Day,
                    6, 10, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
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
        /// 导出列表到文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (FileStream fs = new FileStream("userlist.xml", FileMode.OpenOrCreate))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<User>));
                xmlSerializer.Serialize(fs, UserList);
                MessageBox.Show("导出完成");
            }
        }
    }
}
