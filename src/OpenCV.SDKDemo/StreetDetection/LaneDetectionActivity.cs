using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using OpenCV.Core;
using OpenCV.Android;
using Android.Util;
using OpenCV.SDKDemo.Utilities;
using OpenCV.ImgProc;
using Size = OpenCV.Core.Size;
using Android.Graphics;

namespace OpenCV.SDKDemo.StreetDetection
{
    [Activity(Label = ActivityTags.LaneDetection,
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden)]
    public class LaneDetectionActivity : Activity, View.IOnTouchListener, CameraBridgeViewBase.ICvCameraViewListener2
    {
        private bool mIsColorSelected = false;
        private Mat mRgba;
        private Scalar mBlobColorRgba;
        private Scalar mBlobColorHsv;
        private Mat mSpectrum;
        private Size SPECTRUM_SIZE;
        private Scalar CONTOUR_COLOR;

        // Lane detector
        private LaneDetector mDetector;
        // Lane markings filter
        private LaneMarkingsFilter mLaneMarkFilter;
        // Perspective transform logic
        private PerspectiveTransformer mTransformer;

        // Sign detector
        private SignDetector mSignDetector;

        public CameraBridgeViewBase mOpenCvCameraView { get; private set; }

        BaseLoaderCallback mLoaderCallback;

        public LaneDetectionActivity()
        {
            Log.Info(ActivityTags.LaneDetection, "Instantiated new " + GetType().ToString());
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Log.Info(ActivityTags.LaneDetection, "called onCreate");
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.NoTitle);

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            SetContentView(Resource.Layout.lane_detection_surface_view);
            
            mOpenCvCameraView = FindViewById<CameraBridgeViewBase>(Resource.Id.lane_detection_activity_surface_view);
            mOpenCvCameraView.Visibility = ViewStates.Visible;
            mOpenCvCameraView.SetCvCameraViewListener2(this);
            mLoaderCallback = new Callback(this, this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (mOpenCvCameraView != null)
                mOpenCvCameraView.DisableView();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug(ActivityTags.LaneDetection, "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, mLoaderCallback);
            }
            else
            {
                Log.Debug(ActivityTags.LaneDetection, "OpenCV library found inside package. Using it!");
                mLoaderCallback.OnManagerConnected(LoaderCallbackInterface.Success);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (mOpenCvCameraView != null)
                mOpenCvCameraView.DisableView();
        }

        public void OnCameraViewStarted(int width, int height)
        {
            mRgba = new Mat(height, width, CvType.Cv8uc4);
            mDetector = new LaneDetector();
            mSpectrum = new Mat();
            mBlobColorRgba = new Scalar(255);
            mBlobColorHsv = new Scalar(255);
            SPECTRUM_SIZE = new Size(200, 64);
            CONTOUR_COLOR = new Scalar(255, 0, 0, 255);

            mDetector = new LaneDetector();
            mLaneMarkFilter = new LaneMarkingsFilter();
            mTransformer = new PerspectiveTransformer();

            mSignDetector = new SignDetector();
        }

        public void OnCameraViewStopped()
        {
            mRgba.Release();
        }


        public bool OnTouch(View v, MotionEvent e)
        {            
            /*
            //Mat source = ImgCodecs.Imgcodecs.Imread("test.png");
            // Filter image based on color to find markings
            Mat original = mRgba.Clone();
            //Imgproc.Resize(mRgba, original, new Size(720, 1280));
            Mat bin = mLaneMarkFilter.FilterMarkings(original.Clone());
            Mat birdsEyeView = new Mat(bin.Size(), bin.Type());

            // Generate bird eye view
            Mat a = new Mat();
            Mat b = new Mat();
            mTransformer.GetBirdEye(mRgba, out a, out b, out birdsEyeView);

            // Find markings location
            Mat birdsEyeWithBoxes;
            mDetector.FitLinesInSlidingWindows(birdsEyeView, out birdsEyeWithBoxes, 13);
            */
            //int size = mDetector.LeftPoints.Count;
            //if (mDetector.LeftPoints.Count == mDetector.RightPoints.Count)
            //{
            //    Point[] l = mDetector.ProjectPoints(b, mDetector.LeftPoints).Select(x => new Point((int)x.X, (int)x.Y)).ToArray();
            //    Point[] r = mDetector.ProjectPoints(b, mDetector.RightPoints).Select(x => new Point((int)x.X, (int)x.Y)).ToArray();
            //    Point[] center = new Point[size];
            //    List<MatOfPoint> points = new List<MatOfPoint>();
            //    for (int i = 0; i < size; i++)
            //    {
            //        points.Add(new MatOfPoint(new Point[] { l[i], r[i] }));
            //        Imgproc.FillPoly(original, points, new Scalar(10, 250, 10));
            //    }
            //    points.Clear();
            //    points.Add(new MatOfPoint(l));
            //    Imgproc.FillPoly(original, points, new Scalar(10, 250, 10));

            //    points.Clear();
            //    points.Add(new MatOfPoint(r));
            //    Imgproc.FillPoly(original, points, new Scalar(10, 250, 10));
            //}


            return false; // don't need subsequent touch events
        }

        public Mat OnCameraFrame(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            //mRgba = inputFrame.Rgba();

            Bitmap bitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.test2);
            Mat mat = new Mat();
            Android.Utils.BitmapToMat(bitmap, mat);
            Imgproc.Resize(mat, mRgba, mRgba.Size());
            mat.Release();
            bitmap.Recycle();
            bitmap.Dispose();

            // Start                    

            // Sign detection
            Mat binSign;
            mSignDetector.Update(mRgba, out binSign);
            
            // Filter image based on color to find markings
            Mat bin = mLaneMarkFilter.FilterMarkings(mRgba);

            // Generate bird eye view
            float marginX = 0.42f;
            float marginY = 0.65f;

            Mat a, b, birdsEyeView;
            mTransformer.GetBirdEye(bin, mRgba, marginX, marginY, out a, out b, out birdsEyeView);
        
            // Scale to mini bird view and draw to origin
            Mat birdEyeMiniView = new Mat(birdsEyeView.Size(), CvType.Cv8uc4);// new Mat(birdsEyeView.Height() / 2, birdsEyeView.Width() / 2, mRgba.Type(), new Scalar(0, 255, 0, 255));
            Imgproc.CvtColor(birdsEyeView, birdEyeMiniView, Imgproc.ColorGray2bgra);
            Imgproc.Resize(birdEyeMiniView, birdEyeMiniView, new Size(birdsEyeView.Cols() / 2, birdsEyeView.Rows() / 2));
            birdEyeMiniView.CopyTo(mRgba.RowRange(0, birdsEyeView.Rows() / 2).ColRange(0, birdsEyeView.Cols() / 2));

            List<Core.Rect> rects = mSignDetector.GetSignRects();
            SignDetector.SignType[] types = mSignDetector.GetSignTypes();
            int iRect = 0;
            foreach (var rect in rects)
            {
                if (types[iRect] != SignDetector.SignType.None)
                    Imgproc.Rectangle(mRgba, new Core.Point(rect.X, rect.Y), new Core.Point(rect.X + rect.Width, rect.Y + rect.Height), new Scalar(255, 0, 0, 255), 3);
                iRect++;
            }
            //Imgproc.Resize(binSign, binSign, new Size(mRgba.Cols() / 2, mRgba.Rows() / 2));
            //Mat binSignMini = new Mat(binSign.Size(), CvType.Cv8uc4);
            //Imgproc.CvtColor(binSign, binSignMini, Imgproc.ColorGray2bgra);
            //binSignMini.CopyTo(mRgba.RowRange(0, mRgba.Rows() / 2).ColRange(mRgba.Cols() / 2, mRgba.Cols()));

            // End

            // Release
            birdsEyeView.Release();
            birdEyeMiniView.Release();
            a.Release();
            b.Release();
            bin.Release();

            return mRgba;
        }

        private Scalar ConvertScalarHsv2Rgba(Scalar hsvColor)
        {
            Mat pointMatRgba = new Mat();
            Mat pointMatHsv = new Mat(1, 1, CvType.Cv8uc3, hsvColor);
            Imgproc.CvtColor(pointMatHsv, pointMatRgba, Imgproc.ColorHsv2rgbFull, 4);

            return new Scalar(pointMatRgba.Get(0, 0));
        }
    }

    class Callback : BaseLoaderCallback
    {
        private readonly LaneDetectionActivity _activity;
        public Callback(LaneDetectionActivity activity, Context context)
            : base(context)
        {
            _activity = activity;
        }

        public override void OnManagerConnected(int status)
        {
            switch (status)
            {
                case LoaderCallbackInterface.Success:
                    {
                        Log.Info(ActivityTags.LaneDetection, "OpenCV loaded successfully");
                        _activity.mOpenCvCameraView.EnableView();
                        _activity.mOpenCvCameraView.SetOnTouchListener(_activity);
                    }
                    break;
                default:
                    {
                        base.OnManagerConnected(status);
                    }
                    break;
            }
        }
    }

}