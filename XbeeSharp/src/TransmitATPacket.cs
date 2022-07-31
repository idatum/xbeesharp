namespace XbeeSharp;

/// <summary>
/// Remote AT command request packet.
/// </summary>
public static class TransmitATPacket
{
    /// <summary>
    /// Create underlying XBee frame.
    /// </summary>
    public static bool CreateXbeeFrame(out XbeeFrame? xbeeFrame, XbeeAddress address, byte frameId,
                                       byte[] command, IReadOnlyList<byte> parameterValue, bool escaped)
    {
        if (command == null || command.Length != 2)
        {
            throw new ArgumentException("command");
        }
        xbeeFrame = null;
        ushort dataLen = 0;
        var rawData = new List<byte>();
        // Packet type
        rawData.Add(XbeeFrame.PacketTypeRemoteAT);
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
        // short address.
        foreach (var b in new byte[] {0xFF, 0xFE})
        {
            XbeeFrameBuilder.AppendWithEscape(escaped, rawData, b);
            ++dataLen;
        }
        // Remote command options: 0x02 for apply changes.
        rawData.Add(0x02);
        ++dataLen;
        // AT command
        rawData.Add(command[0]);
        rawData.Add(command[1]);
        dataLen += 2;
        // Parameter value at offset 18 bytes
        foreach (var b in parameterValue)
        {
            XbeeFrameBuilder.AppendWithEscape(escaped, rawData, b);
            ++dataLen;
        }
        // Fill remaining frame bytes.
        // Start byte and big endian data length (includes escape bytes).
        byte dataLenHi = (byte)(dataLen >> 8);
        byte dataLenLo = (byte)(dataLen & 0xFF);
        var prefix = new List<byte>();
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
        if (xbeeFrame == null)
        {
            return false;
        }
        return true;
    }
}
