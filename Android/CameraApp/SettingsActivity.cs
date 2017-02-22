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

namespace CameraApp
{
    [Activity(Label = "SettingsActivity", MainLauncher = true)]
    public class SettingsActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.Settings);

            this.Title = "RemoteView Settings";
          
            Button btnStart = FindViewById<Button>(Resource.Id.btnStart);

            btnStart.Click += delegate {

                var ipAddress = FindViewById<EditText>(Resource.Id.txtIPAddress).Text;
                var mainActivity = new Intent(this, typeof(MainActivity));
                mainActivity.PutExtra("IPAddress", ipAddress);
                StartActivity(mainActivity);
            };
        }


}
}