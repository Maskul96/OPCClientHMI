using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua;
using OPCClientHMI.Model.OPCCommunication;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OPCClientHMI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _mainwindow = this;
        }

        public static MainWindow _mainwindow;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await CommunicationWithOPCServer.Init("opc.tcp://192.168.2.1:4840").ConfigureAwait(false);
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CommunicationWithOPCServer.Session_Close();
        }
    }
}