namespace XbeeSharp;

using System.Text;
using System.IO.Ports;
using Microsoft.Extensions.Logging;

/// <summary>
/// Communicate with XBee device via serial port.
/// </summary>
public class XbeeSerial
{
    /// <summary>
    /// Logging.
    /// </summary>
    private ILogger _logger;
    /// <summary>
    /// Cancellation token.
    /// </summary>
    private CancellationToken _stoppingToken;

    /// <summary>
    /// Constructor.
    /// </summary>
    public XbeeSerial(ILogger logger, CancellationToken stoppingToken)
    {
        if (logger is null)
        {
            throw new ArgumentNullException("logger");
        }

        _logger = logger;
        _stoppingToken = stoppingToken;
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

    /// <summary>
    /// Construct a full frame from bytes read over serial.
    /// </summary>
    public async Task<XbeeFrame?> ReadNextFrameAsync(SerialPort serialPort, bool escaped)
    {
        XbeeFrame? xbeeFrame = null;
        byte[] readBuffer = new byte[1];
        var xbeeFrameBuilder = new XbeeFrameBuilder(escaped);
        while (!_stoppingToken.IsCancellationRequested)
        {
            var bytesRead = await serialPort.BaseStream.ReadAsync(readBuffer, 0, 1);
            if (bytesRead == 0)
            {
                _logger.LogError("Zero bytes returned from serial port.");
                return null;
            }
            xbeeFrameBuilder.Append(readBuffer[0]);
            if (xbeeFrameBuilder.FrameComplete)
            {
                if (!XbeeFrameBuilder.ChecksumValid(xbeeFrameBuilder.Data, escaped))
                {
                    var frameDataBuilder = new StringBuilder();
                    foreach (var b in xbeeFrameBuilder.Data)
                    {
                        frameDataBuilder.Append($"{b:X2}");
                    }
                    _logger.LogError($"Checksum invalid; frame data: {frameDataBuilder.ToString()}");
                    return null;
                }
                xbeeFrame = xbeeFrameBuilder.ToXbeeFrame();
                xbeeFrameBuilder.Reset();
                _logger.LogDebug($"Read frame type 0x{xbeeFrame.FrameType:X2}.");

                return xbeeFrame;
            }
        }

        _logger.LogInformation("Cancelled.");

        return null;
    }
}
