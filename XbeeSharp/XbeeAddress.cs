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
    /// Use 16-bit address address value for 64-bit address field.
    /// </summary>
    public static readonly XbeeAddress CoordinatorAddress = XbeeAddress.Create([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

    /// <summary>
    /// Broadcast address.
    /// </summary>
    public static readonly XbeeAddress BroadcastAddress = XbeeAddress.Create([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF]);

    /// <summary>
    /// Use 16-bit address address value for 64-bit address field.
    /// </summary>
    public static readonly XbeeAddress NetworkAddress = XbeeAddress.Create([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);

    /// <summary>
    /// 16-bit address for coordinator.
    /// </summary>
    public const uint CoordinatorNetworkAddress = 0x0000;

    /// <summary>
    /// 16-bit broadcast address to all routers.
    /// </summary>
    public const uint RouterBroadcastNetworkAddress = 0xFFFC;

    /// <summary>
    /// 16-bit broadcast address to all non-sleepy devices.
    /// </summary>
    public const uint NonSleepBroadcastNetworkAddress = 0xFFFD;

    /// <summary>
    /// 16-bit address indicating instead use 64-bit address.
    /// </summary>
    public const uint UseLongNetworkAddress = 0xFFFE;

    /// <summary>
    /// 16-bit broadcast address to all devices.
    /// </summary>
    public const uint BroadcastNetworkAddress = 0xFFFF;

    /// <summary>
    /// Create from bytes.
    /// </summary>
    public static XbeeAddress Create(IReadOnlyList<byte> address)
    {
        if (address is null || address.Count != 8)
        {
            throw new ArgumentException(null, nameof(address));
        }

        return new XbeeAddress(address);
    }

    /// <summary>
    /// Create from formatted string.
    /// </summary>
    public static XbeeAddress Create(string address)
    {
        // Expecting format like 0x0013010203040506

        if (address is null || address.Length != 18)
        {
            throw new ArgumentException(null, nameof(address));
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
