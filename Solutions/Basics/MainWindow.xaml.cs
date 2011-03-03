using System.Windows;

namespace Basics
{
    using System;
    using System.ComponentModel;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker bgWorker;

        private readonly NuiSource nuiSource;

        private bool showDepth = false;

        public MainWindow()
        {
            InitializeComponent();

            nuiSource = new NuiSource();

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += Worker_DoWork;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.BeginInvoke(
                (Action)delegate
                    { imgCamera.Source = showDepth ? nuiSource.DepthImage : nuiSource.CameraImage; });
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (!bgWorker.IsBusy)
            {
                bgWorker.RunWorkerAsync();
            }
        }

        private void switchView_Click(object sender, RoutedEventArgs e)
        {
            showDepth = !showDepth;
        }

    }
}
