namespace Eyeball
{
    using System;
    using System.Runtime.InteropServices;
    using System.Timers;
    using System.Windows;
    using System.Windows.Forms;

    using Timer = System.Timers.Timer;

    public class MouseTargetPointGenerator : ITargetPointGenerator
    {
        public event EventHandler<TargetPointChangedEventArgs> TargetPointChanged;

        private Timer workerTimer;

        public MouseTargetPointGenerator(int samplesPerSecond)
        {
            workerTimer = new Timer(1000 / samplesPerSecond) { AutoReset = true, Enabled = true };
            workerTimer.Elapsed += this.WorkerTimerElapsed;
        }

        private void WorkerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var currentPosition = new Point(Cursor.Position.X, Cursor.Position.Y);
            
            this.OnTargetPointChanged(new TargetPointChangedEventArgs{ Point = currentPosition });
        }

        protected void OnTargetPointChanged(TargetPointChangedEventArgs e)
        {
            var handler = this.TargetPointChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}