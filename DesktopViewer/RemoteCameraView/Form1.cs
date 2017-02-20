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

namespace RemoteCameraView
{
    public partial class Form1 : Form
    {
        private Service service = null;
        private Timer timer = new Timer();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer.Interval = 10;
            timer.Tick += T_Tick;
        }

        private void T_Tick(object sender, EventArgs e)
        {
            if (service.imageDataQueue.Count> 0)
            {
                byte[] arr = service.imageDataQueue.Dequeue();
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
                service = new Service();
                service.Start();

                timer.Start();

                button2.Text = "Stop";
            }
            else
            {
                service.Terminate();
                service = null;

                timer.Stop();
                
                button2.Text = "Start";

                pictureBox1.Image = null;   
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
