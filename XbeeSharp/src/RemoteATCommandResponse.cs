namespace XbeeSharp;

/// <summary>
/// Extended transmit status packet.
/// </summary>
public class RemoteATCommandResponse
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
    /// Construct receive packet.
    /// </summary>
    private RemoteATCommandResponse(XbeeFrame xbeeFrame, byte frameId, IReadOnlyList<byte> sourceAddress,
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
    /// Create receive packet from XBee frame.
    /// </summary>
    public static bool Parse(out RemoteATCommandResponse? packet, XbeeFrame xbeeFrame)
    {
        packet = null;

        if (xbeeFrame.FrameType != XbeeFrame.PacketTypeRemoteATCommandResponse)
        {
            return false;
        }
        // Frame ID.
        byte frameId = xbeeFrame.FrameData[4];
        // 64-bit source address.
        var sourceAddress = xbeeFrame.FrameData.Take(5..13).ToList();
        // Network address.
        ushort networkAddress = (ushort)(256 * xbeeFrame.FrameData[13] + xbeeFrame.FrameData[14]);
        // AT command.
        var command = new string(new char [] {(char)xbeeFrame.FrameData[15], (char)xbeeFrame.FrameData[16]});
        // Command status
        byte commandStatus = xbeeFrame.FrameData[17];

        packet = new RemoteATCommandResponse(xbeeFrame, frameId, sourceAddress, networkAddress, command, commandStatus);

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
    /// </summary>
    public byte CommandStatus
    {
        get => _commandStatus;
    }
}
