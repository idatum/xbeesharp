namespace XbeeSharp;

/// <summary>
/// Extended transmit status packet.
/// </summary>
public class ExtendedTransmitStatusPacket
{
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    private readonly XbeeFrame _xbeeFrame;
    /// <summary>
    /// Frame ID.
    /// </summary>
    private readonly byte _frameId;
    /// <summary>
    /// Network address.
    /// <summary>
    private readonly ushort _networkAddress;
    /// <summary>
    /// Transmit retry count.
    /// </summary>
    private readonly byte _transmitRetryCount;
    /// <summary>
    /// Delivery status.
    /// </summary>
    private readonly byte _deliveryStatus;
    /// <summary>
    /// Discovery status.
    /// </summary>
    private readonly byte _discoveryStatus;

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
        byte frameId = xbeeFrame.Data[4];
        // Network address.
        ushort networkAddress = XbeeFrameBuilder.ToBigEndian(xbeeFrame.Data[5], xbeeFrame.Data[6]);
        // Transmit retry count.
        byte transmitRetryCount = xbeeFrame.Data[7];
        // Delivery status.
        byte deliveryStatus = xbeeFrame.Data[8];
        // Discovery status.
        byte discoveryStatus = xbeeFrame.Data[9];

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
    /// From Digi documentation:
    /// 0x00 = Success
    /// 0x01 = MAC ACK failure
    /// 0x02 = CCA/LBT failure
    /// 0x03 = Indirect message unrequested / no spectrum available
    /// 0x15 = Invalid destination endpoint
    /// 0x21 = Network ACK failure
    /// 0x22 = Not joined to network
    /// 0x23 = Self-addressed
    /// 0x24 = Address not found
    /// 0x25 = Route not found
    /// 0x26 = Broadcast source failed to hear a neighbor relay the message
    /// 0x2B = Invalid binding table index
    /// 0x2C = Resource error - lack of free buffers, timers, etc.
    /// 0x2D = Attempted broadcast with APS transmission
    /// 0x2E = Attempted unicast with APS transmission, but EE = 0
    /// 0x31 = Internal resource error
    /// 0x32 = Resource error lack of free buffers, timers, etc.
    /// 0x34 = No Secure Session connection
    /// 0x35 = Encryption failure
    /// 0x74 = Data payload too large
    /// 0x75 = Indirect message unrequested
    /// </summary>
    public byte DeliveryStatus
    {
        get => _deliveryStatus;
    }

    /// <summary>
    /// Discovery status.
    /// From Digi documentation:
    /// 0x00 = No discovery overhead
    /// 0x01 = Zigbee address discovery
    /// 0x02 = Route discovery
    /// 0x03 = Zigbee address and route discovery
    /// 0x40 = Zigbee end device extended timeout
    /// </summary>
    public byte DiscoveryStatus
    {
        get => _discoveryStatus;
    }
}
