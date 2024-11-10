namespace XbeeSharp;

/// <summary>
/// Modem status packet.
/// </summary>
public class ModemStatusPacket
{
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    private readonly XbeeFrame _xbeeFrame;
    /// <summary>
    /// Modem status.
    /// </summary>
    private readonly byte _modemStatus;

    /// <summary>
    /// Constructor.
    /// </summary>
    private ModemStatusPacket(XbeeFrame xbeeFrame, byte modemStatus)
    {
        _xbeeFrame = xbeeFrame;
        _modemStatus = modemStatus;
    }

    /// <summary>
    /// Create from XBee frame.
    /// </summary>
    public static bool Parse(out ModemStatusPacket? packet, XbeeFrame xbeeFrame)
    {
        packet = null;

        if (xbeeFrame.FrameType != XbeeFrame.PacketTypeModemStatus)
        {
            return false;
        }
        // Modem status.
        byte modemStatus = xbeeFrame.Data[4];

        packet = new ModemStatusPacket(xbeeFrame, modemStatus);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public const byte FrameType = XbeeFrame.PacketTypeModemStatus;

    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    public XbeeFrame XbeeFrame
    {
        get => _xbeeFrame;
    }

    /// <summary>
    /// Modem status.
    /// From Digi documentation:
    /// 0x00 = Hardware reset or power up
    /// 0x01 = Watchdog timer reset
    /// 0x02 = Joined network
    /// 0x03 = Left network
    /// 0x06 = Coordinator started
    /// 0x07 = Network security key was updated
    /// 0x0B = Network woke up
    /// 0x0C = Network went to sleep
    /// 0x0D = Voltage supply limit exceeded
    /// 0x0E = Digi Remote Manager connected
    /// 0x0F = Digi Remote Manager disconnected
    /// 0x11 = Modem configuration changed while join in progress
    /// 0x12 = Access fault
    /// 0x13 = Fatal error
    /// 0x3B = Secure session successfully established
    /// 0x3C = Secure session ended
    /// 0x3D = Secure session authentication failed
    /// 0x3E = Coordinator detected a PAN ID conflict but took no action
    /// 0x3F = Coordinator changed PAN ID due to a conflict
    /// 0x32 = BLE Connect
    /// 0x33 = BLE Disconnect
    /// 0x34 = Bandmask configuration failed
    /// 0x35 = Cellular component update started
    /// 0x36 = Cellular component update failed
    /// 0x37 = Cellular component update completed
    /// 0x38 = XBee firmware update started
    /// 0x39 = XBee firmware update failed
    /// 0x3A = XBee firmware update applying
    /// 0x40 = Router PAN ID was changed by coordinator due to a conflict
    /// 0x42 = Network Watchdog timeout expired
    /// 0x80 through 0xFF = Stack error
    /// </summary>
    public byte ModemStatus
    {
        get => _modemStatus;
    }
}
