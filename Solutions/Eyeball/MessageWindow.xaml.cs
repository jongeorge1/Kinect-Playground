using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Eyeball
{
    using System.IO;

    using Eyeball.Sensor;

    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow()
        {
            InitializeComponent();

            Sensor.NuiSource.Current.Message += this.Current_Message;
        }

        void Current_Message(object sender, NuiSourceMessageEventArgs e)
        {
            this.textBox1.Text += "\n" + e.Message;
        }
    }
}
