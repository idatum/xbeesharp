namespace XbeeTests;

public static class XbeeTestUtils
{
    public static XbeeFrame? CreateFrameFromBuilder(IReadOnlyList<byte> packet, bool escaped)
    {
        var xbeeFrameBuilder = new XbeeFrameBuilder(escaped);
        Assert.Equal(XbeeFrame.StartByte, packet[0]);
        XbeeFrame? xbeeFrame = null;
        foreach (var nextByte in packet)
        {
            xbeeFrameBuilder.Append(nextByte);
            if (xbeeFrameBuilder.FrameComplete)
            {
                if (!XbeeFrameBuilder.ChecksumValid(xbeeFrameBuilder.Data, escaped))
                {
                    throw new InvalidOperationException();
                }
                xbeeFrame = xbeeFrameBuilder.ToXbeeFrame();
            }
        }

        return xbeeFrame;
    }
}
