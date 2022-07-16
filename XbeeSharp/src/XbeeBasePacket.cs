namespace XbeeSharp;

/// <summary>
/// Common ZigBee packet information.
/// </summary>
public abstract class XbeeBasePacket    
{
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    protected XbeeFrame _xbeeFrame;
    /// <summary>
    /// 64bit source address.
    /// </summary>
    protected IReadOnlyList<byte> _sourceAddress;
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
                             byte receiveOptions)
    {
        _xbeeFrame = xbeeFrame;
        _sourceAddress = sourceAddress;
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
    /// Recieve options.
    /// <summary>
    public byte ReceiveOptions
    {
        get => _receiveOptions;
    }
}
