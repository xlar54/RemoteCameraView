using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using Android.Hardware;
using System;
using Android.Graphics;
using Android.Runtime;
using Android.Content;
using System.Collections.Generic;

namespace CameraApp
{
    [Activity(Label = "CameraApp",  Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private Android.Hardware.Camera mCamera;
        TestCameraView mPreview;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            string ipAddress = Intent.GetStringExtra("IPAddress") ?? "192.168.100.18";

            mCamera = getCameraInstance();
            mPreview = new TestCameraView(this, mCamera, ipAddress);
            FrameLayout preview = (FrameLayout)FindViewById(Resource.Id.camera_preview);
            preview.AddView(mPreview);
            
        }

        public static Android.Hardware.Camera getCameraInstance()
        {
            Android.Hardware.Camera c = null;
            try
            {
                Console.Out.WriteLine("Camera open");
                c = Android.Hardware.Camera.Open();

                Android.Hardware.Camera.Parameters parms = c.GetParameters();

                // Check what resolutions are supported by your camera
                var sizes = parms.SupportedPreviewSizes;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e + "Camera is not available (in use or does not exist)");
            }
            return c; // returns null if camera is unavailable
        }


    }
}

