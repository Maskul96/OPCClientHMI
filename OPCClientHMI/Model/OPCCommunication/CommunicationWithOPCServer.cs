

using Microsoft.Extensions.DependencyInjection;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace OPCClientHMI.Model.OPCCommunication
{
    //Komunikacja z serwerem OPC
    public class CommunicationWithOPCServer
    {
        static SessionReconnectHandler? _sessionreconnectHandler;
        static Session? _session;
        public static bool connectedonce { get; private set; }
        public string? ServerIPAddress { get; private set; }
        public MainWindow obj;

        //przekazanie obiektów z MainWindow do wnętrza klasy poprzez konstruktor
        public CommunicationWithOPCServer(MainWindow obj)
        {
            this.obj = obj;
        }
        //Inicjalizacja połączenia z serwerem OPC
        public static async Task Init(string ServerIPAddress = "")
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
            try
            {
                _session = await Session.Create(
                                _configuration,
                                endpoint,
                                true,
                                _configuration.ApplicationName,
                                60000,
                                null,
                                null);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            if (_session.Connected)
            {
                MessageBox.Show("Połączono z serwerem");
            }
            else
            {
                MessageBox.Show("Brak połączenia");
            }
            IfSessionConnectedComplete();
            _session.KeepAlive += Session_KeepAlive;
            _sessionreconnectHandler = new SessionReconnectHandler(true, 10 * 1000);
        }

        public static void IfSessionConnectedComplete()
        {
            if (_session != null & !connectedonce) connectedonce = true;
        }

        //Odczyt danych
        public static void ReadData()
        {
            try
            {
                ReferenceDescription testValueRef = GetReference("PLC_1.DataBlocksGlobal.CHART.ClearPlot");

                ReadValueId nodeToRead = new ReadValueId();
                nodeToRead.NodeId = (NodeId)testValueRef.NodeId;
                nodeToRead.AttributeId = Attributes.Value;
                Opc.Ua.NodeTypeDescription nodeTypeDescription = new Opc.Ua.NodeTypeDescription();

                MessageBox.Show($"NodeId: {nodeToRead.NodeId}, AttributeId: {nodeToRead.AttributeId} ");

                ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
                nodesToRead.Add(nodeToRead);

                DataValueCollection readResults = null;
                DiagnosticInfoCollection readDiagnosticInfos = null;

                ResponseHeader readHeader = _session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out readResults, out readDiagnosticInfos);

                ClientBase.ValidateResponse(readResults, nodesToRead);
                ClientBase.ValidateDiagnosticInfos(readDiagnosticInfos, nodesToRead);

                string m_value = readResults[0].ToString();
                List<ReadDataModel> values = new List<ReadDataModel>();
                values.Add(new ReadDataModel() { NodeID = nodeToRead.NodeId, AttributeID = nodeToRead.AttributeId, Value = m_value, TimeStamp = readHeader.Timestamp });
                MainWindow._mainwindow.ReadDatalistView.ItemsSource = values;
                MessageBox.Show($"nodesToRead: {readHeader.Timestamp}, m_value: {m_value} ");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //Zmaknięcie sesji
        public static void Session_Close()
        {
            if (_session!=null)
            {
                try
                {
                    _session.Close();
                    if (!_session.Connected)
                    {
                        MessageBox.Show("Rozłączono sesję z serwerem");
                    }


                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }


        }

        //Utrzymanie sesji
        private static void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
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
        //Akceptacja certyfikatów połączenia
        private static void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }

        //Pobiranie opisu zmiennej
        private static ReferenceDescription GetReference(string value)
        {
            ReferenceDescription? currentDescription = null;
            string[] values = value.Split('.');
            ReferenceDescriptionCollection collection = MyBrowse(ObjectIds.ObjectsFolder);
            for (int i = 0; i < values.Length; i++)
            {
                currentDescription = collection.Where(e => e.DisplayName == values[i]).FirstOrDefault();
                collection = MyBrowse((NodeId)currentDescription.NodeId);
            }
            return currentDescription;
        }
        //Wyszukiwanie branchy
        private static ReferenceDescriptionCollection MyBrowse(NodeId sourceId)
        {
            //Wyszukiwanie komponentów node'a
            BrowseDescription nodeToBrowse1 = new BrowseDescription();

            nodeToBrowse1.NodeId = sourceId;
            nodeToBrowse1.BrowseDirection = BrowseDirection.Forward;
            nodeToBrowse1.ReferenceTypeId = ReferenceTypeIds.Aggregates;
            nodeToBrowse1.IncludeSubtypes = true;
            nodeToBrowse1.NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable);
            nodeToBrowse1.ResultMask = (uint)BrowseResultMask.All;

            BrowseDescription nodeToBrowse2 = new BrowseDescription();

            nodeToBrowse2.NodeId = sourceId;
            nodeToBrowse2.BrowseDirection = BrowseDirection.Forward;
            nodeToBrowse2.ReferenceTypeId = ReferenceTypeIds.Organizes;
            nodeToBrowse2.IncludeSubtypes = true;
            nodeToBrowse2.NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable);
            nodeToBrowse2.ResultMask = (uint)BrowseResultMask.All;

            BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection();
            nodesToBrowse.Add(nodeToBrowse1);
            nodesToBrowse.Add(nodeToBrowse2);

            // Pobranie referencji z serwera
            ReferenceDescriptionCollection references = Browse(_session, nodesToBrowse, false);
            //Uzupełnienie potencjalnego treeview 
            //for (int ii = 0; ii < references.Count; ii++)
            //{
            //    ReferenceDescription target = references[ii];
            //}
            return references;


        }

        // Pobranie adresu przestrzeni i zwrócenie znalezionej referencji
        public static ReferenceDescriptionCollection Browse(Session session, BrowseDescriptionCollection nodesToBrowse, bool throwOnError)
        {
            try
            {
                ReferenceDescriptionCollection references = new ReferenceDescriptionCollection();
                BrowseDescriptionCollection unprocessedOperations = new BrowseDescriptionCollection();

                while (nodesToBrowse.Count > 0)
                {
                    // start wyszukiwania
                    BrowseResultCollection results = null;
                    DiagnosticInfoCollection diagnosticInfos = null;

                    session.Browse(
                        null,
                        null,
                        0,
                        nodesToBrowse,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, nodesToBrowse);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);

                    ByteStringCollection continuationPoints = new ByteStringCollection();

                    for (int ii = 0; ii < nodesToBrowse.Count; ii++)
                    {
                        if (StatusCode.IsBad(results[ii].StatusCode))
                        {
                            if (results[ii].StatusCode == StatusCodes.BadNoContinuationPoints)
                            {
                                unprocessedOperations.Add(nodesToBrowse[ii]);
                            }
                            continue;
                        }

                        //sprawdzenie czy wszystkie referencje zostały pobrane
                        if (results[ii].References.Count == 0)
                        {
                            continue;
                        }

                        // zapis rezultatu
                        references.AddRange(results[ii].References);

                        // sprawdzenie punktów kontynuacji - "ContinuationPoint" używane do w opcua do wstrzymywania operacji przeglądania,QueryFirst lub HistoryRead
                        //i umożliwia ich ponowne uruchomienie później poprzez wywołanie funkcji BrowseNext, QueryFirst lub HistoryRead. Operacje są wstrzymywane gdy liczna znalezionych wyników przekracza limity ustawione przez klienta lub serwer
                        if (results[ii].ContinuationPoint != null)
                        {
                            continuationPoints.Add(results[ii].ContinuationPoint);
                        }
                    }

                    ByteStringCollection revisedContinuationPoints = new ByteStringCollection();

                    while (continuationPoints.Count > 0)
                    {
                        session.BrowseNext(
                            null,
                            false,
                            continuationPoints,
                            out results,
                            out diagnosticInfos);

                        ClientBase.ValidateResponse(results, continuationPoints);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);

                        for (int ii = 0; ii < continuationPoints.Count; ii++)
                        {
                            if (StatusCode.IsBad(results[ii].StatusCode))
                            {
                                continue;
                            }
                            if (results[ii].References.Count == 0)
                            {
                                continue;
                            }

                            references.AddRange(results[ii].References);

                            if (results[ii].ContinuationPoint != null)
                            {
                                revisedContinuationPoints.Add(results[ii].ContinuationPoint);
                            }
                        }

                        continuationPoints = revisedContinuationPoints;
                    }

                    nodesToBrowse = unprocessedOperations;
                }
                return references;
            }
            catch (Exception exception)
            {
                if (throwOnError)
                {
                    throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
                }

                return null;
            }
        }

    }
}
