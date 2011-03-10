namespace Eyeball.TargetPointGenerators
{
    using System;
    using System.Windows.Controls;
    using System.Windows.Input;

    public class MouseTargetPointGenerator : ITargetPointGenerator
    {
        private readonly Canvas targetElement;

        public MouseTargetPointGenerator(Canvas targetElement)
        {
            this.targetElement = targetElement;
            targetElement.MouseMove += this.MouseMove;
        }

        public event EventHandler<TargetPointChangedEventArgs> TargetPointChanged;

        protected void OnTargetPointChanged(TargetPointChangedEventArgs e)
        {
            var handler = this.TargetPointChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this.targetElement);
            this.OnTargetPointChanged(new TargetPointChangedEventArgs { Point = position });
        }
    }
}