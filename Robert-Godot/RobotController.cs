using Godot;
using Robert.RobotLogic;

namespace Robert;

public partial class RobotController : Node
{
    public Robot Robot;
    public IRobInterface Interface;
    private RobotVisual _robotVisual;

    public override void _Ready()
    {
        Robot = new StackUpRobot();
        Interface = new EmuInterface("localhost", 8012);
        Interface.Connect();
        _robotVisual = GetNode<RobotVisual>("../Robot");
    }

    public override void _Process(double delta)
    {
        byte? commandByte = Interface.GetCommand();
        if (commandByte != null)
        {
            Robot.Command? command = Robot.CommandByteToEnum(commandByte.Value);
            if (command != null) Robot.StartAction(command.Value);
        }
        
        Robot.Tick();
        RobotState state = Robot.CurrentState;
        
        _robotVisual.ApplyState(state);
        
        if (state is GyromiteRobotState gyromiteState && Interface.Active)
        {
            Interface.SetA(gyromiteState.APressed);
            Interface.SetB(gyromiteState.BPressed);
        }
    }
}