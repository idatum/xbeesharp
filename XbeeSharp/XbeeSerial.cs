namespace XbeeSharp;

using System.IO.Ports;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

/// <summary>
/// Communicate with XBee device via serial port.
/// </summary>
public class XbeeSerial
{
    private readonly ILogger _logger;
    private readonly CancellationToken _stoppingToken;

    public XbeeSerial(ILogger logger, CancellationToken stoppingToken)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stoppingToken = stoppingToken;
    }

    /// <summary>
    /// Construct a full frame from bytes read over serial (synchronous).
    /// </summary>
    public static bool TryReadNextFrame(out XbeeFrame? xbeeFrame, SerialPort serialPort, bool escaped)
    {
        xbeeFrame = null;
        var xbeeFrameBuilder = new XbeeFrameBuilder(escaped);
        
        while (true)
        {
            int readByte = serialPort.ReadByte();
            if (readByte == -1)
                return false;
                
            xbeeFrameBuilder.Append((byte)readByte);
            
            if (!xbeeFrameBuilder.FrameComplete)
                continue;

            if (!XbeeFrameBuilder.ChecksumValid(xbeeFrameBuilder.Data, escaped))
            {
                xbeeFrameBuilder.Reset();
                continue; // Try to recover from bad frame
            }
            
            xbeeFrame = xbeeFrameBuilder.ToXbeeFrame();
            return true;
        }
    }

    /// <summary>
    /// Construct a full frame from bytes read over serial (async).
    /// </summary>
    public async Task<XbeeFrame?> ReadNextFrameAsync(SerialPort serialPort, bool escaped)
    {
        var readBuffer = new byte[1];
        var xbeeFrameBuilder = new XbeeFrameBuilder(escaped);
        var stream = serialPort.BaseStream;

        while (!_stoppingToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(readBuffer, 0, 1, _stoppingToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                _logger.LogError("Zero bytes returned from serial port.");
                return null;
            }

            xbeeFrameBuilder.Append(readBuffer[0]);

            if (!xbeeFrameBuilder.FrameComplete)
                continue;

            if (!XbeeFrameBuilder.ChecksumValid(xbeeFrameBuilder.Data, escaped))
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    LogInvalidChecksum(xbeeFrameBuilder.Data);
                }
                xbeeFrameBuilder.Reset();
                continue; // Try to recover from bad frame
            }

            var xbeeFrame = xbeeFrameBuilder.ToXbeeFrame();
            
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Read frame type 0x{FrameType:X2}.", xbeeFrame.FrameType);
            }
            
            return xbeeFrame;
        }

        _logger.LogInformation("Cancelled.");
        return null;
    }

    private void LogInvalidChecksum(IReadOnlyList<byte> data)
    {
        var hexString = ConvertToHexString(data);
        _logger.LogError("Checksum invalid; frame data: {FrameData}", hexString);
    }

    private static string ConvertToHexString(IReadOnlyList<byte> data)
    {
        var chars = new char[data.Count * 2];
        for (int i = 0; i < data.Count; i++)
        {
            byte b = data[i];
            chars[i * 2] = ToHexChar(b >> 4);
            chars[i * 2 + 1] = ToHexChar(b & 0xF);
        }
        return new string(chars);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ToHexChar(int value) => (char)(value + (value < 10 ? '0' : ('A' - 10)));
}
