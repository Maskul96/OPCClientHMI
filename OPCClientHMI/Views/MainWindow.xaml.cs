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
            ServerOPCURI.Text = "opc.tcp://192.168.2.1:4840"; //tycmzasowe wrzucenie adresu na stałe
        }

        public static MainWindow _mainwindow;

        private async void Button_Click_ConnectToOPCServer(object sender, RoutedEventArgs e)
        {
            if (!CommunicationWithOPCServer.connectedonce)
            {
                await CommunicationWithOPCServer.Init("opc.tcp://192.168.2.1:4840").ConfigureAwait(false);
            }
            else
            {
                MessageBox.Show("Połączenie jest już ustanowione");
            }
            
        }
        private void Button_Click_DisconnectFromOPCServer(object sender, RoutedEventArgs e)
        {
            CommunicationWithOPCServer.Session_Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CommunicationWithOPCServer.Session_Close();
        }

        private void Button_Click_ReadDataFromOPC(object sender, RoutedEventArgs e)
        {
            CommunicationWithOPCServer.ReadData();
        }
    }
}