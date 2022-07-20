namespace XbeeTests;

public class RemoteATPacketTest
{
    // 0x0013010203040506
    static readonly XbeeAddress Address = XbeeAddress.Create(new byte [] {0x00, 0x13, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06});
    // Pin D0 on (0x0005)
    static readonly byte[] PinOnFrameEscaped = {0x7E,0x00,0x7D,0x31,0x17,0x01,0x00,0x7D,0x33,0x01,0x02,0x03,0x04,0x05,0x06,
                                                0xFF,0xFE,0x02,0x44,0x30,0x00,0x05,0x47};

    [Fact]
    public void PinOnEscapedFrame()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(PinOnFrameEscaped, true);
        Xunit.Assert.NotNull(xbeeFrame);
    }

    [Fact]
    public void PinOnPacketEscaped()
    {
        XbeeFrame? xbeeFrame;
        Xunit.Assert.True(RemoteATPacket.CreateXbeeFrame(out xbeeFrame, Address, new byte [] {0x44, 0x30}, new byte [] {0x00, 0x05}, true));
        Xunit.Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(XbeeFrameBuilder.ChecksumValid(xbeeFrame.FrameData, xbeeFrame.Escaped));
        }
    }
}
