namespace XbeeSharp;

using System.Text;

/// <summary>
/// Extended transmit status packet.
/// </summary>
public class NodeIdentificationPacket : ReceiveBasePacket
{
    /// <summary>
    /// Max node identifier string size.
    /// </summary>
    private const int MaxNodeIdentierStringSize = 21;
    /// <summary>
    /// Remote network address.
    /// <summary>
    private ushort _remoteNetworkAddress;
    /// <summary>
    /// Remote source address.
    /// </summary>
    private XbeeAddress _remoteSourceAddress;
    /// <summar>
    /// Node identifier.
    /// </summary>
    private string _nodeIdentifier;
    /// <summary>
    /// Network device type.
    /// </summary>
    private byte _deviceType;

    private NodeIdentificationPacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                     ushort networkAddress, byte options,
                                     ushort remoteNetworkAddress, IReadOnlyList<byte> remoteSourceAddress,
                                     string nodeIdentifier, byte deviceType)
                                   : base(xbeeFrame, sourceAddress, networkAddress, options)
    {
        _xbeeFrame = xbeeFrame;
        _remoteNetworkAddress = remoteNetworkAddress;
        _remoteSourceAddress = XbeeAddress.Create(remoteSourceAddress);
        _nodeIdentifier = nodeIdentifier;
        _deviceType = deviceType;
    }

    /// <summary>
    /// Create receive packet from XBee frame.
    /// </summary>
    public static bool Parse(out NodeIdentificationPacket? packet, XbeeFrame xbeeFrame)
    {
        packet = null;

        if (xbeeFrame.FrameType != XbeeFrame.PacketTypeNodeIdentification)
        {
            return false;
        }
        // 64-bit source address.
        var sourceAddress = xbeeFrame.FrameData.Take(4..12).ToList();
        // Network address.
        ushort networkAddress = (ushort)(256 * xbeeFrame.FrameData[12] + xbeeFrame.FrameData[13]);
        // Options.
        byte options = xbeeFrame.FrameData[14];
        // Remote network address.
        ushort remoteNetworkAddress = (ushort)(256 * xbeeFrame.FrameData[15] + xbeeFrame.FrameData[16]);
        // Remote source address.
        var remoteSourceAddress = xbeeFrame.FrameData.Take(17..25).ToList();
        // Node identifier string.
        var nodeIdentifierBuilder = new StringBuilder();
        var niSize = 0;
        for (; niSize < MaxNodeIdentierStringSize && xbeeFrame.FrameData[25 + niSize] != 0x00; ++niSize)
        {
            var c = (char)xbeeFrame.FrameData[25 + niSize];
            nodeIdentifierBuilder.Append(c);
        }
        ++niSize;
        // Network device type
        byte deviceType = xbeeFrame.FrameData[27 + niSize];

        packet = new NodeIdentificationPacket(xbeeFrame, sourceAddress, networkAddress, options,
                                              remoteNetworkAddress, remoteSourceAddress, nodeIdentifierBuilder.ToString(),
                                              deviceType);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public const byte FrameType = XbeeFrame.PacketTypeNodeIdentification;

    /// <summary>
    /// Remote network address.
    /// <summary>
    public ushort RemoteNetworkAddress
    {
        get => _remoteNetworkAddress;
    }

    /// <summary>
    /// Remote source address.
    /// <summary>
    public XbeeAddress RemoteSourceAddress
    {
        get => _remoteSourceAddress;
    }

    /// <summary>
    /// Node identifier.
    /// </summary>
    public string NodeIdentifier
    {
        get => _nodeIdentifier;
    }

    /// <summary>
    /// Network device type.
    /// </summary>
    public byte DeviceType
    {
        get => _deviceType;
    }
}
