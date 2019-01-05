using Android.App;
using Android.OS;
using Android.Widget;
using OpenCV.SDKDemo.CameraPreview;
using OpenCV.SDKDemo.StreetDetection;

namespace OpenCV.SDKDemo
{
    [Activity(Label = "OpenCV.SDKDemo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            FindViewById<Button>(Resource.Id.cameraPreview)
                .Click += (s, e) => StartActivity(typeof(CameraPreviewActivity));

            
            FindViewById<Button>(Resource.Id.laneDetection)
                .Click += (s, e) => StartActivity(typeof(LaneDetectionActivity));

            
        }
    }
}

