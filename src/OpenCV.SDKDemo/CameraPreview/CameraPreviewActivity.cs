using OpenCV.SDKDemo.Utilities;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Locations;
using Android.Runtime;
using Android.Content;
//using Android.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace OpenCV.SDKDemo.CameraPreview
{
    [Activity(Label = ActivityTags.CameraPreview)]
    public class CameraPreviewActivity : Activity, IOnMapReadyCallback, ILocationListener, GoogleMap.IOnInfoWindowClickListener
    {
        GoogleMap map;
        Spinner spinner;
        LocationManager locationManager;
        String provider;
        LatLng latLngSource;
        LatLng latLngDestination;
        String polyline;

        //Giai ma code
        private List<LatLng> DecodePolyline(string encodedPoints)
        {
            if (string.IsNullOrWhiteSpace(encodedPoints))
            {
                return null;
            }

            int index = 0;
            var polylineChars = encodedPoints.ToCharArray();
            var poly = new List<LatLng>();
            int currentLat = 0;
            int currentLng = 0;
            int next5Bits;

            while (index < polylineChars.Length)
            {
                // calculate next latitude
                int sum = 0;
                int shifter = 0;

                do
                {
                    next5Bits = polylineChars[index++] - 63;
                    sum |= (next5Bits & 31) << shifter;
                    shifter += 5;
                }
                while (next5Bits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length)
                {
                    break;
                }

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                // calculate next longitude
                sum = 0;
                shifter = 0;

                do
                {
                    next5Bits = polylineChars[index++] - 63;
                    sum |= (next5Bits & 31) << shifter;
                    shifter += 5;
                }
                while (next5Bits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length && next5Bits >= 32)
                {
                    break;
                }

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                var mLatLng = new LatLng(Convert.ToDouble(currentLat) / 100000.0, Convert.ToDouble(currentLng) / 100000.0);
                poly.Add(mLatLng);
            }

            return poly;
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            map = googleMap;

            //Optional
            googleMap.UiSettings.ZoomControlsEnabled = true;
            googleMap.UiSettings.CompassEnabled = true;
            googleMap.MoveCamera(CameraUpdateFactory.ZoomIn());
            googleMap.UiSettings.MyLocationButtonEnabled = true;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.CameraPreview);
            spinner = FindViewById<Spinner>(Resource.Id.spinner);
            MapFragment mapFragment = (MapFragment)FragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);

            spinner.ItemSelected += Spinner_ItemSelected;
            spinner.ItemSelected += Spinner_ItemSelected;
            locationManager = (LocationManager)GetSystemService(Context.LocationService);
            provider = locationManager.GetBestProvider(new Criteria(), false);
            Location location = locationManager.GetLastKnownLocation(provider);
            if (location == null)
                System.Diagnostics.Debug.WriteLine("No Location");

            FnProcessOnMap();

        }

        private void Spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            switch (e.Position)
            {
                case 0: //Hybird
                    map.MapType = GoogleMap.MapTypeHybrid;
                    break;
                case 1: //None
                    map.MapType = GoogleMap.MapTypeNone;
                    break;
                case 2: //Normal
                    map.MapType = GoogleMap.MapTypeNormal;
                    break;
                case 3: //Statellite
                    map.MapType = GoogleMap.MapTypeSatellite;
                    break;
                case 4: //Terrain
                    map.MapType = GoogleMap.MapTypeTerrain;
                    break;
                default:
                    map.MapType = GoogleMap.MapTypeNone;
                    break;
            }
        }

        async void FnProcessOnMap()
        {
            await FnLocationToLatLng();

            var editStartPoint = FindViewById<EditText>(Resource.Id.editStartPoint);
            var editEndPoint = FindViewById<EditText>(Resource.Id.editEndPoint);
            var btnFindPath = FindViewById<Button>(Resource.Id.button1);

            editStartPoint.Text = "Thanh Khe, Da Nang, Viet Nam";
            editEndPoint.Text = "Hai Chau, Da Nang, Viet Nam";

            btnFindPath.Click += (e, o) =>
            {
                Constants.strSourceLocation = editStartPoint.Text;
                Constants.strDestinationLocation = editEndPoint.Text;

                if (latLngSource != null && latLngDestination != null)
                {
                    FnDrawPath(Constants.strSourceLocation, Constants.strDestinationLocation);
                }

            };
        }

        void MarkOnMap(string title, LatLng pos)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var marker = new MarkerOptions();
                    marker.SetTitle(title);
                    marker.SetPosition(pos); //Resource.Drawable.BlueDot
                    map.AddMarker(marker);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        async void FnDrawPath(string strSource, string strDestination)
        {
            string strFullDirectionURL = string.Format(Constants.strGoogleDirectionUrl, strSource, strDestination);
            string strJSONDirectionResponse = await FnHttpRequest(strFullDirectionURL);
            if (strJSONDirectionResponse != Constants.strException)
            {
                RunOnUiThread(() =>
                {
                    if (map != null)
                    {
                        map.Clear();
                        MarkOnMap(Constants.strTextSource, latLngSource);
                        MarkOnMap(Constants.strTextDestination, latLngDestination);
                    }
                });
                FnSetDirectionQuery(strJSONDirectionResponse);
            }
            else
            {
                RunOnUiThread(() =>
                   Toast.MakeText(this, Constants.strUnableToConnect, ToastLength.Short).Show());
            }
        }

        void FnSetDirectionQuery(string strJSONDirectionResponse)
        {
            //var txtResult = FindViewById<TextView>(Resource.Id.textResult);

            var objRoutes = JsonConvert.DeserializeObject<GoogleDirectionClass>(strJSONDirectionResponse);

            //objRoutes.routes.Count  --may be more then one 
            //if (objRoutes.routes.Count != 0)
            if (true)
            {
                //string encodedPoints = objRoutes.routes[0].overview_polyline.points;
                //List<LatLng> lstDecodedPoints = DecodePolyline(encodedPoints);
                List<LatLng> lstDecodedPoints = DecodePolyline("yf}`Bk|osS}E\\kBFo@UEEOCMFCNDLDB@?FjBNxAH~@z@xMh@~Hl@|GNtCHbAGXUr@wDnHcDxFY\\_@\\I@OBUNINkBNwBXgCTiHf@k@D}BXuAd@yAp@k@d@_@`@Uf@CAEAQAQDONCLAR@BKLU`@OPg@^eAn@SH_Cn@q@Na@PCCEEOCOBKNAH?BYLuE`ByD|AcJ|EsBx@_JxCoBt@o@n@@^@tBFzEP|@D~BHxEExGKhFIpDE~FSrIEvBC`E@tCT|GlCAXbJ`Bi@bAo@NGKi@?E?CDE@G");

                var polylineOptions = new PolylineOptions();
                polylineOptions = new PolylineOptions();
                polylineOptions.InvokeColor(global::Android.Graphics.Color.Red);
                polylineOptions.InvokeWidth(4);

                foreach (LatLng line in lstDecodedPoints)
                {
                    polylineOptions.Add(line);
                }

                map.AddPolyline(polylineOptions);
            }
        }

        async Task<bool> FnLocationToLatLng()
        {
            try
            {
                var geo = new Geocoder(this);
                //var sourceAddress = await geo.GetFromLocationNameAsync(Constants.strSourceLocation, 1);
                var sourceAddress = await geo.GetFromLocationNameAsync(Constants.strSourceLocation, 1);
                sourceAddress.ToList().ForEach((addr) =>
                {
                    latLngSource = new LatLng(addr.Latitude, addr.Longitude);
                });

                //var destAddress = await geo.GetFromLocationNameAsync(Constants.strDestinationLocation, 1);
                var destAddress = await geo.GetFromLocationNameAsync(Constants.strDestinationLocation, 1);
                destAddress.ToList().ForEach((addr) =>
                {
                    latLngDestination = new LatLng(addr.Latitude, addr.Longitude);
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            locationManager.RequestLocationUpdates(provider, 400, 1, this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            locationManager.RemoveUpdates(this);
        }

        public void OnProviderDisabled(string provider)
        {
            //throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            //throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            //throw new NotImplementedException();
        }

        public void OnInfoWindowClick(Marker marker)
        {
            Toast.MakeText(this, $"Icon {marker.Title} is clicked", ToastLength.Short).Show();
        }

        WebClient webclient;
        async Task<string> FnHttpRequest(string strUri)
        {
            webclient = new WebClient();
            string strResultData;
            try
            {
                strResultData = await webclient.DownloadStringTaskAsync(new Uri(strUri));
                Console.WriteLine(strResultData);
            }
            catch
            {
                strResultData = "Exception";
            }
            finally
            {
                if (webclient != null)
                {
                    webclient.Dispose();
                    webclient = null;
                }
            }

            return strResultData;
        }

        public void OnLocationChanged(Location location)
        {
            Double lat, lng;
            lat = 16.0660217;
            lng = 108.2210158;

            MarkerOptions makerOptions = new MarkerOptions();
            makerOptions.SetPosition(new LatLng(lat, lng));
            makerOptions.SetTitle("My Position");
            map.AddMarker(makerOptions);

            //Move Camera
            CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
            builder.Target(new LatLng(lat, lng));
            builder.Zoom(12);
            CameraPosition cameraPosition = builder.Build();
            CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
            map.MoveCamera(cameraUpdate);
            map.MyLocationEnabled = true;
        }

        string FnHttpRequestOnMainThread(string strUri)
        {
            webclient = new WebClient();
            string strResultData;
            try
            {
                strResultData = webclient.DownloadString(new Uri(strUri));
                Console.WriteLine(strResultData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                strResultData = Constants.strException;
            }
            finally
            {
                if (webclient != null)
                {
                    webclient.Dispose();
                    webclient = null;
                }
            }

            return strResultData;
        }
    }
}