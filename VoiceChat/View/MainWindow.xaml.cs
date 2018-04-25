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
using VoiceChat.ViewModel;

namespace VoiceChat.View
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread threadReceiver;  // Поток для прослушивания входящих сообщений
        
        public MainWindow()
        {
            InitializeComponent();
            VoiceChatVM viewModel = new VoiceChatVM();
            Closing += viewModel.Closing;
            DataContext = viewModel;
        }
    }
}
