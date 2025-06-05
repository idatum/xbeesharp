using System.IO.Ports;
using System.Text;
using XbeeSharp;

const string SerialPortName = "/dev/ttyUSB0";
const int SerialBaudRate = 115200;
const bool Escaped = true;

var serialPort = new SerialPort(SerialPortName, SerialBaudRate)
{
    WriteTimeout = 500
};
serialPort.Open();
while (true)
{
    if (XbeeSerial.TryReadNextFrame(out XbeeFrame? xbeeFrame, serialPort, Escaped))
    {
        if (xbeeFrame is null)
        {
            continue;
        }
        if (xbeeFrame.FrameType == XbeeFrame.PacketTypeReceive)
        {
            if (!ReceivePacket.Parse(out ReceivePacket? receivePacket, xbeeFrame) || receivePacket is null)
            {
                Console.WriteLine("Invalid receive packet.");
                continue;
            }

            var optionText = receivePacket.ReceiveOptions == 1 ? "with response" : "broadcast";
            var sourceAddress = receivePacket.SourceAddress.AsString();
            var data = Encoding.Default.GetString(receivePacket.ReceiveData.ToArray());
            Console.WriteLine($"RX packet {optionText} from {sourceAddress}: {data}");
        }
        else if (xbeeFrame.FrameType == XbeeFrame.PacketTypeReceiveIO)
        {
            if (!ReceiveIOPacket.Parse(out ReceiveIOPacket? receivePacket, xbeeFrame) || receivePacket is null)
            {
                Console.WriteLine("Invalid receive IO packet.");
                continue;
            }
            var optionText = receivePacket.ReceiveOptions == 1 ? "with response" : "broadcast";
            var sourceAddress = receivePacket.SourceAddress.AsString();
            var analogMask = receivePacket.AnalogChannelMask;
            var digitalMask = receivePacket.DigitalChannelMask;
            Console.WriteLine($"RX IO {optionText} from {sourceAddress}: analog mask: {analogMask:X2}, digital mask {digitalMask:X4}");
        }
        else
        {
            Console.WriteLine($"Unsupported frame type: {xbeeFrame.FrameType:X2}");
        }
    }
}
