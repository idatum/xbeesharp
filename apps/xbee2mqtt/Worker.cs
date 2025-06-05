namespace xbee2mqtt;

using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Exceptions;
using Microsoft.Extensions.Configuration;
using XbeeSharp;
using System.IO.Ports;

public class Worker : BackgroundService
{
    private const byte DefaultTransmitFrameId = 1;
    private const byte DefaultTransmitATFrameId = 2;
    private readonly string? _rxTopic;
    private readonly string? _txTopic;
    private readonly string? _atTopic;
    private readonly string? _ioTopic;
    private readonly string? _niTopic;
    private readonly string? _serialPortName;
    private readonly int _serialBaudRate;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;
    private IMqttClient? _mqttClient;

    public Worker(IConfiguration configuration,
                  ILogger<Worker> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        _configuration = configuration;
        _logger = logger;
        // Serial port.
        _serialPortName = _configuration["SERIAL_PORT"];
        if (string.IsNullOrEmpty(_serialPortName))
        {
            throw new InvalidOperationException(nameof(_serialPortName));
        }
        _serialBaudRate = _configuration.GetValue<int>("SERIAL_BAUD");
        if (_serialBaudRate == 0)
        {
            throw new InvalidOperationException(nameof(_serialBaudRate));
        }

        // MQTT topics.
        _rxTopic = _configuration["MQTT_RX_TOPIC"];
        _txTopic = _configuration["MQTT_TX_TOPIC"];
        _atTopic = _configuration["MQTT_AT_TOPIC"];
        _ioTopic = _configuration["MQTT_IO_TOPIC"];
        _niTopic = _configuration["MQTT_NI_TOPIC"];
        if (string.IsNullOrEmpty(_rxTopic) ||
            string.IsNullOrEmpty(_txTopic) ||
            string.IsNullOrEmpty(_atTopic) ||
            string.IsNullOrEmpty(_ioTopic) ||
            string.IsNullOrEmpty(_niTopic))
        {
            throw new InvalidOperationException();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExecuteAsync(stoppingToken);
            }
            catch (MqttCommunicationException ex)
            {
                _logger.LogError("{Exception}", ex.ToString());
                await DisconnectMqtt();
                _logger.LogError("Exiting after communication exception.");
                await Task.Delay(5000);
            }
        }
    }

    private async Task ProcessExecuteAsync(CancellationToken stoppingToken)
    {
        if (await ConnectMqtt() == false)
        {
            throw new ApplicationException("Exiting on failed MQTT connect.");
        }
        if (_mqttClient is null)
        {
            throw new InvalidOperationException(nameof(_mqttClient));
        }

        const bool escaped = true;
        if (String.IsNullOrEmpty(_serialPortName))
        {
            throw new InvalidOperationException(nameof(_serialPortName));
        }
        using var serialPort = new SerialPort(_serialPortName, _serialBaudRate);
        serialPort.WriteTimeout = 500;
        serialPort.Open();

        // Handle MQTT messages that need XBee serial port access.
        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            await HandleMessageAsync(e, serialPort.BaseStream, escaped);
        };

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
                if (!ReceivePacket.Parse(out ReceivePacket? receivePacket, xbeeFrame) || receivePacket is null)
                {
                    _logger.LogError("Invalid receive packet.");
                    continue;
                }
                var sourceAddress = receivePacket.SourceAddress.AsString();
                var optionText = receivePacket.ReceiveOptions == 1 ? "with response" : "broadcast";
                var data = Encoding.Default.GetString([.. receivePacket.ReceiveData]);
                _logger.LogDebug("RX packet {Option} from {SourceAddress} 0x{NetworkAddress:X4}: {Data}", optionText, sourceAddress, receivePacket.NetworkAddress, data);

                var topic = $"{_rxTopic}/{sourceAddress}" ?? throw new InvalidOperationException("rx topic");
                await PublishMessageAsync($"{topic}", receivePacket.ReceiveData);
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeReceiveIO)
            {
                if (!ReceiveIOPacket.Parse(out ReceiveIOPacket? receivePacket, xbeeFrame) || receivePacket is null)
                {
                    _logger.LogError("Invalid receive IO packet.");
                    continue;
                }
                var sourceAddress = receivePacket.SourceAddress.AsString();
                var sampleBuilder = new StringBuilder();
                foreach (var (Dio, Value) in receivePacket.DigitalSamples)
                {
                    sampleBuilder.Append($"DIO{Dio}={Value} ");
                }
                foreach (var (Adc, Value) in receivePacket.AnalogSamples)
                {
                    sampleBuilder.Append($"AD{Adc}={Value:X4} ");
                }
                var samples = sampleBuilder.ToString().Trim();
                _logger.LogInformation("RX IO from {SourceAddress} 0x{NetworkAddress:X4}: {Samples}", sourceAddress, receivePacket.NetworkAddress, samples);
                var topic = $"{_ioTopic}/{sourceAddress}" ?? throw new InvalidOperationException("io topic");
                await PublishMessageAsync($"{topic}", Encoding.ASCII.GetBytes(samples));
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeExtendedTransmitStatus)
            {
                if (!ExtendedTransmitStatusPacket.Parse(out ExtendedTransmitStatusPacket? extendedTransmitStatus, xbeeFrame) || extendedTransmitStatus is null)
                {
                    _logger.LogError("Invalid extended receive status packet.");
                    continue;
                }
                if (extendedTransmitStatus.DeliveryStatus != 0)
                {
                    _logger.LogWarning("Extended transmit status error 0x{DeliveryStatus:X2} from 0x{NetworkAddress:X4}", extendedTransmitStatus.DeliveryStatus, extendedTransmitStatus.NetworkAddress);
                }
                else
                {
                    _logger.LogInformation("Extended transmit status frame id = {FrameId} from 0x{NetworkAddress:X4}", extendedTransmitStatus.FrameId, extendedTransmitStatus.NetworkAddress);
                }
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeModemStatus)
            {
                if (!ModemStatusPacket.Parse(out ModemStatusPacket? modemStatusPacket, xbeeFrame) || modemStatusPacket is null)
                {
                    _logger.LogError("Invalid modem status packet.");
                    continue;
                }
                _logger.LogInformation("Modem status = {ModemStatus}", modemStatusPacket.ModemStatus);
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeRemoteATCommandResponse)
            {
                if (!ATCommandResponsePacket.Parse(out ATCommandResponsePacket? remoteATCommandResponse, xbeeFrame) || remoteATCommandResponse is null)
                {
                    _logger.LogError("Invalid remote AT response packet.");
                    continue;
                }
                if (remoteATCommandResponse.CommandStatus != 0)
                {
                    _logger.LogWarning("Remote AT response packet error 0x{CommandStatus:X2} from {SourceAddress}", remoteATCommandResponse.CommandStatus, remoteATCommandResponse.SourceAddress.AsString());
                }
                else
                {
                    _logger.LogInformation("Remote AT response command {Command} from {SourceAddress}", remoteATCommandResponse.Command, remoteATCommandResponse.SourceAddress.AsString());
                }
            }
            else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeNodeIdentification)
            {
                if (!NodeIdentificationPacket.Parse(out NodeIdentificationPacket? nodeIdentificationPacket, xbeeFrame) || nodeIdentificationPacket is null)
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
                    _logger.LogWarning("Invalid node identification packet device type: {DeviceType:X2} from {RemoteSourceAddress}", nodeIdentificationPacket.DeviceType, remoteSourceAddress);
                }
                _logger.LogInformation("Node identification {NodeIdent} {DeviceType}: {RemoteSourceAddress} {NetworkAddress}", nodeIdent, deviceType, remoteSourceAddress, networkAddress);
                var topic = $"{_niTopic}/{nodeIdentificationPacket.RemoteSourceAddress.AsString()}" ?? throw new InvalidOperationException("ni topic");
                await PublishMessageAsync($"{topic}", Encoding.ASCII.GetBytes($"{deviceType} 0x{nodeIdentificationPacket.RemoteNetworkAddress:X4} {nodeIdent}"));
            }
            // Common known packet types not processed:
            else if (xbeeFrame.FrameType == XbeeFrame.RouteRecordIndicator)
            {
                _logger.LogDebug("Skipping Route Record Indicator packet.");
            }
            else
            {
                _logger.LogWarning("Unsupported frame type: {FrameType:X2}", xbeeFrame.FrameType);
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
        var username = _configuration["MQTT_USERNAME"] ?? String.Empty;
        var password = _configuration["MQTT_PASSWORD"] ?? String.Empty;
        var mqttFactory = new MqttFactory();
        var tlsOptions = new MqttClientTlsOptions
        {
            UseTls = useTls
        };
        var options = new MqttClientOptionsBuilder()
                        .WithCredentials(username, password)
                        .WithProtocolVersion(MqttProtocolVersion.V311)
                        .WithTcpServer(server, port)
                        .WithTlsOptions(tlsOptions)
                        .WithCleanSession(true)
                        .WithKeepAlivePeriod(TimeSpan.FromSeconds(5))
                        .Build();
        _logger.LogInformation("Connecting to {Server}:{Port} with client id {ClientId}", server, port, clientId);
        _mqttClient = mqttFactory.CreateMqttClient();
        if (_mqttClient == null)
        {
            throw new InvalidOperationException("_mqttClient");
        }
        _mqttClient.ConnectedAsync += (MqttClientConnectedEventArgs args) =>
        {
            _logger.LogInformation("MQTT connected");
            return Task.CompletedTask;
        };
        _mqttClient.DisconnectedAsync += (MqttClientDisconnectedEventArgs args) =>
        {
            _logger.LogInformation("MQTT disconnected");
            return Task.CompletedTask;
        };
        var connectResult = await _mqttClient.ConnectAsync(options);
        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            _logger.LogError("MQTT connection failed: {ReasonString}", connectResult.ReasonString);
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
        if (_mqttClient is null)
        {
            throw new InvalidOperationException(nameof(_mqttClient));
        }
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }
        // QoS 2
        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(payload)
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                            .Build();
        _logger.LogInformation("Publishing {Topic}", topic);
        var pubResult = await _mqttClient.PublishAsync(message);
        if (pubResult.ReasonCode != MqttClientPublishReasonCode.Success)
        {
            _logger.LogError("Published failed: {ReasonString}", pubResult.ReasonString);
        }
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e, Stream baseStream, bool escaped)
    {
        try
        {
            if (String.IsNullOrEmpty(_txTopic))
            {
                throw new InvalidOperationException(nameof(_txTopic));
            }
            if (String.IsNullOrEmpty(_atTopic))
            {
                throw new InvalidOperationException(nameof(_atTopic));
            }

            XbeeFrame? xbeeFrame;

            if (e.ApplicationMessage.Topic.StartsWith(_txTopic))
            {
                xbeeFrame = CreateTransmitFrame(e, escaped);
            }
            else if (e.ApplicationMessage.Topic.StartsWith(_atTopic))
            {
                xbeeFrame = CreateATFrame(e, escaped);
            }
            else
            {
                throw new ApplicationException($"Invalid topic and/or payload: {e.ApplicationMessage.Topic}");
            }

            if (xbeeFrame is not null)
            {
                await baseStream.WriteAsync(xbeeFrame.Data.ToArray().AsMemory(0, xbeeFrame.Data.Count));
                _logger.LogInformation("Handled {Topic}", e.ApplicationMessage.Topic);
            }

            LogMqttMessage(e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MQTT message: {Message}", e.ApplicationMessage.Topic);
        }
    }

    private XbeeFrame? CreateTransmitFrame(MqttApplicationMessageReceivedEventArgs e, bool escaped)
    {
        var splitTopic = e.ApplicationMessage.Topic.Split('/');
        var address = splitTopic[^1];
        var xbeeAddress = XbeeAddress.Create(address);

        if (!TransmitPacket.CreateXbeeFrame(out var xbeeFrame, xbeeAddress, DefaultTransmitFrameId, e.ApplicationMessage.PayloadSegment, escaped))
        {
            throw new ApplicationException("Could not create transmit packet.");
        }
        return xbeeFrame;
    }

    private XbeeFrame? CreateATFrame(MqttApplicationMessageReceivedEventArgs e, bool escaped)
    {
        var splitTopic = e.ApplicationMessage.Topic.Split('/');
        var address = splitTopic[^1];
        var payload = e.ApplicationMessage.PayloadSegment;

        if (payload.Count < 2)
            throw new ApplicationException("AT command payload too short.");

        var command = new byte[] { payload[0], payload[1] };
        var parameterValue = new List<byte>(payload).GetRange(2, payload.Count - 2);
        var xbeeAddress = XbeeAddress.Create(address);

        if (!TransmitATPacket.CreateXbeeFrame(out var xbeeFrame, xbeeAddress, DefaultTransmitATFrameId, command, parameterValue, escaped))
        {
            throw new ApplicationException("Could not create remote AT packet.");
        }
        return xbeeFrame;
    }
    private void LogMqttMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        _logger.LogDebug("Topic = {Topic}", e.ApplicationMessage.Topic);
        _logger.LogDebug("Payload = {Payload}", Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment));
        _logger.LogDebug("QoS = {QualityOfServiceLevel}", e.ApplicationMessage.QualityOfServiceLevel);
        _logger.LogDebug("Retain = {Retain}", e.ApplicationMessage.Retain);
    }
}
