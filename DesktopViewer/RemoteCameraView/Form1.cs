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

namespace RemoteCameraView
{

    public partial class Form1 : Form
    {

        private Timer timer = new Timer();
        private Queue<byte[]> imageDataQueue = new Queue<byte[]>();
        private AppServer server = new AppServer();
        AppSession sendSession = null;

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

                pictureBox1.Image = i;
            }
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
            sendSession.Send("TEXT " + txtText.Text);
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
    }
}
