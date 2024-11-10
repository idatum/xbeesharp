namespace XbeeTests;
using System.Text;

public class ReceivePacketTests
{
    private static readonly byte[] ValidRXPacket = [0x7E, 0x00, 0x12, 0x90, 0x00, 0x13, 0xA2, 0x00, 0x87, 0x65,
                                                    0x43, 0x21, 0x56, 0x14, 0x01, 0x54, 0x78, 0x44, 0x61, 0x74, 0x61, 0xB9];
    private const string SourceAddress = "0x0013A20087654321";
    private const string ReceiveData = "TxData";

    [Fact]
    public void ValidPacketUnescaped()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidRXPacket, false);
        Assert.NotNull(xbeeFrame);
        if (xbeeFrame != null)
        {
            Assert.True(ReceivePacket.Parse(out ReceivePacket? packet, xbeeFrame));
            if (packet != null)
            {
                Assert.Equal(XbeeFrame.PacketTypeReceive, xbeeFrame.FrameType);
                Assert.Equal(ReceivePacket.FrameType, xbeeFrame.FrameType);
                Assert.Equal(SourceAddress, packet.SourceAddress.AsString());
                Assert.Equal(ReceiveData, Encoding.UTF8.GetString([.. packet.ReceiveData]));
            }
        }
    }
}
