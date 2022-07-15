﻿namespace xbee2mqtt;

using System.Text;
using System.Diagnostics;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Microsoft.Extensions.Configuration;
using XbeeSharp;
using System.IO.Ports;

class Program
{
    private static IMqttClient? _mqttClient;
    private static Tracing _tracing = new();
    private static IConfigurationRoot? _configuration;
    private static string _rxTopic = String.Empty;
    private static string _txTopic = String.Empty;
    private static string _atTopic = String.Empty;
    private static string _ioTopic = String.Empty;

    private static void OnPublisherConnected(MqttClientConnectedEventArgs x)
    {
        _tracing.Info("MQTT connected");
    }

    private static void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
    {
        _tracing.Info("MQTT disconnected");
    }

    private static async Task<bool> ConnectMqtt()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException();
        }
        var port = _configuration.GetValue<int>("MQTT_PORT");
        var server = _configuration["MQTT_SERVER"];
        var clientId = _configuration["MQTT_CLIENT_ID"];
        var useTls = _configuration.GetValue<bool>("MQTT_USE_TLS");
        _tracing.Debug($"Connecting to {server}:{port} with client id {clientId}");
        var mqttFactory = new MqttFactory();
        var tlsOptions = new MqttClientTlsOptions
        {
            UseTls = useTls
        };
        var options = new MqttClientOptions
        {
            ClientId = clientId,
            ProtocolVersion = MqttProtocolVersion.V311,
            ChannelOptions = new MqttClientTcpOptions
            {
                Server = server,
                Port = port,
                TlsOptions = tlsOptions
            }
        };
        options.Credentials = new MqttClientCredentials
        {
            Username = _configuration["MQTT_USERNAME"],
            Password = Encoding.UTF8.GetBytes(_configuration["MQTT_PASSWORD"])
        };
        options.CleanSession = true;
        _mqttClient = mqttFactory.CreateMqttClient();
        _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnPublisherConnected);
        _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnPublisherDisconnected);
        var connectResult = await _mqttClient.ConnectAsync(options);
        _tracing.Verbose($"MQTT connect result: {connectResult.ResultCode}");

        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            _tracing.Error($"MQTT connection failed: {connectResult.ReasonString}");
            return false;
        }

        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic($"{_txTopic}/#").Build());
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic($"{_atTopic}/#").Build());

        return true;
    }

    private static async Task DisconnectMqtt()
    {
        _tracing.Info("Disconnecting MQTT");
        await _mqttClient.DisconnectAsync();
    }

    private static async Task PublishMessageAsync(string topic, IReadOnlyList<byte> payload)
    {
        // QoS 2
        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(payload)
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                            .Build();
        _tracing.Info($"Publishing {topic}");
        var pubResult = await _mqttClient.PublishAsync(message);
        if (pubResult.ReasonCode != MqttClientPublishReasonCode.Success)
        {
            _tracing.Error($"Published failed: {pubResult.ReasonString}");
        }
    }

    private static async Task ProcessXbeeMessages(string serialPortName, int serialBaudRate)
    {
        const bool escaped = true;
        var serialPort = new SerialPort(serialPortName, serialBaudRate);
        serialPort.Open();
        
        // Handle XBee MQTT messages that need serial port access.
        _mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            try
            {
                XbeeFrame? xbeeFrame;
                if (e.ApplicationMessage.Topic.StartsWith(_txTopic) && e.ApplicationMessage.Payload != null)
                {
                    var splitTopic = e.ApplicationMessage.Topic.Split('/');
                    var address = splitTopic[splitTopic.Length - 1];
                    var xbeeAddress = XbeeAddress.Create(address);
                    if (!XbeeTransmitPacket.CreateXbeeFrame(out xbeeFrame, xbeeAddress, e.ApplicationMessage.Payload, escaped))
                    {
                        _tracing.Error("Could not create XbeeFrame.");
                        return;
                    }
                }
                else if (e.ApplicationMessage.Topic.StartsWith(_atTopic) && e.ApplicationMessage.Payload != null)
                {
                    var splitTopic = e.ApplicationMessage.Topic.Split('/');
                    var address = splitTopic[splitTopic.Length - 1];
                    // Payload first byte is AT command, remaining are parameter value.
                    var command = new byte [] {e.ApplicationMessage.Payload[0], e.ApplicationMessage.Payload[1]};
                    var parameterValue = new List<byte>(e.ApplicationMessage.Payload).GetRange(2, e.ApplicationMessage.Payload.Length - 2);
                    var xbeeAddress = XbeeAddress.Create(address);
                    if (!XbeeRemoteATPacket.CreateXbeeFrame(out xbeeFrame, xbeeAddress, command, parameterValue, escaped))
                    {
                        _tracing.Error("Could not create XbeeFrame.");
                        return;
                    }
                }
                else
                {
                    _tracing.Warning($"Invalid topic and/or paylod: {e.ApplicationMessage.Topic}");
                    return;
                }
                if (xbeeFrame != null)
                {
                    _tracing.Info($"Handled {e.ApplicationMessage.Topic}");
                    serialPort.Write(xbeeFrame.FrameData.ToArray(), 0, xbeeFrame.FrameData.Count);
                }
            }
            catch (Exception ex)
            {
                _tracing.Error(ex.ToString());
            }
            _tracing.Debug($"+ Topic = {e.ApplicationMessage.Topic}");
            _tracing.Debug($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
            _tracing.Debug($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
            _tracing.Debug($"+ Retain = {e.ApplicationMessage.Retain}");
        });

        while (true)
        {
            XbeeFrame? xbeeFrame;
            if (XbeeSerial.TryReadNextFrame(out xbeeFrame, serialPort, escaped))
            {
                if (xbeeFrame == null)
                {
                    _tracing.Warning("Partial frame read.");
                    continue;
                }
                if (xbeeFrame.FrameType == XbeeBasePacket.PacketTypeReceive)
                {
                    XbeeReceivePacket? receivePacket;
                    if (!XbeeReceivePacket.Parse(out receivePacket, xbeeFrame) || receivePacket == null)
                    {
                        _tracing.Error("Invalid receive packet.");
                        continue;
                    }
                    var sourceAddress = XbeeAddress.Create(receivePacket.SourceAddress).AsString();
                    if (_tracing.TraceLevel == TraceLevel.Verbose)
                    {
                        var optionText = receivePacket.ReceiveOptions == 1 ? "with response" : "broadcast";
                        var data = Encoding.Default.GetString(receivePacket.ReceiveData.ToArray());
                        _tracing.Verbose($"RX packet {optionText} from {sourceAddress}: {data}");
                    }
                    var topic = $"{_rxTopic}/{sourceAddress}";
                    if (topic == null)
                    {
                        throw new InvalidOperationException();
                    }
                    await PublishMessageAsync($"{topic}", receivePacket.ReceiveData);
                }
                else if (xbeeFrame.FrameType == XbeeBasePacket.PacketTypeReceiveIO)
                {
                    XbeeReceiveIOPacket? receivePacket;
                    if (!XbeeReceiveIOPacket.Parse(out receivePacket, xbeeFrame) || receivePacket == null)
                    {
                        _tracing.Error("Invalid receive IO packet.");
                        continue;
                    }
                    var sourceAddress = XbeeAddress.Create(receivePacket.SourceAddress).AsString();
                    if (_tracing.TraceLevel == TraceLevel.Verbose || _tracing.TraceLevel == TraceLevel.Info)
                    {
                        var sampleBuilder = new StringBuilder();
                        foreach (var dio in receivePacket.DigitalSamples)
                        {
                            sampleBuilder.Append($"{dio} ");
                        }
                        foreach (var adc in receivePacket.AnalogSamples)
                        {
                            sampleBuilder.Append($"{adc} ");
                        }
                        _tracing.Info($"RX IO from {sourceAddress}: {sampleBuilder.ToString()}");
                        var topic = $"{_ioTopic}/{sourceAddress}";
                        if (topic == null)
                        {
                            throw new InvalidOperationException();
                        }
                        await PublishMessageAsync($"{topic}", Encoding.ASCII.GetBytes(sampleBuilder.ToString()));
                    }
                }
                // Common known packet types not processed:
                else if (xbeeFrame.FrameType == XbeeBasePacket.RouteRecordIndicator)
                {
                    _tracing.Debug("Skipping Route Record Indicator packet.");
                }
                else if (xbeeFrame.FrameType == XbeeBasePacket.ExtendedTransmitStatus)
                {
                    _tracing.Debug("Skipping Extended Transmit Status packet.");
                }
                else if (xbeeFrame.FrameType == XbeeBasePacket.PacketTypeRemoteATCommandResponse)
                {
                    _tracing.Debug("Skipping Remote AT Command Response.");
                }
                else
                {
                    _tracing.Warning($"Unsupported frame type: {xbeeFrame.FrameType:X2}");
                }
            }
        }
    }

    public static async Task Main(string[] args)
    {
        _configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", optional:true, reloadOnChange:true)
                        .AddJsonFile("appsettings.Development.json", optional:true)
                        .AddEnvironmentVariables()
                        .Build();
        if (_configuration == null)
        {
            _tracing.Error("Null configuration.");
            return;
        }
        string serialPortName = _configuration["SERIAL_PORT"];
        int serialBaudRate = _configuration.GetValue<int>("SERIAL_BAUD");

        // Tracing config.
        _tracing.TraceLevel = _configuration.GetValue<TraceLevel>("TRACE_LEVEL");
        // MQTT topics.
        _rxTopic = _configuration["MQTT_RX_TOPIC"];
        _txTopic = _configuration["MQTT_TX_TOPIC"];
        _atTopic = _configuration["MQTT_AT_TOPIC"];
        _ioTopic = _configuration["MQTT_IO_TOPIC"];
        if (String.IsNullOrEmpty(_rxTopic) || String.IsNullOrEmpty(_txTopic))
        {
            _tracing.Error("No MQTT topic defined.");
            throw new InvalidOperationException();
        }

        try
        {
            if (!await ConnectMqtt())
            {
                _tracing.Error("Exiting on failed MQTT connect.");
                return;
            }
            await ProcessXbeeMessages(serialPortName, serialBaudRate);
        }
        finally
        {
            await DisconnectMqtt();
        }
    }
}
