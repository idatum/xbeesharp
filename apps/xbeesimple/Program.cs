﻿using System.IO.Ports;
using System.Text;
using XbeeSharp;

const string SerialPortName = "/dev/ttyUSB0";
const int SerialBaudRate = 115200;
const bool Escaped = true;

var serialPort = new SerialPort(SerialPortName, SerialBaudRate);
serialPort.Open();
while (true)
{
    XbeeFrame? xbeeFrame;
    if (XbeeSerial.TryReadNextFrame(out xbeeFrame, serialPort, Escaped))
    {
        if (xbeeFrame == null)
        {
            continue;
        }
        if (xbeeFrame.FrameType == XbeeBasePacket.PacketTypeReceive)
        {
            XbeeReceivePacket? receivePacket;
            if (!XbeeReceivePacket.Parse(out receivePacket, xbeeFrame) || receivePacket == null)
            {
                Console.WriteLine("Invalid receive packet.");
                continue;
            }

            var optionText = receivePacket.ReceiveOptions == 1 ? "with response" : "broadcast";
            var sourceAddress = XbeeAddress.Create(receivePacket.SourceAddress).AsString();
            var data = Encoding.Default.GetString(receivePacket.ReceiveData.ToArray());
            Console.WriteLine($"RX packet {optionText} from {sourceAddress}: {data}");
        }
        else if (xbeeFrame.FrameType == XbeeBasePacket.PacketTypeReceiveIO)
        {
            XbeeReceiveIOPacket? receivePacket;
            if (!XbeeReceiveIOPacket.Parse(out receivePacket, xbeeFrame) || receivePacket == null)
            {
                Console.WriteLine("Invalid receive IO packet.");
                continue;
            }
            var optionText = receivePacket.ReceiveOptions == 1 ? "with response" : "broadcast";
            var sourceAddress = XbeeAddress.Create(receivePacket.SourceAddress).AsString();
            var sampleCount = receivePacket.sampleCount;
            var analogMask = receivePacket.AnalogChannelMask;
            var digitalMask = receivePacket.DigitalChannelMask;
            Console.WriteLine($"RX IO {optionText} from {sourceAddress}, sample count {sampleCount}: analog mask: {analogMask:X2}, digital mask {digitalMask:X4}");
        }
        else
        {
            Console.WriteLine($"Unsupported frame type: {xbeeFrame.FrameType:X2}");
        }
    }
}
