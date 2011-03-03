namespace Eyeball.TargetPointGenerators
{
    using System.Windows;
    using System.Windows.Forms;

    public class MouseTargetPointGenerator : SamplingTargetPointGenerator
    {
        public MouseTargetPointGenerator(int samplesPerSecond)
            : base(samplesPerSecond)
        {
        }
        
        protected override Point? GetCurrentPosition()
        {
            return new Point(Cursor.Position.X, Cursor.Position.Y);
        }
    }
}