using Android.Runtime;
using OpenCV.Core;
using OpenCV.ImgProc;
using System.Collections.Generic;

namespace OpenCV.SDKDemo.StreetDetection
{
    class LaneDetector
    {
        /// <summary>
        /// Left side of the lane
        /// </summary>
        public List<Point> LeftPoints { get; private set; }

        /// <summary>
        /// Right side of the lane
        /// </summary>
        public List<Point> RightPoints { get; private set; }

        /// <summary>
        /// Bird eye view of the lane
        /// </summary>
        public Mat BirdEye { get; private set; }

        /// <summary>
        /// Run a sliding window algorithm on the bird eye view to find the 2 sides of the lane.
        /// </summary>
        /// <param name="birdEye">bird eye image</param>
        /// <param name="res"> result of windowing </param>
        /// <param name="windows"> number of stacked windows </param>
        public void FitLinesInSlidingWindows(Mat birdEye, out Mat res, int windows)
        {
            LeftPoints = new List<Point>();
            RightPoints = new List<Point>();
            
            // erode by 2x2 kernel
            Imgproc.Erode(birdEye, birdEye, Imgproc.GetStructuringElement(Imgproc.MorphRect, new Size(2, 2)));

            // alloc res have the same size and type as bird eye
            res = new Mat(birdEye.Size(), birdEye.Type());

            // convert to BGR for result drawing
            Imgproc.CvtColor(birdEye, res, Imgproc.ColorGray2bgr);

            // crop half bottom of bird eye
            Mat cropped = new Mat(birdEye, new Rect(0, birdEye.Height() / 2, birdEye.Width(), birdEye.Height() / 2));

            // find left and right starting point
            int left, right;
            SlidingWindowsStartLoc(cropped, out left, out right);

            // current window locations
            int currentWindowLeft = left;
            int currentWindowRight = right;

            // window settings & buffer
            int margin = 100;
            int minpix = 140;
            int winHeight = birdEye.Height() / windows;

            // calculate windows
            for (int i = 0; i < windows; i++)
            {
                // calculate window size and location
                int winYhigh = birdEye.Height() - i * winHeight;
                int winXleftLow = currentWindowLeft - margin;
                int winXrightLow = currentWindowRight - margin;
                Rect leftRect = new Rect(winXleftLow, winYhigh - winHeight, margin * 2, winHeight);
                Rect rightRect = new Rect(winXrightLow, winYhigh - winHeight, margin * 2, winHeight);
                Imgproc.Rectangle(res, new Point(leftRect.X, leftRect.Y), new Point(leftRect.X + leftRect.Width, leftRect.Y + leftRect.Height), new Scalar(20, 20, 255), 3);
                Imgproc.Rectangle(res, new Point(rightRect.X, rightRect.Y), new Point(rightRect.X + rightRect.Width, rightRect.Y + rightRect.Height), new Scalar(20, 20, 255), 3);
                int goodLeft;
                int goodRight;
                
                // save position
                LeftPoints.Add(new Point(winXleftLow + margin, winYhigh - (winHeight / 2)));
                RightPoints.Add(new Point(winXrightLow + margin, winYhigh - (winHeight / 2)));

                Mat birdEyeROI;

                birdEyeROI = birdEye.Submat(leftRect);
                goodLeft = Core.Core.CountNonZero(birdEyeROI);
                
                birdEyeROI = birdEye.Submat(rightRect);
                goodRight = Core.Core.CountNonZero(birdEyeROI);
                
                if (goodLeft > minpix)
                {
                    // recenter
                    birdEyeROI = birdEye.Submat(leftRect);
                    currentWindowLeft = CenterOfLine(birdEyeROI) + leftRect.X;
                }
                if (goodRight > minpix)
                {
                    // recenter
                    birdEyeROI = birdEye.Submat(rightRect);
                    currentWindowRight = CenterOfLine(birdEyeROI) + rightRect.X;
                }
            }

            // Draw midpoints
            foreach (Point p in LeftPoints)
            {
                //res.Draw(new Rectangle(new Point((int)p.X, (int)p.Y), new Size(20, 20)), new Bgr(50, 50, 230), 12);
                Imgproc.Rectangle(res, new Point((int)p.X, (int)p.Y), new Point(p.X + 20, p.Y + 20), new Scalar(50, 50, 230), 12);
            }
            foreach (Point p in RightPoints)
            {
                //res.Draw(new Rectangle(new Point((int)p.X, (int)p.Y), new Size(20, 20)), new Bgr(50, 50, 230), 12);
                Imgproc.Rectangle(res, new Point((int)p.X, (int)p.Y), new Point(p.X + 20, p.Y + 20), new Scalar(50, 50, 230), 12);
            }
            BirdEye = res;
        }

        /// <summary>
        /// Perspective transform a point array with a transformation matrix
        /// </summary>
        /// <param name="trans"> transformation matrix </param>
        /// <param name="points"> point array </param>
        /// <returns> transformed point array </returns>
        public Point[] ProjectPoints(Mat trans, List<Point> points)
        {
            Mat mat = Utils.Converters.Vector_Point2d_to_Mat(points);
            Mat result = Imgproc.GetPerspectiveTransform(mat, trans);
            Utils.Converters.Mat_to_vector_Point(result, points);
            return points.ToArray();
        }

        /// <summary>
        /// Calculate horizontal offset of the vertical line (center) in the binary image 
        /// </summary>
        /// <param name="src"> binary image </param>
        /// <returns> horizontal offset of the vertical line </returns>
        private int CenterOfLine(Mat src)
        {
            // sobel in X-direction
            //Image<Gray, float> sobel = src.Clone().Erode(3).Sobel(1, 0, 3);
            Mat sobel = new Mat(src.Size(), src.Type());
            Imgproc.Erode(src, sobel, Imgproc.GetStructuringElement(Imgproc.MorphRect, new Size(3, 3)));
            //Imgproc.Sobel(src, sobel, src.Type(), 0, 3);

            // min max loc
            double min = 0, max = 0;
            Core.Core.MinMaxLocResult result = Core.Core.MinMaxLoc(sobel);// MinMaxLoc(sobel, ref min, ref max, ref minLoc, ref maxLoc);

            // invalid state
            if (result.MinLoc.X <= result.MaxLoc.X) return src.Width() / 2;
            return ((int)(result.MinLoc.X) - (int)(result.MaxLoc.X)) / 2 + (int)(result.MaxLoc.X);
        }

        /// <summary>
        /// Locates the starting points of the sliding windows by calculating the center of the 2 lines. 
        /// </summary>
        /// <param name="src"> binary bird eye view </param>
        /// <param name="leftMax"> horizontal center left line </param>
        /// <param name="rightMax"> horizontal center right line </param>
        private void SlidingWindowsStartLoc(Mat src, out int leftMax, out int rightMax)
        {
            // Offsets & dimensions
            int xOffset = src.Width() / 2;
            int winHeight = src.Height() / 2;
            int w = src.Width();
            int h = src.Height();
            int whiteTh = winHeight * xOffset / 12;
            Point minP = new Point();
            Point maxP = new Point();
            Mat left = new Mat(1, w, CvType.Cv16uc1);
            Mat right = new Mat(1, w, CvType.Cv16uc1);

            Core.Core.MinMaxLocResult result;
            Mat srcROI;
            // Set ROI to left bottom
            srcROI = src.Submat(new Rect(0, h - winHeight, xOffset, winHeight));

            // If ROI contains low amount of white pixels enlarge window
            if (Core.Core.CountNonZero(src) < whiteTh)
                srcROI = src.Submat(new Rect(0, 0, xOffset, h));

            // Reduce data to x-axis & search max
            Core.Core.Reduce(src, left, 0, 0, CvType.Cv32s);

            result = Core.Core.MinMaxLoc(left);
            leftMax = (int)result.MaxLoc.X;

            // Repeat for right side
            srcROI = src.Submat(new Rect(xOffset, h - winHeight, xOffset, winHeight));
            if (Core.Core.CountNonZero(src) < whiteTh)
            {
                srcROI = src.Submat(new Rect(xOffset, 0, w - xOffset, h));
            }

            Core.Core.Reduce(src, right, 0, 0, CvType.Cv32s);
            result = Core.Core.MinMaxLoc(right);

            // Reset ROI
            rightMax = (int)result.MaxLoc.X + xOffset;
        }
    }
}