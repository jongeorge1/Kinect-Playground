using System.Windows;

namespace Eyeball
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var videoWindow = new VideoWindow();
            videoWindow.Show();

            //var messageWindow = new MessageWindow();
            //messageWindow.Show();
        }
    }
}
