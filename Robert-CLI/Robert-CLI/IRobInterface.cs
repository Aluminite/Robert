namespace Robert_CLI;

public interface IRobInterface
{
    bool Active { get; }
    byte GetCommand();
    void PressA();
    void PressB();
    void ReleaseA();
    void ReleaseB();
    void Connect();
    void Disconnect();
}