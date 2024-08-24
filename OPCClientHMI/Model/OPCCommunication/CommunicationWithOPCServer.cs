

using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace OPCClientHMI.Model.OPCCommunication
{
    //Komunikacja z serwerem OPC
    public class CommunicationWithOPCServer
    {
        static SessionReconnectHandler? _sessionreconnectHandler;
        static Session? _session;
        public string? ServerIPAddress { get;  private set; }
        public MainWindow obj;

        //przekazanie obiektów z MainWindow do wnętrza klasy poprzez konstruktor
        public CommunicationWithOPCServer(MainWindow obj)
        {
            this.obj = obj;
        }
        
        public static async Task Init(string ServerIPAddress="")
        {
            //Przygotowanie instacji opc klienta
            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationType = Opc.Ua.ApplicationType.Client;
            application.ConfigSectionName = "Client";
            application.LoadApplicationConfiguration(false).Wait();
            application.CheckApplicationInstanceCertificate(false, 0).Wait();
            //Konfiguracja opc klienta
            var _configuration = application.ApplicationConfiguration;
            _configuration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;
            string serverurl = ServerIPAddress;
            //Ustawienie endpointa klienta
            var endpointDescription = CoreClientUtils.SelectEndpoint(_configuration, serverurl, true, 15000);
            var endpointConfiguration = EndpointConfiguration.Create(_configuration);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
            //Utworzenie sesji metodą asynchroniczną Create -> program będzie czekał aż dana metoda się wykona i dopiero wtedy będzie wykonywał kolejne instrukcje
            _session = await Session.Create(
                _configuration,
                endpoint,
                true,
                _configuration.ApplicationName,
                60000,
                null,
                null);
            if (_session.Connected)
            {
                MessageBox.Show("Połączono z serwerem");
            }
            else
            {
                MessageBox.Show("Brak połączenia");
            }
            MainWindow._mainwindow.test.Text = _session.MessageContext.NamespaceUris.ToString();
            _session.KeepAlive += Session_KeepAlive;
            _sessionreconnectHandler = new SessionReconnectHandler(true, 10 * 1000);

        }

        //Utrzymanie sesji
        private static void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            MainWindow._mainwindow.test.Text = e.Status.ToString();
            if (ServiceResult.IsBad(e.Status))
            {
                _sessionreconnectHandler.BeginReconnect(_session, 1000, Server_ReconnectComplete);
            }
        }
        //Ponowne połączenie
        private static void Server_ReconnectComplete(object? sender, EventArgs e)
        {
            if (_sessionreconnectHandler.Session != null)
            {
                if (!ReferenceEquals(_session, _sessionreconnectHandler.Session))
                {
                    var session = _session;
                    session.KeepAlive -= Session_KeepAlive;
                    _session = _sessionreconnectHandler.Session as Session;
                    _session.KeepAlive += Session_KeepAlive;
                    Utils.SilentDispose(session);
                }
            }
        }

        private static void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }
    }
}
