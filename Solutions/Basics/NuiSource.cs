namespace Basics
{
    using System;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using xn;

    public class NuiSource
    {
        private Context context;

        private ImageGenerator imageGenerator;
        private ImageMetaData imageMetadata;
        private WriteableBitmap cameraImage;

        private DepthGenerator depthGenerator;
        private DepthMetaData depthMetadata;
        private WriteableBitmap depthImage;
        private int[] depthHistogram; 

        public NuiSource()
        {
            context = new Context("openni.xml");

            // Initialise generators
            imageGenerator = this.context.FindExistingNode(NodeType.Image) as ImageGenerator;
            depthGenerator = this.context.FindExistingNode(NodeType.Depth) as DepthGenerator;

            imageMetadata = new ImageMetaData();
            var imageMapMode = imageGenerator.GetMapOutputMode();

            depthMetadata = new DepthMetaData();
            var depthMapMode = depthGenerator.GetMapOutputMode();
            depthHistogram = new int[depthGenerator.GetDeviceMaxDepth()];

            // Initialise bitmaps
            cameraImage = new WriteableBitmap((int)imageMapMode.nXRes, (int)imageMapMode.nYRes, 96, 96, PixelFormats.Rgb24, null);
            depthImage = new WriteableBitmap((int)depthMapMode.nXRes, (int)depthMapMode.nYRes, 96, 96, PixelFormats.Rgb24, null);

            // Initialise background thread
            var cameraThread = new Thread(this.CameraThread) { IsBackground = true };
            cameraThread.Start();

            var userGenerator = new UserGenerator(context);
            userGenerator.NewUser += this.UserGenerator_NewUser;
            userGenerator.LostUser += this.UserGenerator_LostUser;
        }

        private void UserGenerator_LostUser(ProductionNode node, uint id)
        {
        }

        private void UserGenerator_NewUser(ProductionNode node, uint id)
        {
        }

        private void CameraThread()
        {
            while (true)
            {
                context.WaitAndUpdateAll();
                imageGenerator.GetMetaData(imageMetadata);
                depthGenerator.GetMetaData(depthMetadata);
            }
        }

        public ImageSource CameraImage
        {
            get
            {
                if (cameraImage != null)
                {
                    cameraImage.Lock();
                    cameraImage.WritePixels(new Int32Rect(0, 0, imageMetadata.XRes, imageMetadata.YRes), imageMetadata.ImageMapPtr, (int)imageMetadata.DataSize, cameraImage.BackBufferStride);
                    cameraImage.Unlock();
                }

                return cameraImage;
            }
        }

        public ImageSource DepthImage
        {
            get
            {
                if (depthImage != null)
                {
                    this.UpdateHistogram(depthMetadata);

                    depthImage.Lock();

                    unsafe
                    {
                        var pDepth = (ushort*)depthGenerator.GetDepthMapPtr().ToPointer();
                        for (var y = 0; y < depthMetadata.YRes; ++y)
                        {
                            var pDest = (byte*)depthImage.BackBuffer.ToPointer() + y * depthImage.BackBufferStride;
                            for (var x = 0; x < depthMetadata.XRes; ++x, ++pDepth, pDest += 3)
                            {
                                var pixel = (byte)depthHistogram[*pDepth];
                                pDest[0] = 0;
                                pDest[1] = pixel;
                                pDest[2] = pixel;
                            }
                        }
                    }

                    depthImage.AddDirtyRect(new Int32Rect(0, 0, depthMetadata.XRes, depthMetadata.YRes));
                    depthImage.Unlock();
                }

                return depthImage;
            }
        }

        private unsafe void UpdateHistogram(DepthMetaData depthMD)
        {
            // Reset.
            depthHistogram = new int[depthHistogram.Length];

            var pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

            var points = 0;

            for (var y = 0; y < depthMD.YRes; ++y)
            {
                for (var x = 0; x < depthMD.XRes; ++x, ++pDepth)
                {
                    var depthVal = *pDepth;
                    if (depthVal != 0)
                    {
                        depthHistogram[depthVal]++;
                        points++;
                    }
                }
            }
            
            for (var i = 1; i < depthHistogram.Length; i++)
            {
                depthHistogram[i] += depthHistogram[i - 1];
            }
            
            if (points > 0)
            {
                for (int i = 1; i < depthHistogram.Length; i++)
                {
                    depthHistogram[i] = (int)(256 * (1.0f - (depthHistogram[i] / (float)points)));
                }
            }
        }
    }
}
