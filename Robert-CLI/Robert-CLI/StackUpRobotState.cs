namespace Robert_CLI;

public record StackUpRobotState : RobotState
{
    public required StackUpRobot.Block[][] Blocks { get; init; }
    public required StackUpRobot.Block[] HeldBlocks { get; init; }
    public required StackUpRobot.Block[] ToppledBlocks { get; init; }
}