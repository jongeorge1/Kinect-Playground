namespace Eyeball.TargetPointGenerators
{
    using System;
    using System.Linq;
    using System.Windows;

    public class NuiSourceTargetPointGenerator : SamplingTargetPointGenerator
    {
        public NuiSourceTargetPointGenerator(int samplesPerSecond)
            : base(samplesPerSecond)
        {
        }

        protected override Point? GetCurrentPosition()
        {
            var players = NuiSource.NuiSource.Current.PlayersInOrderOfAppearance;
            if (players.Count() == 0)
            {
                return null;
            }

            var com = NuiSource.NuiSource.Current.GetScreenCoordinatesForPlayer(players.First());
            return com;
        }
    }
}