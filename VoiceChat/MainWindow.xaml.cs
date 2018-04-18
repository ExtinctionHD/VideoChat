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
using VoiceChat.Classes;

namespace VoiceChat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IPAddress localIp;
        private BdtpClient bdtpClient;

        private WaveIn input;                       // Поток записи
        private WaveOut output;                     // Поток воспроизведения
        private BufferedWaveProvider bufferStream;  // Беферный поток для передачи через сеть

        private Thread threadReceiver;  // Поток для прослушивания входящих сообщений
        
        public MainWindow()
        {
            InitializeComponent();
            
            localIp = Dns.GetHostAddresses(Dns.GetHostName()).Where(x => x.AddressFamily == AddressFamily.InterNetwork).Last();
            bdtpClient = new BdtpClient(localIp);
            DataContext = bdtpClient;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtblockIP.Text = "Local IP: " + localIp.ToString();

            InitializeAudio();

            // Создаем поток для прослушки
            threadReceiver = new Thread(new ThreadStart(Receiving));
            threadReceiver.Start();

            input.StartRecording();
        }

        private void InitializeAudio()
        {
            // Cоздаем поток для записи нашей речи определяем его формат - 
            // частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
            input = new WaveIn();
            input.WaveFormat = new WaveFormat(8000, 16, 1);
            input.DataAvailable += Voice_Input;

            // Создание потока для прослушивания входящиего звука
            output = new WaveOut();
            bufferStream = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            output.Init(bufferStream);
        }

        // Обработка входящей речи
        private void Voice_Input(object sender, WaveInEventArgs e)
        {
            bdtpClient.Send(e.Buffer);
        }

        private void Receiving()
        {
            while (true)
            {
                if (bdtpClient.Listen() != null)
                {
                    Dispatcher.Invoke(ShowRemoteIP);
                }

                output.Play();
                while (bdtpClient.IsConnected)
                {
                    byte[] data = bdtpClient.Receive();
                    bufferStream.AddSamples(data, 0, data.Length);

                    Thread.Sleep(0);
                }

                output.Pause();
                Thread.Sleep(0);
            }
        }

        private void ShowRemoteIP()
        {
            txtboxIP.Text = bdtpClient.RemoteIP.ToString();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bdtpClient.Connect(IPAddress.Parse(txtboxIP.Text));
            }
            catch { }
        }

        private void btnReceive_Click(object sender, RoutedEventArgs e)
        {
            bdtpClient.Disconnect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bdtpClient.Disconnect();
            threadReceiver.Abort();
        }
    }
}
