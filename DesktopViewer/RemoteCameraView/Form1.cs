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
        private Connection connection = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            connection = new Connection();
            connection.Listen();

            Timer t = new Timer();
            t.Interval = 10;
            t.Tick += T_Tick;
            t.Start();


        }

        private void T_Tick(object sender, EventArgs e)
        {
            if (connection.imageDataQueue.Count> 0)
            {
                byte[] arr = connection.imageDataQueue.Dequeue();
                Image i = byteArrayToImage(arr);

                pictureBox1.Image = i;

                Console.WriteLine(System.DateTime.Now.Ticks);
            }
        }

        public Image byteArrayToImage(byte[] byteArrayIn)
        {
            using (var ms = new MemoryStream(byteArrayIn))
            {
                return Image.FromStream(ms);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }
    }
}
