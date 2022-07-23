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
