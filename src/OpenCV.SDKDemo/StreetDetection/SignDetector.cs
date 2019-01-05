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
using OpenCV.ObjDetect;

namespace OpenCV.SDKDemo.StreetDetection
{
    class SignDetector
    {
        // HSV color definitions
        //private Scalar HsvRedMin1 = new Scalar(50, 70, 70);
        //private Scalar HsvRedMax1 = new Scalar(100, 255, 255);
        //private Scalar HsvRedMin2 = new Scalar(50, 70, 70);
        //private Scalar HsvRedMax2 = new Scalar(100, 255, 255);
        
        private Scalar HsvBlueMin = new Scalar(20, 70, 70);
        private Scalar HsvBlueMax = new Scalar(50, 255, 255);

        //private Scalar HsvWhiteMin = new Scalar(20, 0, 180);
        //private Scalar HsvWhiteMax = new Scalar(255, 80, 255);

        // Sign macros
        private const int SIGN_WIDTH = 32;
        private const int SIGN_HEIGHT = 32;

        private const float LIMIT_DIF_SIGN_SIZE = 0.5f;
        private const float LIMIT_DIF_SIGN_AREA = 0.4f;
        private const float MIN_SIGN_SIZE_PER_FRAME = 0.001f;

        private float PI = 3.14f;

        public enum SignType
        {
            Stop,
            TurnLeft,
            TurnRight,
            ProTurnLeft,
            ProTurnRight,
            Prohibit,
            UpperSpeedLimit40,
            LowerSpeedLimit30,
            EndLowerSpeedLimit30,
            NationalSpeedLimit,
            None
        };
        
        HOGDescriptor mHog;
        //libsvm.SVM mSvm;
        
        // For detecting        
        List<Core.Rect> mRects = new List<Core.Rect>();
        private LaneMarkingsFilter mMarkingsFilter = new LaneMarkingsFilter();

        // For recognizing
        SignType[] mTypes;
        
        public void LoadModel()
        {
            mHog = new HOGDescriptor(new Size(SIGN_WIDTH, SIGN_HEIGHT),
            new Size(SIGN_WIDTH / 2, SIGN_HEIGHT / 2),
            new Size(SIGN_WIDTH / 4, SIGN_HEIGHT / 4),
            new Size(SIGN_WIDTH / 4, SIGN_HEIGHT / 4),
            9);

            //SVMProblem problem;
            //mSvm.Import("svm_traffic_sign.xml");
            // m_svm = cv::ml::SVM::create();
            // m_svm->setType(ml::SVM::Types::C_SVC);
            // m_svm->setKernel(ml::SVM::KernelTypes::RBF);
            // m_svm->setC(12.5);
            // m_svm->setGamma(0.50625);
            //m_svm = Algorithm::load<SVM>(modelPath);
            //m_svm->load(modelPath);
        }

        public SignDetector()
        {
            LoadModel();
        }

        private void Resize<T>(List<T> list, int sz, T c)
        {
            int cur = list.Count;
            if (sz < cur)
                list.RemoveRange(sz, cur - sz);
            else if (sz > cur)
            {
                if (sz > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                    list.Capacity = sz;
                list.AddRange(Enumerable.Repeat(c, sz - cur));
            }
        }

        private bool Detect(Mat bin)
        {
            // Reset
            mRects.Clear();

            // Find contours
            Mat hierarchy = new Mat();
            IList<MatOfPoint> contoursList = new JavaList<MatOfPoint>();
            Imgproc.FindContours(bin.Clone(), contoursList, hierarchy, Imgproc.RetrTree, Imgproc.ChainApproxSimple);
            
            // Filter contours
            bool detected = false;
            for (int iContour = 0; iContour < contoursList.Count(); iContour++)
            {
                // Areas
                Core.Rect rect = Imgproc.BoundingRect(contoursList[iContour]);
                double ellipseArea = PI * (rect.Width / 2) * (rect.Height / 2);
                double area = Imgproc.ContourArea(contoursList[iContour]);
                
                // Ratios
                double boundWidthPerHeight = (double)rect.Width / rect.Height;
                double areaPerEllipse = (double)(area) / ellipseArea;
                double rectPerFrame = (double)(rect.Area()) / (bin.Size().Width * bin.Size().Height);

                // Check constraints			
                if (rectPerFrame > MIN_SIGN_SIZE_PER_FRAME)
                    if (1 - LIMIT_DIF_SIGN_SIZE < boundWidthPerHeight && boundWidthPerHeight < 1 + LIMIT_DIF_SIGN_SIZE)
                        if (1 - LIMIT_DIF_SIGN_AREA < areaPerEllipse && areaPerEllipse < 1 + LIMIT_DIF_SIGN_AREA)
                        {
                            mRects.Add(rect);
                            detected = true;
                        }
            }
            
            return detected;
        }

        public void Recognize(Mat bin, Mat gray)
        {
            mTypes = new SignType[mRects.Count];

            int iRect = 0;
            foreach (Core.Rect rect in mRects)
            {
                // Crop
                Mat graySign = new Mat(gray, rect); 
                Imgproc.Resize(graySign, graySign, new Size(SIGN_WIDTH, SIGN_HEIGHT));

                // Compute HOG descriptor
                MatOfFloat descriptors = new MatOfFloat();
                mHog.Compute(graySign, descriptors);

                Mat fm = new Mat(descriptors.Size(), CvType.Cv32f);
                // predict matrix transposition
                //mTypes[iRect] = (SignType)(int)(mSvm.Predict(fm.T()));
                iRect++;
            }            
        }

        private Mat FilterMarkings(Mat src)
        {
            // Filter (pass) white & yellow
            Mat blue = mMarkingsFilter.FilterHSV(src, HsvBlueMin, HsvBlueMax);
            
            // Equalize histogram and thresh
            //Mat bin = new Mat(src.Rows(), src.Cols(), CvType.Cv8uc1, Scalar.All(0));

            //Core.Core.Bitwise_or(bin, blue, bin);
            
            //kernel.SetTo(new Scalar(1));
            //Imgproc.MorphologyEx(bin, bin, Imgproc.MorphClose, kernel, new Point(-1, -1), 1);

            //blue.Release();
            
            return blue;
        }

        public SignType Update(Mat src, out Mat bin)
        {
            Mat hsv = new Mat(), gray = new Mat();
            Imgproc.CvtColor(src, hsv, Imgproc.ColorBgr2hsv);
            Imgproc.CvtColor(src, gray, Imgproc.ColorBgr2gray);

            //ip->Binarialize(hsvImg, EColor::BLUE, binImg);
            bin = FilterMarkings(hsv);
            
            if (Detect(bin))
                Recognize(bin, gray);
            
            return SignType.None;
        }

	    public List<Core.Rect> GetSignRects() { return mRects; }
        public SignType[] GetSignTypes() { return mTypes; }
    }
}