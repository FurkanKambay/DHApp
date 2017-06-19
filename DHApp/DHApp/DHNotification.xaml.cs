using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace DHApp
{
    public partial class DHNotification : UserControl
    {
        public string Text { get; }
        public string Time { get; }
        public string Url { get; }
        public string IconUrl { get; }
        //public bool IsNew { get; set; }

        public DHNotification(string text, string time, string url, string iconUrl)
        {
            Text = text;
            Time = time;
            Url = url;
            IconUrl = iconUrl;

            InitializeComponent();
        }
    }
}
