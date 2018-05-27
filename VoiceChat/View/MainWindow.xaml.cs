using System.Windows;
using VoiceChat.ViewModel;

namespace VoiceChat.View
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            VoiceChatVM vm = new VoiceChatVM();
            DataContext = vm;

            Closing += vm.Closing_Executed;
        }
    }
}
