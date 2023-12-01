namespace XbeeTests;

public class XbeeFrameTest
{
    private static readonly byte[] ValidRXPacket = new byte[] {0x7E, 0x00, 0x1E, 0x90, 0x00, 0x14, 0xA1, 0x00, 0x40, 0xC3, 0x54,
                                                               0x9D, 0xF1, 0xA8, 0x02, 0x43, 0x3D, 0x32, 0x34, 0x2E, 0x39, 0x30, 0x26, 0x50,
                                                               0x3D, 0x31, 0x30, 0x32, 0x37, 0x2E, 0x36, 0x36, 0x0D, 0x8A};
    private static readonly byte[] EscapedTXPacket = new byte[] {0x7E, 0x00, XbeeFrame.EscapeByte, 0x31, 0x10, 0x01,
                                                                  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFE,
                                                                  0x00, 0x00, 0x7D, 0x31, XbeeFrame.EscapeByte, 0x33, XbeeFrame.EscapeByte, 0x5E, 0x51};
    private static readonly byte[] EscapedChecksumTxPacket = new byte[] {0x7E, 0x00, 0x0F, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                                          0xFF, 0xFE, 0x00, 0x00, 0xE0, 0x7D, 0x31};

    private static readonly byte[] ChecksumEscapeBytePacket = new byte[] {0x7E, 0x00, 0x0F, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                                             0xFF, 0xFE, 0x00, 0x00, 0x74, 0x7D};

    [Fact]
    public void XBeeFrameBuilderCtorEscaped()
    {
        var xbeeFrameBuilder = new XbeeFrameBuilder(true);
        Xunit.Assert.NotNull(xbeeFrameBuilder);
        Xunit.Assert.True(xbeeFrameBuilder.Escaped);
    }

    [Fact]
    public void XBeeFrameBuilderCtorDefault()
    {
        // Default is escaped == true.
        XBeeFrameBuilderCtorEscaped();
    }


    [Fact]
    public void XBeeFrameBuilderCtor()
    {
        var xbeeFrameBuilder = new XbeeFrameBuilder(false);
        Xunit.Assert.NotNull(xbeeFrameBuilder);
        Xunit.Assert.False(xbeeFrameBuilder.Escaped);
    }

    [Fact]
    public void FullPacketFrameCtorUnescaped()
    {
        var xbeeFrame = new XbeeFrame(new List<byte>(ValidRXPacket), false);
        Xunit.Assert.NotNull(xbeeFrame.Data);
        Xunit.Assert.Equal(0x1E, xbeeFrame.FrameDataLength);
        Xunit.Assert.Equal(0x8A, xbeeFrame.FrameChecksum);
        Xunit.Assert.Equal(xbeeFrame.FrameDataLength, ValidRXPacket.Length - 4);
    }

    [Fact]
    public void FullPacketFrameCtorEscaped()
    {
        var xbeeFrame = new XbeeFrame(new List<byte>(EscapedTXPacket), true);
        Xunit.Assert.NotNull(xbeeFrame.Data);
        // Data length is escaped (0x11).
        Xunit.Assert.NotEqual(0x11, xbeeFrame.FrameDataLength);
        Xunit.Assert.Equal(XbeeFrame.EscapeByte, xbeeFrame.FrameDataLength);
    }


    [Fact]
    public void FullPacketChecksumBuilderEscaped()
    {
        var xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidRXPacket, true);

        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.Equal(xbeeFrame.FrameDataLength, ValidRXPacket.Length - 4);
        }
    }

    [Fact]
    public void FullPacketChecksumBuilderUnescaped()
    {
        var xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidRXPacket, false);

        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.Equal(xbeeFrame.FrameDataLength, ValidRXPacket.Length - 4);
        }
    }

    [Fact]
    public void FullPacketEscapedChecksumBuilder()
    {
        Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(EscapedChecksumTxPacket, true));

        var xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(EscapedChecksumTxPacket, true);

        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            // 19 bytes total - delimiter - length - checksum; don't count 0x7d byte for escaped checksum.
            Xunit.Assert.Equal(15, xbeeFrame.FrameDataLength);
        }
    }


    [Fact]
    public void FullPacketInvalidChecksumBuilderEscaped()
    {
        var invalidChecksumPacket = new List<byte>(ValidRXPacket);
        invalidChecksumPacket[invalidChecksumPacket.Count - 1] = 0x00;

        Xunit.Assert.Throws<InvalidOperationException>(() => XbeeTestUtils.CreateFrameFromBuilder(invalidChecksumPacket, true));
    }

    [Fact]
    public void FullPacketValidCalculatedChecksumUnescaped()
    {
        var packet = ValidRXPacket;
        var escaped = false;

        var xbeeFrameBuilder = new XbeeFrameBuilder(escaped);
        Xunit.Assert.Equal(packet[0], XbeeFrame.StartByte);
        XbeeFrame? xbeeFrame = null;
        byte calculatedChecksum = 0;
        foreach (var nextByte in packet)
        {
            xbeeFrameBuilder.Append(nextByte);
            if (xbeeFrameBuilder.FrameComplete)
            {
                calculatedChecksum = XbeeFrameBuilder.CalculateChecksum(xbeeFrameBuilder.Data.ToList().GetRange(0, xbeeFrameBuilder.Data.Count - 1), escaped);
                if (!XbeeFrameBuilder.ChecksumValid(xbeeFrameBuilder.Data, escaped))
                {
                    throw new InvalidOperationException();
                }
                xbeeFrame = xbeeFrameBuilder.ToXbeeFrame();
            }
        }
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.Equal(calculatedChecksum, xbeeFrame.FrameChecksum);
        }
    }

    [Fact]
    public void FullPacketValidCalculatedChecksumEscaped()
    {
        var packet = EscapedTXPacket;
        var escaped = true;

        var xbeeFrameBuilder = new XbeeFrameBuilder(escaped);
        Xunit.Assert.Equal(packet[0], XbeeFrame.StartByte);
        XbeeFrame? xbeeFrame = null;
        byte calculatedChecksum = 0;
        foreach (var nextByte in packet)
        {
            xbeeFrameBuilder.Append(nextByte);
            if (xbeeFrameBuilder.FrameComplete)
            {
                calculatedChecksum = XbeeFrameBuilder.CalculateChecksum(xbeeFrameBuilder.Data.ToList().GetRange(0, xbeeFrameBuilder.Data.Count - 1), escaped);
                Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrameBuilder.Data, escaped));
                xbeeFrame = xbeeFrameBuilder.ToXbeeFrame();
            }
        }
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.Equal(calculatedChecksum, xbeeFrame.FrameChecksum);
        }
    }

    [Fact]
    public void FrameTypeUnscaped()
    {
        var xbeeFrame = new XbeeFrame(new List<byte>(ValidRXPacket), true);
        Xunit.Assert.NotNull(xbeeFrame.Data);
        Xunit.Assert.Equal(XbeeFrame.PacketTypeReceive, xbeeFrame.FrameType);
    }

    [Fact]
    public void FrameTypeEscaped()
    {
        var xbeeFrame = new XbeeFrame(new List<byte>(EscapedTXPacket), true);
        Xunit.Assert.NotNull(xbeeFrame.Data);
        Xunit.Assert.Equal(XbeeFrame.PacketTypeTransmit, xbeeFrame.FrameType);
    }

    [Fact]
    public void ChecksumValueIsEscapeBytePacket()
    {
        var xbeeFrame = new XbeeFrame(new List<byte>(ChecksumEscapeBytePacket), false);
        Xunit.Assert.NotNull(xbeeFrame.Data);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, true));
        }
    }
}
