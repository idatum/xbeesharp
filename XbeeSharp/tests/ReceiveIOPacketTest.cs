namespace XbeeTests;

public class ReceivePacketIOTests
{
    private static readonly byte[] ValidIOPacket = new byte[] {XbeeFrame.StartByte, 0x00, 0x16, 0x92, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34, 0x56,
                                                               0x78, 0x87, 0xAC, 0x01, 0x01, 0x00, 0x38, 0x06, 0x00, 0x28, 0x02, 0x25, 0x00,
                                                               0xF8, 0xEA};

    [Fact]
    public void CreateFromFrameBuilderUnescaped()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidIOPacket, false);
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.FrameData, false));
        }
    }

    [Fact]
    public void ValidPacketUnescaped()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidIOPacket, false);
        Xunit.Assert.NotNull(xbeeFrame);
        ReceiveIOPacket? packet;
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(ReceiveIOPacket.Parse(out packet, xbeeFrame));
            if (packet != null)
            {
                Xunit.Assert.Equal(XbeeFrame.PacketTypeReceiveIO, xbeeFrame.FrameType);
                Xunit.Assert.Equal(ReceiveIOPacket.FrameType, xbeeFrame.FrameType);
            }
        }
    }
}
