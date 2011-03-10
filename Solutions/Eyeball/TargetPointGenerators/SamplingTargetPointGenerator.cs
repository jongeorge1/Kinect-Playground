namespace Eyeball.TargetPointGenerators
{
    using System;
    using System.Timers;
    using System.Windows;

    public abstract class SamplingTargetPointGenerator : ITargetPointGenerator
    {
        public event EventHandler<TargetPointChangedEventArgs> TargetPointChanged;

        private Timer workerTimer;

        public SamplingTargetPointGenerator(int samplesPerSecond)
        {
            workerTimer = new Timer(1000 / samplesPerSecond);
            workerTimer.Elapsed += this.WorkerTimerElapsed;
            workerTimer.AutoReset = false;
            workerTimer.Start();
        }

        private void WorkerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var currentPosition = this.GetCurrentPosition();
            
            this.OnTargetPointChanged(new TargetPointChangedEventArgs{ Point = currentPosition });

            workerTimer.Start();
        }

        protected abstract Point? GetCurrentPosition();

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