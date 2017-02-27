using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SuperSocket;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketBase.Config;
using System.Net;
using Newtonsoft.Json;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace RemoteCameraView
{

    public partial class Form1 : Form
    {

        private Timer timer = new Timer();
        private Queue<byte[]> imageDataQueue = new Queue<byte[]>();
        private AppServer server = new AppServer();
        AppSession sendSession = null;

        Image<Bgr, Byte> imgOriginal;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            server.Setup(new ServerConfig { Port = 4680,MaxRequestLength = 100000, ReceiveBufferSize=100000 });

            server.NewSessionConnected += new SessionHandler<AppSession>(appServer_NewSessionConnected);
            server.NewRequestReceived += new RequestHandler<AppSession, StringRequestInfo>(appServer_NewRequestReceived);

            timer.Interval = 10;
            timer.Tick += T_Tick;

            // Transparent background...  
            pictureBoxOverlay.BackColor = Color.Transparent;

            // Change parent for overlay PictureBox...
            pictureBoxOverlay.Parent = pictureBox1;

            // Change overlay PictureBox position in new parent...
            pictureBoxOverlay.Location = new Point(0, 0);

            ClearPictureBox(pictureBoxOverlay);
        }


        string dataBody = "";

        private void appServer_NewRequestReceived(AppSession session, StringRequestInfo requestInfo)
        {
            switch (requestInfo.Key.ToUpper())
            {
                case ("ECHO"):
                    session.Send(requestInfo.Body);
                    break;
                case ("IMG"):
                    {
                        dataBody = requestInfo.Parameters[0];
                        break;
                    }
                case ("END"):
                    {
                        try
                        { 
                            byte[] bdata = Convert.FromBase64String(dataBody);
                            imageDataQueue.Enqueue(bdata);
                            session.Send("OK");
                        }
                        catch (Exception ex)
                        {
                            session.Send("ERR");
                        }
                        break;
                    }

                default:
                    {
                        if (requestInfo.Parameters.Count() == 0)
                        {
                            dataBody += requestInfo.Key;
                        }

                        break;
                    }
            }
        }

        private void appServer_NewSessionConnected(AppSession session)
        {
            session.SocketSession.Client.NoDelay = true;
            session.SocketSession.Client.ReceiveTimeout = 60000;
            session.SocketSession.Client.ReceiveBufferSize = 100000;
            session.Send("OK");

            IPEndPoint remoteIpEndPoint = session.SocketSession.RemoteEndPoint;
            this.Invoke((MethodInvoker)delegate {
                
                label1.Text = "Remote IP: " + remoteIpEndPoint.Address;
            });
            

            sendSession = session;
        }

        private void T_Tick(object sender, EventArgs e)
        {
            if (imageDataQueue.Count > 0)
            {
                byte[] arr = imageDataQueue.Dequeue();
                Image i = byteArrayToImage(arr);

                imgOriginal = new Image<Bgr, Byte>(new Bitmap(i));

                if (checkBox1.Checked)
                    imgOriginal = FindCircles(imgOriginal);

                if (checkBox2.Checked)
                    imgOriginal = MatchTemplate(imgOriginal);

                //imgOriginal = CascadeClassify(imgOriginal);

                pictureBox1.Image = imgOriginal.ToBitmap();
                //pictureBox1.Image = i;
            }
        }

        private Image<Bgr, byte> MatchTemplate(Image<Bgr, byte> image)
        {
            Image<Bgr, byte> template = new Image<Bgr, byte>("I:\\template.png");

            try
            {
                using (Image<Gray, float> result = image.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                {
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    // Somewhere between 0.75 and 0.95 would be good.
                    if (maxValues[0] > 0.7)
                    {
                        // This is a match. Do something with it, for example draw a rectangle around it.
                        Rectangle match = new Rectangle(maxLocations[0], template.Size);
                        image.Draw(match, new Bgr(Color.Red), 3);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return image;

        }

        private Image<Bgr, byte> DrawContours(Image<Gray, byte> image, VectorOfVectorOfPoint contours, Bgr color, int thickness, int maxLevel)
        {
            Image<Bgr, Byte> resultImage = new Image<Bgr, byte>(image.Size);
            resultImage = image.Convert<Bgr, Byte>();
            CvInvoke.DrawContours(resultImage, 
                contours, 
                1,
                color.MCvScalar,
                thickness,
                LineType.AntiAlias,
                null,
                maxLevel,
                new Point(0, 0));

            return resultImage;
        }

        private Image<Bgr, byte> FindCircles(Image<Bgr, byte> original)
        {
            //Capture capWebcam = new Capture();
            //imgOriginal = capWebcam.QueryFrame();

            Image<Gray, byte> mask = original.InRange(new Bgr(0, 0, 175), new Bgr(50, 50, 255));
            //mask = imgOriginal.InRange(new Bgr(9, 86, 6), new Bgr(70, 255, 255));
            //mask = mask.Canny(149, 149);
            //imgOriginal = imgOriginal.Resize(160, 120, Inter.Area);

            mask = mask.SmoothGaussian(9);
            mask = mask.Erode(2).Dilate(2);

            CircleF[] circles = mask.HoughCircles(new Gray(100), new Gray(50), 2, mask.Height / 4, 10, 400)[0];

            foreach (CircleF circle in circles)
            {
                if (txtBarcodeData.Text != "") txtBarcodeData.AppendText(Environment.NewLine);

                txtBarcodeData.AppendText(
                    "ball position = x" + circle.Center.X.ToString().PadLeft(4) +
                    ", y =" + circle.Center.Y.ToString().PadLeft(4) +
                    ", radius =" + circle.Radius.ToString("###.000").PadLeft(7));

                txtBarcodeData.ScrollToCaret();

                int x = (int)circle.Center.X;
                int y = (int)circle.Center.Y;

                // Draws a small green circle at the center point
                CvInvoke.Circle(original, new Point(x, y), 3, new MCvScalar(0, 255, 0), 1, LineType.EightConnected, 0);

                // Draw a bounding circle around the object
                original.Draw(circle, new Bgr(Color.Green), 2);

                // Draw text on the object
                original.Draw("Ball", new Point(x, y), FontFace.HersheyPlain, 1, new Bgr(Color.Red), 1, LineType.AntiAlias, false);

                sendSession.Send("MOUSE " + x.ToString() + "," + y.ToString());
                //sendSession.Send("LINE " + x.ToString() + "," + y.ToString()+ "," + x.ToString() + "," + y.ToString());
            }

            pictureBox2.Image = mask.ToBitmap();

            return original;

        }

        private Image<Bgr, byte> CascadeClassify(Image<Bgr, byte> image)
        {
            try
            {
                CascadeClassifier classifier = new CascadeClassifier("I:\\cascade.xml");

                var grayframe = image.Convert<Gray, byte>();

                var objs = classifier.DetectMultiScale(grayframe, 1.3, 5); //the actual detection happens here
                foreach (var obj in objs)
                {
                    image.Draw(obj, new Bgr(Color.BurlyWood), 3);

                }

                pictureBox2.Image = grayframe.ToBitmap();

                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return image;
        }

        public Image byteArrayToImage(byte[] byteArrayIn)
        {
            try
            {
                using (var ms = new MemoryStream(byteArrayIn))
                {
                    return Image.FromStream(ms);
                }
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = pictureBox1.Image;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == "Start")
            {
                server.Start();
                timer.Start();

                button2.Text = "Stop";
                ClearPictureBox(pictureBoxOverlay);


            }
            else
            {
                sendSession.Send("TERMINATE");

                server.Stop();
                timer.Stop();

                button2.Text = "Start";
                pictureBox1.Image = null;
                ClearPictureBox(pictureBoxOverlay);
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            sendSession.Send("TEXT 0,200," + txtText.Text);
            txtText.Text = "";
        }

        private void chkFlash_CheckedChanged(object sender, EventArgs e)
        {
            if (chkFlash.Checked)
                sendSession.Send("FLASH");
            else
                sendSession.Send("NOFLASH");
        }

        private Point? _Previous = null;

        private void button4_Click(object sender, EventArgs e)
        {
            sendSession.Send("CLEAR");
            ClearPictureBox(pictureBoxOverlay);
        }

        private void pictureBoxOverlay_Click(object sender, EventArgs e)
        {

        }

        private void pictureBoxOverlay_MouseDown(object sender, MouseEventArgs e)
        {
            _Previous = e.Location;
            pictureBoxOverlay_MouseMove(sender, e);
        }

        private void pictureBoxOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (_Previous != null)
            {
                if (pictureBoxOverlay.Image == null)
                {
                    ClearPictureBox(pictureBoxOverlay);
                }
                using (Graphics g = Graphics.FromImage(pictureBoxOverlay.Image))
                {
                    g.DrawLine(Pens.Green, _Previous.Value, e.Location);
                    sendSession.Send("LINE " + _Previous.Value.X + "," + _Previous.Value.Y + "," + e.Location.X + "," + e.Location.Y);
                }
                pictureBoxOverlay.Invalidate();
                _Previous = e.Location;
            }

            if (server.SessionCount > 0)
            {
                sendSession.Send("MOUSE " + e.Location.X + "," + e.Location.Y);
            }
            
        }

        void ClearPictureBox(PictureBox picturebox)
        {
            Bitmap bmp = new Bitmap(picturebox.Width, picturebox.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
            }
            pictureBoxOverlay.Image = bmp;
        }

        private void pictureBoxOverlay_MouseUp(object sender, MouseEventArgs e)
        {
            _Previous = null;
        }

        private void pictureBoxOverlay_MouseEnter(object sender, EventArgs e)
        {
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            txtBarcodeValue.Text = Spire.Barcode.BarcodeScanner.ScanOne((Bitmap)pictureBox2.Image);

            string json = GET("http://api.upcdatabase.org/json/52c04adc61d47db0c6a13be67231cfad/" + txtBarcodeValue.Text);
            UpcDatabaseResponse r = JsonConvert.DeserializeObject<UpcDatabaseResponse>(json);

            if (r.valid == "true")
            {
                txtBarcodeData.Text = "Item: " + r.itemname + Environment.NewLine;
                txtBarcodeData.Text += "Alias: " + r.alias + Environment.NewLine;
                txtBarcodeData.Text += "Description: " + r.description + Environment.NewLine;
                txtBarcodeData.Text += "Avg Price: " + r.avg_price + Environment.NewLine;
            }
            else
            {
                txtBarcodeData.Text = "Item not located in database. Got to www.upcdatabase.org to add it.";
            }
            

        }

        string GET(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();
                    // log errorText
                }
                throw;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
