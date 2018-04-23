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
    /// <summary>
    /// Предоставляет сетевые службы по протоколу BDTP (Babey Duplex Transmission Protocol)
    /// </summary>
    class BdtpClient : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Возвращает значение указывающие установлено ли соединение.
        /// </summary>
        public bool Connected
        {
            get
            {
                return RemoteIP != null;
            }
        }

        /// <summary>
        /// Возвращает IP-адрес с которым установлено соединение.
        /// </summary>
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

                PropertyChanged(this, new PropertyChangedEventArgs("Connected"));
            }
        }
        private IPAddress remoteIP;

        private TcpListener tcpListener;
        private TcpClient tcpController;

        private UdpClient udpSender;
        private UdpClient udpReceiver;
        
        /// <summary>
        /// Возвращает или задает номер управляющего порта.
        /// </summary>
        public int TcpPort { get; set; } = 11000;

        /// <summary>
        /// Возвращает или задает номер отправляющиего порта.
        /// </summary>
        public int SenderPort { get; set; } = 11001;

        /// <summary>
        /// Возвращает или задает номер принимающего порта.
        /// </summary>
        public int ReceiverPort { get; set; } = 11002;
        
        /// <summary>
        /// Возвращает или задает локальный IP-адрес.
        /// </summary>
        public IPAddress LocalIP { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса BdtpClient и связывает его с заданным локальным IP-адресом.
        /// </summary>
        /// <param name="localIP">Объект IPAddress локального узла</param>
        public BdtpClient(IPAddress localIP)
        {
            LocalIP = localIP;

            tcpListener = new TcpListener(LocalIP, TcpPort);
            tcpController = new TcpClient();
            udpSender = new UdpClient(new IPEndPoint(LocalIP, SenderPort));
            udpReceiver = new UdpClient(new IPEndPoint(LocalIP, ReceiverPort));
        }

        /// <summary>
        /// Подключает клиента к удаленному BDTP-узлу, используя указанный IP-адрес.
        /// </summary>
        /// <param name="remoteIP">Объект IPAddress узла, к которому выполняется подключение.</param>
        /// <returns>true Если удалось установить соединение; в противном случае — false.</returns>
        public bool Connect(IPAddress remoteIP)
        {
            if (Connected)
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
        
        /// <summary>
        /// Принимает ожидающий запрос на подключение.
        /// </summary>
        /// <returns>Объект IPAddress узла, с которого выполнено подключение.</returns>
        public IPAddress Accept()
        {
            while (Connected) { }

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

        /// <summary>
        /// Отправляет байты данных узлу, с которым установлено соединение.
        /// </summary>
        /// <param name="data">Массив объектов типа byte, содержащий данные для отправки.</param>
        /// <returns>Число успешно отправленных байтов</returns>
        public int Send(byte[] data)
        {
            if (!Connected)
            {
                return 0;
            }

            IPEndPoint remoteEP = new IPEndPoint(RemoteIP, ReceiverPort);
            return udpSender.Send(data, data.Length, remoteEP);
        }

        /// <summary>
        /// Возвращает данные, которые были отправлены со связанного удаленного узла.
        /// </summary>
        /// <returns>Массив объектов типа byte содержащий полученные данные.</returns>
        public byte[] Receive()
        {
            if (!Connected)
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

        /// <summary>
        /// Закрывает подключение с текущим удаленным узлом и позволяет повторно установить соединение.
        /// </summary>
        public void Disconnect()
        {
            RemoteIP = null;

            tcpListener.Stop();
            tcpController.Close();

            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Disconnect(true);

            udpReceiver.Close();
            udpReceiver = new UdpClient(new IPEndPoint(LocalIP, ReceiverPort));
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
            while (count != 0 && Connected);

            Disconnect();
        }
    }
}
