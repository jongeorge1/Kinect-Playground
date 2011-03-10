namespace Eyeball.TargetPointGenerators
{
    using System.Linq;
    using System.Windows;

    using Eyeball.Sensor;

    public class NuiSourceTargetPointGenerator : SamplingTargetPointGenerator
    {
        public NuiSourceTargetPointGenerator(int samplesPerSecond)
            : base(samplesPerSecond)
        {
        }

        protected override Point? GetCurrentPosition()
        {
            var players = NuiSource.Current.PlayersInOrderOfAppearance;
            if (players.Count() == 0)
            {
                return null;
            }

            var com = NuiSource.Current.GetRealWorldCoordinatesForPlayer(players.First());

            return new Point(com.X, com.Y);
        }
    }
}