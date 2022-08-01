namespace XbeeSharp;

/// <summary>
/// XBee API packet.
/// https://www.digi.com/resources/documentation/digidocs/pdfs/90002002.pdf
/// </summary>
public class XbeeFrame
{
    /// <summary>
    /// TX request.
    /// </summary>
    public const byte PacketTypeTransmit = 0x10;
    /// <summary>
    /// Remote AT Command Request
    /// </summary>
    public const byte PacketTypeRemoteAT = 0x17;
    /// <summary>
    /// Modem status.
    /// </summary>
    public const byte PacketTypeModemStatus = 0x8A;
    /// <summary>
    /// Extended Transmit Status
    /// </summary>
    public const byte PacketTypeExtendedTransmitStatus = 0x8B;
    /// <summary>
    /// RX receive.
    /// </summary>
    public const byte PacketTypeReceive = 0x90;
    /// <summary>
    /// RX IO data received.
    /// </summary>
    public const byte PacketTypeReceiveIO = 0x92;
    /// <summary>
    /// Node Identification Indicator.
    /// </summary>
    public const byte PacketTypeNodeIdentification = 0x95;
    /// <summary>
    /// Remote AT Command Response.
    /// <summary>
    public const byte PacketTypeRemoteATCommandResponse = 0x97;
    /// <summary>
    /// Route Record Indicator
    /// </summary>
    public const byte RouteRecordIndicator = 0xA1;
    /// <summary>
    /// Start byte indicating beginning of packet.
    /// </summary>
    public const byte StartByte = 0x7E;
    /// <summary>
    /// Escape
    /// </summary>
    public const byte EscapeByte = 0x7D;
    /// <summary>
    // XON
    /// </summary>
    public const byte Xon = 0x11;
    /// <summary>
    /// XOFF
    /// </summary>
    public const byte Xoff = 0x13;
    /// <summary>
    /// Full packet.
    /// </summary>
    private List<byte> _data;
    /// <summary>
    /// Checksum
    /// </summary>
    private byte _checksum;
    /// <summary>
    /// Data length.
    /// </summary>
    private int _frameDataLength;
    /// <summary>
    /// API frame escaped.
    /// </summary>
    private bool _escaped;

    /// <summary>
    /// Construct from packet data.
    /// </summary>
    public XbeeFrame(List<byte> data, bool escaped=true)
    {
        _data = data;
        // Big-endian
        _frameDataLength = XbeeFrameBuilder.ToBigEndian(data[1], data[2]);
        _checksum = data[data.Count - 1];
        _escaped = escaped;
    }

    /// <summary>
    /// Find offset of start of frame data (first byte after length).
    /// </summary>
    public static int GetDataOffset(IReadOnlyList<byte> data, bool escaped)
    {
        if (!escaped || data.Count < 3)
        {
            return 3;
        }
        // Start at first data length byte.
        var dataOffset = 1;
        if (data[dataOffset] == XbeeFrame.EscapeByte)
        {
            // Skip escape byte.
            ++dataOffset;
            if (dataOffset >= data.Count)
            {
                return dataOffset + 1;
            }
        }
        // Move to second data length byte.
        ++ dataOffset;
        if (data[dataOffset] == XbeeFrame.EscapeByte)
        {
            // Skip escape byte.
            ++dataOffset;
            if (dataOffset >= data.Count)
            {
                return dataOffset;
            }
        }
        // Move to beginning of data.
        ++dataOffset;

        return dataOffset;
    }

    /// <summary>
    /// Whether there were escaped bytes.
    /// </summary>
    public bool Escaped
    {
        get => _escaped;
    }
    
    /// <summary>
    /// All frame data.
    /// </summary>
    public IReadOnlyList<byte> Data
    {
        get => _data;
    }

    /// <summary>
    /// Frame type.
    /// </summary>
    public byte FrameType
    {
        // Unescaped offset for frame type is 3.
        get
        {
            if (_escaped)
            {
                return _data[GetDataOffset(_data, _escaped)];
            }
            else
            {
                return _data[3];
            }
        }
    }

    /// <summary>
    /// Frame checksum.
    /// </summary>
    public byte FrameChecksum
    {
        get => _checksum;
    }

    /// <summary>
    /// Frame data length.
    /// </summary>
    public int FrameDataLength
    {
        get => _frameDataLength;
    }
}
