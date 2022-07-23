namespace XbeeTests;

public class NodeIdentificationTest
{
    private static byte [] RemoteDevicePacket = new byte [] {0x7E, 0x00, 0x27, 0x95, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34, 0x56, 0x78, 0xFF,
                                                             0xFE, 0xC2, 0xFF, 0xFE, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34, 0x56, 0x78, 0x4C,
                                                             0x48, 0x37, 0x35, 0x00, 0xFF, 0xFE, 0x01, 0x01, 0xC1, 0x05, 0x10, 0x1E, 0x00,
                                                             0x14, 0x00, 0x08, 0x0D};
    [Fact]
    public void CreateWithExpectedFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(RemoteDevicePacket, false);
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.FrameData, false));
            NodeIdentificationPacket? packet;
            Xunit.Assert.True(NodeIdentificationPacket.Parse(out packet, xbeeFrame));
            Xunit.Assert.NotNull(packet);
            if (packet != null)
            {
                Xunit.Assert.Equal(0xC2, packet.ReceiveOptions);
                Xunit.Assert.Equal(0xFFFE, packet.RemoteNetworkAddress);
                Xunit.Assert.Equal("0x0013A20012345678", packet.RemoteSourceAddress.AsString());
                Xunit.Assert.Equal("LH75", packet.NodeIdentifier);
                Xunit.Assert.Equal(0x01, packet.DeviceType);
            }
        }
    }
}
