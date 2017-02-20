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
using Android.Util;
using System.IO;
using Java.Net;
using Java.IO;
using System.Threading;

namespace CameraApp
{
    [Activity(Label = "CameraApp",  Icon = "@drawable/icon")]
    public class MainActivity : Activity, Android.Hardware.Camera.IPreviewCallback, Android.Views.ISurfaceHolderCallback
    {
        SurfaceView cameraView;
        ISurfaceHolder holder;
        CustomImageView customImageView;
        Android.Hardware.Camera camera;

        private float RectLeft, RectTop, RectRight, RectBottom;

        int deviceHeight, deviceWidth;
        private int curFrame = 0;
        private int skipFrame = 3;
        private Socket socket;
        private string TAG = "MyActivity";
        private static int PORT = 4680;
        private string IP_ADDR = "192.168.100.18";

        Queue<byte[]> queue = new Queue<byte[]>();
        Queue<string> inputQueue = new Queue<string>();
        static object Lock = new object();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            camera = Android.Hardware.Camera.Open();
            string ipAddress = Intent.GetStringExtra("IPAddress") ?? "192.168.100.18";

            cameraView = (SurfaceView)FindViewById(Resource.Id.camera_preview);
            holder = cameraView.Holder;
            holder.SetType(SurfaceType.PushBuffers);
            holder.AddCallback((Android.Views.ISurfaceHolderCallback)this);
            //cameraView.SetSecure(true);

            customImageView = (CustomImageView)FindViewById(Resource.Id.imagearea);

            //getting the device heigth and width
            deviceWidth = 320; // Resources.DisplayMetrics.WidthPixels;
            deviceHeight = 240; // Resources.DisplayMetrics.HeightPixels;

            IP_ADDR = ipAddress;

            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
            StrictMode.SetThreadPolicy(policy);

            ThreadPool.QueueUserWorkItem(o => SendData());
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                camera.SetPreviewDisplay(holder);

                customImageView.DrawLine(0, 0, 300, 300, Color.Green, 5);

            }
            catch (Exception e)
            {
            }
        }


        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            /*camera.SetPreviewCallback(null);
            camera.Release();
            camera = null;*/
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            if (holder.Surface == null)
                return;

            try
            {
                camera.StopPreview();

            }
            catch (Exception e)
            {
                Log.Debug("Exception", e.Message);
                return;
            }

            try
            {
                //Configration Camera Parameter(full-size)
                Android.Hardware.Camera.Parameters parameters = camera.GetParameters();

                parameters.SetPictureSize(320, 240);
                parameters.SetPreviewSize(320, 240);

                //this.width = parameters.PreviewSize.Width;
                //this.height = parameters.PreviewSize.Height;

                parameters.PreviewFormat = Android.Graphics.ImageFormatType.Nv21;// ImageFormat.Nv21;

                camera.SetParameters(parameters);
                camera.SetPreviewDisplay(holder);
                camera.SetPreviewCallback(this);
                camera.StartPreview();
            }
            catch (Exception e)
            {
            }
        }

        public void OnPreviewFrame(byte[] data, Android.Hardware.Camera camera)
        {
            try
            {
                curFrame++;

                if (curFrame == skipFrame)
                {
                    //convert YuvImage(NV21) to JPEG Image data
                    YuvImage yuvimage = new YuvImage(data, Android.Graphics.ImageFormatType.Nv21, deviceWidth, deviceHeight, null);
                    MemoryStream baos = new MemoryStream();
                    yuvimage.CompressToJpeg(new Rect(0, 0, deviceWidth, deviceHeight), 100, baos);
                    byte[] jdata = baos.ToArray();

                    queue.Enqueue(jdata);
                    
                    curFrame = 0;
                }

            }
            catch (Exception e)
            {
                Log.Debug(TAG, "Error getting preview frame: " + e.Message);
            }
        }

        public void SendData()
        {
            while (true)
            {
                if (queue.Count > 0)
                {
                    byte[] jdata = queue.Dequeue();

                    socket = new Socket();
                    socket.Connect(new InetSocketAddress(IP_ADDR, PORT));

                    BufferedOutputStream outStream = new BufferedOutputStream(socket.OutputStream);
                    
                    Log.Debug(TAG, "sending data... ");
                    outStream.Write(jdata);
                    outStream.Flush();
                    outStream.Close();
                }
            }
        }

    }
}

