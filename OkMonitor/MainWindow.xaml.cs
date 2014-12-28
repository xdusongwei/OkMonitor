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

        private TextBlock ContainerTB
        {
            get;
            set;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for(int i =0;i<30;i++)
            {
                var bd = new Border();
                bd.Width = 4;
                bd.Height = 0;
                bd.Background = new SolidColorBrush(Colors.White);
                bd.BorderBrush = null;
                bd.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                bd.Opacity = 0.7;
                spTrade.Children.Add(bd);
            }
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
                quoteTimer.Stop();
                try
                {
                    var jss = new JavaScriptSerializer();
                    WebClient client = new WebClient();
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    var last = 0.0;
                    var progress = 0.5;
                    dynamic trades = null;
                    using (Stream data = client.OpenRead("https://www.okcoin.cn/api/v1/ticker.do?symbol=btc_cny"))
                    {
                        using(StreamReader reader = new StreamReader(data))
                        {
                            string s = reader.ReadToEnd();
                            var dict = jss.Deserialize<dynamic>(s);
                            last = double.Parse(dict["ticker"]["last"]);
                        }
                    }
                    using (Stream data = client.OpenRead("https://www.okcoin.cn/api/v1/depth.do?symbol=btc_cny"))
                    {
                        using (StreamReader reader = new StreamReader(data))
                        {
                            string s = reader.ReadToEnd();
                            var dict = jss.Deserialize<dynamic>(s);
                            var ask = 0.0;
                            var bid = 0.0;
                            foreach (var i in dict["asks"])
                                ask -= Math.Log(((double)i[0] - last + 0.001) * 0.001) * (double)i[1];
                            foreach (var i in dict["bids"])
                                bid -= Math.Log((-(double)i[0] + last + 0.001) * 0.001) * (double)i[1];
                            if(ask + bid != 0)
                                progress = bid / (ask + bid);
                        }
                    }

                    using (Stream data = client.OpenRead("https://www.okcoin.cn/api/v1/kline.do?symbol=btc_cny&type=1min&size=30"))
                    {
                        using (StreamReader reader = new StreamReader(data))
                        {
                            string s = reader.ReadToEnd();
                            trades = jss.Deserialize<dynamic>(s);
                        }
                    }
                    tbPrice.Dispatcher.Invoke(() =>
                    {
                        if(trades != null)
                        {
                            var j = 0;
                            var max = 0.0;
                            foreach (var i in trades)
                            {
                                if ((double)i[5] > max)
                                    max = (double)i[5];
                            }
                            if (max == 0)
                                max = 20;
                            foreach (var i in trades)
                            {
                                var bd = spTrade.Children[j] as Border;
                                if ((double)i[4] >= (double)i[1])
                                    (bd.Background as SolidColorBrush).Color = Colors.DodgerBlue;
                                else
                                    (bd.Background as SolidColorBrush).Color = Colors.Crimson;
                                
                                bd.Height = (double)i[5] / max * 20;
                                j++;
                            }
                        }
                        var stop = (bdAskPower.Background as LinearGradientBrush).GradientStops[2];
                        var powerAnim = new DoubleAnimation();
                        powerAnim.Duration = TimeSpan.FromMilliseconds(1000);
                        if(!double.IsNaN(progress))
                            powerAnim.To = progress;
                        (bdAskPower.Background as LinearGradientBrush).GradientStops[2].BeginAnimation(GradientStop.OffsetProperty, powerAnim);

                        stop = (bdAskPower.Background as LinearGradientBrush).GradientStops[1];
                        powerAnim = new DoubleAnimation();
                        if (progress < 0.5)
                            powerAnim.Duration = TimeSpan.FromMilliseconds(100);
                        else
                            powerAnim.Duration = new TimeSpan(0, 0, 1);
                        powerAnim.To = 0.45;
                        if (!double.IsNaN(progress))
                            powerAnim.To = progress - 0.05;
                        if (powerAnim.To < 0)
                            powerAnim.To = 0;
                        (bdAskPower.Background as LinearGradientBrush).GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, powerAnim);

                        stop = (bdAskPower.Background as LinearGradientBrush).GradientStops[3];
                        powerAnim = new DoubleAnimation();
                        if (progress > 0.5)
                            powerAnim.Duration = TimeSpan.FromMilliseconds(100);
                        else
                            powerAnim.Duration = new TimeSpan(0, 0, 1);
                        powerAnim.To = 0.55;
                        if (!double.IsNaN(progress))
                            powerAnim.To = progress + 0.05;
                        if (powerAnim.To > 1)
                            powerAnim.To = 1;
                        (bdAskPower.Background as LinearGradientBrush).GradientStops[3].BeginAnimation(GradientStop.OffsetProperty, powerAnim);

                        var borderAnim = new ColorAnimation();
                        if (last > ContainerTB.Width)
                            borderAnim.To = Color.FromArgb(255, 137, 249, 96);
                        else if (last < ContainerTB.Width)
                            borderAnim.To = Color.FromArgb(255, 255, 65, 65);
                        else
                            borderAnim.To = Colors.CadetBlue;
                        borderAnim.Duration = TimeSpan.FromMilliseconds(300);
                        (bdFlag.BorderBrush as SolidColorBrush).BeginAnimation(SolidColorBrush.ColorProperty, borderAnim);
                        var priceAnim = new DoubleAnimation();
                        priceAnim.To = last;
                        priceAnim.Duration = new TimeSpan(0, 0, 1);
                        ContainerTB.BeginAnimation(TextBlock.WidthProperty, priceAnim);
                    });
                }
                catch
                { }
                quoteTimer.Start();
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
