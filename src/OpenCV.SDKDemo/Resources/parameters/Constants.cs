using System;

namespace OpenCV.SDKDemo.CameraPreview
{
    static class Constants
    {
        internal static string strGoogleServerKey = "AIzaSyB8msqEz_2QmVmOcxJXDdmR4cHmOmMgSdw";
        ///static string strGoogleServerDirKey = "A*********xe1Tc8-_t6Dq6CocGdb9nN-bc08CE";
        internal static string strGoogleDirectionUrl = "https://maps.googleapis.com/maps/api/directions/json?origin={0}&destination={1}&key=" + strGoogleServerKey + "";
        //internal static string strGeoCodingUrl = "https://maps.googleapis.com/maps/api/geocode/json?{0}&key=" + strGoogleServerKey + "";
        internal static string strSourceLocation = "Thanh Khe, Da Nang, Viet Nam";
        internal static string strDestinationLocation = "Hai Chau, Da Nang, Viet Nam";

        internal static string strException = "Exception";
        internal static string strTextSource = "Source";
        internal static string strTextDestination = "Destination";

        internal static string strNoInternet = "No online connection. Please review your internet connection";
        internal static string strPleaseWait = "Please wait...";
        internal static string strUnableToConnect = "Unable to connect server!,Please try after sometime";
    }
}