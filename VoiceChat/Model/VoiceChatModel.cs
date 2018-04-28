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

namespace VoiceChat.Model
{
    /// <summary>
    /// Представляет данные и логику работы приложения
    /// </summary>
    public class VoiceChatModel: INotifyPropertyChanged
    {
        private enum Receipts
        {
            Accept
        }
        public enum States
        {
            Wait,
            IncomingCall,
            OutcomingCall,
            Talk
        }

        private NotifyBdtpClient bdtpClient;
        private Thread waitCall;
        private Thread receiveVoice;
        
        private WaveIn input;                       
        private WaveOut output;                     
        private BufferedWaveProvider bufferStream;  

        public IPAddress RemoteIP
        {
            get
            {
                return remoteIP;
            }
            set
            {
                remoteIP = value;
                OnPropertyChanged("RemoteIP");
            }
        }
        private IPAddress remoteIP;

        public IPAddress LocalIP
        {
            get
            {
                return bdtpClient.LocalIP;
            }
        }

        public States State
        {
            get
            {
                return state;
            }
            private set
            {
                state = value;
                if (state == States.Talk)
                {
                    StartSendVoice();
                    output.Play();
                    receiveVoice = new Thread(ReceiveVoice);
                    receiveVoice.Start();
                }
                else
                {
                    EndSendVoice();
                    output.Stop();
                }
            }
        }
        private States state;

        /// <summary>
        /// Событие возникающее при изменении свойства объекта.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса VoiceChatModel
        /// </summary>
        public VoiceChatModel()
        {
            bdtpClient = new NotifyBdtpClient(GetLocalIP());

            InitializeAudio();

            waitCall = new Thread(WaitCall);
            waitCall.Start();
        }

        private void InitializeAudio()
        {
            // Cоздаем поток для записи нашей речи определяем его формат - 
            // частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
            input = new WaveIn();
            input.WaveFormat = new WaveFormat(8000, 16, 1);

            // Создание потока для прослушивания входящиего звука
            output = new WaveOut();
            bufferStream = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            output.Init(bufferStream);
        }

        private IPAddress GetLocalIP()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            return addresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Last();
        }

        public void BeginCall()
        {
            State = States.IncomingCall;
            bdtpClient.Connect(remoteIP);
            WaitAccept();
        }

        private void WaitAccept()
        {
            while (bdtpClient.ReceiveReceipt() != new byte[] { (byte)Receipts.Accept }) ;
            State = States.Talk;
        }

        public void EndCall()
        {
            bdtpClient.Disconnect();
            State = States.Wait;
        }

        public void AcceptCall()
        {
            bdtpClient.SendReceipt(new byte[] { (byte)Receipts.Accept });
            State = States.Talk;
        }

        public void DeclineCall()
        {
            bdtpClient.Disconnect();
            State = States.Wait;
        }

        public void WaitCall()
        {
            if (bdtpClient.Accept())
            {
                State = States.IncomingCall;
            }
        }

        public void StartSendVoice()
        {
            input.DataAvailable += SendVoice;
            input.StartRecording();
        }

        public void EndSendVoice()
        {
            input.StopRecording();
            input.DataAvailable -= SendVoice;
        }

        private void SendVoice(object sender, WaveInEventArgs e)
        {
            if (State != States.Talk)
            {
                return;
            }

            bdtpClient.Send(e.Buffer);
        }

        public void ReceiveVoice()
        {
            while(State == States.Talk)
            {
                byte[] data = bdtpClient.Receive();
                bufferStream.AddSamples(data, 0, data.Length);
                Thread.Sleep(0);
            }
        }

        public void Closing()
        {
            EndCall();
            waitCall?.Abort();
            receiveVoice?.Abort();
        }
    }
}
