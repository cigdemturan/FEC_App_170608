using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System.Xml;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows;
using System.Runtime.InteropServices;

namespace FEC_App_170608
{
    class Worker
    {
        public string _fileName { get; set; }
        private string _folderNameRec = "recording";
        private string _rgbFolder, _verticesFolder, _animationUnitFolder, _depthFolder, _infraredFolder;
        //public FileStream jointsFileStream { get; set; }
        public int counterFile;
        public int counterFrame;
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private readonly int bytesPerPixelBI = (PixelFormats.Gray8.BitsPerPixel + 7) / 8;
        public FrameDescription colorFrameDescription { get; set; }
        public Body[] _bodies = new Body[6];

        public void CreateFolders()
        {
            //_rgbFolder = _folderNameRec + "/rgb/";
            //Directory.CreateDirectory(_rgbFolder);
            //_depthFolder = _folderNameRec + "/depth/";
            //Directory.CreateDirectory(_depthFolder);
            _verticesFolder = _folderNameRec + "/vertices/";
            Directory.CreateDirectory(_verticesFolder);
            _animationUnitFolder = _folderNameRec + "/AnU/";
            Directory.CreateDirectory(_animationUnitFolder);
        }
        public void CreateFolderRGB()
        {
            _rgbFolder = _folderNameRec + "/rgb_" + counterFile + "/";
            Directory.CreateDirectory(_rgbFolder);
            //_infraredFolder = _folderNameRec + "/infrared_" + counterFile + "/";
            //Directory.CreateDirectory(_infraredFolder);
        }
        public FileStream InilizeVertices3DStream()
        {
            //long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            //string verticesFile = _verticesFolder + "vertices_" + counterFile + "_" + milliseconds + ".txt";
            string verticesFile = _verticesFolder + "vertices3D_" + counterFile + ".txt";
            FileStream verticesFileStream = new FileStream(verticesFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            return verticesFileStream;
        }
        public FileStream InilizeVertices2DStream()
        {
            //long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            //string verticesFile = _verticesFolder + "vertices_" + counterFile + "_" + milliseconds + ".txt";
            string verticesFile = _verticesFolder + "vertices2D_" + counterFile + ".txt";
            FileStream verticesFileStream = new FileStream(verticesFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            return verticesFileStream;
        }
        public FileStream InilizeAnimationUnitStream()
        {
            //long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            //string animationUnitFile = _animationUnitFolder + "AnU_" + counterFile + "_" + milliseconds + "_.txt";
            string animationUnitFile = _animationUnitFolder + "AnU_" + counterFile + ".txt";
            FileStream animationUnitStream = new FileStream(animationUnitFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            return animationUnitStream;
        }

        public void WriteColor(ColorFrame _colorFrame, string milliseconds)
        {
            string filePath = _rgbFolder + "/image_" + milliseconds + ".png";
            colorFrameDescription = _colorFrame.FrameDescription;
            //Console.WriteLine("colorFrame is here");
            byte[] pixelsColor = new byte[colorFrameDescription.Width * colorFrameDescription.Height * bytesPerPixel];
            if (_colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                _colorFrame.CopyRawFrameDataToArray(pixelsColor);
            }
            else
            {
                _colorFrame.CopyConvertedFrameDataToArray(pixelsColor, ColorImageFormat.Bgra);
            }

            int stride = colorFrameDescription.Width * PixelFormats.Bgr32.BitsPerPixel / 8;
            BitmapSource _imageRGB = BitmapSource.Create(colorFrameDescription.Width, colorFrameDescription.Height, 96, 96, PixelFormats.Bgr32, null, pixelsColor, stride);
            BitmapSource _imageRGB_cropped = new CroppedBitmap(_imageRGB, new Int32Rect(850, 250, 400, 500));
            using (FileStream rgb_FileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_imageRGB_cropped));
                encoder.Save(rgb_FileStream);
                rgb_FileStream.Close();
                rgb_FileStream.Dispose();
            }

        }

        public void WriteDepth(DepthFrame _depthFrame, long milliseconds)
        {
            FrameDescription depthFrameDescription = _depthFrame.FrameDescription;
            //uint depthSize = depthFrameDescription.LengthInPixels;
            byte[] pixelsDepth = null;
            ushort[] depthData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height]; //Depth
            pixelsDepth = new byte[depthFrameDescription.Width * depthFrameDescription.Height * bytesPerPixel]; //Depth
            _depthFrame.CopyFrameDataToArray(depthData);

            ushort minDepth = _depthFrame.DepthMinReliableDistance;
            ushort maxDepth = _depthFrame.DepthMaxReliableDistance;

            int colorImageIndex = 0;
            for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex)
            {
                ushort depth = depthData[depthIndex];
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixelsDepth[colorImageIndex++] = intensity; // Blue
                pixelsDepth[colorImageIndex++] = intensity; // Green
                pixelsDepth[colorImageIndex++] = intensity; // Red

                ++colorImageIndex;
            }

            int stride = depthFrameDescription.Width * PixelFormats.Bgr32.BitsPerPixel / 8;

            BitmapSource _imageDepth = BitmapSource.Create(depthFrameDescription.Width, depthFrameDescription.Height, 96, 96, PixelFormats.Bgr32, null, pixelsDepth, stride);
            //Console.WriteLine("here");
            string filePath = _depthFolder + "/image" + milliseconds + ".png";

            using (FileStream depth_FileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_imageDepth));
                encoder.Save(depth_FileStream);
                //Console.WriteLine("saved..");
                depth_FileStream.Close();
                depth_FileStream.Dispose();
            }
            _depthFrame.Dispose();
        }

        public void WriteInfrared(InfraredFrame _infraredFrame, string milliseconds)
        {
            FrameDescription infraredFrameDescription = _infraredFrame.FrameDescription;
            //uint depthSize = depthFrameDescription.LengthInPixels;
            byte[] pixelsInfrared = null;
            ushort[] infraredData = new ushort[infraredFrameDescription.Width * infraredFrameDescription.Height]; //Depth
            pixelsInfrared = new byte[infraredFrameDescription.Width * infraredFrameDescription.Height * bytesPerPixel]; //Depth
            _infraredFrame.CopyFrameDataToArray(infraredData);


            int colorImageIndex = 0;
            for (int infraredIndex = 0; infraredIndex < infraredData.Length; ++infraredIndex)
            {
                ushort ir = infraredData[infraredIndex];
                byte intensity = (byte)(ir >> 8);

                pixelsInfrared[colorImageIndex++] = intensity; // Blue
                pixelsInfrared[colorImageIndex++] = intensity; // Green   
                pixelsInfrared[colorImageIndex++] = intensity; // Red

                ++colorImageIndex;
            }

            int stride = infraredFrameDescription.Width * PixelFormats.Bgr32.BitsPerPixel / 8;

            BitmapSource _imageInfrared = BitmapSource.Create(infraredFrameDescription.Width, infraredFrameDescription.Height, 96, 96, PixelFormats.Bgr32, null, pixelsInfrared, stride);
            //Console.WriteLine("here");
            string filePath = _infraredFolder + "/image" + milliseconds + ".png";

            using (FileStream infrared_FileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_imageInfrared));
                encoder.Save(infrared_FileStream);
                //Console.WriteLine("saved..");
                infrared_FileStream.Close();
                infrared_FileStream.Dispose();
            }
            _infraredFrame.Dispose();
        }


        public void WriteDepthInColorSpace(DepthFrame _depthFrame, CoordinateMapper _coordinateMapper, long milliseconds)
        {
            FrameDescription depthFrameDescription = _depthFrame.FrameDescription;
            byte[] pixelsDepth = null;
            ushort[] depthData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height]; //Depth
            pixelsDepth = new byte[colorFrameDescription.Width * colorFrameDescription.Height * bytesPerPixel]; //Depth
            
            _depthFrame.CopyFrameDataToArray(depthData);
            DepthSpacePoint[] _depthSpacePoints = new DepthSpacePoint[colorFrameDescription.Width * colorFrameDescription.Height];
            _coordinateMapper.MapColorFrameToDepthSpace(depthData, _depthSpacePoints);

            ushort[] depthDataInColor = new ushort[colorFrameDescription.Width * colorFrameDescription.Height];

            // Get the min and max reliable depth for the current frame
            //ushort minDepth = _depthFrame.DepthMinReliableDistance;
            //ushort maxDepth = _depthFrame.DepthMaxReliableDistance;
            ushort minDepth = 500;
            ushort maxDepth = 2500;

            //Console.WriteLine("{0}", minDepth);
            //Console.WriteLine("{0}", maxDepth);

            for (int colorY = 0; colorY < colorFrameDescription.Height; colorY++)
            {
                for (int colorX = 0; colorX < colorFrameDescription.Width; colorX++)
                {
                    //const long colorIndex = colorY * colorFrameDescription.Width + colorX;
                    int colorIndex = ((colorFrameDescription.Width * colorY) + colorX);
                    int colorIndexPixel = ((colorFrameDescription.Width * colorY) + colorX) * bytesPerPixel;
                    //int depthX = static_cast<int>
                    int depthX = (int)Math.Floor(_depthSpacePoints[colorIndex].X + 0.5);
                    int depthY = (int)Math.Floor(_depthSpacePoints[colorIndex].Y + 0.5);

                    if ((0 <= depthX && (depthX < depthFrameDescription.Width) && (0 <= depthY) && (depthY < depthFrameDescription.Height)))
                    {
                        int depthIndex = depthY * depthFrameDescription.Width + depthX;
                        depthDataInColor[colorIndex] = depthData[depthIndex];

                        ushort depth = depthData[depthIndex];
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                        pixelsDepth[colorIndexPixel] = intensity;
                        pixelsDepth[colorIndexPixel + 1] = intensity;
                        pixelsDepth[colorIndexPixel + 2] = intensity;
                    }
                }
            }


            int stride = colorFrameDescription.Width * PixelFormats.Bgr32.BitsPerPixel / 8;
            BitmapSource _imageDepth = BitmapSource.Create(colorFrameDescription.Width, colorFrameDescription.Height, 96, 96, PixelFormats.Bgr32, null, pixelsDepth, stride);
            string filePath = _depthFolder + "/depth" + milliseconds + ".png";
            BitmapSource _imageDepth_cropped = new CroppedBitmap(_imageDepth, new Int32Rect(900, 280, 500, 500));
            using (FileStream depth_FileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_imageDepth_cropped));
                encoder.Save(depth_FileStream);
                //Console.WriteLine("saved..");
                depth_FileStream.Close();
                depth_FileStream.Dispose();
            }
  
            _depthFrame.Dispose();
        }

        public void WriteAnimationUnits(FaceAlignment _faceAlignment, FileStream _animationUnitStream, TimeSpan milliseconds, bool flag = true)
        {
            var _animationUnitWriter = new StreamWriter(_animationUnitStream);
            _animationUnitWriter.Write("\r\n");
            _animationUnitWriter.Write("{0}, ", milliseconds);
            if (flag)
            {
                IReadOnlyDictionary<FaceShapeAnimations, float> _animationUnits = _faceAlignment.AnimationUnits;
                Dictionary<FaceShapeAnimations, float> _animationUnitsDict = new Dictionary<FaceShapeAnimations, float>();

                foreach (FaceShapeAnimations _anuType in _animationUnits.Keys)
                {
                    _animationUnitWriter.Write("{0}, ", _animationUnits[_anuType]);
                }

            }
            else
            {
                _animationUnitWriter.Write("NaN, ");
            }
            _animationUnitWriter.Flush();

        }

        public void WriteVertices(FaceModel _faceModel, FaceAlignment _faceAlignment, FileStream _vertices3DFileStream, FileStream _vertices2DFileStream, CoordinateMapper _coordinateMapper, TimeSpan milliseconds, bool flag = true)
        {
            var vertices3DWriter = new StreamWriter(_vertices3DFileStream);
            vertices3DWriter.Write("\r\n");
            vertices3DWriter.Write("{0}, ", milliseconds);
            var vertices2DWriter = new StreamWriter(_vertices2DFileStream);
            vertices2DWriter.Write("\r\n");
            vertices2DWriter.Write("{0}, ", milliseconds);

            if (flag)
            {
                var _vertices3D = _faceModel.CalculateVerticesForAlignment(_faceAlignment);

                //CameraSpacePoint[] _vertices3D = _faceModel.CalculateVerticesForAlignment(_faceAlignment);
                //ColorSpacePoint[] _vertices2D = null;

                //_coordinateMapper.MapCameraPointsToColorSpace(_vertices3D, _vertices2D);

                for (int index = 0; index < _vertices3D.Count; index++)
                {
                    
                    CameraSpacePoint vertice3D = _vertices3D[index];
                    //if (vertice3D.)
                    ColorSpacePoint vertice2D = _coordinateMapper.MapCameraPointToColorSpace(vertice3D);
                    //DepthSpacePoint vertice2D = _coordinateMapper.MapCameraPointToDepthSpace(vertice3D);

                    vertices3DWriter.Write("{0}, ", vertice3D.X);
                    vertices3DWriter.Write("{0}, ", vertice3D.Y);
                    vertices3DWriter.Write("{0}, ", vertice3D.Z);

                    vertices2DWriter.Write("{0}, ", vertice2D.X);
                    vertices2DWriter.Write("{0}, ", vertice2D.Y);
                    
                }
            }
            else
            {
                vertices3DWriter.Write("NaN, ");
                vertices2DWriter.Write("NaN, ");
            }

            vertices3DWriter.Flush();
            vertices2DWriter.Flush();
        } 

    }
}
