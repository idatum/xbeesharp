namespace XbeeTests;

public class ModemStatusTest
{
    private static readonly byte[] ValidPacket = [0x7E, 0x00, 0x02, 0x8A, 0x00, 0x75];

    [Fact]
    public void CreateWithExpectedFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidPacket, false);
        Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, false));
            ModemStatusPacket? packet;
            Assert.True(ModemStatusPacket.Parse(out packet, xbeeFrame));
            Assert.NotNull(packet);
            if (packet != null)
            {
                Assert.Equal(0x00, packet.ModemStatus);
            }
        }
    }
}
