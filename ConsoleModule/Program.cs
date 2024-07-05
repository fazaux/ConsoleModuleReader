using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static SerialPort _serialPort;
    static AutoResetEvent _dataReceivedEvent = new AutoResetEvent(false);
    static byte[] _header = new byte[] { 0xC8, 0x8C };
    static byte[] _endFrame = new byte[] { 0x0D, 0x0A };

    static async Task Main(string[] args)
    {
        _serialPort = new SerialPort("COM8", 115200, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            ReadTimeout = 500,
            WriteTimeout = 500
        };
        _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        _serialPort.Open();

        await SendDataAsync(HexCommand.getTransmitPower);


        while (true)
        {
            string input = Console.ReadLine();
            if (input == "exit")
            {
                break;
            }
            else if (input == "scan")
            {
                await SendDataAsync(HexCommand.continouesScan);
            }
            else if (input == "stop")
            {
                await SendDataAsync(HexCommand.stopScan);
            }
            else if (input == "version")
            {
                await SendDataAsync(HexCommand.getHardwareVersion);
                await SendDataAsync(HexCommand.getFirmwareVersion);
                await SendDataAsync(HexCommand.getDeviceID);
            }
            else if (input == "power")
            {
                await SendDataAsync(HexCommand.getTransmitPower);
            }
            else if (input == "antena")
            {
                await SendDataAsync(HexCommand.getAntenaStatus);
            }
        }

        Console.ReadKey();
        _serialPort.Close();
    }

    private static async Task SendDataAsync(byte[] data)
    {
        if (_serialPort.IsOpen)
        {
            await _serialPort.BaseStream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"Sent: {BitConverter.ToString(data)}");
        }
    }
    private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        if (sender is SerialPort sp)
        {
            try
            {
                int bytesToRead = sp.BytesToRead;
                byte[] buffer = new byte[bytesToRead];
                sp.Read(buffer, 0, bytesToRead);

                int i = 0;

                while (i <= buffer.Length - _header.Length - 2 - _endFrame.Length)
                {
                    // Check for header
                    if (buffer[i] == _header[0] && buffer[i + 1] == _header[1])
                    {
                        // Check for frame length
                        int frameLength = (buffer[i + 2] << 8) + buffer[i + 3];

                        // Check if the frame length matches the remaining data length
                        if (i + frameLength <= buffer.Length)
                        {
                            // Check for footer
                            if (buffer[i + frameLength - 2] == _endFrame[0] && buffer[i + frameLength - 1] == _endFrame[1])
                            {
                                // Valid frame found
                                byte[] frame = new byte[frameLength];
                                Array.Copy(buffer, i, frame, 0, frameLength);

                                Console.WriteLine($"Valid frame received: {BitConverter.ToString(frame)} | {frame.Length} | {frame[4]:X2}");

                                // Process the valid frame as needed
                                //ProcessValidFrame(frame);

                                // Move index to the end of the current frame
                                i += frameLength;
                            }
                            else
                            {
                                // Invalid footer, skip this frame and continue searching
                                i += 2;
                            }
                        }
                        else
                        {
                            // Incomplete frame, wait for more data
                            break;
                        }
                    }
                    else
                    {
                        i++; // Move forward to continue searching for the next header
                    }
                }
            }
            catch (TimeoutException) { }
        }
    }

    //private static async Task continouesScan()
    //{
    //    while (true)
    //    {
    //        _dataReceivedEvent.WaitOne();
    //        await Task.Delay(100);
    //    }
    //}

    // **************************************************************************************
    // Hex Command  
    // **************************************************************************************

    internal static class HexCommand
    {
        // Scan Command
        public static byte[] continouesScan { get; } = new byte[] { 0xC8, 0x8C, 0x00, 0x0A, 0x82, 0x27, 0x10, 0xBF, 0x0D, 0x0A };
        public static byte[] singleScan { get; } = new byte[] { 0xC8, 0x8C, 0x00, 0x0A, 0x80, 0x00, 0x64, 0xEE, 0x0D, 0x0A };
        public static byte[] stopScan { get; } = new byte[] { 0xC8, 0x8C, 0x00, 0x08, 0x8C, 0x84, 0x0D, 0x0A };

        // get version command
        public static byte[] getHardwareVersion { get; } = new byte[] { 0xC8, 0x8C, 0x00, 0x08, 0x00, 0x08, 0x0D, 0x0A };
        public static byte[] getFirmwareVersion { get; } = new byte[] { 0xC8, 0x8C, 0x00, 0x08, 0x02, 0x0A, 0x0D, 0x0A };
        public static byte[] getDeviceID { get; } = new byte[] { 0xC8, 0x8C, 0x00, 0x08, 0x04, 0x0C, 0x0D, 0x0A };

        // get transmit command
        public static byte[] getTransmitPower { get; } = new byte[] { 0xC8, 0x8C, 0x00, 0x08, 0x12, 0x1A, 0x0D, 0x0A };

        // antenna
        public static byte[] getAntenaStatus { get; } = new byte[] { 0xC8, 0x8C, 0x00, 0x08, 0x2A, 0x22, 0x0D, 0x0A };
    }
}
