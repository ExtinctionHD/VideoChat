using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.ComponentModel;

namespace VoiceChat.Classes
{
    class BdtpClient: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsConnected
        {
            get
            {
                return RemoteIP != null;
            }
        }

        public IPAddress RemoteIP
        {
            get
            {
                return remoteIP;
            }
            private set
            {
                remoteIP = value;

                if (value != null)
                {
                    tcpListener?.Stop();
                    Thread thread = new Thread(new ThreadStart(WaitForDisconnect));
                    thread.Start();
                }

                RaisePropertyChanged("IsConnected");
            }
        }
        private IPAddress remoteIP;

        private TcpListener tcpListener;
        private TcpClient tcpController;

        private UdpClient udpSender;
        private UdpClient udpReceiver;

        public int TcpPort { get; set; } = 11000;
        public int SenderPort { get; set; } = 11001;
        public int receiverPort { get; set; } = 11002;

        public IPAddress LocalIP { get; set; }

        public BdtpClient(IPAddress localIP)
        {
            LocalIP = localIP;

            tcpListener = new TcpListener(LocalIP, TcpPort);
                tcpController = new TcpClient();
            udpSender = new UdpClient(new IPEndPoint(LocalIP, SenderPort));
            udpReceiver = new UdpClient(new IPEndPoint(LocalIP, receiverPort));
        }

        public bool Connect(IPAddress remoteIP)
        {
            if (IsConnected)
            {
                return false;
            }

            try
            {
                tcpController = new TcpClient();
                tcpController.Connect(remoteIP, TcpPort);
            }
            catch { return false; }

            NetworkStream stream = tcpController.GetStream();
            byte[] bytes = LocalIP.GetAddressBytes();
            stream.Write(bytes, 0, bytes.Length);

            RemoteIP = remoteIP;
            return true;
        }

        public IPAddress Listen()
        {
            while (IsConnected) { }

            tcpListener.Start();

            try
            {
                tcpController = tcpListener.AcceptTcpClient();
            }
            catch { return null; }

            RemoteIP = GetIPAddress(tcpController.GetStream());
            return RemoteIP;
        }

        private IPAddress GetIPAddress(NetworkStream stream)
        {
            byte[] buffer = new byte[256];
            int count = 0;

            do
            {
                count += stream.Read(buffer, count, buffer.Length);
            }
            while (stream.DataAvailable);

            byte[] bytes = new byte[count];
            Array.Copy(buffer, bytes, count);

            return new IPAddress(bytes);
        }

        public int Send(byte[] data)
        {
            if (!IsConnected)
            {
                return 0;
            }

            IPEndPoint remoteEP = new IPEndPoint(RemoteIP, receiverPort);
            return udpSender.Send(data, data.Length, remoteEP);
        }

        public byte[] Receive()
        {
            if (!IsConnected)
            {
                return Array.Empty<byte>();
            }

            try
            {
                IPEndPoint senderEP = null;
                return udpReceiver.Receive(ref senderEP);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        public void Disconnect()
        {
            DisconnectBase();
        }

        public void Disconnect(Action callback)
        {
            DisconnectBase();
            callback.Invoke();
        }

        private void DisconnectBase()
        {
            tcpListener.Stop();
            tcpController.Close();

            udpReceiver.Close();
            udpReceiver = new UdpClient(new IPEndPoint(LocalIP, receiverPort));

            RemoteIP = null;
        }

        private void WaitForDisconnect()
        {
            NetworkStream stream = tcpController.GetStream();

            int count = 0;
            do
            {
                byte[] buffer = new byte[256];
                try
                {
                    count = stream.Read(buffer, 0, buffer.Length);
                }
                catch { }
            }
            while (count != 0 && IsConnected);

            Disconnect();
        }
    }
}
