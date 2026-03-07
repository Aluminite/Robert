using System.IO.Ports;

namespace Robert.RobotLogic;

public class HardwareInterface(string port, int baudRate) : IRobInterface
{
    private readonly SerialPort _serialPort = new SerialPort(port, baudRate);

    public bool Active => _serialPort.IsOpen;
    private bool _aPressed;
    private bool _bPressed;

    public void Connect()
    {
        _serialPort.Open();
        _serialPort.Write(_aPressed ? "A" : "a");
        _serialPort.Write(_bPressed ? "B" : "b");
    }

    public void Disconnect()
    {
        _serialPort.Close();
    }

    public byte? GetCommand()
    {
        if (Active && _serialPort.BytesToRead >= 1)
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

    public void SetA(bool pressed)
    {
        if (pressed != _aPressed)
        {
            _aPressed = pressed;
            if (Active) _serialPort.Write(pressed ? "A" : "a");
        }
    }

    public void SetB(bool pressed)
    {
        if (pressed != _bPressed)
        {
            _bPressed = pressed;
            if (Active) _serialPort.Write(pressed ? "B" : "b");
        }
    }
}