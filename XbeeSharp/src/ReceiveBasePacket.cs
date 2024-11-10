namespace XbeeSharp;

/// <summary>
/// Common receive packet fields.
/// </summary>
/// <remarks>
/// Constructor.
/// </remarks>
public abstract class ReceiveBasePacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                            ushort networkAddress, byte receiveOptions)
{
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    protected XbeeFrame _xbeeFrame = xbeeFrame;
    /// <summary>
    /// 64bit source address.
    /// </summary>
    protected XbeeAddress _sourceAddress = XbeeAddress.Create(sourceAddress);
    /// <summary>
    /// Network address.
    /// </summary>
    protected ushort _networkAddress = networkAddress;
    /// <summary>
    /// Receive options bit field.
    /// </summary>
    protected byte _receiveOptions = receiveOptions;

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
