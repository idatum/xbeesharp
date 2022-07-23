namespace XbeeSharp;

/// <summary>
/// Transmit request packet.
/// </summary>
public static class TransmitPacket
{
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
        var frameData = new List<byte>();
        // Packet type
        frameData.Add(XbeeFrame.PacketTypeTransmit);
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
        // Network address.
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
        // Start byte and big endian data length.
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
}
