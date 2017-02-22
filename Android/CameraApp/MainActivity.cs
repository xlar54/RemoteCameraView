using Android.App;
using Android.OS;
using Android.Views;
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
using static Android.Hardware.Camera;

namespace CameraApp
{
    [Activity(Label = "CameraApp",  Icon = "@drawable/icon")]
    public class MainActivity : Activity, Android.Hardware.Camera.IPreviewCallback, Android.Views.ISurfaceHolderCallback
    {
        SurfaceView cameraView;
        ISurfaceHolder holder;
        CustomImageView customImageView;
        Android.Hardware.Camera camera;

        int deviceHeight, deviceWidth;
        private Socket socket;
        private string TAG = "Camera Preview";
        private static int PORT = 4680;
        private string IP_ADDR = "192.168.100.18";

        Queue<byte[]> queue = new Queue<byte[]>();
        Queue<string> inputQueue = new Queue<string>();

        static object Lock = new object();

        bool isRunning = true;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            camera = Android.Hardware.Camera.Open();

            string ipAddress = (Intent.GetStringExtra("IPAddress") == "" ? "192.168.100.18" : Intent.GetStringExtra("IPAddress"));

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

            socket = new Socket();
            socket.SendBufferSize = 100000;
            socket.TcpNoDelay = true;

            try
            {
                socket.Connect(new InetSocketAddress(IP_ADDR, PORT));

                if (socket.IsConnected)
                {
                    ThreadPool.QueueUserWorkItem(o => SendFrames());
                }
            }
            catch (Exception e)
            {
                Log.Debug("Exception", e.Message);
            }
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                camera.SetPreviewDisplay(holder);
                

            }
            catch (Exception e)
            {
                Log.Debug("Exception", e.Message);
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            if (isCameraInUse())
            {
                camera.StopPreview();
                camera.Release();
                camera = null;
            }
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
                parameters.PreviewFormat = Android.Graphics.ImageFormatType.Nv21;// ImageFormat.Nv21;

                camera.SetParameters(parameters);
                camera.SetPreviewDisplay(holder);
                camera.SetPreviewCallback(this);
                camera.StartPreview();
            }
            catch (Exception e)
            {
                Log.Debug("Exception", e.Message);
            }
        }

        public void OnPreviewFrame(byte[] data, Android.Hardware.Camera camera)
        {
            if (!isRunning)
            {
                if (isCameraInUse())
                {
                    camera.Release();
                    camera = null;
                }

                
                this.Finish();
            }
                

            try
            {
                //convert YuvImage(NV21) to JPEG Image data
                YuvImage yuvimage = new YuvImage(data, Android.Graphics.ImageFormatType.Nv21, deviceWidth, deviceHeight, null);
                MemoryStream baos = new MemoryStream();
                yuvimage.CompressToJpeg(new Rect(0, 0, deviceWidth, deviceHeight), 50, baos);
                byte[] jdata = baos.ToArray();

                queue.Enqueue(jdata);

                
            }
            catch (Exception e)
            {
                Log.Debug(TAG, "Error getting preview frame: " + e.Message);
            }
        }

        public void SendFrames()
        {
            try
            {
                BufferedReader reader = new BufferedReader(new InputStreamReader(socket.InputStream));
                PrintWriter writer = new PrintWriter(socket.OutputStream, true);

                while (true)
                {
                    string data = reader.ReadLine();

                    if (data == "OK")
                    {
                        // Continuosly send frames if they exist in the queue
                        while (true)
                        {
                            if (queue.Count > 0)
                            {
                                writer.Print("IMG " + System.Convert.ToBase64String(queue.Dequeue()) + "\r\n");
                                writer.Flush();

                                writer.Print("END\r\n");
                                writer.Flush();
                                break;
                            }
                        }
                    }

                    if (data.StartsWith("TEXT "))
                    {
                        RunOnUiThread(() =>
                        {
                            customImageView.DrawText(data.Substring(5), 50, 50, 72, Color.Cyan);
                        });

                    }

                    if (data.StartsWith("CIRCLE "))
                    {
                        string dataParams = data.Substring(7);
                        string[] dataArray = dataParams.Split(',');

                        int x = Convert.ToInt32(dataArray[0]);
                        int y = Convert.ToInt32(dataArray[1]);

                        RunOnUiThread(() =>
                        {
                            customImageView.DrawCircle(x, y, 10, Color.Green, 5);
                            //customImageView.DrawLine(0, 0, 200, 200, Color.Green, 5);
                        });
                    }


                    if (data.Equals("FLASH"))
                        SetFlash(true);

                    if (data.Equals("NOFLASH"))
                        SetFlash(false);

                    if (data.Equals("TERMINATE"))
                    {
                        isRunning = false;
                        Thread.CurrentThread.Abort();
                    }

                    if (data.Equals("CLEAR"))
                    {
                        RunOnUiThread(() =>
                        {
                            customImageView.Clear();
                        });
                    }


                }
            }
            catch (Exception e)
            {
                Log.Debug("Exception: ", e.Message);
                Thread.CurrentThread.Abort();
            }
        }

        public void SetFlash(bool flash)
        {
            Android.Hardware.Camera.Parameters parameters = camera.GetParameters();

            if (flash)
            {
                parameters.FlashMode = Parameters.FlashModeTorch;
            }
            else
            {
                parameters.FlashMode = Parameters.FlashModeOff;
            }

            camera.SetParameters(parameters);
        }

        public bool isCameraInUse()
        {
            camera = null;
            try
            {
                camera = Android.Hardware.Camera.Open();
            }
            catch (Exception e)
            {
                return true;
            }
            finally
            {
                if (camera != null) camera.Release();
            }
            return false;
        }

    }
}

