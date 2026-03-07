namespace Robert.RobotLogic;

public class DummyInterface : IRobInterface
{
    public bool Active => false;

    public byte? GetCommand()
    {
        return null;
    }

    public void SetA(bool pressed)
    {
    }

    public void SetB(bool pressed)
    {
    }

    public void Connect()
    {
    }

    public void Disconnect()
    {
    }
}