namespace XbeeSharp;

/// <summary>
/// Common receive packet fields.
/// </summary>
public abstract class ReceiveBasePacket    
{
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    protected XbeeFrame _xbeeFrame;
    /// <summary>
    /// 64bit source address.
    /// </summary>
    protected XbeeAddress _sourceAddress;
    /// <summary>
    /// Network address.
    /// </summary>
    protected ushort _networkAddress;
    /// <summary>
    /// Receive options bit field.
    /// </summary>
    protected byte _receiveOptions;
    /// <summary>
    /// Receive data.
    /// </summary>

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ReceiveBasePacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                ushort networkAddress, byte receiveOptions)
    {
        _xbeeFrame = xbeeFrame;
        _sourceAddress = XbeeAddress.Create(sourceAddress);
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
    public XbeeAddress SourceAddress
    {
        get => _sourceAddress;
    }

    /// <summary>
    /// Network address.
    /// <summary>
    public ushort NetworkAddress
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
