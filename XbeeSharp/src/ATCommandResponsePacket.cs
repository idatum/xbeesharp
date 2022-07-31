namespace XbeeSharp;

/// <summary>
/// Remote AT command response packet.
/// </summary>
public class ATCommandResponsePacket
{
    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    private XbeeFrame _xbeeFrame;
    /// <summary>
    /// Frame ID.
    /// </summary>
    private byte _frameId;
    /// <summary>
    /// Source address.
    /// </summary>
    private XbeeAddress _sourceAddress;
    /// <summary>
    /// Network address.
    /// <summary>
    private ushort _networkAddress;
    /// <summary>
    /// AT command.
    /// </summary>
    private string _command;
    /// <summary>
    /// Delivery status.
    /// </summary>
    private byte _commandStatus;

    /// <summary>
    /// Constructor.
    /// </summary>
    private ATCommandResponsePacket(XbeeFrame xbeeFrame, byte frameId, IReadOnlyList<byte> sourceAddress,
                                    ushort networkAddress, string command, byte commandStatus)
    {
        _xbeeFrame = xbeeFrame;
        _frameId = frameId;
        _sourceAddress = XbeeAddress.Create(sourceAddress);
        _networkAddress = networkAddress;
        _command = command;
        _commandStatus = commandStatus;
    }

    /// <summary>
    /// Create from XBee frame.
    /// </summary>
    public static bool Parse(out ATCommandResponsePacket? packet, XbeeFrame xbeeFrame)
    {
        packet = null;

        if (xbeeFrame.FrameType != XbeeFrame.PacketTypeRemoteATCommandResponse)
        {
            return false;
        }
        // Frame ID.
        byte frameId = xbeeFrame.Data[4];
        // 64-bit source address.
        var sourceAddress = xbeeFrame.Data.Take(5..13).ToList();
        // Network address.
        ushort networkAddress = XbeeFrameBuilder.ToBigEndian(xbeeFrame.Data[13], xbeeFrame.Data[14]);
        // AT command.
        var command = new string(new char [] {(char)xbeeFrame.Data[15], (char)xbeeFrame.Data[16]});
        // Command status
        byte commandStatus = xbeeFrame.Data[17];

        packet = new ATCommandResponsePacket(xbeeFrame, frameId, sourceAddress, networkAddress, command, commandStatus);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public const byte FrameType = XbeeFrame.PacketTypeRemoteATCommandResponse;

    /// <summary>
    /// Underlying XBee frame.
    /// </summary>
    public XbeeFrame XbeeFrame
    {
        get => _xbeeFrame;
    }

    /// <summary>
    /// Frame ID.
    /// </summary>
    public byte FrameId
    {
        get => _frameId;
    }

    /// <summary>
    /// Source address.
    /// </summary>
    public XbeeAddress SourceAddress
    {
        get => _sourceAddress;
    }

    /// <summary>
    /// Network address.
    /// <summary>
    public ushort NetworkAddress
    {
        get => _networkAddress;
    }

    /// <summary>
    /// Transmit retry count.
    /// </summary>
    public string Command
    {
        get => _command;
    }

    /// <summary>
    /// Delivery status.
    /// From Digi documentation:
    /// 0x00 = OK
    /// 0x01 = ERROR
    /// 0x02 = Invalid command
    /// 0x03 = Invalid parameter
    /// 0x04 = Transmission failure
    /// 0x0B = No Secure Session - Remote command access
    ///        requires a secure session be established first
    /// 0x0C = Encryption error
    /// 0x0D = Command was sent insecurely
    /// </summary>
    public byte CommandStatus
    {
        get => _commandStatus;
    }
}
