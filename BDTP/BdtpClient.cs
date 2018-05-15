using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BDTP
{
    /// <summary>
    /// Предоставляет сетевые службы по протоколу BDTP (Babey Duplex Transmission Protocol)
    /// </summary>
    public class BdtpClient
    {
        /// <summary>
        /// Представляет размер буфера для подтверждений
        /// </summary>
        public const int BUFFER_SIZE = 1024;

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
        public virtual IPAddress RemoteIP
        {
            get
            {
                return remoteIP;
            }
            protected set
            {
                remoteIP = value;

                if (value != null)
                {
                    tcpListener?.Stop();
                    Thread thread = new Thread(new ThreadStart(WaitReceipt));
                    thread.Start();
                }
            }
        }
        private IPAddress remoteIP;

        /// <summary>
        /// Возвращает связанный локальный IP-адрес.
        /// </summary>
        public IPAddress LocalIP { get; private set; }

        private TcpListener tcpListener;
        private TcpClient tcpController;

        private UdpClient udpSender;
        private UdpClient udpReceiver;

        /// <summary>
        /// Возвращает номер управляющего порта.
        /// </summary>
        public int TcpPort { get; private set; } = 11000;

        /// <summary>
        /// Возвращает номер отправляющиего порта.
        /// </summary>
        public int SenderPort { get; private set; } = 11001;

        /// <summary>
        /// Возвращает номер принимающего порта.
        /// </summary>
        public int ReceiverPort { get; private set; } = 11002;

        /// <summary>
        /// Происходит при приеме подтверждения со стороны удаленного узла
        /// </summary>
        public event Action<byte[]> ReceiptReceived;

        /// <summary>
        /// Инициализирует новый экземпляр класса BdtpClient и связывает его с заданным локальным IP-адресом.
        /// </summary>
        /// <param name="localIP">Объект IPAddress локального узла</param>
        public BdtpClient(IPAddress localIP)
        {
            LocalIP = localIP;

            InitializeClient();
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса BdtpClient с заданными портами и связывает его с заданным локальным IP-адресом.
        /// </summary>
        /// <param name="localIP">Объект IPAddress локального узла.</param>
        /// <param name="tcpPort">Номер управляющего порта.</param>
        /// <param name="receiverPort">Номер порта для принятия данных.</param>
        /// <param name="senderPort">Номер порта для отправления данных.</param>
        public BdtpClient(IPAddress localIP, int tcpPort, int receiverPort, int senderPort)
        {
            LocalIP = localIP;

            TcpPort = tcpPort;
            ReceiverPort = receiverPort;
            SenderPort = senderPort;

            InitializeClient();
        }

        private void InitializeClient()
        {
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
        public virtual bool Connect(IPAddress remoteIP)
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

            SendReceipt(LocalIP.GetAddressBytes());

            RemoteIP = remoteIP;
            return true;
        }

        /// <summary>
        /// Принимает ожидающий запрос на подключение.
        /// </summary>
        /// <returns>true Если удалось принять соединение; в противном случае — false.</returns>
        public virtual bool Accept()
        {
            while (Connected) { }
            
            try
            {
                tcpListener.Start();
                tcpController = tcpListener.AcceptTcpClient();
            }
            catch { return false; }

            RemoteIP = new IPAddress(ReceiveReceipt());

            return true;
        }

        /// <summary>
        /// Прекращает ожидание входящего запроса на подключение
        /// </summary>
        public virtual void StopAccept()
        {
            tcpListener.Stop();
        }

        /// <summary>
        /// Отправляет байты данных по протоколу UDP, узлу, с которым установлено соединение.
        /// </summary>
        /// <param name="data">Массив объектов типа byte, содержащий данные для отправки.</param>
        /// <returns>Число успешно отправленных байтов</returns>
        public virtual int Send(byte[] data)
        {
            if (!Connected)
            {
                return 0;
            }

            IPEndPoint remoteEP = new IPEndPoint(RemoteIP, ReceiverPort);
            return udpSender.Send(data, data.Length, remoteEP);
        }

        /// <summary>
        /// Возвращает данные, которые были отправлены со связанного узла по протоколу UDP.
        /// </summary>
        /// <returns>Массив объектов типа byte содержащий полученные данные.</returns>
        public virtual byte[] Receive()
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
        /// Отправляет подтверждение по протоколу TCP, узлу, с которым установлено соединение.
        /// </summary>
        /// <param name="data">Массив объектов типа byte, содержащий данные для отправки.</param>
        /// <returns>true Если удалось отправить данные; в противном случае — false.</returns>
        public virtual bool SendReceipt(byte[] data)
        {
            if (data.Length > BUFFER_SIZE)
            {
                return false;
            }

            NetworkStream stream = tcpController.GetStream();
            stream.Write(data, 0, data.Length);

            return true;
        }
        
        private byte[] ReceiveReceipt()
        {
            NetworkStream stream = tcpController.GetStream();
            byte[] buffer = new byte[BUFFER_SIZE];
            int count;

            try
            {
                count = stream.Read(buffer, 0, BUFFER_SIZE);
            }
            catch
            {
                return Array.Empty<byte>();
            }

            byte[] result = new byte[count];
            Array.Copy(buffer, result, count);

            return result;
        }

        /// <summary>
        /// Закрывает подключение с текущим удаленным узлом и позволяет повторно установить соединение.
        /// </summary>
        public virtual void Disconnect()
        {
            if (!Connected)
            {
                return;
            }

            RemoteIP = null;

            tcpListener.Stop();
            tcpController.Close();

            udpReceiver.Close();
            udpReceiver = new UdpClient(new IPEndPoint(LocalIP, ReceiverPort));
        }
        
        private void WaitReceipt()
        {
            int count = 0;
            do
            {
                byte[] buffer = ReceiveReceipt();
                count = buffer.Length;

                ReceiptReceived(buffer);
            }
            while (Connected);
        }
    }
}
