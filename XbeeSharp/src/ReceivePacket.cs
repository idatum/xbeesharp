namespace XbeeSharp;

/// <summary>
/// Recieve packet.
/// </summary>
public class ReceivePacket : XbeeBasePacket
{
    /// <summary>
    /// Receive data.
    /// </summary>
    private IReadOnlyList<byte> _receiveData;

    /// <summary>
    /// Construct receive packet.
    /// </summary>
    private ReceivePacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                byte receiveOptions,
                                IReadOnlyList<byte> receiveData)
                                : base(xbeeFrame, sourceAddress, receiveOptions)
    {
        _receiveData = receiveData;
    }

    /// <summary>
    /// Create receive packet from XBee frame.
    /// </summary>
    public static bool Parse(out ReceivePacket? packet, XbeeFrame xbeeFrame)
    {
        packet = null;
        const int DataOffset = 15;

        if (xbeeFrame.FrameType != XbeeFrame.PacketTypeReceive ||
            xbeeFrame.FrameDataLength <= DataOffset)
        {
            return false;
        }

        var frameData = new List<byte>(xbeeFrame.FrameData);
        // 64-bit source address.
        var sourceAddress = frameData.GetRange(4, 8);
        // 16-bit source network address.
        var networkAddress = frameData.GetRange(12, 2);
        // Receive option.
        var receiveOption = frameData[14];
        // Receive data
        // 15 byte offset; length is data frame length - 15 byte offset + start byte + 2 length bytes.
        var receiveData = frameData.GetRange(DataOffset, xbeeFrame.FrameDataLength - DataOffset + 3);

        packet = new ReceivePacket(xbeeFrame, sourceAddress, receiveOption, receiveData);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public byte FrameType
    {
        get => XbeeFrame.PacketTypeReceive;
    }

    /// <summary>
    /// Receive data.
    /// </summary>
    public IReadOnlyList<byte> ReceiveData
    {
        get => _receiveData;
    }
}
