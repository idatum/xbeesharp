namespace XbeeTests;

public class RemoteATCommandResponseTest
{
    private static byte [] ValidPacketTransmissionFailure = new byte [] {0x7E, 0x00, 0x0F, 0x97, 0x27, 0x00, 0x13, 0xA2, 0x00, 0x12,
                                                                         0x34, 0x56, 0x78, 0xFF, 0xFE, 0x49, 0x44, 0x04, 0xEA};
    private static byte [] ValidPacketSucess = new byte [] {0x7E, 0x00, 0x11, 0x97, 0x27, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34,
                                                                0x56, 0x78, 0xFF, 0xFE, 0x54, 0x50, 0x00, 0x00, 0x2F, 0xA8};

    [Fact]
    public void CreateWithExpectedTransmissionFailureFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidPacketTransmissionFailure, false);
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.FrameData, false));
            RemoteATCommandResponse? packet;
            Xunit.Assert.True(RemoteATCommandResponse.Parse(out packet, xbeeFrame));
            Xunit.Assert.NotNull(packet);
            if (packet != null)
            {
                Xunit.Assert.Equal(0x27, packet.FrameId);
                Xunit.Assert.Equal("0x0013A20012345678", packet.SourceAddress.AsString());
                Xunit.Assert.Equal(0xFFFe, packet.NetworkAddress);
                Xunit.Assert.Equal("ID", packet.Command);
                Xunit.Assert.Equal(0x04, packet.CommandStatus);
            }
        }
    }

    [Fact]
    public void CreateWithExpectedSuccessFields()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidPacketSucess, false);
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.FrameData, false));
            RemoteATCommandResponse? packet;
            Xunit.Assert.True(RemoteATCommandResponse.Parse(out packet, xbeeFrame));
            Xunit.Assert.NotNull(packet);
            if (packet != null)
            {
                Xunit.Assert.Equal(0x27, packet.FrameId);
                Xunit.Assert.Equal("0x0013A20012345678", packet.SourceAddress.AsString());
                Xunit.Assert.Equal(0xFFFE, packet.NetworkAddress);
                Xunit.Assert.Equal("TP", packet.Command);
                Xunit.Assert.Equal(0x00, packet.CommandStatus);
            }
        }
    }
}
