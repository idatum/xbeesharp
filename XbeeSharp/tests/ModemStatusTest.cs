namespace XbeeTests;

public class ModemStatusTest
{
    private static byte [] ValidPacket = new byte [] {0x7E, 0x00, 0x02, 0x8A, 0x00, 0x75};

    [Fact]
    public void CreateWithExpectedFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidPacket, false);
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.FrameData, false));
            ModemStatusPacket? packet;
            Xunit.Assert.True(ModemStatusPacket.Parse(out packet, xbeeFrame));
            Xunit.Assert.NotNull(packet);
            if (packet != null)
            {
                Xunit.Assert.Equal(0x00, packet.ModemStatus);
            }
        }
    }
}
