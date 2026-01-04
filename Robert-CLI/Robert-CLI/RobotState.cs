namespace Robert_CLI;

public record RobotState
{
    public required double Height { get; init; }
    public required double Rotation { get; init; }
    public required double ArmsDistance { get; init; }
    public required bool LedOn { get; init; }
    public required Robot.Action CurrentAction { get; init; }
}