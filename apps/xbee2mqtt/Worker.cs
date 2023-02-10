namespace xbee2mqtt;

using System.Text;
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

public class Worker : BackgroundService
{
    private const byte DefaultTransmitFrameId = 1;
    private const byte DefaultTransmitATFrameId = 2;
    private IMqttClient? _mqttClient;
    private IConfigurationRoot? _configuration;
    private string? _rxTopic;
    private string? _txTopic;
    private string? _atTopic;
    private string? _ioTopic;
    private string? _niTopic;
    private string? _serialPortName;
    private int _serialBaudRate;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    #pragma warning disable CS8604
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Configure() == false)
        {
            _logger.LogError("Exiting on invalid configuration.");
            return;
        }

        if (await ConnectMqtt() == false)
        {
            _logger.LogError("Exiting on failed MQTT connect.");
            return;
        }

        const bool escaped = true;
        var serialPort = new SerialPort(_serialPortName, _serialBaudRate);
        serialPort.WriteTimeout = 500;
        serialPort.Open();

        // Handle MQTT messages that need XBee serial port access.
        _mqttClient.UseApplicationMessageReceivedHandler(async e =>
        {
            await HandleMessageAsync(e, serialPort.BaseStream, escaped);
        });

        var xbeeSerialAsync = new XbeeSerial(_logger, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            XbeeFrame? xbeeFrame = await xbeeSerialAsync.ReadNextFrameAsync(serialPort, escaped);
            if (xbeeFrame is null)
            {
                _logger.LogWarning("Partial frame read.");
                continue;
            }

            if (xbeeFrame.FrameType == XbeeFrame.PacketTypeReceive)
            {
                ReceivePacket? receivePacket;
                if (!ReceivePacket.Parse(out receivePacket, xbeeFrame) || receivePacket is null)
                {
                    _logger.LogError("Invalid receive packet.");
                    continue;
                }
                var sourceAddress = receivePacket.SourceAddress.AsString();
                var optionText = receivePacket.ReceiveOptions == 1 ? "with response" : "broadcast";
                var data = Encoding.Default.GetString(receivePacket.ReceiveData.ToArray());
                _logger.LogDebug($"RX packet {optionText} from {sourceAddress} 0x{receivePacket.NetworkAddress:X4}: {data}");
                
                var topic = $"{_rxTopic}/{sourceAddress}";
                if (topic is null)
                {
                    _logger.LogError("Null topic.");
                    throw new InvalidOperationException();
                }
                await PublishMessageAsync($"{topic}", receivePacket.ReceiveData);
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeReceiveIO)
            {
                ReceiveIOPacket? receivePacket;
                if (!ReceiveIOPacket.Parse(out receivePacket, xbeeFrame) || receivePacket is null)
                {
                    _logger.LogError("Invalid receive IO packet.");
                    continue;
                }
                var sourceAddress = receivePacket.SourceAddress.AsString();
                var sampleBuilder = new StringBuilder();
                foreach (var dio in receivePacket.DigitalSamples)
                {
                    sampleBuilder.Append($"DIO{dio.Dio}={dio.Value} ");
                }
                foreach (var adc in receivePacket.AnalogSamples)
                {
                    sampleBuilder.Append($"AD{adc.Adc}={adc.Value:X4} ");
                }
                var samples = sampleBuilder.ToString().Trim();
                _logger.LogInformation($"RX IO from {sourceAddress} 0x{receivePacket.NetworkAddress:X4}: {samples}");
                var topic = $"{_ioTopic}/{sourceAddress}";
                if (topic is null)
                {
                    throw new InvalidOperationException("topic");
                }
                await PublishMessageAsync($"{topic}", Encoding.ASCII.GetBytes(samples));
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeExtendedTransmitStatus)
            {
                ExtendedTransmitStatusPacket? extendedTransmitStatus;
                if (!ExtendedTransmitStatusPacket.Parse(out extendedTransmitStatus, xbeeFrame) || extendedTransmitStatus is null)
                {
                    _logger.LogError("Invalid extended receive status packet.");
                    continue;
                }
                if (extendedTransmitStatus.DeliveryStatus != 0)
                {
                    _logger.LogWarning($"Extended transmit status error 0x{extendedTransmitStatus.DeliveryStatus:X2} from 0x{extendedTransmitStatus.NetworkAddress:X4}");
                }
                else
                {
                    _logger.LogInformation($"Extended transmit status frame id = {extendedTransmitStatus.FrameId} from 0x{extendedTransmitStatus.NetworkAddress:X4}");
                }
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeModemStatus)
            {
                ModemStatusPacket? modemStatusPacket;
                if (!ModemStatusPacket.Parse(out modemStatusPacket, xbeeFrame) || modemStatusPacket is null)
                {
                    _logger.LogError("Invalid modem status packet.");
                    continue;
                }
                _logger.LogInformation($"Modem status = {modemStatusPacket.ModemStatus}");
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeRemoteATCommandResponse)
            {
                ATCommandResponsePacket? remoteATCommandResponse;
                if (!ATCommandResponsePacket.Parse(out remoteATCommandResponse, xbeeFrame) || remoteATCommandResponse is null)
                {
                    _logger.LogError("Invalid remote AT response packet.");
                    continue;
                }
                if (remoteATCommandResponse.CommandStatus != 0)
                {
                    _logger.LogWarning($"Remote AT response packet error 0x{remoteATCommandResponse.CommandStatus:X2} from {remoteATCommandResponse.SourceAddress.AsString()}");
                }
                else
                {
                    _logger.LogInformation($"Remote AT response command {remoteATCommandResponse.Command} from {remoteATCommandResponse.SourceAddress.AsString()}");
                }
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeNodeIdentification)
            {
                NodeIdentificationPacket? nodeIdentificationPacket;
                if (!NodeIdentificationPacket.Parse(out nodeIdentificationPacket, xbeeFrame) || nodeIdentificationPacket is null)
                {
                    _logger.LogError("Invalid node identification packet.");
                    continue;
                }
                var remoteSourceAddress = nodeIdentificationPacket.RemoteSourceAddress.AsString();
                var networkAddress = $"0x{nodeIdentificationPacket.RemoteNetworkAddress:X4}";
                var nodeIdent = string.IsNullOrWhiteSpace(nodeIdentificationPacket.NodeIdentifier) ? String.Empty : nodeIdentificationPacket.NodeIdentifier.Trim();
                var deviceType = nodeIdentificationPacket.DeviceType switch
                {
                    0x00 => "coordinator",
                    0x01 => "router",
                    0x02 => "end device",
                    _ => "(undefined)",
                };
                if (string.IsNullOrEmpty(deviceType))
                {
                    _logger.LogWarning($"Invalid node identification packet device type: {nodeIdentificationPacket.DeviceType:X2} from {remoteSourceAddress}");
                }
                _logger.LogInformation($"Node identification {nodeIdent} {deviceType}: {remoteSourceAddress} {networkAddress}");
                var topic = $"{_niTopic}/{nodeIdentificationPacket.RemoteSourceAddress.AsString()}";
                if (topic is null)
                {
                    throw new InvalidOperationException("topic");
                }
                await PublishMessageAsync($"{topic}", Encoding.ASCII.GetBytes($"{deviceType} 0x{nodeIdentificationPacket.RemoteNetworkAddress:X4} {nodeIdent}"));
            }
            // Common known packet types not processed:
            else if (xbeeFrame.FrameType == XbeeFrame.RouteRecordIndicator)
            {
                _logger.LogDebug("Skipping Route Record Indicator packet.");
            }
            else
            {
                _logger.LogWarning($"Unsupported frame type: {xbeeFrame.FrameType:X2}");
            }
        }
    }
    
    private void OnPublisherConnected(MqttClientConnectedEventArgs x)
    {
        _logger.LogInformation("MQTT connected");
    }

    private void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
    {
        _logger.LogInformation("MQTT disconnected");
    }

    private async Task<bool> ConnectMqtt()
    {
        if (_configuration is null)
        {
            throw new InvalidOperationException("_configuration");
        }
        var port = _configuration.GetValue<int>("MQTT_PORT");
        var server = _configuration["MQTT_SERVER"];
        var clientId = _configuration["MQTT_CLIENT_ID"];
        var useTls = _configuration.GetValue<bool>("MQTT_USE_TLS");
        _logger.LogInformation($"Connecting to {server}:{port} with client id {clientId}");
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
        var mqtt_password = _configuration["MQTT_PASSWORD"] ?? String.Empty;
        options.Credentials = new MqttClientCredentials
        {
            Username = _configuration["MQTT_USERNAME"],
            Password = Encoding.UTF8.GetBytes(mqtt_password)
        };
        options.CleanSession = true;
        _mqttClient = mqttFactory.CreateMqttClient();
        _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnPublisherConnected);
        _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnPublisherDisconnected);
        var connectResult = await _mqttClient.ConnectAsync(options);
        _logger.LogDebug($"MQTT connect result: {connectResult.ResultCode}");

        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            _logger.LogError($"MQTT connection failed: {connectResult.ReasonString}");
            return false;
        }

        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic($"{_txTopic}/#").Build());
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic($"{_atTopic}/#").Build());

        return true;
    }

    private async Task DisconnectMqtt()
    {
        _logger.LogInformation("Disconnecting MQTT");
        await _mqttClient.DisconnectAsync();
    }

    private async Task PublishMessageAsync(string topic, IReadOnlyList<byte> payload)
    {
        // QoS 2
        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(payload)
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                            .Build();
        _logger.LogInformation($"Publishing {topic}");
        var pubResult = await _mqttClient.PublishAsync(message);
        if (pubResult.ReasonCode != MqttClientPublishReasonCode.Success)
        {
            _logger.LogError($"Published failed: {pubResult.ReasonString}");
        }
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e, Stream baseStream, bool escaped)
    {
        try
        {
            XbeeFrame? xbeeFrame;
            if (e.ApplicationMessage.Topic.StartsWith(_txTopic) && e.ApplicationMessage.Payload is not null)
            {
                var splitTopic = e.ApplicationMessage.Topic.Split('/');
                var address = splitTopic[splitTopic.Length - 1];
                var xbeeAddress = XbeeAddress.Create(address);
                if (false == TransmitPacket.CreateXbeeFrame(out xbeeFrame, xbeeAddress, DefaultTransmitFrameId, e.ApplicationMessage.Payload, escaped))
                {
                    _logger.LogError("Could not create transmit packet.");
                    return;
                }
            }
            else if (e.ApplicationMessage.Topic.StartsWith(_atTopic) && e.ApplicationMessage.Payload is not null)
            {
                var splitTopic = e.ApplicationMessage.Topic.Split('/');
                var address = splitTopic[splitTopic.Length - 1];
                // Payload first two bytes are AT command (e.g. D0), remaining are parameter value (e.g. 0x00 0x05).
                var command = new byte [] {e.ApplicationMessage.Payload[0], e.ApplicationMessage.Payload[1]};
                var parameterValue = new List<byte>(e.ApplicationMessage.Payload).GetRange(2, e.ApplicationMessage.Payload.Length - 2);
                var xbeeAddress = XbeeAddress.Create(address);
                if (false == TransmitATPacket.CreateXbeeFrame(out xbeeFrame, xbeeAddress, DefaultTransmitATFrameId, command, parameterValue, escaped))
                {
                    _logger.LogError("Could not create remote AT packet.");
                    return;
                }
            }
            else
            {
                _logger.LogWarning($"Invalid topic and/or paylod: {e.ApplicationMessage.Topic}");
                return;
            }
            if (xbeeFrame is not null)
            {
                await baseStream.WriteAsync(xbeeFrame.Data.ToArray(), 0, xbeeFrame.Data.Count);
                _logger.LogInformation($"Handled {e.ApplicationMessage.Topic}");
            }
            _logger.LogDebug($"Topic = {e.ApplicationMessage.Topic}");
            _logger.LogDebug($"Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
            _logger.LogDebug($"QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
            _logger.LogDebug($"Retain = {e.ApplicationMessage.Retain}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }

    private bool Configure()
    {
        _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional:true, reloadOnChange:true)
                .AddEnvironmentVariables()
                .Build();
        if (_configuration is null)
        {
            _logger.LogError("Null configuration.");
            return false;
        }

        // Serial port.
        _serialPortName = _configuration["SERIAL_PORT"];
        _serialBaudRate = _configuration.GetValue<int>("SERIAL_BAUD");
        if (String.IsNullOrEmpty(_serialPortName))
        {
            _logger.LogError("No serial port given.");
            return false;
        }

        // MQTT topics.
        _rxTopic = _configuration["MQTT_RX_TOPIC"];
        _txTopic = _configuration["MQTT_TX_TOPIC"];
        _atTopic = _configuration["MQTT_AT_TOPIC"];
        _ioTopic = _configuration["MQTT_IO_TOPIC"];
        _niTopic = _configuration["MQTT_NI_TOPIC"];
        if (String.IsNullOrEmpty(_rxTopic) ||
            String.IsNullOrEmpty(_txTopic) ||
            String.IsNullOrEmpty(_atTopic) ||
            String.IsNullOrEmpty(_ioTopic) ||
            String.IsNullOrEmpty(_niTopic))
        {
            _logger.LogError("Null configuration.");
            return false;
        }

        return true;
    }
}