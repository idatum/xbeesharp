namespace XbeeSharp;

/// <summary>
/// Common ZigBee packet information.
/// </summary>
public abstract class XbeeBasePacket    
{
    /// <summary>
    /// https://www.digi.com/resources/documentation/digidocs/pdfs/90002002.pdf
    ///
    // TX request.
    /// </summary>
    public const byte PacketTypeTransmit = 0x10;
    /// <summary>
    /// Remote AT Command Request
    /// </summary>
    public const byte PacketRemoteAT = 0x17;
    /// <summary>
    /// RX receive.
    /// </summary>
    public const byte PacketTypeReceive = 0x90;
    /// <summary>
    /// RX IO data received.
    /// </summary>
    public const byte PacketTypeReceiveIO = 0x92;
    /// <summary>
    /// Remote AT Command Response.
    /// <summary>
    public const byte PacketTypeRemoteATCommandResponse = 0x97;
    /// <summary>
    /// Route Record Indicator
    /// </summary>
    public const byte RouteRecordIndicator = 0xA1;
    /// <summary>
    /// Extended Transmit Status
    /// </summary>
    public const byte ExtendedTransmitStatus = 0x8B;
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    protected XbeeFrame _xbeeFrame;
    /// <summary>
    /// 64bit source address.
    /// </summary>
    protected IReadOnlyList<byte> _sourceAddress;
    /// <summary>
    /// 16bit network address.
    /// </summary>
    protected IReadOnlyList<byte> _networkAddress;
    /// <summary>
    /// Receive options bit field.
    /// </summary>
    protected byte _receiveOptions;
    /// <summary>
    /// Receive data.
    /// </summary>

    /// <summary>
    /// Construct ZigBee packet.
    /// </summary>
    protected XbeeBasePacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                        IReadOnlyList<byte> networkAddress, byte receiveOptions)
    {
        _xbeeFrame = xbeeFrame;
        _sourceAddress = sourceAddress;
        _networkAddress = networkAddress;
        _receiveOptions = receiveOptions;
    }

    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    public XbeeFrame XbeeFrame
    {
        get => _xbeeFrame;
    }
    
    /// <summary>
    /// 64bit source address.
    /// </summary>
    public IReadOnlyList<byte> SourceAddress
    {
        get => _sourceAddress;
    }

    /// <summary>
    /// 16bit network address.
    /// </summary>
    public IReadOnlyList<byte> NetworkAddress
    {
        get => _networkAddress;
    }

    /// <summary>
    /// Recieve options.
    /// <summary>
    public byte ReceiveOptions
    {
        get => _receiveOptions;
    }
}
