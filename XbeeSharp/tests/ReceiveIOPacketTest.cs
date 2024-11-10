namespace XbeeTests;

public class ReceivePacketIOTests
{
    private static readonly byte[] ValidFullIOPacket = [0x7E, 0x00, 0x16, 0x92, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34, 0x56, 0x78,
                                                        0x87, 0xAC, 0x01, 0x01, 0x00, 0x38, 0x06, 0x00, 0x28, 0x02, 0x25, 0x00, 0xF8, 0xEA];
    private static readonly byte[] ValidAnalogIOPacket = [0x7E, 0x00, 0x14, 0x92, 0x00, 0x7D, 0x33, 0xA2, 0x00, 0x12, 0x34, 0x56, 0x78,
                                                            0x87, 0xAC, 0x01, 0x01, 0x00, 0x00, 0x06, 0x02, 0x25, 0x00, 0xF8, 0x4A];
    private const string SourceAddress = "0x0013A20012345678";

    [Fact]
    public void CreateFromFrameBuilder()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidFullIOPacket, false);
        Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, false));
        }
    }

    [Fact]
    public void ExpectedFullSamples()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidFullIOPacket, false);
        Assert.NotNull(xbeeFrame);
        ReceiveIOPacket? packet;
        if (xbeeFrame != null)
        {
            Assert.True(ReceiveIOPacket.Parse(out packet, xbeeFrame));
            if (packet != null)
            {
                Assert.Equal(XbeeFrame.PacketTypeReceiveIO, xbeeFrame.FrameType);
                Assert.Equal(ReceiveIOPacket.FrameType, xbeeFrame.FrameType);
                Assert.Equal(SourceAddress, packet.SourceAddress.AsString());
                Assert.Equal(0x01, packet.ReceiveOptions);
                Assert.Equal(0x0038, packet.DigitalChannelMask);
                Assert.Equal(0x06, packet.AnalogChannelMask);
                Assert.Equal(3, packet.DigitalSamples.Count);
                // DIO3 high
                Assert.Equal(3, packet.DigitalSamples[0].Dio);
                Assert.True(packet.DigitalSamples[0].Value);
                // DIO4 low
                Assert.Equal(4, packet.DigitalSamples[1].Dio);
                Assert.False(packet.DigitalSamples[1].Value);
                // DIO5 high
                Assert.Equal(5, packet.DigitalSamples[2].Dio);
                Assert.True(packet.DigitalSamples[2].Value);
                Assert.Equal(2, packet.AnalogSamples.Count);
                // AD1
                Assert.Equal(1, packet.AnalogSamples[0].Adc);
                Assert.Equal(0x0225, packet.AnalogSamples[0].Value);
                // AD2
                Assert.Equal(2, packet.AnalogSamples[1].Adc);
                Assert.Equal(0x00F8, packet.AnalogSamples[1].Value);
            }
        }
    }

    [Fact]
    public void ExpectedAnalogOnlySamples()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidAnalogIOPacket, true);
        Assert.NotNull(xbeeFrame);
        ReceiveIOPacket? packet;
        if (xbeeFrame != null)
        {
            Assert.True(ReceiveIOPacket.Parse(out packet, xbeeFrame));
            if (packet != null)
            {
                Assert.Equal(XbeeFrame.PacketTypeReceiveIO, xbeeFrame.FrameType);
                Assert.Equal(ReceiveIOPacket.FrameType, xbeeFrame.FrameType);
                Assert.Equal(SourceAddress, packet.SourceAddress.AsString());
                Assert.Equal(0x01, packet.ReceiveOptions);
                Assert.Equal(0x0000, packet.DigitalChannelMask);
                Assert.Equal(0x06, packet.AnalogChannelMask);
                Assert.Empty(packet.DigitalSamples);
                Assert.Equal(2, packet.AnalogSamples.Count);
                // AD1
                Assert.Equal(1, packet.AnalogSamples[0].Adc);
                Assert.Equal(0x0225, packet.AnalogSamples[0].Value);
                // AD2
                Assert.Equal(2, packet.AnalogSamples[1].Adc);
                Assert.Equal(0x00F8, packet.AnalogSamples[1].Value);
            }
        }
    }
}
