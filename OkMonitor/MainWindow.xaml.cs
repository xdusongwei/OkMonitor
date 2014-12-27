using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Windows.Media.Animation;
using System.Globalization;

namespace OkMonitor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            

        }

        private TextBlock ContainerTB
        {
            get;
            set;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var tmpTB = new TextBlock();
            ContainerTB = tmpTB;
            ContainerTB.Width = 0;
            var quoteTimer = new Timer(1000);
            var binding = new Binding();
            binding.Source = ContainerTB;
            binding.Path = new PropertyPath("Width");
            binding.Converter = new DoubleToStringConverter();
            BindingOperations.SetBinding(tbPrice, TextBlock.TextProperty, binding);
            quoteTimer.Elapsed += (qsender, qe) => 
            {
                try
                {
                    var jss = new JavaScriptSerializer();
                    WebClient client = new WebClient();
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    Stream data = client.OpenRead("https://www.okcoin.cn/api/v1/ticker.do?symbol=btc_cny");
                    StreamReader reader = new StreamReader(data);
                    string s = reader.ReadToEnd();
                    var dict = jss.Deserialize<dynamic>(s);
                    var last = double.Parse(dict["ticker"]["last"]);
                    data.Close();
                    reader.Close();
                    tbPrice.Dispatcher.Invoke(() =>
                    {
                        var priceAnim = new DoubleAnimation();
                        priceAnim.To = last;
                        priceAnim.Duration = new TimeSpan(0, 0, 1);
                        ContainerTB.BeginAnimation(TextBlock.WidthProperty, priceAnim);
                    });
                }
                catch
                { }
            };
            quoteTimer.Start();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double price = (double)value;
            return price.ToString("0.00");
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
