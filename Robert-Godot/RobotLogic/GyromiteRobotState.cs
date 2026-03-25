#nullable enable
using System;
using System.Text;

namespace Robert.RobotLogic;

public record GyromiteRobotState : RobotState
{
    public record GyroState
    {
        public required int Number { get; init; }
        public required int Column { get; init; }
        public required int Height { get; init; }
        public required bool Toppled { get; init; }
        public required TimeSpan SpinTimer { get; init; }
    }

    public required bool APressed { get; init; }
    public required bool BPressed { get; init; }
    public required GyroState[] Gyros { get; init; }
    public required GyroState? HeldItem { get; init; }
}