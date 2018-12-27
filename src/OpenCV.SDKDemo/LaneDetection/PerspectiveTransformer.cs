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
    public class PerspectiveTransformer
    {
        /// <summary>
        /// Creates the transformation matrix (in both directions) to wrap the zone in front of the car.
        /// </summary>
        /// <param name="src"> source image </param>
        /// <param name="transform"> transformation matrix </param>
        /// <param name="inv_transform"> inverse transformation matrix </param>
        /// <param name="warp"> wrapped zone of interest </param>
        public void GetBirdEye(Mat src, float marginX, float marginY, out Mat transform, out Mat inv_transform, out Mat warp)
        {
            // Create perspective in front of the car
            Core.Point[] sourcePeaks =
            {
                new Core.Point(src.Width() * marginX,         src.Height() * marginY),
                new Core.Point(src.Width() * (1 - marginX),   src.Height() * marginY),
                new Core.Point(src.Width(),                   src.Height()),
                new Core.Point(0,                               src.Height()),
                new Core.Point(src.Width() * marginX,         src.Height() * marginY)
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
                new Core.Point(src.Width(),   0),
                new Core.Point(src.Width(),   src.Height()),
                new Core.Point(0,               src.Height()),
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
            warp = new Mat(src.Size(), src.Type());
            Imgproc.WarpPerspective(src, warp, transform, src.Size(), Imgproc.InterLinear);

            // Draw
            for (int iPeak = 0; iPeak < sourcePeaks.GetLength(0) - 1; iPeak++)
                Imgproc.Line(src, sourcePeaks[iPeak], sourcePeaks[iPeak + 1], new Scalar(0, 255, 0), 3);

        }
    }
}