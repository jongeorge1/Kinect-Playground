namespace Eyeball
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;

    using Eyeball.TargetPointGenerators;

    /// <summary>
    ///   Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public Color EyeColour;

        private readonly List<Rgb> Colours = new List<Rgb>();

        private readonly Random random = new Random();

        private const double IrisScaleInitial = 1.5; // 1.0 was too small when compared to real eyeballs

        private ITargetPointGenerator targetPointGenerator;

        public MainWindow()
        {
            this.InitializeComponent();
            this.ConsumeInitParams();
            if (this.Colours.Count > 0)
            {
                var col = this.Colours[0];
                this.EyeColour.R = (byte)col.Red;
                this.EyeColour.G = (byte)col.Green;
                this.EyeColour.B = (byte)col.Blue;
                this.EyeColour.A = 255;
            }
            else
            {
                this.RandomiseEyeColour();
            }
            this.UpdateEyeColour();

            targetPointGenerator.TargetPointChanged += this.TargetPointGenerator_TargetPointChanged;
        }

        /// <summary>
        ///   Adjust the iris based on the distance and angle of the supplied coords in relation to the center of EyeInteraction object
        /// </summary>
        /// <param name = "mouse">coords of mouse</param>
        /// <param name = "maxDistance">maximum distance allowed by the Iris from center</param>
        /// <param name = "minY">minimum Y value possible</param>
        /// <param name = "maxY">maximum Y value possible</param>
        public void AdjustEye(Point mouse, double maxDistance, double minY, double maxY)
        {
            var center = new Point();

            this.EyeInteraction.Dispatcher.Invoke(new Action(() => center = new Point(this.EyeInteraction.Width / 2, this.EyeInteraction.Height / 2)));

            double distance = Distance(center, mouse);
            if (distance > maxDistance)
            {
                distance = maxDistance; // maximum distance from center
            }
            //System.Diagnostics.Debug.WriteLine(Mouse);
            var scaleFactor = IrisScaleInitial - (distance / 200);
            
            // this bit now scales correctly based on maximum distance
            this.IrisScaleTransform.Dispatcher.Invoke(new Action(() => this.IrisScaleTransform.ScaleY = scaleFactor));

            var radians = Math.Atan2(mouse.Y - center.Y, mouse.X - center.X);
            var newAngle = radians * (180 / Math.PI);
            newAngle += 90;

            this.IrisRotate.Dispatcher.Invoke(new Action(() => this.IrisRotate.Angle = newAngle));

            this.IrisPatternRotate.Dispatcher.Invoke(new Action(() => this.IrisPatternRotate.Angle = -newAngle));
            
            var newLocation = GetLocation(radians, distance);
            // a couple of minor adjustments based on up and down values
            if (newLocation.Y < minY)
            {
                newLocation.Y = minY;
            }

            if (newLocation.Y > maxY)
            {
                newLocation.Y = maxY;
            }

            //if (distance>
            this.Iris.Dispatcher.Invoke(new Action(() =>
                {
                    this.Iris.SetValue(Canvas.LeftProperty, (center.X / 2) + newLocation.X);
                    this.Iris.SetValue(Canvas.TopProperty, (center.Y / 2) + newLocation.Y);
                }));
        }

        /// <summary>
        ///   Return distance from Point 1 to Point 2
        /// </summary>
        /// <param name = "p1"></param>
        /// <param name = "p2"></param>
        /// <returns></returns>
        private static double Distance(Point p1, Point p2)
        {
            double xDist = p1.X - p2.X;
            double yDist = p1.Y - p2.Y;
            return Math.Sqrt(xDist * xDist + yDist * yDist);
        }

        private void ConsumeInitParams()
        {
            this.DisplayGuides.Visibility = Visibility.Collapsed;
        
            this.Colours.Add(new Rgb("56;101;169"));
            this.Colours.Add(new Rgb("56;101;169"));
            this.Colours.Add(new Rgb("66;133;232"));
            this.Colours.Add(new Rgb("99;121;85"));
            this.Colours.Add(new Rgb("135;208;86"));
            this.Colours.Add(new Rgb("179;123;98"));
            this.Colours.Add(new Rgb("231;100;43"));
            this.Colours.Add(new Rgb("160;51;229"));
            this.Colours.Add(new Rgb("239;101;32"));
            this.Colours.Add(new Rgb("239;223;32"));

            this.IrisScaleTransform.ScaleX = IrisScaleInitial;
            this.IrisScaleTransform.ScaleY = IrisScaleInitial;

            //this.targetPointGenerator = new MouseTargetPointGenerator(25);
            this.targetPointGenerator = new NuiSourceTargetPointGenerator(25);
        }

        // simple wrapper to return either empty strings of values from IDictionary

        /// <summary>
        ///   Add 400 random lines to the iris
        /// </summary>
        private void DrawIrisPattern()
        {
            // remove existing pattern first
            int patternCount = this.IrisPattern.Children.Count;
            for (int i = patternCount - 1; i > 0; i--)
            {
                if (this.IrisPattern.Children[i] is Line)
                {
                    this.IrisPattern.Children.Remove(this.IrisPattern.Children[i]);
                }
            }
            for (int c = 0; c < 400; c++)
            {
                var angle = this.random.Next(360);
                double length = ((double)this.random.Next(40)) / 100;
                this.IrisPattern.Children.Add(this.GenerateLine(length, angle));
            }
        }

        /// <summary>
        ///   Cause new iris colour and dialation when clicked.
        /// </summary>
        private void EyeShadow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.RandomiseEyeColour();
            this.UpdateEyeColour();
            this.UnDialate.Begin();
        }

        private void TargetPointGenerator_TargetPointChanged(object sender, TargetPointChangedEventArgs e)
        {
            // Get position of point relative to that of eye interaction canvas
            var relativeTarget = new Point(0, 0);
            
            this.EyeInteraction.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        if (e.Point.HasValue)
                        {
                            relativeTarget = this.EyeInteraction.PointFromScreen(e.Point.Value);
                        }
                        else
                        {
                            relativeTarget = new Point(0, 0);
                        }
                    }
                    catch
                    {
                    }
                }));

            this.debug.Dispatcher.Invoke(new Action(() =>
                {
                    if (e.Point.HasValue)
                    {
                        this.debug.Text = string.Format("{0}, {1}", e.Point.Value.X, e.Point.Value.Y);
                    }
                    else
                    {
                        this.debug.Text = "No point returned.";
                    }
                }));

            // Move eye)
            this.AdjustEye(relativeTarget, 40.0, -10.0, 25.0);
        }

/// <summary>
        ///   Handle mouse movements over object
        /// </summary>
        private void EyeShadow_MouseMove(object sender, MouseEventArgs e)
        {
            var mouse = e.GetPosition(this.EyeInteraction);
            this.AdjustEye(mouse, 40.0, -10.0, 25.0);
        }

        /// <summary>
        ///   Single iris line
        /// </summary>
        /// <returns>new Line</returns>
        private Line GenerateLine(double length, int angle)
        {
            var line = new Line { X1 = 30.0, Y1 = 30.0, X2 = 30.0, Y2 = 60.0, StrokeThickness = 1 };
            
            var lgb = new LinearGradientBrush();
            var gsc = new GradientStopCollection();
            var gs = new GradientStop();
            var startColor = new Color { A = 255, R = this.EyeColour.R, G = this.EyeColour.G };

            if (startColor.G < 200)
            {
                startColor.G += 50; // most eye colours have a green hint
            }

            startColor.B = this.EyeColour.B;
            gs.Color = startColor;
            gs.Offset = 0.0;
            gsc.Add(gs);
            
            var gs2 = new GradientStop();
            var endColor = new Color { A = 0, R = 100, G = 100, B = 100 };
            
            gs2.Color = endColor;
            gs2.Offset = 0.5 + length; // length = 0.0 - 0.4;
            gsc.Add(gs2);
            lgb.GradientStops = gsc;
            lgb.StartPoint = new Point(0, 0);
            lgb.EndPoint = new Point(0, 1);
            line.Stroke = lgb;
            
            var rt = new RotateTransform { Angle = angle, CenterX = 30, CenterY = 30 };
            line.RenderTransform = rt;
            
            return line;
        }

        /// <summary>
        ///   Generate a new point relative to 0,0 based on supplied angle and distance
        /// </summary>
        /// <param name = "angle"></param>
        /// <param name = "distance"></param>
        /// <returns></returns>
        private static Point GetLocation(double angle, double distance)
        {
            var newPoint = new Point { X = distance * Math.Cos(angle), Y = distance * Math.Sin(angle) };
            return newPoint;
        }

        private void RandomiseEyeColour()
        {
            if (this.Colours.Count == 0)
            {
                switch (this.random.Next(5))
                {
                    case 0:
                        this.EyeColour.R = 0;
                        this.EyeColour.G = 255;
                        this.EyeColour.B = 0;
                        break;
                    case 1:
                        this.EyeColour.R = 255;
                        this.EyeColour.G = 0;
                        this.EyeColour.B = 0;
                        break;
                    case 2:
                        this.EyeColour.R = 255;
                        this.EyeColour.G = 0;
                        this.EyeColour.B = 255;
                        break;
                    case 3:
                        this.EyeColour.R = 0;
                        this.EyeColour.G = 0;
                        this.EyeColour.B = 255;
                        break;
                    default:
                        this.EyeColour.R = 255;
                        this.EyeColour.G = 255;
                        this.EyeColour.B = 0;
                        break;
                }
            }
            else
            {
                var col = this.Colours[this.random.Next(this.Colours.Count)];
                // do not allow repeats
                if ((this.EyeColour.R == (byte)col.Red) && (this.EyeColour.G == (byte)col.Green) &&
                    (this.EyeColour.B == (byte)col.Blue) && this.Colours.Count > 1)
                {
                    this.RandomiseEyeColour();
                }
                else
                {
                    this.EyeColour.R = (byte)col.Red;
                    this.EyeColour.G = (byte)col.Green;
                    this.EyeColour.B = (byte)col.Blue;
                }
            }
            this.EyeColour.A = 255;
        }

        /// <summary>
        ///   Set iris color and call for a new pattern
        /// </summary>
        private void UpdateEyeColour()
        {
            this.MainEyeColour.Color = this.EyeColour;
            this.DrawIrisPattern();
        }
    }
}