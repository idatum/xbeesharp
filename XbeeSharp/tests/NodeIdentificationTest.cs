namespace XbeeTests;

public class NodeIdentificationTest
{
    private static readonly byte[] RemoteDevicePacket = [0x7E, 0x00, 0x27, 0x95, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34, 0x56, 0x78, 0xFF,
                                                        0xFE, 0xC2, 0xFF, 0xFE, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34, 0x56, 0x78, 0x4C,
                                                        0x48, 0x37, 0x35, 0x00, 0xFF, 0xFE, 0x01, 0x01, 0xC1, 0x05, 0x10, 0x1E, 0x00,
                                                        0x14, 0x00, 0x08, 0x0D];
    [Fact]
    public void CreateWithExpectedFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(RemoteDevicePacket, false);
        Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, false));
            Assert.True(NodeIdentificationPacket.Parse(out NodeIdentificationPacket? packet, xbeeFrame));
            Assert.NotNull(packet);
            if (packet != null)
            {
                Assert.Equal(0xC2, packet.ReceiveOptions);
                Assert.Equal(XbeeAddress.UseLongNetworkAddress, packet.RemoteNetworkAddress);
                Assert.Equal("0x0013A20012345678", packet.RemoteSourceAddress.AsString());
                Assert.Equal("LH75", packet.NodeIdentifier);
                Assert.Equal(0x01, packet.DeviceType);
            }
        }
    }
}
