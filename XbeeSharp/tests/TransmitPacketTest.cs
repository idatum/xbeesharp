namespace XbeeTests;

using System.Text;

public class TransmitPacketTest
{
    static readonly XbeeAddress Address = XbeeAddress.Create(new byte [] {0x00, 0x13, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06});

    [Fact]
    public void LargePayloadUnescaped()
    {
        var reasonablyLargePayload = Encoding.UTF8.GetBytes("0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");
        XbeeFrame? xbeeFrame;
        bool created = TransmitPacket.CreateXbeeFrame(out xbeeFrame, Address, reasonablyLargePayload, false);
        Xunit.Assert.NotNull(xbeeFrame);
        Xunit.Assert.True(created);
    }

    [Fact]
    public void LargePayloadEscaped()
    {
        var reasonablyLargePayload = Encoding.UTF8.GetBytes("7D214567897D212345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");
        XbeeFrame? xbeeFrame;
        bool created = TransmitPacket.CreateXbeeFrame(out xbeeFrame, Address, reasonablyLargePayload, true);
        Xunit.Assert.NotNull(xbeeFrame);
        Xunit.Assert.True(created);
    }
}
