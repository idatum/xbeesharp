namespace XbeeTests;

using System.Text;

public class TransmitPacketTest
{
    static readonly XbeeAddress Address = XbeeAddress.Create(new byte [] {0x00, 0x13, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06});
    private readonly XbeeAddress EmptyAddress = XbeeAddress.Create(new byte [] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00});

    [Fact]
    public void LargePayloadUnescaped()
    {
        var reasonablyLargePayload = Encoding.UTF8.GetBytes("0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");
        XbeeFrame? xbeeFrame;
        bool created = TransmitPacket.CreateXbeeFrame(out xbeeFrame, Address, 1, reasonablyLargePayload, false);
        Xunit.Assert.NotNull(xbeeFrame);
        Xunit.Assert.True(created);
    }

    [Fact]
    public void LargePayloadEscaped()
    {
        var reasonablyLargePayload = Encoding.UTF8.GetBytes("7D214567897D212345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");
        XbeeFrame? xbeeFrame;
        bool created = TransmitPacket.CreateXbeeFrame(out xbeeFrame, Address, 1, reasonablyLargePayload, true);
        Xunit.Assert.NotNull(xbeeFrame);
        Xunit.Assert.True(created);
    }

    [Fact]
    public void EscapedChecksumTxPacket()
    {
        XbeeFrame? xbeeFrame;
        // Escaped checksum should be: 0x7D, 0x31
        Xunit.Assert.True(TransmitPacket.CreateXbeeFrame(out xbeeFrame, EmptyAddress, 1, new byte [] {0xE0}, true));
        if (xbeeFrame != null)
        {
            // Array length should include extra escape byte (0x7D).
            Xunit.Assert.Equal(20, xbeeFrame.Data.Count);
            Xunit.Assert.Equal(0x7D, xbeeFrame.Data[xbeeFrame.Data.Count - 2]);
        }
    }
}
