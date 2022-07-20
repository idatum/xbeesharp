namespace XbeeSharp;

/// <summary>
/// Recieve packet.
/// </summary>
public class ReceivePacket : XbeeBasePacket
{
    /// <summary>
    /// Network address.
    /// </summary>
    private ushort _networkAddress;
    /// <summary>
    /// Receive data.
    /// </summary>
    private IReadOnlyList<byte> _receiveData;

    /// <summary>
    /// Construct receive packet.
    /// </summary>
    private ReceivePacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                ushort networkAddress, byte receiveOptions,
                                IReadOnlyList<byte> receiveData)
                                : base(xbeeFrame, sourceAddress, receiveOptions)
    {
        _networkAddress = networkAddress;
        _receiveData = receiveData;
    }

    /// <summary>
    /// Create receive packet from XBee frame.
    /// </summary>
    public static bool Parse(out ReceivePacket? packet, XbeeFrame xbeeFrame)
    {
        packet = null;
        const int DataOffset = 15;

        if (xbeeFrame.FrameType != ReceivePacket.FrameType ||
            xbeeFrame.FrameDataLength <= DataOffset)
        {
            return false;
        }

        var frameData = new List<byte>(xbeeFrame.FrameData);
        // 64-bit source address.
        var sourceAddress = frameData.GetRange(4, 8);
        // Network address.
        ushort networkAddress = (ushort)(256 * frameData[12] + frameData[13]);
        // Receive options.
        var receiveOptions = frameData[14];
        // Receive data
        // 15 byte offset; length is data frame length - 15 byte offset + start byte + 2 length bytes.
        var receiveData = frameData.GetRange(DataOffset, xbeeFrame.FrameDataLength - DataOffset + 3);

        packet = new ReceivePacket(xbeeFrame, sourceAddress, networkAddress, receiveOptions, receiveData);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public const byte FrameType = XbeeFrame.PacketTypeReceive;

    /// <summary>
    /// Network address.
    /// <summary>
    public ushort NetworkAddress
    {
        get => _networkAddress;
    }
    
    /// <summary>
    /// Receive data.
    /// </summary>
    public IReadOnlyList<byte> ReceiveData
    {
        get => _receiveData;
    }
}
