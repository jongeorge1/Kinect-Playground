namespace Eyeball
{
    using System;

    public interface ITargetPointGenerator
    {
        event EventHandler<TargetPointChangedEventArgs> TargetPointChanged;
    }
}