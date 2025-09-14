namespace XbeeSharp;

/// <summary>
/// XBee frame builder.
/// </summary>
public class XbeeFrameBuilder
{
    private List<byte> _data = [];
    private readonly bool _escaped;
    private bool _escapeNextByte;
    private int _dataLength;

    /// <summary>
    /// Construct with escaped flag.
    /// </summary>
    public XbeeFrameBuilder(bool escaped)
    {
        _escaped = escaped;
        Reset();
    }

    /// <summary>
    /// Calculate checksum byte.
    /// </summary>
    public static byte CalculateChecksum(IReadOnlyList<byte> data, bool escaped)
    {
        var dataOffset = XbeeFrame.GetDataOffset(data, escaped);
        int total = 0;
        
        if (!escaped)
        {
            // Fast path for non-escaped data
            for (var i = dataOffset; i < data.Count; ++i)
            {
                total += data[i];
            }
        }
        else
        {
            // Escaped data path
            bool nextByteEscaped = false;
            for (var i = dataOffset; i < data.Count; ++i)
            {
                byte b = data[i];
                if (b == XbeeFrame.EscapeByte && !nextByteEscaped)
                {
                    nextByteEscaped = true;
                    continue;
                }
                total += nextByteEscaped ? (byte)(0x20 ^ b) : b;
                nextByteEscaped = false;
            }
        }
        
        return (byte)(0xFF - (byte)(total & 0xFF));
    }

    /// <summary>
    /// Validate packet checksum.
    /// </summary>
    public static bool ChecksumValid(IReadOnlyList<byte> data, bool escaped)
    {
        var dataOffset = XbeeFrame.GetDataOffset(data, escaped);
        int total = 0;
        int lastIndex = data.Count - 1;
        
        if (!escaped)
        {
            // Fast path for non-escaped data
            for (var i = dataOffset; i <= lastIndex; ++i)
            {
                total += data[i];
            }
            return (total & 0xFF) == 0xFF;
        }
        
        // Escaped data path
        bool nextByteEscaped = false;
        for (var i = dataOffset; i <= lastIndex; ++i)
        {
            byte b = data[i];
            if (b == XbeeFrame.EscapeByte && !nextByteEscaped && i < lastIndex)
            {
                nextByteEscaped = true;
                continue;
            }
            total += nextByteEscaped ? (byte)(0x20 ^ b) : b;
            nextByteEscaped = false;
        }
        
        return (total & 0xFF) == 0xFF;
    }

    /// <summary>
    /// Append byte; escape if needed.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool AppendWithEscape(bool escaped, IList<byte> data, byte b)
    {
        if (escaped && ShouldEscape(b))
        {
            data.Add(XbeeFrame.EscapeByte);
            data.Add((byte)(0x20 ^ b));
            return true;
        }
        data.Add(b);
        return false;
    }

    /// <summary>
    /// Whether byte should be escaped for packets expecting escaped bytes.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool ShouldEscape(byte b)
    {
        // Use bitwise OR for branchless comparison
        return b == XbeeFrame.StartByte | 
               b == XbeeFrame.EscapeByte | 
               b == XbeeFrame.Xon | 
               b == XbeeFrame.Xoff;
    }

    /// <summary>
    /// Helper to create a ushort from big-endian byte sequence.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static ushort ToBigEndian(byte hi, byte lo) => (ushort)((hi << 8) | lo);

    /// <summary>
    /// Whether to expect escape byte in data.
    /// </summary>
    public bool Escaped => _escaped;

    /// <summary>
    /// Add next received byte in construction of packet.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public bool Append(byte nextByte)
    {
        if (nextByte == XbeeFrame.StartByte)
        {
            Reset();
            _data.Add(nextByte);
            return false; // New frame
        }
        
        if (_escaped && nextByte == XbeeFrame.EscapeByte)
        {
            _escapeNextByte = true;
            return true;
        }
        
        if (_escapeNextByte)
        {
            _escapeNextByte = false;
            _data.Add((byte)(nextByte ^ 0x20));
            return true;
        }
        
        _data.Add(nextByte);
        return true;
    }

    /// <summary>
    /// Raw packet data.
    /// </summary>
    public IReadOnlyList<byte> Data => _data;

    /// <summary>
    /// Whether the length of data matches data length field.
    /// </summary>
    public bool FrameComplete
    {
        get
        {
            var dataOffset = XbeeFrame.GetDataOffset(_data, _escaped);
            if (_data.Count < dataOffset)
                return false;

            if (_dataLength == 0 && _data.Count >= 3)
            {
                _dataLength = (_data[1] << 8) | _data[2];
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
            throw new InvalidOperationException("Frame is not complete.");

        return new XbeeFrame(_data, _escaped);
    }

    /// <summary>
    /// Reset state.
    /// </summary>
    public void Reset()
    {
        _data.Clear(); // More efficient than creating new list
        _dataLength = 0;
        _escapeNextByte = false;
    }

    /// <summary>
    /// Extract frame data length field.
    /// </summary>
    private static ushort GetFrameDataLength(IReadOnlyList<byte> data, bool escaped)
    {
        if (!escaped)
            return ToBigEndian(data[1], data[2]);

        byte dataLenHi = 0;
        byte dataLenLo = 0;
        int offset = 1;
        
        if (data[offset] == XbeeFrame.EscapeByte)
        {
            dataLenHi = (byte)(0x20 ^ data[++offset]);
        }
        else
        {
            dataLenHi = data[offset];
        }
        
        if (data[++offset] == XbeeFrame.EscapeByte)
        {
            dataLenLo = (byte)(0x20 ^ data[++offset]);
        }
        else
        {
            dataLenLo = data[offset];
        }
        
        return ToBigEndian(dataLenHi, dataLenLo);
    }
}
