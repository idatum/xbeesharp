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
                                       byte frameId, IReadOnlyList<byte> data, bool escaped = false)
    {
        if (data is null)
        {
            throw new ArgumentException("data");
        }
        xbeeFrame = null;
        ushort dataLen = 0;
        var rawData = new List<byte>();
        // Packet type
        rawData.Add(XbeeFrame.PacketTypeTransmit);
        ++dataLen;
        // Frame ID
        rawData.Add(frameId);
        ++dataLen;
        // Long address.
        foreach (var b in address.LongAddress)
        {
            XbeeFrameBuilder.AppendWithEscape(escaped, rawData, b);
            ++dataLen;
        }
        // Network address.
        foreach (var b in new byte[] { 0xFF, 0xFE })
        {
            rawData.Add(b);
            ++dataLen;
        }
        // Broadcast radius
        rawData.Add(0x00);
        ++dataLen;
        // TX options
        rawData.Add(0x00);
        ++dataLen;
        // Data at offset 17 bytes
        foreach (var b in data)
        {
            XbeeFrameBuilder.AppendWithEscape(escaped, rawData, b);
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
        rawData.InsertRange(0, prefix);
        // Checksum (escape if needed).
        var checksum = XbeeFrameBuilder.CalculateChecksum(rawData, escaped);
        XbeeFrameBuilder.AppendWithEscape(escaped, rawData, checksum);
        if (!XbeeFrameBuilder.ChecksumValid(rawData, escaped))
        {
            throw new InvalidOperationException();
        }
        xbeeFrame = new XbeeFrame(rawData, escaped);
        if (xbeeFrame is null)
        {
            return false;
        }
        return true;
    }
}
