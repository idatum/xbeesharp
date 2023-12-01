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
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }
        _configuration = configuration;
        _logger = logger;
        // Serial port.
        _serialPortName = _configuration["SERIAL_PORT"];
        if (String.IsNullOrEmpty(_serialPortName))
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
        if (String.IsNullOrEmpty(_rxTopic) ||
            String.IsNullOrEmpty(_txTopic) ||
            String.IsNullOrEmpty(_atTopic) ||
            String.IsNullOrEmpty(_ioTopic) ||
            String.IsNullOrEmpty(_niTopic))
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
                _logger.LogError(ex.ToString());
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
                    throw new InvalidOperationException("rx topic");
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
                    throw new InvalidOperationException("io topic");
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
                    throw new InvalidOperationException("ni topic");
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
        _logger.LogInformation($"Connecting to {server}:{port} with client id {clientId}");
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
            if (String.IsNullOrEmpty(_txTopic))
            {
                throw new InvalidOperationException(nameof(_txTopic));
            }
            if (String.IsNullOrEmpty(_atTopic))
            {
                throw new InvalidOperationException(nameof(_atTopic));
            }
            if (e.ApplicationMessage.Topic.StartsWith(_txTopic))
            {
                var splitTopic = e.ApplicationMessage.Topic.Split('/');
                var address = splitTopic[splitTopic.Length - 1];
                var xbeeAddress = XbeeAddress.Create(address);
                if (false == TransmitPacket.CreateXbeeFrame(out xbeeFrame, xbeeAddress, DefaultTransmitFrameId, e.ApplicationMessage.PayloadSegment, escaped))
                {
                    throw new ApplicationException("Could not create transmit packet.");
                }
            }
            else if (e.ApplicationMessage.Topic.StartsWith(_atTopic))
            {
                var splitTopic = e.ApplicationMessage.Topic.Split('/');
                var address = splitTopic[splitTopic.Length - 1];
                // Payload first two bytes are AT command (e.g. D0), remaining are parameter value (e.g. 0x00 0x05).
                var command = new byte[] { e.ApplicationMessage.PayloadSegment[0], e.ApplicationMessage.PayloadSegment[1] };
                var parameterValue = new List<byte>(e.ApplicationMessage.PayloadSegment).GetRange(2, e.ApplicationMessage.PayloadSegment.Count - 2);
                var xbeeAddress = XbeeAddress.Create(address);
                if (false == TransmitATPacket.CreateXbeeFrame(out xbeeFrame, xbeeAddress, DefaultTransmitATFrameId, command, parameterValue, escaped))
                {
                    throw new ApplicationException("Could not create remote AT packet.");
                }
            }
            else
            {
                throw new ApplicationException($"Invalid topic and/or paylod: {e.ApplicationMessage.Topic}");
            }
            if (xbeeFrame is not null)
            {
                await baseStream.WriteAsync(xbeeFrame.Data.ToArray(), 0, xbeeFrame.Data.Count);
                _logger.LogInformation($"Handled {e.ApplicationMessage.Topic}");
            }
            _logger.LogDebug($"Topic = {e.ApplicationMessage.Topic}");
            _logger.LogDebug($"Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
            _logger.LogDebug($"QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
            _logger.LogDebug($"Retain = {e.ApplicationMessage.Retain}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }
}
