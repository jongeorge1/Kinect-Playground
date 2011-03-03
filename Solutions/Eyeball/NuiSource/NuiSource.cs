namespace Eyeball.NuiSource
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using xn;

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

        private readonly UserGenerator userGenerator;

        private int[] depthHistogram;

        private List<uint> playersInOrderOfAppearance = new List<uint>();

        private NuiSource()
        {
            this.context = new Context("openni.xml");

            // Initialise generators
            this.imageGenerator = this.context.FindExistingNode(NodeType.Image) as ImageGenerator;
            this.depthGenerator = this.context.FindExistingNode(NodeType.Depth) as DepthGenerator;

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

            this.userGenerator.NewUser += this.UserGenerator_NewUser;
            this.userGenerator.LostUser += this.UserGenerator_LostUser;
            this.userGenerator.StartGenerating();

            // Initialise background thread
            var cameraThread = new Thread(this.CameraThread) { IsBackground = true };
            cameraThread.Start();
        }

        public event EventHandler<NuiSourceMessageEventArgs> Message;

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

        public IEnumerable<uint> PlayersInOrderOfAppearance
        {
            get
            {
                return new Collection<uint>(this.playersInOrderOfAppearance);
            }
        }

        public Point GetScreenCoordinatesForPlayer(uint player)
        {
            const int sourceWidth = 1280;
            const int sourceHeight = 960;

            var screenX = Screen.PrimaryScreen.Bounds.Width;
            var screenY = Screen.PrimaryScreen.Bounds.Height;

            var screenXMultiplier = (double)screenX / sourceWidth;
            var screenYMultiplier = -(double)screenY / sourceHeight;


            var com = this.userGenerator.GetCoM(player);

            // Quick and dirty translation
            var x = com.X * screenXMultiplier + (sourceWidth / 2);
            var y = com.Y * screenYMultiplier + (sourceHeight / 4);

            return new Point(x, y);
        }

        protected void OnMessage(string message)
        {
            var handler = this.Message;
            if (handler != null)
            {
                handler(this, new NuiSourceMessageEventArgs { Message = message });
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
        }

        private void UserGenerator_NewUser(ProductionNode node, uint id)
        {
            this.playersInOrderOfAppearance.Add(id);
            this.OnMessage("New player detected and assigned Id " + id);
        }
    }
}