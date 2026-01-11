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

    public byte GetCommand()
    {
        int byteRead = _serialPort.ReadByte();

        // Turn the one hexadecimal character into a byte
        if (byteRead >= '0' && byteRead <= '9')
        {
            return (byte)(byteRead - '0');
        }

        if (byteRead >= 'a' && byteRead <= 'f')
        {
            return (byte)(byteRead - 'a' + 10);
        }

        return 0xff;
    }

    private bool _aPressed;

    public void SetA(bool pressed)
    {
        if (pressed != _aPressed)
        {
            _serialPort.Write(pressed ? "A" : "a");
            _aPressed = pressed;
        }
    }

    private bool _bPressed;

    public void SetB(bool pressed)
    {
        if (pressed != _bPressed)
        {
            _serialPort.Write(pressed ? "B" : "b");
            _bPressed = pressed;
        }
    }
}