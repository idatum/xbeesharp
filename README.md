# xbeesharp
Initial .NET 6 C# implementation of minimal [Digi XBee](https://www.digi.com/products/embedded-systems/digi-xbee/rf-modules/2-4-ghz-rf-modules/xbee-zigbee) ZigBee API operations, focusing on transmit request, remote AT command request, and receive packet.

Initial applications include [xbee2mqtt](https://github.com/idatum/xbeesharp/tree/main/apps/xbee2mqtt), which maps XBee receive packets to MQTT messages, as well as transmitting messages and remote AT commands from subscribed MQTT messages.

Much work to do. This is the initial working prototype.

### References

[XBee/XBee-PRO® S2C Zigbee® RF Module User Guide](https://www.digi.com/resources/documentation/digidocs/pdfs/90002002.pdf)

[xbee-python](https://github.com/niolabs/python-xbee)
