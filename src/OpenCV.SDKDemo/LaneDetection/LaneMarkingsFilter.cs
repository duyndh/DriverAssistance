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

using OpenCV.Core;
using OpenCV.ImgProc;

namespace OpenCV.SDKDemo.LaneDetection
{
    public class LaneMarkingsFilter : IDisposable
    {
        // HSV color definitions
        private Scalar HsvYellowMin = new Scalar(50, 70, 70);
        private Scalar HsvYellowMax = new Scalar(100, 255, 255);
        private Scalar HsvWhiteMin = new Scalar(20, 0, 180);
        private Scalar HsvWhiteMax = new Scalar(255, 80, 255);

        // Kernel for 'closing' morphology
        private Mat kernel = new Mat(5, 5, CvType.Cv8uc1);

        /// <summary>
        /// Generates a binary image where only the lane markings wil be high.
        /// Filters based on color (allows white & yellow markings).
        /// </summary>
        /// <param name="src"> BGR image to filter </param>
        /// <returns> Binary lane markings image </returns>
        public Mat FilterMarkings(Mat src)
        {       
            // Filter (pass) white & yellow
            Mat white = FilterHSV(src, HsvWhiteMin, HsvWhiteMax);
            Mat yellow = FilterHSV(src, HsvYellowMin, HsvYellowMax);

            // Equalize histogram and thresh
            Mat whiteFromEq = GetWhiteFromHistogramEq(src, 250, 255);
            Mat bin = new Mat(src.Rows(), src.Cols(), CvType.Cv8uc1, Scalar.All(0));

            Core.Core.Bitwise_or(bin, white, bin);
            Core.Core.Bitwise_or(bin, yellow, bin);
            Core.Core.Bitwise_or(bin, whiteFromEq, bin);

            //kernel.SetTo(new Scalar(1));
            Imgproc.MorphologyEx(bin, bin, Imgproc.MorphClose, kernel, new Point(-1, -1), 1);

            white.Release();
            yellow.Release();
            whiteFromEq.Release();

            return bin;
        }

        /// <summary>
        /// HSV color filter, passes all pixels between the min-max limits. 
        /// </summary>
        /// <param name="src"> Image to filter </param>
        /// <param name="min"> lower hsv point </param>
        /// <param name="max"> upper hsv point </param>
        /// <returns> binary image (high pixels are in range) </returns>
        private Mat FilterHSV(Mat src, Scalar min, Scalar max)
        {
            Mat hsv = new Mat(src.Size(), src.Type());
            Imgproc.CvtColor(src, hsv, Imgproc.ColorBgr2hsv);
            Mat result = new Mat(hsv.Size(), CvType.Cv8uc1);
            Core.Core.InRange(hsv, min, max, result);
            hsv.Release();
            return result;
        }

        /// <summary>
        /// Equalizes the histogram of an image and thresholds the result.
        /// </summary>
        /// <param name="src"> Image to eq & thresh </param>
        /// <param name="thresh"> thresh level </param>
        /// <param name="max"> max value to use </param>
        /// <returns> binary image (high pixels are in range) </returns>
        private Mat GetWhiteFromHistogramEq(Mat src, byte thresh, byte max)
        {
            Mat gray = new Mat(src.Size(), src.Type());
            Imgproc.CvtColor(src, gray, Imgproc.ColorBgr2gray);
            Imgproc.EqualizeHist(gray, gray);
            Imgproc.Threshold(gray, gray, thresh, max, Imgproc.ThreshBinary);
            return gray;
        }

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            kernel.Dispose();
        }
    }
}


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//using Android.App;
//using Android.Content;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;

//namespace OpenCV.SDKDemo.LaneDetect
//{
//    class LaneMarkingsFilter
//    {
//    }
//}