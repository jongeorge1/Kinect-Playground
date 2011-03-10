namespace Eyeball.Sensor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using xn;

    using Point3D = System.Windows.Media.Media3D.Point3D;

    public class NuiSource
    {
        private static readonly NuiSource CurrentSource = new NuiSource();

        private readonly WriteableBitmap cameraImage;

        private readonly Context context;

        private readonly DepthGenerator depthGenerator;

        private readonly WriteableBitmap depthImage;

        private readonly DepthMetaData depthMetadata;

        private readonly ImageGenerator imageGenerator;

        private readonly ImageMetaData imageMetadata;

        private readonly List<uint> playersInOrderOfAppearance = new List<uint>();

        private readonly UserGenerator userGenerator;

        private int[] depthHistogram;

        private NuiSource()
        {
            this.context = new Context("openni.xml");

            // Initialise generators
            this.imageGenerator = this.context.FindExistingNode(NodeType.Image) as ImageGenerator;
            this.depthGenerator = this.context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            this.depthGenerator.GetAlternativeViewPointCap().SetViewPoint(this.imageGenerator);

            this.userGenerator = new UserGenerator(this.context);
            this.imageMetadata = new ImageMetaData();
            var imageMapMode = this.imageGenerator.GetMapOutputMode();

            this.depthMetadata = new DepthMetaData();
            var depthMapMode = this.depthGenerator.GetMapOutputMode();
            this.depthHistogram = new int[this.depthGenerator.GetDeviceMaxDepth()];

            // Initialise bitmaps
            this.cameraImage = new WriteableBitmap(
                (int)imageMapMode.nXRes, (int)imageMapMode.nYRes, 96, 96, PixelFormats.Rgb24, null);
            this.depthImage = new WriteableBitmap(
                (int)depthMapMode.nXRes, (int)depthMapMode.nYRes, 96, 96, PixelFormats.Rgb24, null);

            // Initialise user generator
            this.userGenerator.NewUser += this.UserGenerator_NewUser;
            this.userGenerator.LostUser += this.UserGenerator_LostUser;
            this.userGenerator.StartGenerating();
            this.ShowPlayerLabels = true;

            // Initialise background thread
            var cameraThread = new Thread(this.CameraThread) { IsBackground = true };
            cameraThread.Start();
        }

        public event EventHandler<EventArgs> ClosestUserChanged;

        public event EventHandler<NuiSourceMessageEventArgs> Message;

        public event EventHandler<EventArgs> UserFound;

        public event EventHandler<EventArgs> UserLost;

        public static NuiSource Current
        {
            get
            {
                return CurrentSource;
            }
        }

        public ImageSource CameraImage
        {
            get
            {
                if (this.cameraImage != null)
                {
                    this.cameraImage.Lock();
                    this.cameraImage.WritePixels(
                        new Int32Rect(0, 0, this.imageMetadata.XRes, this.imageMetadata.YRes),
                        this.imageMetadata.ImageMapPtr,
                        (int)this.imageMetadata.DataSize,
                        this.cameraImage.BackBufferStride);

                    this.cameraImage.Unlock();
                }

                return this.cameraImage;
            }
        }

        public ImageSource DepthImage
        {
            get
            {
                if (this.depthImage != null)
                {
                    this.UpdateHistogram(this.depthMetadata);

                    this.depthImage.Lock();

                    unsafe
                    {
                        var pDepth = (ushort*)this.depthGenerator.GetDepthMapPtr().ToPointer();
                        for (var y = 0; y < this.depthMetadata.YRes; ++y)
                        {
                            var pDest = (byte*)this.depthImage.BackBuffer.ToPointer() +
                                        y * this.depthImage.BackBufferStride;
                            for (var x = 0; x < this.depthMetadata.XRes; ++x, ++pDepth, pDest += 3)
                            {
                                var pixel = (byte)this.depthHistogram[*pDepth];
                                pDest[0] = 0;
                                pDest[1] = pixel;
                                pDest[2] = pixel;
                            }
                        }
                    }

                    this.depthImage.AddDirtyRect(new Int32Rect(0, 0, this.depthMetadata.XRes, this.depthMetadata.YRes));
                    this.depthImage.Unlock();
                }

                return this.depthImage;
            }
        }

        public double HorizontalDepthOfField
        {
            get
            {
                return this.depthGenerator.GetFieldOfView().fHFOV;
            }
        }

        public int MaximumDepth
        {
            get
            {
                return this.depthGenerator.GetDeviceMaxDepth();
            }
        }

        public IEnumerable<uint> PlayersInOrderOfAppearance
        {
            get
            {
                return new Collection<uint>(this.playersInOrderOfAppearance);
            }
        }

        public bool ShowPlayerLabels { get; set; }

        public double VerticalDepthOfField
        {
            get
            {
                return this.depthGenerator.GetFieldOfView().fVFOV;
            }
        }

        public Point3D GetProjectedCoordinatesForPlayer(uint player)
        {
            var realWorldCom = this.userGenerator.GetCoM(player);
            var com = this.depthGenerator.ConvertRealWorldToProjective(realWorldCom);

            return new Point3D(com.X, com.Y, com.Z);
        }

        public Point3D GetRealWorldCoordinatesForPlayer(uint player)
        {
            var realWorldCom = this.userGenerator.GetCoM(player);

            return new Point3D(realWorldCom.X, realWorldCom.Y, realWorldCom.Z);
        }

        protected void OnClosestUserChanged()
        {
            var handler = this.ClosestUserChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected void OnMessage(string message)
        {
            var handler = this.Message;
            if (handler != null)
            {
                handler(this, new NuiSourceMessageEventArgs { Message = message });
            }
        }

        protected void OnUserFound()
        {
            var handler = this.UserFound;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected void OnUserLost()
        {
            var handler = this.UserLost;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void CameraThread()
        {
            while (true)
            {
                try
                {
                    this.context.WaitOneUpdateAll(this.depthGenerator);
                }
                catch
                {
                }

                this.imageGenerator.GetMetaData(this.imageMetadata);
                this.depthGenerator.GetMetaData(this.depthMetadata);
            }
        }

        private unsafe void UpdateHistogram(DepthMetaData depthMD)
        {
            // Reset.
            this.depthHistogram = new int[this.depthHistogram.Length];

            var pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

            var points = 0;

            for (var y = 0; y < depthMD.YRes; ++y)
            {
                for (var x = 0; x < depthMD.XRes; ++x, ++pDepth)
                {
                    var depthVal = *pDepth;
                    if (depthVal != 0)
                    {
                        this.depthHistogram[depthVal]++;
                        points++;
                    }
                }
            }

            for (var i = 1; i < this.depthHistogram.Length; i++)
            {
                this.depthHistogram[i] += this.depthHistogram[i - 1];
            }

            if (points > 0)
            {
                for (int i = 1; i < this.depthHistogram.Length; i++)
                {
                    this.depthHistogram[i] = (int)(256 * (1.0f - (this.depthHistogram[i] / (float)points)));
                }
            }
        }

        private void UserGenerator_LostUser(ProductionNode node, uint id)
        {
            this.playersInOrderOfAppearance.Remove(id);
            this.OnMessage("Lost player with Id " + id);
            this.OnUserLost();
        }

        private void UserGenerator_NewUser(ProductionNode node, uint id)
        {
            this.playersInOrderOfAppearance.Add(id);
            this.OnMessage("New player detected and assigned Id " + id);
            this.OnUserFound();
        }
    }
}