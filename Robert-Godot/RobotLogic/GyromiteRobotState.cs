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

    public override string Visualize()
    {
        int rotationInt = (int)Math.Round(Rotation) + 2;
        int heightInt = (int)Math.Round(Height);
        StringBuilder output = new StringBuilder(200);

        output.AppendFormat("L/R: {0:0.000} Height: {1:0.000} Arms: {2:0.000} LED: {3}\x1b[K\n", Rotation,
            Height, ArmsDistance, LedOn ? "\x1b[101m\x1b[97mOn\x1b[0m" : "Off");

        bool armsOpen = ArmsDistance > 0.3;
        for (int row = 5; row >= 0; row--)
        {
            for (int col = 0; col <= 4; col++)
            {
                string cellChar = "-";
                if (col == 0 && row is 0 or 1) cellChar = "\x1b[40m\x1b[97mS\x1b[0m";
                foreach (GyroState gyro in Gyros)
                {
                    if (col == gyro.Column)
                    {
                        switch (gyro.Height - row)
                        {
                            case 0:
                                cellChar = "\x1b[97m|\x1b[0m";
                                break;
                            case 1:
                                cellChar = $"\x1b[97m{gyro.Number + 1}\x1b[0m";
                                break;
                            case 2 or 3 when cellChar != "\x1b[40m\x1b[97mS\x1b[0m":
                                cellChar = "\x1b[97m|\x1b[0m";
                                break;
                        }
                    }
                }

                if (row == heightInt && col == rotationInt)
                {
                    output.Append(armsOpen ? '<' : '>');
                    output.Append(cellChar);
                    output.Append(armsOpen ? '>' : '<');
                }
                else
                {
                    output.Append(' ');
                    output.Append(cellChar);
                    output.Append(' ');
                }
            }

            output.Append("\x1b[K\n");
        }

        output.Append(
            " \x1b[40m\x1b[97mS\x1b[0m  \x1b[101m\x1b[97mB\x1b[0m  \x1b[104m\x1b[97mA\x1b[0m  \x1b[40m\x1b[97mT\x1b[0m  \x1b[40m\x1b[97mT\x1b[0m \x1b[K\n");


        foreach (GyroState gyro in Gyros)
        {
            output.AppendFormat("Gyro {0}: Toppled: {1}, Spin timer: {2}\x1b[K\n", gyro.Number + 1, gyro.Toppled,
                gyro.SpinTimer);
        }

        output.Append("Pressed buttons: ");
        if (APressed) output.Append("A ");
        if (BPressed) output.Append("B ");
        output.Append("\x1b[K\n");

        return output.ToString();
    }
}