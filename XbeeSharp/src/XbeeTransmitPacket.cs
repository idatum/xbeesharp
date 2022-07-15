namespace XbeeSharp;

/// <summary>
/// Transmit request packet.
/// </summary>
public class XbeeTransmitPacket : XbeeBasePacket
{
    /// <summary>
    /// Receive data.
    /// </summary>
    private IReadOnlyList<byte> _receiveData;

    /// <summary>
    /// Escaped packet.
    /// <summary>
    private bool _escaped;

    /// <summary>
    /// Construct receive packet.
    /// </summary>
    private XbeeTransmitPacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                IReadOnlyList<byte> networkAddress, byte receiveOptions,
                                IReadOnlyList<byte> receiveData, bool escaped=true)
                                : base(xbeeFrame, sourceAddress, networkAddress, receiveOptions)
    {
        _receiveData = receiveData;
        _escaped = escaped;
    }

    /// <summary>
    /// Create underlying XBee frame.
    /// </summary>
    public static bool CreateXbeeFrame(out XbeeFrame? xbeeFrame, XbeeAddress address,
                                        IReadOnlyList<byte> data, bool escaped=false)
    {
        if (data == null)
        {
            throw new ArgumentException("data");
        }
        xbeeFrame = null;
        ushort dataLen = 0;
        List<byte> frameData = new List<byte>();
        // Packet type
        frameData.Add(XbeeBasePacket.PacketTypeTransmit);
        ++dataLen;
        // Frame ID
        frameData.Add(1);
        ++dataLen;
        // Long address.
        foreach (var b in address.LongAddress)
        {
            XbeeFrameBuilder.AppendWithEscape(escaped, frameData, b);
            ++dataLen;
        }
        // short address.
        foreach (var b in new byte[] {0xFF, 0xFE})
        {
            frameData.Add(b);
            ++dataLen;
        }
        // Broadcast radius
        frameData.Add(0x00);
        ++dataLen;
        // TX options
        frameData.Add(0x00);
        ++dataLen;
        // Data at offset 17 bytes
        foreach (var b in data)
        {
            XbeeFrameBuilder.AppendWithEscape(escaped, frameData, b);
            ++dataLen;
        }
        // Fill remaing frame bytes.
        // Start byte and big endian data length (includes escaped bytes).
        byte dataLenHi = (byte)(dataLen >> 8);
        byte dataLenLo = (byte)(dataLen & 0xFF);
        List<byte> prefix = new List<byte>();
        prefix.Add(XbeeFrame.StartByte);
        XbeeFrameBuilder.AppendWithEscape(escaped, prefix, dataLenHi);
        XbeeFrameBuilder.AppendWithEscape(escaped, prefix, dataLenLo);
        frameData.InsertRange(0, prefix);
        // Checksum
        frameData.Add(XbeeFrameBuilder.CalculateChecksum(frameData, escaped));
        if (!XbeeFrameBuilder.ChecksumValid(frameData, escaped))
        {
            throw new InvalidOperationException();
        }
        xbeeFrame = new XbeeFrame(frameData, escaped);
        if (xbeeFrame == null)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public byte FrameType
    {
        get => XbeeBasePacket.PacketTypeReceive;
    }

    /// <summary>
    /// Receive options bit field.
    /// </summary>
    public IReadOnlyList<byte> ReceiveData
    {
        get => _receiveData;
    }
}
