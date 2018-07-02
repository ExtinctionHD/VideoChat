using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BDTP
{
    /// <summary>
    /// Предоставляет сетевые службы по протоколу BDTP (Babey Duplex Transmission Protocol)
    /// </summary>
    public class BdtpClient: IDisposable
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
        /// Возвращает количество линий для передачи и приема данных.
        /// </summary>
        public int LineCount { get; }

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

        private UdpClient[] udpSenders;
        private UdpClient[] udpReceivers;

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
        public int ReceiverPort { get; private set; } = 11011;

        /// <summary>
        /// Происходит при приеме подтверждения со стороны удаленного узла
        /// </summary>
        public event Action<byte[]> ReceiptReceived;

        /// <summary>
        /// Инициализирует новый экземпляр класса BdtpClient и связывает его с заданным локальным IP-адресом и указанным числом линий для приема и отправки данных.
        /// </summary>
        /// <param name="localIP">Объект IPAddress локального узла</param>
        /// <param name="lineCount">Число линий для приема и передачи данных</param>
        public BdtpClient(IPAddress localIP, int lineCount)
        {
            if (lineCount > 10)
            {
                throw new OverflowException("Too many lines");
            }

            LineCount = lineCount;
            LocalIP = localIP;

            InitializeClient();
        }

        private void InitializeClient()
        {
            tcpListener = new TcpListener(LocalIP, TcpPort);
            tcpController = new TcpClient();

            udpSenders = new UdpClient[LineCount];
            udpReceivers = new UdpClient[LineCount];

            for (int i = 0; i < LineCount; i++)
            {
                udpSenders[i] = new UdpClient(new IPEndPoint(LocalIP, SenderPort + i));
                udpReceivers[i] = new UdpClient(new IPEndPoint(LocalIP, ReceiverPort + i));
            }
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
        /// Отправляет байты данных по протоколу UDP, узлу, с которым установлено соединение по линии с заданным индексом.
        /// </summary>
        /// <param name="data">Массив объектов типа byte, содержащий данные для отправки.</param>
        /// <param name="index">Индекс линии по которой необходимо отправить данные.</param>
        /// <returns>Число успешно отправленных байтов</returns>
        public virtual int Send(byte[] data, int index)
        {
            if (!Connected)
            {
                return 0;
            }

            IPEndPoint remoteEP = new IPEndPoint(RemoteIP, ReceiverPort + index);
            return udpSenders[index].Send(data, data.Length, remoteEP);
        }

        /// <summary>
        /// Возвращает данные, которые были отправлены со связанного узла по указанной линии по протоколу UDP.
        /// </summary>
        /// <param name="index">Индекс линии с которой необходимо принять данные.</param>
        /// <returns>Массив объектов типа byte содержащий полученные данные.</returns>
        public virtual byte[] Receive(int index)
        {
            if (!Connected)
            {
                return Array.Empty<byte>();
            }

            IPEndPoint senderEP = null;
            byte[] bytes = null;
            try
            {
                bytes = udpReceivers[index].Receive(ref senderEP);
            }
            catch
            {
                bytes = Array.Empty<byte>();
            }

            if (senderEP?.Address.Equals(RemoteIP) != true)
            {
                bytes = Array.Empty<byte>();
            }

            return bytes;
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

            try
            {
                NetworkStream stream = tcpController.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch { return false; }

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

        private void WaitReceipt()
        {
            do
            {
                byte[] buffer = ReceiveReceipt();
                ReceiptReceived(buffer);
            }
            while (Connected);
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

            for (int i = 0; i < LineCount; i++)
            {
                udpReceivers[i].Close();
                udpReceivers[i] = new UdpClient(new IPEndPoint(LocalIP, ReceiverPort + i));
            }
        }

        /// <summary>
        /// Освобождает все управляемые и неуправляемые ресурсы, используемые BdtpClient.
        /// </summary>
        public void Dispose()
        {
            RemoteIP = null;

            tcpController.Dispose();
            
            for (int i = 0; i < LineCount; i++)
            {
                udpReceivers[i].Dispose();
                udpSenders[i].Dispose();
            }

            tcpListener.Stop();
        }
    }
}
