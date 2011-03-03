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
            workerTimer = new Timer(1000 / samplesPerSecond) { AutoReset = true, Enabled = true };
            workerTimer.Elapsed += this.WorkerTimerElapsed;
        }

        private void WorkerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var currentPosition = this.GetCurrentPosition();
            
            this.OnTargetPointChanged(new TargetPointChangedEventArgs{ Point = currentPosition });
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