namespace XbeeSharp;

/// <summary>
/// Modem status packet.
/// </summary>
public class ModemStatusPcket
{
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    private XbeeFrame _xbeeFrame;
    /// <summary>
    /// Modem status.
    /// </summary>
    private byte _modemStatus;

    /// <summary>
    /// Constructor.
    /// </summary>
    private ModemStatusPcket(XbeeFrame xbeeFrame, byte modemStatus)
    {
        _xbeeFrame = xbeeFrame;
        _modemStatus = modemStatus;
    }

    /// <summary>
    /// Create from XBee frame.
    /// </summary>
    public static bool Parse(out ModemStatusPcket? packet, XbeeFrame xbeeFrame)
    {
        packet = null;

        if (xbeeFrame.FrameType != XbeeFrame.PacketTypeExtendedTransmitStatus)
        {
            return false;
        }
        // Modem status.
        byte modemStatus = xbeeFrame.FrameData[4];

        packet = new ModemStatusPcket(xbeeFrame, modemStatus);

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
    /// </summary>
    public byte ModemStatus
    {
        get => _modemStatus;
    }
}
