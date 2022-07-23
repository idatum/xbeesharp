namespace XbeeSharp;

/// <summary>
/// Extended transmit status packet.
/// </summary>
public class ExtendedTransmitStatusPacket
{
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    private XbeeFrame _xbeeFrame;
    /// <summary>
    /// Frame ID.
    /// </summary>
    private byte _frameId;
    /// <summary>
    /// Network address.
    /// <summary>
    private ushort _networkAddress;
    /// <summary>
    /// Transmit retry count.
    /// </summary>
    private byte _transmitRetryCount;
    /// <summary>
    /// Delivery status.
    /// </summary>
    private byte _deliveryStatus;
    /// <summary>
    /// Discovery status.
    /// </summary>
    private byte _discoveryStatus;

    /// <summary>
    /// Constructor.
    /// </summary>
    private ExtendedTransmitStatusPacket(XbeeFrame xbeeFrame, byte frameId, ushort networkAddress,
                                   byte transmitRetryCount, byte deliveryStatus, byte discoveryStatus)
    {
        _xbeeFrame = xbeeFrame;
        _frameId = frameId;
        _networkAddress = networkAddress;
        _transmitRetryCount = transmitRetryCount;
        _deliveryStatus = deliveryStatus;
        _discoveryStatus = discoveryStatus;
    }

    /// <summary>
    /// Create from XBee frame.
    /// </summary>
    public static bool Parse(out ExtendedTransmitStatusPacket? packet, XbeeFrame xbeeFrame)
    {
        packet = null;

        if (xbeeFrame.FrameType != XbeeFrame.PacketTypeExtendedTransmitStatus)
        {
            return false;
        }
        // Frame ID.
        byte frameId = xbeeFrame.FrameData[4];
        // Network address.
        ushort networkAddress = XbeeFrameBuilder.ToBigEndian(xbeeFrame.FrameData[5], xbeeFrame.FrameData[6]);
        // Transmit retry count.
        byte transmitRetryCount = xbeeFrame.FrameData[7];
        // Delivery status.
        byte deliveryStatus = xbeeFrame.FrameData[8];
        // Discovery status.
        byte discoveryStatus = xbeeFrame.FrameData[9];

        packet = new ExtendedTransmitStatusPacket(xbeeFrame, frameId, networkAddress, transmitRetryCount, deliveryStatus, discoveryStatus);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public const byte FrameType = XbeeFrame.PacketTypeExtendedTransmitStatus;

    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    public XbeeFrame XbeeFrame
    {
        get => _xbeeFrame;
    }

    /// <summary>
    /// Frame ID.
    /// </summary>
    public byte FrameId
    {
        get => _frameId;
    }

    /// <summary>
    /// Network address.
    /// <summary>
    public ushort NetworkAddress
    {
        get => _networkAddress;
    }

    /// <summary>
    /// Transmit retry count.
    /// </summary>
    public byte TransmitRetryCount
    {
        get => _transmitRetryCount;
    }

    /// <summary>
    /// Delivery status.
    /// </summary>
    public byte DeliveryStatus
    {
        get => _deliveryStatus;
    }

    /// <summary>
    /// Discovery status.
    /// </summary>
    public byte DiscoveryStatus
    {
        get => _discoveryStatus;
    }
}
