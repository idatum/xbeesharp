namespace XbeeTests;

public class RemoteATCommandResponseTest
{
    private static readonly byte[] ValidPacketTransmissionFailure = [0x7E, 0x00, 0x0F, 0x97, 0x27, 0x00, 0x13, 0xA2, 0x00, 0x12,
                                                                    0x34, 0x56, 0x78, 0xFF, 0xFE, 0x49, 0x44, 0x04, 0xEA];
    private static readonly byte[] ValidPacketSucess = [0x7E, 0x00, 0x11, 0x97, 0x27, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34,
                                                        0x56, 0x78, 0xFF, 0xFE, 0x54, 0x50, 0x00, 0x00, 0x2F, 0xA8];

    [Fact]
    public void CreateWithExpectedTransmissionFailureFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidPacketTransmissionFailure, false);
        Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, false));
            Assert.True(ATCommandResponsePacket.Parse(out ATCommandResponsePacket? packet, xbeeFrame));
            Assert.NotNull(packet);
            if (packet != null)
            {
                Assert.Equal(0x27, packet.FrameId);
                Assert.Equal("0x0013A20012345678", packet.SourceAddress.AsString());
                Assert.Equal(XbeeAddress.UseLongNetworkAddress, packet.NetworkAddress);
                Assert.Equal("ID", packet.Command);
                Assert.Equal(0x04, packet.CommandStatus);
            }
        }
    }

    [Fact]
    public void CreateWithExpectedSuccessFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidPacketSucess, false);
        Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, false));
            Assert.True(ATCommandResponsePacket.Parse(out ATCommandResponsePacket? packet, xbeeFrame));
            Assert.NotNull(packet);
            if (packet != null)
            {
                Assert.Equal(0x27, packet.FrameId);
                Assert.Equal("0x0013A20012345678", packet.SourceAddress.AsString());
                Assert.Equal(XbeeAddress.UseLongNetworkAddress, packet.NetworkAddress);
                Assert.Equal("TP", packet.Command);
                Assert.Equal(0x00, packet.CommandStatus);
            }
        }
    }
}
