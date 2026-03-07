using System.Net.Sockets;
using Godot;
using Robert.RobotLogic;

namespace Robert;

public partial class RobotController : Node
{
    public Robot Robot;
    public IRobInterface Interface;
    private RobotVisual _robotVisual;
    private ConfigManager _configManager;

    public override void _Ready()
    {
        Robot = new Robot();
        Interface = new DummyInterface();
        _robotVisual = GetNode<RobotVisual>("../Robot");
        _configManager = GetNode<ConfigManager>("../ConfigManager");
    }

    public override void _Process(double delta)
    {
        byte? commandByte = null;
        try
        {
            commandByte = Interface.GetCommand();
        }
        catch (SocketException ex)
        {
            _configManager.ShowError($"Error: {ex.Message}");
        }

        if (commandByte != null)
        {
            Robot.Command? command = Robot.CommandByteToEnum(commandByte.Value);
            if (command != null) Robot.StartAction(command.Value);
        }

        Robot.Tick();
        RobotState state = Robot.CurrentState;

        _robotVisual.ApplyState(state);

        if (state is GyromiteRobotState gyromiteState)
        {
            try
            {
                Interface.SetA(gyromiteState.APressed);
                Interface.SetB(gyromiteState.BPressed);
            }
            catch (SocketException ex)
            {
                _configManager.ShowError($"Error: {ex.Message}");
            }
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            Interface.Disconnect();
        }
    }
}