namespace Eyeball.TargetPointGenerators
{
    using System;

    public interface ITargetPointGenerator
    {
        event EventHandler<TargetPointChangedEventArgs> TargetPointChanged;
    }
}