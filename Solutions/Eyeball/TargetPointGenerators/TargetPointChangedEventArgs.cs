namespace Eyeball.TargetPointGenerators
{
    using System;
    using System.Windows;

    public class TargetPointChangedEventArgs : EventArgs
    {
        public Point? Point { get; set; }
    }
}