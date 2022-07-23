namespace XbeeSharp;

/// <summary>
/// I/O sample indicator packet.
/// </summary>
public class ReceiveIOPacket : ReceiveBasePacket
{
    /// <summary>
    /// Digital sample mask.
    /// </summary>
    private ushort _digitalChannelMask;
    /// <summary>
    /// Analog sample mask.
    /// </summary>
    private byte _analogChannelMask;
    /// <summary>
    /// Digital samples.
    /// </summary>
    private IReadOnlyList<(int Dio, bool Value)> _digitalSamples;
    /// <summary>
    /// Analog samples.
    /// </summary>
    private IReadOnlyList<(int Adc, ushort Value)> _analogSamples;

    /// <summary>
    /// Constructor.
    /// </summary>
    private ReceiveIOPacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                ushort networkAddress, byte receiveOptions,
                                ushort digitalChannelMask, byte analogChannelMask,
                                IReadOnlyList<(int Dio, bool Value)> digitalSamples, IReadOnlyList<(int Adc, ushort Value)> analogSamples)
                                : base(xbeeFrame, sourceAddress, networkAddress, receiveOptions)
    {
        _digitalChannelMask = digitalChannelMask;
        _analogChannelMask = analogChannelMask;
        _digitalSamples = digitalSamples;
        _analogSamples = analogSamples;
    }

    /// <summary>
    /// Create from XBee frame.
    /// </summary>
    public static bool Parse(out ReceiveIOPacket? packet, XbeeFrame xbeeFrame)
    {
        packet = null;
        const int DataOffset = 15;
        if (xbeeFrame.FrameType != XbeeFrame.PacketTypeReceiveIO ||
            xbeeFrame.FrameDataLength <= DataOffset)
        {
            return false;
        }
        // 64-bit source address.
        var sourceAddress = xbeeFrame.FrameData.Take(4..12).ToList();
        // 16-bit source network address.
        var networkAddress = XbeeFrameBuilder.ToBigEndian(xbeeFrame.FrameData[12], xbeeFrame.FrameData[13]);
        // Receive option.
        var receiveOptions = xbeeFrame.FrameData[14];
        // Sample count (offset 15) skipped and is always == 1.
        // Digital channel mask bytes.
        // 1st byte: x x x D12 D11 D10 x x
        // 2nd byte: D7 D6 D4 D3 D2 D1 D0
        ushort digitalChannelMask = XbeeFrameBuilder.ToBigEndian(xbeeFrame.FrameData[16], xbeeFrame.FrameData[17]);
        // Analog channel mask byte: x x x A3 A2 A1 A0
        byte analogChannelMask = xbeeFrame.FrameData[18];
        // Digital samples.
        var digitalSamples = new List<(int Dio, bool Value)>();
        ushort digitalValues = digitalChannelMask > 0 ? XbeeFrameBuilder.ToBigEndian(xbeeFrame.FrameData[19], xbeeFrame.FrameData[20]) : (ushort)0;
        for (var i = 0; i < 15; ++i)
        {
            var mask = ((ushort)1 << i) & digitalChannelMask;
            if (mask > 0)
            {
                bool bitVal = (digitalValues & mask) > 0 ? true : false;
                {
                    var sample = (Dio: i, Value: bitVal);
                    digitalSamples.Add(sample);
                }
            }
        }
        // Analog samples
        var analogSamples = new List<(int Adc, ushort Value)>();
        var analogOffset = digitalSamples.Count > 0 ? 21 : 19;
        foreach (var i in new int [] {0, 1, 2, 3, 7})
        {
            var mask = 0x01 << i;
            if (0 != (mask & analogChannelMask))
            {
                ushort adcVal = XbeeFrameBuilder.ToBigEndian(xbeeFrame.FrameData[analogOffset], xbeeFrame.FrameData[analogOffset + 1]);
                var sample = (Adc: i, Value: adcVal);
                analogSamples.Add(sample);
                analogOffset += 2;
            }
        }

        packet = new ReceiveIOPacket(xbeeFrame, sourceAddress, networkAddress, receiveOptions,
                                     digitalChannelMask, analogChannelMask,
                                     digitalSamples, analogSamples);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public const byte FrameType = XbeeFrame.PacketTypeReceiveIO;

    /// <summary>
    /// Digital sample mask.
    /// </summary>
    public ushort DigitalChannelMask
    {
        get => _digitalChannelMask;
    }

    /// <summary>
    /// Analog sample mask.
    /// </summary>
    public byte AnalogChannelMask
    {
        get => _analogChannelMask;
    }

    /// <summary>
    /// Digital samples.
    /// </summary>
    public IReadOnlyList<(int Dio, bool Value)> DigitalSamples
    {
        get => _digitalSamples;
    }

    /// <summary>
    /// Analog samples.
    /// </summary>
    public IReadOnlyList<(int Adc, ushort Value)> AnalogSamples
    {
        get => _analogSamples;
    }
}
