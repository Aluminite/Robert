using System.IO.Ports;

namespace Robert_CLI;

public class HardwareInterface : IRobInterface
{
    private readonly SerialPort _serialPort;

    public HardwareInterface(string port, int baudRate)
    {
        _serialPort = new SerialPort(port, baudRate);
    }

    public bool Active => _serialPort.IsOpen;

    public void Connect()
    {
        _serialPort.Open();
    }

    public void Disconnect()
    {
        _serialPort.Close();
    }

    public byte? GetCommand()
    {
        if (_serialPort.BytesToRead >= 1)
        {
            int byteRead = _serialPort.ReadByte();
            
            // Turn the one hexadecimal character into a byte
            if (byteRead >= '0' && byteRead <= '9')
            {
                return (byte) (byteRead - '0');
            }
            if (byteRead >= 'a' && byteRead <= 'f')
            {
                return (byte) (byteRead - 'a' + 10);
            }
        }

        return null;
    }

    public void PressA()
    {
        _serialPort.Write("A");
    }

    public void ReleaseA()
    {
        _serialPort.Write("a");
    }

    public void PressB()
    {
        _serialPort.Write("B");
    }

    public void ReleaseB()
    {
        _serialPort.Write("b");
    }
}