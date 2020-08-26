using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;


using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.Core.App;

using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;

using Com.Sirl.Core;
using Com.Sirl.Core.Listeners;
using Com.Sirl.Core.Location;
using Com.Sirl.Core.Recording;
using Com.Sirl.Mapping;
using Com.Sirl.Retail.UI;
using Android.Widget;
using Android.Content.PM;
using Android;
using Com.Sirl.Core.Models;
using Com.Sirl.Core.Location.Filters;
using Com.Sirl.Mapping.Routeutils;

namespace SIRLTutorial
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILocationUpdateListener
    {
        private const String TAG = "ShoppersPortal";

        //Example External Trip ID. This value may be a String or Integer.
        private const String EXAMPLE_EXTERNAL_TRIP_ID_STRING = "EXAMPLE_EXT_TRIP_ID";
        private const int EXAMPLE_EXTERNAL_TRIP_ID_INT = 123;

        //Example External User ID. This value may be a String or Integer.
        private const String EXAMPLE_EXTERNAL_USER_ID_STRING = "EXAMPLE_EXT_USER_ID";
        private const int EXAMPLE_EXTERNAL_USER_ID_INT = 321;

        private const String EXAMPLE_PRODUCT_COLLECTED_ID = "EXAMPLE_PRODUCT_ID";
        private const String EXAMPLE_TRANSACTION_DATA = "{\n" +
           "    \"total\": 39.22,\n" +
           "    \"productsCollected\": [\n" +
           "        {\n" +
           "            \"upc\": \"EXAMPLE_PRODUCT_ID\",\n" +
           "            \"quantity\": 2\n" +
           "        }\n" +
           "    ]\n" +
           "}";

        private SirlPipsManager mSirlManager;
        private TripStateListener mTripStateListener;
        private ExternalLogger mExternalLogger;
        private SirlMapFragment mSirlMapFragment;
        private SearchFragment mSirlSearchFragment;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            mExternalLogger = ExternalLogger.Instance;
            mTripStateListener = new TutorialTripStateListener(this, mExternalLogger);

            mSirlMapFragment = (SirlMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map);
            mSirlSearchFragment = (SearchFragment)SupportFragmentManager.FindFragmentById(Resource.Id.search_bar);

            setupSirl();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, System.EventArgs eventArgs)
        {
            View view = (View)sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);


            if (grantResults.Length > 0 && requestCode == 1)
            {
                if (grantResults[0] == Permission.Granted)
                {
                    Log.Debug("permission", "location permissions granted");
                    initializeSirl();
                }
                else
                {
                    //Close the Activity & App if Location Permissions not granted
                    Finish();
                }
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (mSirlManager != null)
            {
                mSirlManager.DeregisterTripStateListener(mTripStateListener);
                mSirlManager.DeregisterLocationListener(this);
                mSirlManager.StopLocationUpdates();
            }
        }

        public void OnLocationUpdate(Location location)
        {
            //
            // Quick Start - Toast Location Updates
            //
            //string locationString =
            //     string.Format(
            //         "Your position is x:{0:F2}, y:{1:F2}",
            //         location.GetX(),
            //         location.GetY()
            //     );
            //Toast.MakeText(this, locationString, ToastLength.Short).Show();

        }

        private void setupSirl()
        {
            if (hasPermissions())
            {
                initializeSirl();
            }
            else
            {
                requestPermissions();
            }
        }

        private void initializeSirl()
        {
            LocationProviderConfig locationProviderConfig =
                new LocationProviderConfig.Builder(Android.App.Application.Context)
                    //We disable background mode here to allow manual control, but
                    //this can be enabled to allow the system to automatically handle
                    //location update state.
                    .EnableBackgroundMode(false)
                    .UseMappedLocationResolver(new TutorialMappedLocationResolver(Android.App.Application.Context))
                    .UseLocationEngine(new TutorialEngine())
                    .UseLocationFilters(createTestLocationFilters())
                    .Build();

            SirlPipsManager.Config config = new SirlPipsManager.Config(Android.App.Application.Context);
            config.SetAPIKey("9mkYplpPuo2PCes7dqhIU74yg5dgO16M9D5hcqJS");
            config.SetLocationConfig(locationProviderConfig);

            mSirlManager = SirlPipsManager.GetInstance(config);

            mSirlSearchFragment.RegisterRouteStatusListener(new TutorialRouteStatusListener());
            mSirlSearchFragment.AttachMapFragment(mSirlMapFragment);

            mSirlManager.RegisterTripStateListener(mTripStateListener);
            mSirlManager.RegisterLocationListener(this);
            mSirlManager.StartLocationUpdates();
        }

        private Boolean hasPermissions()
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                return !((ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation)
                            != (int)Permission.Granted)
                        || (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation)
                            != (int)Permission.Granted)
                        || (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Internet)
                            != (int)Permission.Granted));
            }
            return true;
        }

        private void requestPermissions()
        {
            ActivityCompat.RequestPermissions(this, new String[]
                    {Manifest.Permission.AccessFineLocation,
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.Internet}, 1);
        }

        private LocationFilters createTestLocationFilters()
        {
            return new LocationFilters.Builder()
                    .ShouldLockToRegion(false)
                    .ShouldPredict(false)
                    .Build();
        }

        private class TutorialEngine : SirlLocationEngine
        {
            public Handler mHandler = new Handler();

            public override void InitializeForMappedLocation(MappedLocation mappedLocation)
            {
                //For this test engine, this hook is not necessary. For any custom location engine,
                //this hook is meant to initialize the component with any relevant configurations 
                //for the current location.
            }

            public override string EngineVersion
            {
                // Metadata.xml XPath method reference: path="/api/package[@name='com.sirl.core.location']/class[@name='SirlLocationEngine']/method[@name='getEngineVersion' and count(parameter)=0]"
                [Register("getEngineVersion", "()Ljava/lang/String;", "GetGetEngineVersionHandler")]
                get
                {
                    return "Tutorial Test Engine";
                }
            }

            public override void StartPolling()
            {
                _ = mHandler.Post(new RandomPositionRunnable(this));
            }

            public override void StopPolling()
            {
                mHandler.RemoveCallbacksAndMessages(null);
            }
        }


        private class RandomPositionRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            TutorialEngine mEngine = null;
            public RandomPositionRunnable(TutorialEngine engine)
            {
                mEngine = engine;
            }

            public void Run()
            {
                mEngine.mHandler.PostDelayed(this, 1000);

                Random rand = new Random();

                Location randomLocation = new Location(
                        11.5 + rand.NextDouble() - 0.5,
                        4.5 + rand.NextDouble() - 0.5,
                        1.5
                );

                mEngine.Update(randomLocation);
            }
        }

        private class TutorialMappedLocationResolver : MappedLocationResolver
        {
            public TutorialMappedLocationResolver(Context context)
                    : base(context) { }

            public override void DetermineMappedLocation(IMappedLocationResolveCallback cb)
            {
                //This hook is automatically invoked periodically. This is used
                //to determine the ranging state of the user (e.g. entered, left, 
                //or no change). For this tutorial, we continuously return the 
                //test environment. This can be returned many or a single time - 
                //this decision is left up to the client.

                cb.EnteredLocation(new MappedLocation(10));
            }
        }

        private class TutorialRouteStatusListener : Java.Lang.Object, IRouteStatusListener
        {
            public void OnRouteStart()
            {
                Log.Debug("ShopperPortal", "Route start!");
            }

            public void OnRouteComplete()
            {
                Log.Debug("ShopperPortal", "Route complete!");
            }

            public void OnRouteFail(RouteError routeError)
            {
                Log.Error("ShopperPortal", "Route error: " + routeError.Message);
            }
        }

        private class TutorialTripStateListener : TripStateListener
        {
            private Context context;
            private ExternalLogger externalLogger;

            public TutorialTripStateListener(Context context, ExternalLogger logger)
            {
                this.context = context;
                this.externalLogger = logger;
            }

            public override void OnTripStart()
            {
                //Signifies that the trip has begun. One or more of the
                //calls for before the trip should be used in this hook.
                Log.Debug(TAG, "Trip Started");
                Toast.MakeText(
                      context,
                      "Trip Started.",
                      ToastLength.Short
                ).Show();

                //Record User ID and Trip ID here. One of the String/Integer interfaces
                //may be used.

                //String interfaces
                externalLogger.RecordExternalUserId(EXAMPLE_EXTERNAL_USER_ID_STRING);
                externalLogger.RecordExternalTripId(EXAMPLE_EXTERNAL_TRIP_ID_STRING);
            }

            public override void OnTripStop()
            {
                //Signifies that the trip has begun. One or more of the
                //calls for after the trip should be used in this hook.
                Log.Debug(TAG, "Trip Stopped");
                Toast.MakeText(
                      context,
                      "Trip Ended.",
                      ToastLength.Short
                ).Show();

                externalLogger.RecordTransactionLog(EXAMPLE_TRANSACTION_DATA);
            }
        }
    }
}
