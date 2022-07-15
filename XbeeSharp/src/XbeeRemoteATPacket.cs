namespace XbeeSharp;

/// <summary>
/// Transmit request packet.
/// </summary>
public class XbeeRemoteATPacket : XbeeBasePacket
{
    /// <summary>
    /// AT command.
    /// </summary>
    private byte[] _command;
    /// <summary>
    /// Parameter value.
    /// </summary>
    private IReadOnlyList<byte> _parameterValue;

    /// <summary>
    /// Construct receive packet.
    /// </summary>
    private XbeeRemoteATPacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                IReadOnlyList<byte> networkAddress, byte receiveOptions,
                                byte[] command, IReadOnlyList<byte> parameterValue)
                                : base(xbeeFrame, sourceAddress, networkAddress, receiveOptions)
    {
        _command = command;
        _parameterValue = parameterValue;
    }

    /// <summary>
    /// Create underlying XBee frame.
    /// </summary>
    public static bool CreateXbeeFrame(out XbeeFrame? xbeeFrame, XbeeAddress address, byte[] command,
                                        IReadOnlyList<byte> parameterValue, bool escaped)
    {
        if (command == null || command.Length != 2)
        {
            throw new ArgumentException("command");
        }
        xbeeFrame = null;
        ushort dataLen = 0;
        List<byte> frameData = new List<byte>();
        // Packet type
        frameData.Add(XbeeBasePacket.PacketRemoteAT);
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
            XbeeFrameBuilder.AppendWithEscape(escaped, frameData, b);
            ++dataLen;
        }
        // Remote command options: 0x02 for apply changes.
        frameData.Add(0x02);
        ++dataLen;
        // AT command
        frameData.Add(command[0]);
        frameData.Add(command[1]);
        dataLen += 2;
        // Parameter value at offset 18 bytes
        foreach (var b in parameterValue)
        {
            XbeeFrameBuilder.AppendWithEscape(escaped, frameData, b);
            ++dataLen;
        }
        // Fill remaining frame bytes.
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
        get => XbeeBasePacket.PacketRemoteAT;
    }

    /// <summary>
    /// AT command.
    /// </summary>
    public byte[] Command
    {
        get => _command;
    }

    /// <summary>
    /// Parameter value.
    /// </summary>
    public IReadOnlyList<byte> ParameterValue
    {
        get => _parameterValue;
    }
}
