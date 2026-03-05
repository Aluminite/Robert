namespace Robert.RobotLogic;

public interface IRobInterface
{
    bool Active { get; }
    byte? GetCommand();
    void SetA(bool pressed);
    void SetB(bool pressed);
    void Connect();
    void Disconnect();
}