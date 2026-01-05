using System.Net;
using System.Net.Sockets;

namespace Robert_CLI;

public class EmuInterface : IRobInterface
{
    private readonly Socket _socket;
    private readonly IPEndPoint _endPoint;

    public EmuInterface(string hostname, int port)
    {
        IPHostEntry ipHost = Dns.GetHostEntry(hostname);
        IPAddress ipAddr = ipHost.AddressList[0];
        _endPoint = new IPEndPoint(ipAddr, port);
        _socket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }

    public bool Active => _socket.Connected;

    public void Connect()
    {
        _socket.Connect(_endPoint);
    }

    public void Disconnect()
    {
        _socket.Disconnect(true);
    }

    public byte GetCommand()
    {
        if (!_socket.Connected) throw new InvalidOperationException("Not connected yet");

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

    public void PressA()
    {
        _socket.Send(new[] { (byte)'A' });
    }

    public void PressB()
    {
        _socket.Send(new[] { (byte)'B' });
    }

    public void ReleaseA()
    {
        _socket.Send(new[] { (byte)'a' });
    }

    public void ReleaseB()
    {
        _socket.Send(new[] { (byte)'a' });
    }
}