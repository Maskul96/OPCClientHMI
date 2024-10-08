﻿

using HiveMQtt.Client;
using HiveMQtt.Client.Events;
using HiveMQtt.Client.Options;
using HiveMQtt.MQTT5.ReasonCodes;
using HiveMQtt.MQTT5.Types;
using Opc.Ua;
using System.Windows;

namespace OPCClientHMI.OPCUA_MQTTGateway
{
    public class MQTTClient
    {
        //klasa do wysyłania danych jako MQTTClient do HiveMQ Cloud Cluster
        public static bool ExitRequested { get; set; }
        public static int MessageCount { get; set; }
        public static int PublishesReceivedCount { get; set; }

        public MQTTClient()
        {
        }

        public HiveMQClient CreateMQTTClient()
        {
            var options = new HiveMQClientOptions
            {
                Host = "6c0924b9cdbd422b8294214d4db4c568.s1.eu.hivemq.cloud",
                Port = 8883,
                UseTLS = true,
                UserName = "myusername",
                Password = "Ctec1234",
            };

            return new HiveMQClient(options);
        }

        public static async Task PublishData()
        {
            MQTTClient mqttClient = new MQTTClient();
            var client = mqttClient.CreateMQTTClient();

            HiveMQtt.Client.Results.ConnectResult connectResult;
            try
            {
                connectResult = await client.ConnectAsync().ConfigureAwait(false);
                if (connectResult.ReasonCode == ConnAckReasonCode.Success)
                {
                    Console.WriteLine($"Connect successful: {connectResult}");
                }
                else
                {
                    // FIXME: Add ToString
                    Console.WriteLine($"Connect failed: {connectResult}");
                    Environment.Exit(-1);
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine($"Error connecting to the MQTT Broker with the following socket error: {e.Message}");
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error connecting to the MQTT Broker with the following message: {e.Message}");
                Environment.Exit(-1);
            }

            var msg = new string(/*lang=json,strict*/
                                "{\"interference\": \"1029384\"}");
            var result = await client.PublishAsync("tests/ssss/111", msg, QualityOfService.ExactlyOnceDelivery).ConfigureAwait(false);
            var result1 = await client.PublishAsync("tests/ssss/222", msg, QualityOfService.ExactlyOnceDelivery).ConfigureAwait(false);
            //Więcej opcji publikowania w dokumentacji: https://hivemq.github.io/hivemq-mqtt-client-dotnet/docs/publishing

            Console.WriteLine(result);
            Console.WriteLine(result1);

            client.DisconnectAsync().ConfigureAwait(false);
        }

        public static async Task SubscribeTopic()
        {
            MQTTClient mqttClient = new MQTTClient();
            var client = mqttClient.CreateMQTTClient();

            MessageCount = 0;
            PublishesReceivedCount = 0;

            HiveMQtt.Client.Results.ConnectResult connectResult;
            try
            {
                connectResult = await client.ConnectAsync().ConfigureAwait(false);
                if (connectResult.ReasonCode == ConnAckReasonCode.Success)
                {
                    Console.WriteLine($"Connect successful: {connectResult}");
                }
                else
                {
                    // FIXME: Add ToString
                    Console.WriteLine($"Connect failed: {connectResult}");
                    Environment.Exit(-1);
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine($"Error connecting to the MQTT Broker with the following socket error: {e.Message}");
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error connecting to the MQTT Broker with the following message: {e.Message}");
                Environment.Exit(-1);
            }

            client.OnMessageReceived += (sender, args) =>
            {
                MessageCount++;
                MessageBox.Show("Message Received: {}", args.PublishMessage.PayloadAsString);
            };

            var subscriberesult = await client.SubscribeAsync("tests/1", QualityOfService.ExactlyOnceDelivery).ConfigureAwait(false);
            //Więcej opcji subskrypcji w dokumentaacji https://hivemq.github.io/hivemq-mqtt-client-dotnet/docs/subscribing

            //Z DOKUMENTACJI PER SUBSCRIPTION CALLBACK WITH "WITHSUBSCRIPTION" - pozwalajace uzyc Topic Filtra, ktory pozwala w rozny sposób obsłużyć różne tematy, pod które sie podpinami subskrypcją
            #region przykład z dokumentacji
            //public SubscribeOptionsBuilder WithSubscription(TopicFilter topicFilter, EventHandler<OnMessageReceivedEventArgs>? handler = null)
            //var builder = new SubscribeOptionsBuilder();
            //var options = builder.WithSubscription(
            //        new TopicFilter("test/topic", QualityOfService.AtLeastOnceDelivery),
            //        (sender, e) =>
            //        {
            //            Console.WriteLine($"Message received on topic {e.Topic}: {e.Message}");
            //        })
            //    .Build();

            //Alternatywnie message handler niezaleznie zdefiniowany anie za pomocą funkcji lambda:
            //            private static void MessageHandler(object? sender, OnMessageReceivedEventArgs eventArgs)
            //            {
            //                Console.WriteLine("Message Received: {}", eventArgs.PublishMessage.PayloadAsString)
            //}

            //            var builder = new SubscribeOptionsBuilder();
            //            var options = builder.WithSubscription(
            //                    new TopicFilter("test/topic", QualityOfService.AtLeastOnceDelivery),
            //                    MessageHandler)
            //                .Build();
            #endregion

        }
    }
}
