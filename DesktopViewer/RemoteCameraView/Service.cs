using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

public class Service
{
    private TcpListener listener = new TcpListener(4680);
    private TcpClient client = null;
    private NetworkStream netStream = null;

    public Queue<byte[]> imageDataQueue = new Queue<byte[]>();

    public bool isConnected = false;
    private bool isRunning = true;

    public void Start()
    {
        Thread thread = new Thread(new ThreadStart(HandleConnections));
        thread.Start();
    }


    public void Terminate()
    {
        isRunning = false;
    }


    private void HandleConnections()
    {
        listener.Start();

        while (isRunning)
        {
            if (listener.Pending())
            {
                client = listener.AcceptTcpClient();
                netStream = client.GetStream();
                isConnected = true;

                byte[] buffer = new byte[4096];
                using (MemoryStream ms = new MemoryStream())
                {
                    int numBytesRead;
                    while ((numBytesRead = netStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, numBytesRead);
                    }

                    byte[] data = ms.ToArray();
                    imageDataQueue.Enqueue(data);
                }

                client.Close();
                client = null;

                Console.WriteLine(imageDataQueue.Count);
            }
        }

        if (client != null && client.Connected)
            client.Close();

        client = null;
        isConnected = false;

        listener.Stop();
    }

    public void SendImage(System.Drawing.Bitmap data)
    {
        bool t1 = client.Client.Poll(1, SelectMode.SelectRead);
        bool t2 = (client.Client.Available == 0);

        if (t1 && t2)
        {
            Start();
        }

        if (isConnected)
        {
            BinaryWriter writer = new BinaryWriter(netStream);

            // convert (downsize) to jpg
            MemoryStream ms = new MemoryStream();
            data.Save(ms, ImageFormat.Jpeg);
            Image img = Image.FromStream(ms);


            IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(netStream, img);

            ms.Close();
            netStream.Flush();

        }
    }
}