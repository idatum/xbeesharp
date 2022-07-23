namespace XbeeSharp;

/// <summary>
/// XBee frame builder.
/// </summary>
public class XbeeFrameBuilder
{
    /// <summary>
    /// Frame data.
    /// </summary>
    private List<byte> _data = new();
    /// <summary>
    /// Whether to expect escape byte in data.
    /// </summary>
    private bool _escaped;
    /// <summary>
    /// Keep state of escaped byte.
    /// </summary>
    private bool _escapeNextByte;
    /// <summary>
    /// Value of data length field (2 bytes).
    /// </summary>
    private int _dataLength;

    /// <summary>
    /// Construct with escaped flag.
    /// </summary>
    public XbeeFrameBuilder(bool escaped)
    {
        Reset();
        _escaped = escaped;
    }

    /// <summary>
    /// Calculate checksum byte.
    /// </summary>
    public static byte CalculateChecksum(IReadOnlyList<byte> data, bool escaped)
    {
        var dataOffset = XbeeFrame.GetDataOffset(data, escaped);
        int total = 0;
        bool nextByteEscaped = false;
        for (var i = dataOffset; i < data.Count; ++i)
        {
            if (escaped && data[i] == XbeeFrame.EscapeByte)
            {
                nextByteEscaped = true;
                continue;
            }
            if (nextByteEscaped)
            {
                total += (byte)(0x20 ^ data[i]);
                nextByteEscaped = false;
            }
            else
            {
                total += data[i];
            }
        }
        byte checksum = (byte)(0xFF & total);
        checksum = (byte)(0xFF - checksum);

        return checksum;
    }

    /// <summary>
    /// Validate packet checksum.
    /// </summary>
    public static bool ChecksumValid(IReadOnlyList<byte> data, bool escaped)
    {
        var dataOffset = XbeeFrame.GetDataOffset(data, escaped);
        var checksum = data[data.Count - 1];
        int total = 0;
        bool nextByteEscaped = false;
        for (var i = dataOffset; i < data.Count - 1; ++i)
        {
            if (escaped && data[i] == XbeeFrame.EscapeByte)
            {
                nextByteEscaped = true;
                continue;
            }
            if (nextByteEscaped)
            {
                total += (byte)(0x20 ^ data[i]);
                nextByteEscaped = false;
            }
            else
            {
                total += data[i];
            }
        }

        total += checksum;
        total &= 0xff;

        return total == 0xff;
    }

    /// <summary>
    /// Append byte; escape if needed.
    /// </summary>
    public static bool AppendWithEscape(bool escaped, IList<byte> frameData, byte b)
    {
        if (escaped && ShouldEscape(b))
        {
            frameData.Add(XbeeFrame.EscapeByte);
            frameData.Add((byte)(0x20 ^ b));
            return true;
        }
        else
        {
            frameData.Add(b);
        }
        return false;
    }

    /// <summary>
    /// Whether byte should be escaped.
    /// </summary>
    public static bool ShouldEscape(byte b)
    {
        return XbeeFrame.StartByte == b ||
                XbeeFrame.EscapeByte == b ||
                XbeeFrame.Xon == b ||
                XbeeFrame.Xoff == b;
    }

    /// <summary>
    /// Helper to create a ushort from big-endian byte sequence.
    /// </summary>
    public static ushort ToBigEndian(byte hi, byte lo)
    {
        return (ushort)((hi << 8) + lo);
    }

    /// <summary>
    /// Whether to expect escape byte in data.
    /// </summary>
    public bool Escaped
    {
        get => _escaped;
        set => _escaped = value;
    }

    /// <summary>
    /// Add next received byte in construction of packet.
    /// Frame field bytes (unescaped):
    /// 1 - Start delimiter (0x7E)
    /// 2,3 - Length (big-endian)
    /// 4,n - API-specific structure of size n
    /// n + 1 - Checksum.
    /// If escaped (AP parameter == 2), the escape is removed (0x7D) and next
    /// byte XOR'd with 0x20 when reading packet.
    /// </summary>
    public bool Append(byte nextByte)
    {
        if (XbeeFrame.StartByte == nextByte)
        {
            Reset();
            _data.Add(nextByte);
            // Indicate not same partial frame i.e. new frame.
            return false;
        }
        else if (_escaped && XbeeFrame.EscapeByte == nextByte)
        {
            _escapeNextByte = true;
        }
        else if (_escapeNextByte)
        {
            _escapeNextByte = false;
            var unescapedByte = nextByte ^ 0x20;
            _data.Add((byte)unescapedByte);
        }
        else
        {
            _data.Add((byte)nextByte);
        }
        // Indicate same partial frame.
        return true;
    }

    /// <summary>
    /// Raw packet data.
    /// </summary>
    public IReadOnlyList<byte> Data
    {
        get => _data;
    }

    /// <summary>
    /// Whether the length of data matches data length field.
    /// </summary>
    public bool FrameComplete
    {
        get
        {
            var dataOffset = XbeeFrame.GetDataOffset(_data, _escaped);
            if (_data.Count < dataOffset)
            {
                return false;
            }
            else if (_dataLength == 0)
            {
                _dataLength = 0xff * _data[1] + _data[2];
            }
            
            return _dataLength == (_data.Count - 4);
        }
    }

    /// <summary>
    /// Construct XbeeFrame.
    /// </summary>
    public XbeeFrame ToXbeeFrame()
    {
        if (!FrameComplete)
        {
            throw new InvalidOperationException();
        }

        return new XbeeFrame(_data, _escaped);
    }

    /// <summary>
    /// Reset state.
    /// </summary>
    public void Reset()
    {
        _data = new List<byte>();
        _dataLength = 0;
        _escapeNextByte = false;
    }

    /// <summary>
    /// Extract data length field.
    /// </summary>
    private static ushort GetDataLength(IReadOnlyList<byte> data, bool escaped)
    {
        if (!escaped)
        {
            return XbeeFrameBuilder.ToBigEndian(data[1], data[2]);
        }

        byte dataLenHi = 0;
        byte dataLenLo = 0;
        var dataLenOffset = 1;
        if (data[dataLenOffset] == XbeeFrame.EscapeByte)
        {
            ++dataLenOffset;
            dataLenHi = (byte)(0x20 ^ data[dataLenOffset]);
        }
        ++dataLenOffset;
        if (data[dataLenOffset] == XbeeFrame.EscapeByte)
        {
            ++dataLenOffset;
            dataLenLo = (byte)(0x20 ^ data[dataLenOffset]);
        }
        var dataLen = XbeeFrameBuilder.ToBigEndian(dataLenHi, dataLenLo);

        return dataLen;
    }
}
