using System.Windows;

namespace Eyeball
{
    using System;
    using System.ComponentModel;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : Window
    {
        private readonly BackgroundWorker bgWorker;

        private readonly NuiSource.NuiSource nuiSource;

        public VideoWindow()
        {
            InitializeComponent();

            nuiSource = NuiSource.NuiSource.Current;

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += Worker_DoWork;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.BeginInvoke(
                (Action)delegate { imgCamera.Source = nuiSource.CameraImage; });
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (!bgWorker.IsBusy)
            {
                bgWorker.RunWorkerAsync();
            }
        }
    }
}
