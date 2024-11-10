namespace XbeeTests;

public class XbeeAddressTest
{
    [Fact]
    public void XbeeAddressCreateFromString()
    {
        var xbeeAddress = XbeeAddress.Create("0x001301A2B3C4D5E6");
        Assert.NotNull(xbeeAddress);
        Assert.NotNull(xbeeAddress.LongAddress);
        Assert.Equal(8, xbeeAddress.LongAddress.Count);
    }

    [Fact]
    public void XbeeAddressInvalidFromString()
    {
        Assert.Throws<ArgumentException>(() => XbeeAddress.Create("0x00010203040506070809"));
        Assert.Throws<ArgumentException>(() => XbeeAddress.Create(String.Empty));
    }

    [Fact]
    public void XbeeAddressInvalidFromBytes()
    {
        Assert.Throws<ArgumentException>(() => XbeeAddress.Create(new byte[] { 0 }));
        Assert.Throws<ArgumentException>(() => XbeeAddress.Create(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
    }

    [Fact]
    public void XbeeAddressFullCircle()
    {
        const string stringAddress = "0x001301A2B3C4D5E6";
        var address = new byte[] { 0x00, 0x13, 0x01, 0xA2, 0xB3, 0xC4, 0xD5, 0xE6 };
        var xbeeAddress = XbeeAddress.Create(address);
        Assert.NotNull(xbeeAddress);
        var addressAsString = xbeeAddress.AsString();
        Assert.Equal(stringAddress, addressAsString);
        for (var i = 0; i < address.Length; ++i)
        {
            Assert.Equal(address[i], xbeeAddress.LongAddress[i]);
        }
    }
}