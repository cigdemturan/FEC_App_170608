using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System.IO;

namespace FEC_App_170608
{
    class Program
    {
        public static KinectSensor _sensor = null;
        public static MultiSourceFrameReader _multiSourceFrameReader = null;
        public static HighDefinitionFaceFrameSource _faceSource = null;
        public static HighDefinitionFaceFrameReader _faceReader = null;
        public static FaceAlignment _faceAlignment = null;
        public static FaceModel _faceModel = null;
        public static CoordinateMapper _coordinateMapper = null;
        public static Worker _worker = new Worker();
        public static FileStream _animationUnitStream, _vertices2DFileStream, _vertices3DFileStream;
        public static bool trackingSuccess = false;
        static void Main(string[] args)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();
                Console.WriteLine("sensorOpened");
                if (_sensor.IsOpen)
                {
                    _coordinateMapper = _sensor.CoordinateMapper;
                    _multiSourceFrameReader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color |FrameSourceTypes.Infrared | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
                    _multiSourceFrameReader.MultiSourceFrameArrived += multiSourceReader_FrameArrived;

                    _faceSource = new HighDefinitionFaceFrameSource(_sensor);
                    _faceReader = _faceSource.OpenReader();
                    _faceReader.FrameArrived += FaceReader_FrameArrived;

                    _faceModel = new FaceModel();
                    _faceAlignment = new FaceAlignment();

                }
            }
            string input = Console.ReadLine();
            _sensor.Close();
        }

        static void multiSourceReader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            MultiSourceFrame _multiSourceFrame = e.FrameReference.AcquireFrame();
            if (_multiSourceFrame == null)
            {
                return;
            }
            if (_worker.counterFile == 0 && _worker.counterFrame == 0)
            {
                _worker.CreateFolders();
                _worker.CreateFolderRGB();
                _animationUnitStream = _worker.InilizeAnimationUnitStream();
                _vertices2DFileStream = _worker.InilizeVertices2DStream();
                _vertices3DFileStream = _worker.InilizeVertices3DStream();
            }
            //Console.WriteLine(_worker.counterFrame);
            //long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            TimeSpan timeSpan = DateTime.Now.TimeOfDay;
            string timeNow = timeSpan.ToString();
            timeNow = timeNow.Replace('.', '_');
            timeNow = timeNow.Replace(':', '_');
            //Console.WriteLine(timeNow);

            Task.Factory.StartNew(() =>
            {
                ColorFrame _colorFrame = null;
                using (_colorFrame = _multiSourceFrame.ColorFrameReference.AcquireFrame())
                {
                    if (_colorFrame != null)
                    {
                        _worker.WriteColor(_colorFrame, timeNow);
                    }
                }
            });

            /*
            Task.Factory.StartNew(() =>
            {
                InfraredFrame _infraredFrame = null;
                using (_infraredFrame = _multiSourceFrame.InfraredFrameReference.AcquireFrame())
                {
                    //_worker.WriteDepth(_depthFrame, milliseconds);
                    if (_infraredFrame != null)
                    {
                        _worker.WriteInfrared(_infraredFrame, timeNow);
                    }
                    
                }
            });
            */

            /*
            Task.Factory.StartNew(() =>
            {
                DepthFrame _depthFrame = null;
                using (_depthFrame = _multiSourceFrame.DepthFrameReference.AcquireFrame())
                {
                    //_worker.WriteDepth(_depthFrame, milliseconds);
                    if (_depthFrame != null)
                    {
                        _worker.WriteDepthInColorSpace(_depthFrame, _coordinateMapper, milliseconds);
                    }
                    
                }
            });
            */

            Task.Factory.StartNew(() =>
            {
                BodyFrame _bodyFrame = null;
                using (_bodyFrame = _multiSourceFrame.BodyFrameReference.AcquireFrame())
                {
                    if (_bodyFrame != null)
                    {
                        _bodyFrame.GetAndRefreshBodyData(_worker._bodies);
                        //Console.WriteLine("bodyArrived");

                        Body body = _worker._bodies.Where(b => b.IsTracked).FirstOrDefault();

                        if (!_faceSource.IsTrackingIdValid)
                        {
                            if (body != null)
                            {
                                _faceSource.TrackingId = body.TrackingId;
                            }
                        }
                    }
                }
            });

            if (_worker.counterFrame++ >= 1000)
            {
                //Console.WriteLine("here");
                Console.WriteLine("999 frames have been saved..");
                _worker.counterFrame = 0;
                _worker.counterFile++;
                _worker.CreateFolderRGB();
                _animationUnitStream = _worker.InilizeAnimationUnitStream();
                _vertices2DFileStream = _worker.InilizeVertices2DStream();
                _vertices3DFileStream = _worker.InilizeVertices3DStream();
            }


        }

        static void FaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                TimeSpan milliseconds = DateTime.Now.TimeOfDay;
                //Console.WriteLine(milliseconds);
                using (var _faceFrame = e.FrameReference.AcquireFrame())
                {

                    if (_faceFrame != null && _faceFrame.IsFaceTracked)
                    {
                        if (!trackingSuccess)
                        {
                            Console.WriteLine("started face tracking..");
                            trackingSuccess = true;
                        }
                        //Console.WriteLine("faceArrived");
                        _faceFrame.GetAndRefreshFaceAlignmentResult(_faceAlignment);
                        Task.Factory.StartNew(() =>
                        {
                            _worker.WriteAnimationUnits(_faceAlignment, _animationUnitStream, milliseconds);
                        });
                        Task.Factory.StartNew(() =>
                        {
                            _worker.WriteVertices(_faceModel, _faceAlignment, _vertices3DFileStream, _vertices2DFileStream, _coordinateMapper, milliseconds);
                        });
                    }
                    else
                    {
                        if (trackingSuccess)
                        {
                            Console.WriteLine("tracking is lost..");
                            trackingSuccess = false;
                        }
                        _worker.WriteAnimationUnits(_faceAlignment, _animationUnitStream, milliseconds, false);
                        _worker.WriteVertices(null, null, _vertices3DFileStream, _vertices2DFileStream, null, milliseconds, false);
                    }
                }
            });
        }
    }
}
