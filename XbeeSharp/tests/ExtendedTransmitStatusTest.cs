namespace XbeeTests;

public class ExtendedTransmitStatusTest
{
    private static byte[] ValidPacket = new byte[] { 0x7E, 0x00, 0x07, 0x8B, 0x52, 0x12, 0x34, 0x02, 0x00, 0x01, 0xD9 };

    [Fact]
    public void CreateWithExpectedFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidPacket, false);
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, false));
            ExtendedTransmitStatusPacket? packet;
            Xunit.Assert.True(ExtendedTransmitStatusPacket.Parse(out packet, xbeeFrame));
            Xunit.Assert.NotNull(packet);
            if (packet != null)
            {
                Xunit.Assert.Equal(0x52, packet.FrameId);
                Xunit.Assert.Equal(0x1234, packet.NetworkAddress);
                Xunit.Assert.Equal(0x02, packet.TransmitRetryCount);
                Xunit.Assert.Equal(0x00, packet.DeliveryStatus);
                Xunit.Assert.Equal(0x01, packet.DiscoveryStatus);
            }
        }
    }
}
