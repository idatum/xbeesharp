namespace XbeeSharp;
 
using System.Text;

/// <summary>
/// XBee address.
/// </summary>
public class XbeeAddress
{
    /// <summary>
    /// Address bytes.
    /// </summary>
    private IReadOnlyList<byte> _address;

    /// <summary>
    /// Constructor.
    /// </summary>
    private XbeeAddress(IReadOnlyList<byte> address)
    {
        _address = address;
    }

    /// <summary>
    /// Create from bytes.
    /// </summary>
    public static XbeeAddress Create(IReadOnlyList<byte> address)
    {
        if (address == null || address.Count != 8)
        {
            throw new ArgumentException("address");
        }

        return new XbeeAddress(address);
    }

    /// <summary>
    /// Create from formatted string.
    /// </summary>
    public static XbeeAddress Create(string address)
    {
        // Expecting format like 0x0013010203040506

        if (address == null || address.Length != 18)
        {
            throw new ArgumentException("address");
        }

        var createAddress = new List<byte>(8);
        for (var i = 2; i < address.Length; i += 2)
        {
            createAddress.Add(Convert.ToByte(address.Substring(i, 2), 16));
        }

        return Create(createAddress);
    }

    /// <summary>
    /// 64 bit address.
    /// </summary>
    public IReadOnlyList<byte> LongAddress
    {
        get => _address;
    }

    /// <summary>
    /// Address in formatted string.
    /// </summary>
    public string AsString()
    {
        var builder = new StringBuilder(10);
        builder.Append("0x");
        foreach (var b in _address)
        {
            builder.Append($"{b:X2}");
        }

        return builder.ToString();
    }
}
