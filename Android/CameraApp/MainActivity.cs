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
using Android.Widget;

namespace CameraApp
{
    [Activity(Label = "CameraApp",  Icon = "@drawable/icon")]
    public class MainActivity : Activity, Android.Hardware.Camera.IPreviewCallback, Android.Views.ISurfaceHolderCallback
    {
        Android.Hardware.Camera camera;
        SurfaceView cameraView;
        ISurfaceHolder holder;
        CustomImageView customImageView;
        Queue<byte[]> frameQueue = new Queue<byte[]>();
        Queue<string> inputQueue = new Queue<string>();

        int deviceHeight, deviceWidth;
        private Socket socket;
        private string TAG = "Remote View";
        private static int PORT = 4680;
        private string IP_ADDR = "";
        private bool isRunning = true;
        private int scaleX, scaleY;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);
            ActionBar.Hide();

            IP_ADDR = (Intent.GetStringExtra("IPAddress") == "" ? "192.168.100.7" : Intent.GetStringExtra("IPAddress"));
            deviceWidth = 320; // Resources.DisplayMetrics.WidthPixels;
            deviceHeight = 240; // Resources.DisplayMetrics.HeightPixels;

            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
            StrictMode.SetThreadPolicy(policy);

            InitializeCameraView();
            InitializeControls();
            InitializeSocket();

        }

        private void InitializeCameraView()
        {
            camera = Android.Hardware.Camera.Open();
            cameraView = (SurfaceView)FindViewById(Resource.Id.camera_preview);
            holder = cameraView.Holder;
            holder.SetType(SurfaceType.PushBuffers);
            holder.AddCallback((Android.Views.ISurfaceHolderCallback)this);
            //cameraView.SetSecure(true);
        }

        private void InitializeControls()
        {
            customImageView = (CustomImageView)FindViewById(Resource.Id.imagearea);

            Button btnExit = (Button)FindViewById(Resource.Id.btnExit);
            btnExit.Background.SetColorFilter(Color.Green, PorterDuff.Mode.Multiply);
            btnExit.Click += delegate {
                EndSession("Remote Session Ended");
            };
        }

        private void InitializeSocket()
        {
            socket = new Socket();
            socket.SendBufferSize = 100000;
            socket.TcpNoDelay = true;

            try
            {
                socket.Connect(new InetSocketAddress(IP_ADDR, PORT), 5000);

                if (socket.IsConnected)
                {
                    ThreadPool.QueueUserWorkItem(o => SendFrames());
                }
            }
            catch (Exception e)
            {
                Log.Debug("Exception", e.Message);
                EndSession(e.Message);
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
                //FrameLayout layout = (FrameLayout)FindViewById(Resource.Id.frame);
                //ViewGroup.LayoutParams parms = layout.LayoutParameters;
                //parms.Height = 960;
                //parms.Width = 1280;

                //parms.Height = 1440;
                //parms.Width = 1920;
                //layout.LayoutParameters = parms;

                Display display = WindowManager.DefaultDisplay;
                Point size = new Point();
                display.GetSize(size);
                int displayWidth = size.X;
                int displayHeight = size.Y;


                // resolution of desktop viewer is 640x480
                scaleX = Convert.ToInt32(displayWidth / 640);
                scaleY = Convert.ToInt32(displayHeight / 480);

                //Configration Camera Parameter(full-size)
                Android.Hardware.Camera.Parameters parameters = camera.GetParameters();
                Android.Hardware.Camera.Size bestPreviewSize = GetBestPreviewSize(width, height, parameters);

                parameters.SetPictureSize(320, 240);
                //parameters.SetPreviewSize(bestPreviewSize.Width, bestPreviewSize.Height);
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
                EndSession("Remote session ended");
                
            try
            {
                //convert YuvImage(NV21) to JPEG Image data
                YuvImage yuvimage = new YuvImage(data, Android.Graphics.ImageFormatType.Nv21, deviceWidth, deviceHeight, null);
                MemoryStream memStream = new MemoryStream();
                yuvimage.CompressToJpeg(new Rect(0, 0, deviceWidth, deviceHeight), 50, memStream);
                byte[] jdata = memStream.ToArray();

                frameQueue.Enqueue(jdata);

            }
            catch (Exception e)
            {
                Log.Debug(TAG, "Error getting preview frame: " + e.Message);
            }
        }

        private Android.Hardware.Camera.Size GetBestPreviewSize(int width, int height, Android.Hardware.Camera.Parameters parameters)
        {
            Android.Hardware.Camera.Size bestSize = null;
            IList<Android.Hardware.Camera.Size> sizeList = parameters.SupportedPreviewSizes;

            bestSize = sizeList[0];

            for (int i = 1; i < sizeList.Count; i++)
            {
                if ((sizeList[i].Width * sizeList[i].Height) >
                  (bestSize.Width * bestSize.Height))
                {
                    bestSize = sizeList[i];
                }
            }

            return bestSize;
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
                            if (frameQueue.Count > 0)
                            {
                                writer.Print("IMG " + System.Convert.ToBase64String(frameQueue.Dequeue()) + "\r\n");
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
                            customImageView.DrawText(data.Substring(5), 0, 200, 48, Color.Cyan);
                        });

                    }

                    if (data.StartsWith("CIRCLE "))
                    {
                        string dataParams = data.Substring(7);
                        string[] dataArray = dataParams.Split(',');

                        int x = Convert.ToInt32(dataArray[0]) * scaleY;
                        int y = Convert.ToInt32(dataArray[1]) * scaleY;

                        RunOnUiThread(() =>
                        {
                            customImageView.DrawCircle(x, y, 10, Color.Green, 5);
                        });
                    }

                    if (data.StartsWith("LINE "))
                    {
                        string dataParams = data.Substring(5);
                        string[] dataArray = dataParams.Split(',');

                        int startx = Convert.ToInt32(dataArray[0]) * scaleX;
                        int starty = Convert.ToInt32(dataArray[1]) * scaleY;
                        int endx = Convert.ToInt32(dataArray[2]) * scaleX;
                        int endy = Convert.ToInt32(dataArray[3]) * scaleY;

                        RunOnUiThread(() =>
                        {
                            customImageView.DrawLine(startx, starty, endx, endy, Color.Green, 5);
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
                isRunning = false;
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

        public void EndSession(string message)
        {
            if (isCameraInUse())
            {
                camera.Release();
                camera = null;
            }

            if (message != "")
                Toast.MakeText(this, message, ToastLength.Long).Show();

            this.Finish();
        }
    }
}

