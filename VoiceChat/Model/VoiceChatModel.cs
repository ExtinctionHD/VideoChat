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
    // Voice Chat - это не только входящие но и исходящие вызовы
    // Voice Chat - это общение
    // Voice Chat - это рост
    // Voice Chat - это свобода
    // VOICE CHAAAAAT!!
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
            OutgoingCall,
            IncomingCall,
            Talk,
            Close
        }

        private enum Lines
        {
            Audio,
            Video
        }

        private const int LINES_COUNT = 2;

        private BdtpClient bdtpClient;
        private Thread waitCall;
        private Thread receiveVoice;
        private Thread receiveVideo;
        
        private WaveIn input;                       
        private WaveOut output;                     
        private BufferedWaveProvider bufferStream;

        private VideoCaptureDevice videoDevice;
        public ImageSource VideoFrame { get; set; }

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

                ControlMedia(ringtone, States.IncomingCall);
                ControlMedia(dialtone, States.OutgoingCall);
            }
        }
        private States state;

        // Плееры звуков
        private MediaPlayer ringtone;
        private MediaPlayer dialtone;

        // Время звонка
        public TimeSpan CallTime
        {
            get
            {
                return callTime;
            }
            set
            {
                callTime = value;
                OnPropertyChanged("CallTime");
            }
        }
        private TimeSpan callTime;
        private DispatcherTimer callTimer;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
        
        public VoiceChatModel()
        {
            bdtpClient = new BdtpClient(GetLocalIP(), LINES_COUNT);

            InitializeEvents();
            InitializeAudio();
            InitializeVideo();
            InitializeMedia();
            InitializeTimers();

            BeginWaitCall();
        }

        // Инициализация
        private void InitializeEvents()
        {
            bdtpClient.ReceiptReceived += ReceiveAccept;
            bdtpClient.ReceiptReceived += ReceiveDisconnect;
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
        private void InitializeVideo()
        {
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count != 0)
            {
                videoDevice = new VideoCaptureDevice(videoDevices[0].MonikerString);
            }
        }
        private void InitializeMedia()
        {
            LoadMedia(ref ringtone, "Source/Ringtone.mp3");

            LoadMedia(ref dialtone, "Source/Dialtone.mp3");
            dialtone.Volume = 0.1;
        }
        private void InitializeTimers()
        {
            callTimer = new DispatcherTimer();
            callTimer.Interval = TimeSpan.FromSeconds(1);
            callTimer.Tick += (sender, e) => CallTime += callTimer.Interval;
        }

        // Работа со свуками
        private void LoadMedia(ref MediaPlayer media, string path)
        {
            media = new MediaPlayer();
            media.Open(new Uri(path, UriKind.Relative));
            media.MediaEnded += Media_Restart;
        }
        private void Media_Restart(object sender, EventArgs e)
        {
            MediaPlayer media = sender as MediaPlayer;
            media.Stop();
            media.Play();
        }
        private void ControlMedia(MediaPlayer media, States state)
        {
            if (media == null)
            {
                return;
            }
            try
            {
                if (State == state)
                {
                    media.Dispatcher.Invoke(() => media.Play());
                }
                else
                {
                    media.Dispatcher.Invoke(() => media.Stop());
                }
            }
            catch { }
        }

        private IPAddress GetLocalIP()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            return addresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Last();
        }

        // Обработчики приема подтверждений
        private void ReceiveAccept(byte[] buffer)
        {
            if (buffer.Length == 1 && buffer[0] == (byte)Receipts.Accept)
            {
                State = States.Talk;
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
            State = States.OutgoingCall;
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
        private bool WaitAccept()
        {
            while (bdtpClient.Connected && State != States.Talk) ;
            
            if (State == States.Talk)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void EndCall()
        {
            if (State == States.Talk)
            {
                EndTalk();
            }

            bdtpClient.Disconnect();

            BeginWaitCall();
        }

        // Входящий вызов
        public void AcceptCall()
        {
            if (bdtpClient.SendReceipt(new byte[] { (byte)Receipts.Accept }))
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
            if (State == States.Close)
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
            State = States.WaitCall;

            if (bdtpClient.Accept())
            {
                RemoteIP = bdtpClient.RemoteIP;
                
                State = States.IncomingCall;
            }

            EndWaitCall();
        }

        // Разговор
        private void BeginTalk()
        {
            State = States.Talk;

            callTime = new TimeSpan(0);
            callTimer.Start();

            // Передача звука
            input.DataAvailable += SendVoice;
            input.StartRecording();

            // Принятие звука
            output.Play();
            receiveVoice = new Thread(ReceiveVoice);
            receiveVoice.Start();

            // Передача видео
            if (videoDevice != null)
            {
                videoDevice.NewFrame += SendVideo;
                videoDevice.Start();
            }
            
            // Принятие видео
            receiveVideo = new Thread(ReceiveVideo);
            receiveVideo.Start();
        }
        private void EndTalk()
        {
            // Завершение передачи звука
            input.StopRecording();
            input.DataAvailable -= SendVoice;

            // Завершение принятия звука
            receiveVoice?.Abort();
            output.Stop();
            
            // Завершение передачи видео
            if (videoDevice != null)
            {
                videoDevice.NewFrame -= SendVideo;
                videoDevice.SignalToStop();
            }

            // Завершение принятия видео
            receiveVideo?.Abort();
            VideoFrame = null;

            callTimer.Stop();
            callTime = new TimeSpan(0);
        }

        // Передача и прием звука
        private void SendVoice(object sender, WaveInEventArgs e)
        {
            if (State != States.Talk)
            {
                return;
            }

            bdtpClient.Send(e.Buffer, (int)Lines.Audio);
        }
        private void ReceiveVoice()
        {
            while(bdtpClient.Connected && State != States.Close)
            {
                byte[] data = bdtpClient.Receive((int)Lines.Audio);
                bufferStream.AddSamples(data, 0, data.Length);
                Thread.Sleep(0);
            }
        }

        // Передача и прием видео
        private void SendVideo(object sender, NewFrameEventArgs e)
        {
            if (State != States.Talk)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                e.Frame.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);

                bdtpClient.Send(stream.ToArray(), (int)Lines.Video);
            }
        }
        private void ReceiveVideo()
        {
            while (bdtpClient.Connected && State != States.Close)
            {
                byte[] data = bdtpClient.Receive((int)Lines.Video);

                if (data == Array.Empty<byte>())
                {
                    continue;
                }
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        using (Bitmap frame = new Bitmap(stream))
                        {

                            try
                            {
                                VideoFrame = ImageSourceForBitmap(frame);
                                OnPropertyChanged("VideoFrame");
                            }
                            catch { }
                        }
                    }
                });

                Thread.Sleep(0);
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        private ImageSource ImageSourceForBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        // Закрытие модели
        public void Closing()
        {
            if (State == States.Talk)
            {
                EndTalk();
            }
            State = States.Close;

            EndCall();
            bdtpClient.Dispose();
        }
    }
}
