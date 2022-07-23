namespace XbeeSharp;

/// <summary>
/// Recieve packet.
/// </summary>
public class ReceivePacket : ReceiveBasePacket
{
    /// <summary>
    /// Receive data.
    /// </summary>
    private IReadOnlyList<byte> _receiveData;

    /// <summary>
    /// Constructor.
    /// </summary>
    private ReceivePacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                ushort networkAddress, byte receiveOptions,
                                IReadOnlyList<byte> receiveData)
                                : base(xbeeFrame, sourceAddress, networkAddress, receiveOptions)
    {
        _receiveData = receiveData;
    }

    /// <summary>
    /// Create from XBee frame.
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

        // 64-bit source address.
        var sourceAddress = xbeeFrame.FrameData.Take(4..12).ToList();
        // Network address.
        ushort networkAddress = XbeeFrameBuilder.ToBigEndian(xbeeFrame.FrameData[12], xbeeFrame.FrameData[13]);
        // Receive options.
        var receiveOptions = xbeeFrame.FrameData[14];
        // Receive data
        // 15 byte offset to length of frame + start byte + 2 bytes for length field.
        var receiveDataRange = new Range(DataOffset, xbeeFrame.FrameDataLength + 3);
        var receiveData = xbeeFrame.FrameData.Take(receiveDataRange).ToList();

        packet = new ReceivePacket(xbeeFrame, sourceAddress, networkAddress, receiveOptions, receiveData);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public const byte FrameType = XbeeFrame.PacketTypeReceive;

    /// <summary>
    /// Receive data.
    /// </summary>
    public IReadOnlyList<byte> ReceiveData
    {
        get => _receiveData;
    }
}
