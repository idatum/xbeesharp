namespace XbeeSharp;

/// <summary>
/// XBee API packet.
/// </summary>
public class XbeeFrame
{
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
    private List<byte> _frameData;
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
    public XbeeFrame(List<byte> frameData, bool escaped=true)
    {
        _frameData = frameData;
        // Big-endian
        _frameDataLength = 0xff * frameData[1] + frameData[2];
        _checksum = frameData[frameData.Count - 1];
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
    /// Whether escaped packet.
    /// </summary>
    public bool Escaped
    {
        get => _escaped;
    }
    
    /// <summary>
    /// Packet data.
    /// </summary>
    public IReadOnlyList<byte> FrameData
    {
        get => _frameData;
    }

    /// <summary>
    /// Packet type.
    /// </summary>
    public byte FrameType
    {
        // Unescaped offset for frame type is 3.
        get
        {
            if (_escaped)
            {
                return _frameData[GetDataOffset(_frameData, _escaped)];
            }
            else
            {
                return _frameData[3];
            }
        }
    }

    /// <summary>
    /// Checksum.
    /// </summary>
    public byte FrameChecksumSum
    {
        get => _checksum;
    }

    /// <summary>
    /// Packet data length.
    /// </summary>
    public int FrameDataLength
    {
        get => _frameDataLength;
    }
}
