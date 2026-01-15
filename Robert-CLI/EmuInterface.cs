using System.Net.Sockets;

namespace Robert_CLI;

public class EmuInterface(string hostname, int port) : IRobInterface
{
    private readonly Socket _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

    public bool Active => _socket.Connected;

    public void Connect()
    {
        _socket.Connect(hostname, port);
    }

    public void Disconnect()
    {
        _socket.Disconnect(true);
    }

    public byte GetCommand()
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

        return 0xff;
    }

    private bool _aPressed;
    
    public void SetA(bool pressed)
    {
        if (pressed != _aPressed)
        {
            _socket.Send(new[] { pressed ? (byte)'A' : (byte)'a' });
            _aPressed = pressed;
        }
    }

    private bool _bPressed;
    
    public void SetB(bool pressed)
    {
        if (pressed != _bPressed)
        {
            _socket.Send(new[] { pressed ? (byte)'B' : (byte)'b' });
            _bPressed = pressed;
        }
    }
}