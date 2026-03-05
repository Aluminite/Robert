using System.IO.Ports;

namespace Robert.RobotLogic;

public class HardwareInterface(string port, int baudRate) : IRobInterface
{
    private readonly SerialPort _serialPort = new SerialPort(port, baudRate);

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
            if (byteRead is >= '0' and <= '9')
            {
                return (byte)(byteRead - '0');
            }

            if (byteRead is >= 'a' and <= 'f')
            {
                return (byte)(byteRead - 'a' + 10);
            }
        }

        return null;
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