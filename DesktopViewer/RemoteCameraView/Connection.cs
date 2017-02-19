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

public class Connection
{
    TcpListener listener = new TcpListener(4680);
    TcpClient client = null;
    public bool isWaiting = false;
    public bool isConnected = false;
    private bool halt = false;
    public Queue<byte[]> imageDataQueue = new Queue<byte[]>();

    NetworkStream netStream = null;

UdpClient udpSock = new UdpClient(4680);

    public void Listen()
    {
        Thread thread = new Thread(new ThreadStart(WaitForConnection));
        thread.Start();
    }


    public void Terminate()
    {
        halt = true;
    }

    private void WaitForUdpConnection()
    {
        
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = new byte[1024];

        udpSock.BeginReceive(new AsyncCallback(recv), null);
        
    }

    private void recv(IAsyncResult res)
    {
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 4680);
        byte[] data = udpSock.EndReceive(res, ref RemoteIpEndPoint);

        imageDataQueue.Enqueue(data);

        udpSock.BeginReceive(new AsyncCallback(recv), null);
    }

    private void WaitForConnection()
    {
        listener.Start();

        while (!halt)
        {
            if (listener.Pending())
            {
                client = listener.AcceptTcpClient();
                netStream = client.GetStream();
                isConnected = true;

                byte[] buffer = new byte[1024];
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
            }
        }

        if (client.Connected)
            client.Close();

        client = null;
        isConnected = false;
    }

    public void SendImage(System.Drawing.Bitmap data)
    {
        bool t1 = client.Client.Poll(1, SelectMode.SelectRead);
        bool t2 = (client.Client.Available == 0);

        if (t1 && t2)
        {
            Listen();
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

    public byte[] ReceiveData()
    {
        byte[] data = null;
        bool t1 = client.Client.Poll(1, SelectMode.SelectRead);
        bool t2 = (client.Client.Available > 0);

        if (t1 && t2 && client.Connected)
        {
            byte[] buffer = new byte[1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int numBytesRead;
                while ((numBytesRead = netStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, numBytesRead);
                }
                
                data = ms.ToArray();
            }
        }


        return data;
    }




}
