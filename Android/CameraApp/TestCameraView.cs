using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using Android.Hardware;
using System;
using Android.Graphics;
using Android.Runtime;
using Android.Content;
using Java.Net;
using Java.IO;
using Android.Util;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace CameraApp
{
    [Activity(Label = "CameraApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class TestCameraView : SurfaceView, Android.Hardware.Camera.IPreviewCallback, Android.Views.ISurfaceHolderCallback
    {
        ISurfaceHolder mHolder;
        private Android.Hardware.Camera mCamera;
        private int width;
        private int height;
        private static int PORT = 4680;
        private static string IP_ADDR = "192.168.100.18";
        private byte[] mFrameBuffer;
        private Context con;

        Queue<byte[]> queue = new Queue<byte[]>();

        private Socket socket;
        private string TAG = "MyActivity";

        private System.Net.Sockets.UdpClient usocket;

        private int curFrame = 0;
        private int skipFrame = 3;

        public TestCameraView(Context context, Android.Hardware.Camera camera) : base(context)
        {
            mCamera = camera;
            mHolder = Holder;
            mHolder.AddCallback(this);
            mHolder.SetType(SurfaceType.PushBuffers);

            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
            StrictMode.SetThreadPolicy(policy);

            ThreadPool.QueueUserWorkItem(o => SendData());

        }

        public void SendData()
        {
            while (true)
            {
                if(queue.Count > 0)
                {
                    byte[] jdata = queue.Dequeue();

                    socket = new Socket();
                    socket.Connect(new InetSocketAddress(IP_ADDR, PORT));

                    BufferedOutputStream outStream = new BufferedOutputStream(socket.OutputStream);

                    Log.Debug(TAG, "sending data... ");
                    outStream.Write(jdata);
                    outStream.Flush();
                    outStream.Close();

                    /*usocket = new System.Net.Sockets.UdpClient();
                    usocket.Connect(IP_ADDR, PORT);
                    usocket.Send(jdata, jdata.Length);
                    usocket.Close();*/

                }
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
                    YuvImage yuvimage = new YuvImage(data, Android.Graphics.ImageFormatType.Nv21, this.width, this.height, null);
                    MemoryStream baos = new MemoryStream();
                    yuvimage.CompressToJpeg(new Rect(0, 0, width, height), 100, baos);
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

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            try
            {
                mCamera.StopPreview();
            }
            catch (Exception e)
            {
            }
            try
            {
                //Configration Camera Parameter(full-size)
                Android.Hardware.Camera.Parameters parameters = mCamera.GetParameters();

                parameters.SetPictureSize(320, 240);
                parameters.SetPreviewSize(320, 240);

                this.width = parameters.PreviewSize.Width;
                this.height = parameters.PreviewSize.Height;

                parameters.PreviewFormat = Android.Graphics.ImageFormatType.Nv21;// ImageFormat.Nv21;
                
                mCamera.SetParameters(parameters);
                mCamera.SetPreviewCallback(this);
                mCamera.StartPreview();
            }
            catch (Exception e)
            {
            }
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                mCamera.SetPreviewDisplay(holder);
            }
            catch (Exception e)
            {
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            mCamera.SetPreviewCallback(null);
            mCamera.Release();
            mCamera = null;
        }
    }
}

