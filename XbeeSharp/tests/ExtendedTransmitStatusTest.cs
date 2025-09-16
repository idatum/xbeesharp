namespace XbeeTests;

public class ExtendedTransmitStatusTest
{
    private static byte[] ValidPacket = [0x7E, 0x00, 0x07, 0x8B, 0x52, 0x12, 0x34, 0x02, 0x00, 0x01, 0xD9];

    [Fact]
    public void CreateWithExpectedFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidPacket, false);
        Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, false));
            Assert.True(ExtendedTransmitStatusPacket.Parse(out ExtendedTransmitStatusPacket? packet, xbeeFrame));
            Assert.NotNull(packet);
            if (packet != null)
            {
                Assert.Equal(0x52, packet.FrameId);
                Assert.Equal(0x1234, packet.NetworkAddress);
                Assert.Equal(0x02, packet.TransmitRetryCount);
                Assert.Equal(0x00, packet.DeliveryStatus);
                Assert.Equal(0x01, packet.DiscoveryStatus);
            }
        }
    }
}
