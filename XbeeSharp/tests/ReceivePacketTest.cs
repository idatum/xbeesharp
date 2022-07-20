namespace XbeeTests;

using System.Text;

public class ReceivePacketTests
{
    private static readonly byte[] ValidRXPacket = new byte[] {XbeeFrame.StartByte, 0x00, 0x1E, 0x90, 0x00, 0x14, 0xA1, 0x00, 0x40, 0xC3, 0x54,
                                                               0x9D, 0xF1, 0xA8, 0x02, 0x43, 0x3D, 0x32, 0x34, 0x2E, 0x39, 0x30, 0x26, 0x50,
                                                               0x3D, 0x31, 0x30, 0x32, 0x37, 0x2E, 0x36, 0x36, 0x0D, 0x8A};
    private const string ReceiveData = "C=24.90&P=1027.66\r";
    
    [Fact]
    public void ValidPacketUnescaped()
    {
        XbeeFrame? xbeeFrame = XbeeTestUtils.CreateFrameFromBuilder(ValidRXPacket, false);
        Xunit.Assert.NotNull(xbeeFrame);
        ReceivePacket? packet;
        if (xbeeFrame != null)
        {
            Xunit.Assert.True(ReceivePacket.Parse(out packet, xbeeFrame));
            if (packet != null)
            {
                Xunit.Assert.Equal(XbeeFrame.PacketTypeReceive, xbeeFrame.FrameType);
                Xunit.Assert.Equal(ReceivePacket.FrameType, xbeeFrame.FrameType);
                Xunit.Assert.Equal(ReceiveData, Encoding.UTF8.GetString(packet.ReceiveData.ToArray()));
            }
        }
    }
}
