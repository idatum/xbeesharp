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
        var frameData = new List<byte>();
        // Packet type
        frameData.Add(XbeeFrame.PacketTypeRemoteAT);
        ++dataLen;
        // Frame ID
        frameData.Add(frameId);
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
        // Start byte and big endian data length (includes escape bytes).
        byte dataLenHi = (byte)(dataLen >> 8);
        byte dataLenLo = (byte)(dataLen & 0xFF);
        var prefix = new List<byte>();
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
