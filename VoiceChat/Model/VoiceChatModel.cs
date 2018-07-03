using BDTP;
using NAudio.Wave;
using System;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VoiceChat.Model
{
    // Подтверждения
    public enum Flags
    {
        Accept,
        BeginVideoSend,
        EndVideoSend
    }

    // Состояния модели
    public enum ModelStates
    {
        WaitCall,
        OutgoingCall,
        IncomingCall,
        Talk,
        Close
    }

    public class VoiceChatModel : INotifyPropertyChanged
    {
        private const int LINES_COUNT = 2;

        public AudioSharing audio;
        public VideoSharing video;

        public BdtpClient bdtpClient;
        private Thread waitCall;

        public bool Connected
        {
            get
            {
                return bdtpClient.Connected;
            }
        }

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
        public ModelStates State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                OnPropertyChanged("State");

                mediaSounds.ControlSounds();
            }
        }
        private ModelStates state;

        public CallTimer callTimer;

        private MediaSounds mediaSounds;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
        
        public VoiceChatModel()
        {
            bdtpClient = new BdtpClient(GetLocalIP(), LINES_COUNT);

            audio = new AudioSharing(this);
            video = new VideoSharing(this);
            mediaSounds = new MediaSounds(this);
            callTimer = new CallTimer();

            InitializeEvents();

            BeginWaitCall();
        }

        // Инициализация
        private void InitializeEvents()
        {
            bdtpClient.ReceiptReceived += ReceiveAccept;
            bdtpClient.ReceiptReceived += ReceiveDisconnect;
            bdtpClient.ReceiptReceived += video.ReceiveFlags;
        }

        private IPAddress GetLocalIP()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            return addresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Last();
        }

        // Обработчики приема подтверждений
        public static bool IsFlag(Flags flag, byte[] buffer)
        {
            return buffer.Length == 1 && buffer[0] == (byte)flag;
        }
        private void ReceiveAccept(byte[] buffer)
        {
            if (IsFlag(Flags.Accept, buffer))
            {
                State = ModelStates.Talk;
            }
        }
        private void ReceiveDisconnect(byte[] buffer)
        {
            if (buffer.Length == 0)
            {
                EndCall();
            }
        }

        // Исходящий вызов
        public void BeginCall()
        {
            State = ModelStates.OutgoingCall;
            EndWaitCall();

            // Подключение и ожидание ответа
            if (bdtpClient.Connect(remoteIP) && WaitAccept())
            {
                BeginTalk();
            }
            else
            {
                EndCall();
            }
        }
        public void EndCall()
        {
            if (State == ModelStates.Talk)
            {
                EndTalk();
            }

            bdtpClient.Disconnect();

            BeginWaitCall();
        }
        private bool WaitAccept()
        {
            while (bdtpClient.Connected && State != ModelStates.Talk) ;

            if (State == ModelStates.Talk)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Входящий вызов
        public void AcceptCall()
        {
            if (bdtpClient.SendReceipt(new byte[] { (byte)Flags.Accept }))
            {
                BeginTalk();
            }
        }
        public void DeclineCall()
        {
            bdtpClient.Disconnect();

            BeginWaitCall();
        }

        // Ожидание входящего вызова
        private void BeginWaitCall()
        {
            if (State == ModelStates.Close)
            {
                return;
            }

            Thread.Sleep(100);

            waitCall = new Thread(WaitCall);
            try
            {
                waitCall.Start();
            }
            catch { }
        }
        private void EndWaitCall()
        {
            bdtpClient.StopAccept();
            waitCall.Interrupt();
            waitCall.Abort();
            waitCall.Join();
        }
        private void WaitCall()
        {
            State = ModelStates.WaitCall;

            if (bdtpClient.Accept())
            {
                RemoteIP = bdtpClient.RemoteIP;
                
                State = ModelStates.IncomingCall;
            }

            EndWaitCall();
        }

        // Разговор
        private void BeginTalk()
        {
            State = ModelStates.Talk;
            
            callTimer.Start();
            
            audio.BeginSend();
            audio.BeginReceive();

            video.BeginReceive();
        }
        private void EndTalk()
        {
            audio.EndSend();
            audio.EndReceive();

            video.EndSend();
            video.EndReceive();
            video.ClearFrames();

            callTimer.Stop();
        }

        // Закрытие модели
        public void Closing()
        {
            if (State == ModelStates.Talk)
            {
                EndTalk();
            }
            State = ModelStates.Close;

            EndCall();
            bdtpClient.Dispose();
        }
    }
}
