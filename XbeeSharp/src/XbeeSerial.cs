namespace XbeeSharp;

using System.IO.Ports;

/// <summary>
/// Communicate with XBee device via serial port.
/// </summary>
public class XbeeSerial
{
    /// <summary>
    /// Construct a full frame from bytes read over serial.
    /// </summary>
    public static async Task<XbeeFrame?> ReadNextFrameAsync(SerialPort serialPort, bool escaped)
    {
        XbeeFrame? xbeeFrame = null;
        byte [] readBuffer = new byte [1];
        var xbeeFrameBuilder = new XbeeFrameBuilder(escaped);
        while (true)
        {
            var bytesRead = await serialPort.BaseStream.ReadAsync(readBuffer, 0, 1);
            if (bytesRead == 0)
            {
                return null;
            }
            xbeeFrameBuilder.Append(readBuffer[0]);
            if (xbeeFrameBuilder.FrameComplete)
            {
                if (!XbeeFrameBuilder.ChecksumValid(xbeeFrameBuilder.Data, escaped))
                {
                    return null;
                }
                xbeeFrame = xbeeFrameBuilder.ToXbeeFrame();
                xbeeFrameBuilder.Reset();
                return xbeeFrame;
            }
        }
    }

    /// <summary>
    /// Construct a full frame from bytes read over serial.
    /// </summary>
    public static bool TryReadNextFrame(out XbeeFrame? xbeeFrame, SerialPort serialPort, bool escaped)
    {
        xbeeFrame = null;
        var xbeeFrameBuilder = new XbeeFrameBuilder(escaped);
        while (true)
        {
            byte nextByte = (byte)serialPort.ReadByte();
            xbeeFrameBuilder.Append(nextByte);
            if (xbeeFrameBuilder.FrameComplete)
            {
                if (!XbeeFrameBuilder.ChecksumValid(xbeeFrameBuilder.Data, escaped))
                {
                    return false;
                }
                xbeeFrame = xbeeFrameBuilder.ToXbeeFrame();
                xbeeFrameBuilder.Reset();
                return true;
            }
        }
    }
}
