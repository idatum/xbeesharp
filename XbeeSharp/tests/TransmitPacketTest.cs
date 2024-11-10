namespace XbeeTests;
using System.Text;

public class TransmitPacketTest
{
    private static readonly XbeeAddress Address = XbeeAddress.Create([0x00, 0x13, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]);
    private static readonly XbeeAddress EmptyAddress = XbeeAddress.Create([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

    [Fact]
    public void LargePayloadUnescaped()
    {
        var reasonablyLargePayload = Encoding.UTF8.GetBytes("0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");
        bool created = TransmitPacket.CreateXbeeFrame(out XbeeFrame? xbeeFrame, Address, 1, reasonablyLargePayload, false);
        Assert.NotNull(xbeeFrame);
        Assert.True(created);
    }

    [Fact]
    public void LargePayloadEscaped()
    {
        var reasonablyLargePayload = Encoding.UTF8.GetBytes("7D214567897D212345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");
        bool created = TransmitPacket.CreateXbeeFrame(out XbeeFrame? xbeeFrame, Address, 1, reasonablyLargePayload, true);
        Assert.NotNull(xbeeFrame);
        Assert.True(created);
    }

    [Fact]
    public void EscapedChecksumTxPacket()
    {
        // Escaped checksum should be: 0x7D, 0x31
        Assert.True(TransmitPacket.CreateXbeeFrame(out XbeeFrame? xbeeFrame, EmptyAddress, 1, [0xE0], true));
        if (xbeeFrame != null)
        {
            // Array length should include extra escape byte (0x7D).
            Assert.Equal(20, xbeeFrame.Data.Count);
            Assert.Equal(0x7D, xbeeFrame.Data[xbeeFrame.Data.Count - 2]);
        }
    }
}
