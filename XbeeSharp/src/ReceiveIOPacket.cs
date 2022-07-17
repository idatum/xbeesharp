namespace XbeeSharp;

/// <summary>
/// Sample IO frame.
/// </summary>
public class ReceiveIOPacket : XbeeBasePacket
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
    private IReadOnlyList<string> _digitalSamples;
    /// <summary>
    /// Analog samples.
    /// </summary>
    private IReadOnlyList<string> _analogSamples;

    /// <summary>
    /// Construct IO sample packet.
    /// </summary>
    private ReceiveIOPacket(XbeeFrame xbeeFrame, IReadOnlyList<byte> sourceAddress,
                                byte receiveOptions,
                                ushort digitalChannelMask, byte analogChannelMask,
                                IReadOnlyList<string> digitalSamples, IReadOnlyList<string> analogSamples)
                                : base(xbeeFrame, sourceAddress, receiveOptions)
    {
        _digitalChannelMask = digitalChannelMask;
        _analogChannelMask = analogChannelMask;
        _digitalSamples = digitalSamples;
        _analogSamples = analogSamples;
    }

    /// <summary>
    /// Create IO sample packet from XBee frame.
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
        var frameData = new List<byte>(xbeeFrame.FrameData);
        // 64-bit source address.
        var sourceAddress = frameData.GetRange(4, 8);
        // 16-bit source network address.
        var networkAddress = frameData.GetRange(12, 2);
        // Receive option.
        var receiveOption = frameData[14];
        // Sample sets count.
        var sampleCount = frameData[15];
        // Digital channel mask bytes.
        // 1st byte: x x x D12 D11 D10 x x
        // 2nd byte: D7 D6 D4 D3 D2 D1 D0
        ushort digitalChannelMask = (ushort)(0xff * frameData[16] + frameData[17]);
        // Analog channel mask byte: x x x A3 A2 A1 A0
        byte analogChannelMask = frameData[18];
        // Extract sample pairs.
        // Digital samples.
        var digitalSamples = new List<string>();
        ushort digitalValues = digitalChannelMask > 0 ? (ushort)(frameData[19] * 0xFF + frameData[20]) : (ushort)0;
        for (var i = 0; i < 15; ++i)
        {
            var mask = ((ushort)1 << i) & digitalChannelMask;
            if (mask > 0)
            {
                int bitVal = (digitalValues & mask) > 0 ? 1 : 0;
                {
                    var sample = $"DIO{i}={bitVal}";
                    digitalSamples.Add(sample);
                }
            }
        }
        // Analog samples
        var analogSamples = new List<string>();
        var analogOffset = digitalSamples.Count > 0 ? 22 : 19;
        foreach (var i in new int [] {0, 1, 2, 3, 7})
        {
            var mask = 0x01 << i;
            if (0 != (mask & analogChannelMask))
            {
                var adcVal = frameData[analogOffset];
                var sample = i < 7 ? $"AD{i}={adcVal}" : $"V+={adcVal}";
                analogSamples.Add(sample);
                analogOffset += 2;
            }
        }

        packet = new ReceiveIOPacket(xbeeFrame, sourceAddress, receiveOption,
                                        digitalChannelMask, analogChannelMask,
                                        digitalSamples, analogSamples);

        return true;
    }

    /// <summary>
    /// XBee frame indicator.
    /// </summary>
    public byte FrameType
    {
        get => XbeeFrame.PacketTypeReceiveIO;
    }

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
    public IReadOnlyList<string> DigitalSamples
    {
        get => _digitalSamples;
    }

    /// <summary>
    /// Analog samples.
    /// </summary>
    public IReadOnlyList<string> AnalogSamples
    {
        get => _analogSamples;
    }
}
