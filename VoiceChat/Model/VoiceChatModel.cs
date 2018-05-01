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
    public class VoiceChatModel: INotifyPropertyChanged
    {
        // Подтверждения
        private enum Receipts
        {
            Accept
        }

        // Состояния модели
        public enum States
        {
            WaitCall,
            OutcomingCall,
            IncomingCall,
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

        // Текущее состояние
        public States State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                OnPropertyChanged("State");
            }
        }
        private States state;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
        
        public VoiceChatModel()
        {
            bdtpClient = new NotifyBdtpClient(GetLocalIP());
            InitializeAudio();

            StartWaitCall();
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

        // Исходящий вызов
        public async void BeginCall()
        {
            State = States.OutcomingCall;

            await Task.Run(() =>
            {
                EndWaitCall();

                // Подключение о ожидание ответа
                if (bdtpClient.Connect(remoteIP) && WaitAccept())
                {
                    StartSendVoice();
                    StartReceiveVoice();
                }
                else
                {
                    StartWaitCall();
                }
            });
        }
        private bool WaitAccept()
        {
            try
            {
                while (bdtpClient.ReceiveReceipt() != new byte[] { (byte)Receipts.Accept }) ;
            }
            catch { return false; }

            State = States.Talk;
            return true;
        }
        public void EndCall()
        {
            EndSendVoice();
            EndReceiveVoice();

            bdtpClient.Disconnect();

            StartWaitCall();
        }

        // Входящий вызов
        public void AcceptCall()
        {
            if (bdtpClient.SendReceipt(new byte[] { (byte)Receipts.Accept }))
            {
                State = States.Talk;
            }
        }
        public void DeclineCall()
        {
            bdtpClient.Disconnect();

            StartWaitCall();
        }

        // Ожидания входящего вызова
        private void StartWaitCall()
        {
            State = States.WaitCall;

            waitCall?.Abort();
            waitCall = new Thread(WaitCall);
            waitCall.Start();
        }
        private void EndWaitCall()
        {
            bdtpClient.StopAccept();
            waitCall?.Abort();
        }
        private void WaitCall()
        {
            if (bdtpClient.Accept())
            {
                State = States.IncomingCall;
            }
        }

        // Передачи звука
        private void StartSendVoice()
        {
            input.DataAvailable += SendVoice;
            input.StartRecording();
        }
        private void EndSendVoice()
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

        // Приема звука
        private void StartReceiveVoice()
        {
            output.Play();
            receiveVoice = new Thread(ReceiveVoice);
            receiveVoice.Start();
        }
        private void EndReceiveVoice()
        {
            receiveVoice?.Abort();
            output.Stop();
        }
        private void ReceiveVoice()
        {
            while(bdtpClient.Connected)
            {
                byte[] data = bdtpClient.Receive();
                bufferStream.AddSamples(data, 0, data.Length);
                Thread.Sleep(0);
            }
        }

        // Закрытие модели
        public void Closing()
        {
            bdtpClient.StopAccept();
        }
    }
}
