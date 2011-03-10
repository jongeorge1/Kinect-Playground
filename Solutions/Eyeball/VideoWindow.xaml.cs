using System.Windows;

namespace Eyeball
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : Window
    {
        private readonly BackgroundWorker bgWorker;

        private readonly Sensor.NuiSource nuiSource;

        public VideoWindow()
        {
            InitializeComponent();

            nuiSource = Sensor.NuiSource.Current;

            this.hdof.Text = nuiSource.HorizontalDepthOfField.ToString();
            this.vdof.Text = nuiSource.VerticalDepthOfField.ToString();
            this.maxDepth.Text = nuiSource.MaximumDepth.ToString();

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += Worker_DoWork;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
                    {
                        imgCamera.Source = nuiSource.CameraImage;

                        if (this.nuiSource.PlayersInOrderOfAppearance.Count() > 0)
                        {
                            var firstPlayer = this.nuiSource.PlayersInOrderOfAppearance.First();

                            var projectedCoordinates = nuiSource.GetProjectedCoordinatesForPlayer(firstPlayer);
                            var realWorldCoordinates = nuiSource.GetRealWorldCoordinatesForPlayer(firstPlayer);

                            realWorld.Text = string.Format("{0:0.0}, {1:0.0}, {2:0.0}", realWorldCoordinates.X, realWorldCoordinates.Y, realWorldCoordinates.Z);
                            projected.Text = string.Format("{0:0.0}, {1:0.0}, {2:0.0}", projectedCoordinates.X, projectedCoordinates.Y, projectedCoordinates.Z);
                        }
                        else
                        {
                            realWorld.Text = "No current player.";
                            projected.Text = "No current player.";
                        }
                        
                    });
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
