using System.Net.Sockets;

namespace Robert.RobotLogic;

public class EmuInterface(string hostname, int port) : IRobInterface
{
    private readonly Socket _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

    public bool Active => _socket.Connected;
    private bool _aPressed;
    private bool _bPressed;

    public void Connect()
    {
        _socket.Connect(hostname, port);
        _socket.Send(new[] { _aPressed ? (byte)'A' : (byte)'a' });
        _socket.Send(new[] { _bPressed ? (byte)'B' : (byte)'b' });
    }

    public void Disconnect()
    {
        _socket.Disconnect(false);
    }

    public byte? GetCommand()
    {
        // Is there actually data available?
        if (Active && _socket.Poll(0, SelectMode.SelectRead))
        {
            byte[] buffer = new byte[1];
            _socket.Receive(buffer, 1, SocketFlags.None);

            // Turn the one hexadecimal character into a byte
            if (buffer[0] >= '0' && buffer[0] <= '9')
            {
                return (byte)(buffer[0] - '0');
            }

            if (buffer[0] >= 'a' && buffer[0] <= 'f')
            {
                return (byte)(buffer[0] - 'a' + 10);
            }
        }

        return null;
    }

    public void SetA(bool pressed)
    {
        if (pressed != _aPressed)
        {
            _aPressed = pressed;
            if (Active) _socket.Send(new[] { pressed ? (byte)'A' : (byte)'a' });
        }
    }

    public void SetB(bool pressed)
    {
        if (pressed != _bPressed)
        {
            _bPressed = pressed;
            if (Active) _socket.Send(new[] { pressed ? (byte)'B' : (byte)'b' });
        }
    }
}