namespace XbeeTests;

public class ReceivePacketIOTests
{
    private static readonly byte[] ValidFullIOPacket = new byte[] {0x7E, 0x00, 0x16, 0x92, 0x00, 0x13, 0xA2, 0x00, 0x12, 0x34, 0x56, 0x78,
                                                               0x87, 0xAC, 0x01, 0x01, 0x00, 0x38, 0x06, 0x00, 0x28, 0x02, 0x25, 0x00, 0xF8, 0xEA};
    private static readonly byte[] ValidAnalogIOPacket = new byte[] {0x7E, 0x00, 0x14, 0x92, 0x00, 0x7D, 0x33, 0xA2, 0x00, 0x12, 0x34, 0x56, 0x78,
                                                                     0x87, 0xAC, 0x01, 0x01, 0x00, 0x00, 0x06, 0x02, 0x25, 0x00, 0xF8, 0x4A};
    private const string SourceAddress = "0x0013A20012345678";

    [Fact]
    public void CreateFromFrameBuilder()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidFullIOPacket, false);
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.Data, false));
        }
    }

    [Fact]
    public void ExpectedFullSamples()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidFullIOPacket, false);
        Xunit.Assert.NotNull(xbeeFrame);
        ReceiveIOPacket? packet;
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(ReceiveIOPacket.Parse(out packet, xbeeFrame));
            if (packet != null)
            {
                Xunit.Assert.Equal(XbeeFrame.PacketTypeReceiveIO, xbeeFrame.FrameType);
                Xunit.Assert.Equal(ReceiveIOPacket.FrameType, xbeeFrame.FrameType);
                Xunit.Assert.Equal(SourceAddress, packet.SourceAddress.AsString());
                Xunit.Assert.Equal(0x01, packet.ReceiveOptions);
                Xunit.Assert.Equal(0x0038, packet.DigitalChannelMask);
                Xunit.Assert.Equal(0x06, packet.AnalogChannelMask);
                Xunit.Assert.Equal(3, packet.DigitalSamples.Count);
                // DIO3 high
                Xunit.Assert.Equal(3, packet.DigitalSamples[0].Dio);
                Xunit.Assert.True(packet.DigitalSamples[0].Value);
                // DIO4 low
                Xunit.Assert.Equal(4, packet.DigitalSamples[1].Dio);
                Xunit.Assert.False(packet.DigitalSamples[1].Value);
                // DIO5 high
                Xunit.Assert.Equal(5, packet.DigitalSamples[2].Dio);
                Xunit.Assert.True(packet.DigitalSamples[2].Value);
                Xunit.Assert.Equal(2, packet.AnalogSamples.Count);
                // AD1
                Xunit.Assert.Equal(1, packet.AnalogSamples[0].Adc);
                Xunit.Assert.Equal(0x0225, packet.AnalogSamples[0].Value);
                // AD2
                Xunit.Assert.Equal(2, packet.AnalogSamples[1].Adc);
                Xunit.Assert.Equal(0x00F8, packet.AnalogSamples[1].Value);
            }
        }
    }

    [Fact]
    public void ExpectedAnalogOnlySamples()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidAnalogIOPacket, true);
        Xunit.Assert.NotNull(xbeeFrame);
        ReceiveIOPacket? packet;
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(ReceiveIOPacket.Parse(out packet, xbeeFrame));
            if (packet != null)
            {
                Xunit.Assert.Equal(XbeeFrame.PacketTypeReceiveIO, xbeeFrame.FrameType);
                Xunit.Assert.Equal(ReceiveIOPacket.FrameType, xbeeFrame.FrameType);
                Xunit.Assert.Equal(SourceAddress, packet.SourceAddress.AsString());
                Xunit.Assert.Equal(0x01, packet.ReceiveOptions);
                Xunit.Assert.Equal(0x0000, packet.DigitalChannelMask);
                Xunit.Assert.Equal(0x06, packet.AnalogChannelMask);
                Xunit.Assert.Equal(0, packet.DigitalSamples.Count);
                Xunit.Assert.Equal(2, packet.AnalogSamples.Count);
                // AD1
                Xunit.Assert.Equal(1, packet.AnalogSamples[0].Adc);
                Xunit.Assert.Equal(0x0225, packet.AnalogSamples[0].Value);
                // AD2
                Xunit.Assert.Equal(2, packet.AnalogSamples[1].Adc);
                Xunit.Assert.Equal(0x00F8, packet.AnalogSamples[1].Value);
            }
        }
    }
}
