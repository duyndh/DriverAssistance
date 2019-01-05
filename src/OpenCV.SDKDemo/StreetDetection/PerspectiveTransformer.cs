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

namespace OpenCV.SDKDemo.StreetDetection
{
    public class PerspectiveTransformer
    {
        /// <summary>
        /// Creates the transformation matrix (in both directions) to wrap the zone in front of the car.
        /// </summary>
        /// <param name="src"> source image </param>
        /// <param name="transform"> transformation matrix </param>
        /// <param name="inv_transform"> inverse transformation matrix </param>
        /// <param name="warp"> wrapped zone of interest </param>
        public void GetBirdEye(Mat bin, Mat src, float marginX, float marginY, out Mat transform, out Mat inv_transform, out Mat warp)
        {
            // Create perspective in front of the car
            Core.Point[] sourcePeaks =
            {
                new Core.Point(bin.Width() * marginX,         bin.Height() * marginY),
                new Core.Point(bin.Width() * (1 - marginX),   bin.Height() * marginY),
                new Core.Point(bin.Width(),                   bin.Height()),
                new Core.Point(0,                               bin.Height()),
                new Core.Point(bin.Width() * marginX,         bin.Height() * marginY)
            };
            MatOfPoint2f sourceMat = new MatOfPoint2f(
                    sourcePeaks[0],
                    sourcePeaks[1],
                    sourcePeaks[2],
                    sourcePeaks[3]
                    );

            Core.Point[] targetPeaks =
            {
                new Core.Point(0,               0),
                new Core.Point(bin.Width(),   0),
                new Core.Point(bin.Width(),   bin.Height()),
                new Core.Point(0,               bin.Height()),
                new Core.Point(0,               0)
            };
            // Generate transformation matrix in both directions
            MatOfPoint2f targetMat = new MatOfPoint2f(
                    targetPeaks[0],
                    targetPeaks[1],
                    targetPeaks[2],
                    targetPeaks[3]
                    );
            
            transform = Imgproc.GetPerspectiveTransform(sourceMat, targetMat);
            inv_transform = Imgproc.GetPerspectiveTransform(targetMat, sourceMat);
            warp = new Mat(bin.Size(), bin.Type());
            Imgproc.WarpPerspective(bin, warp, transform, bin.Size(), Imgproc.InterLinear);

            // Draw
            for (int iPeak = 0; iPeak < sourcePeaks.GetLength(0) - 1; iPeak++)
                Imgproc.Line(src, sourcePeaks[iPeak], sourcePeaks[iPeak + 1], new Scalar(0, 255, 0), 3);

        }
    }
}