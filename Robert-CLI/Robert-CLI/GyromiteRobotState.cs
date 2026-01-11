using System.Text;

namespace Robert_CLI;

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

        output.AppendFormat("L/R: {0:0.000} Height: {1:0.000} Arms: {2:0.000} LED: {3}\e[K\n", Rotation,
            Height, ArmsDistance, LedOn ? "On " : "Off");

        bool armsOpen = ArmsDistance > 0.3;
        for (int row = 5; row >= 0; row--)
        {
            for (int col = 0; col <= 4; col++)
            {
                string cellChar = "-";
                if (col == 0 && row is 0 or 1) cellChar = "\e[40m\e[97mS\e[0m";
                foreach (GyroState gyro in Gyros)
                {
                    if (col == gyro.Column)
                    {
                        switch (gyro.Height - row)
                        {
                            case 0:
                                cellChar = "\e[97m|\e[0m";
                                break;
                            case 1:
                                cellChar = $"\e[97m{gyro.Number + 1}\e[0m";
                                break;
                            case 2 or 3 when cellChar != "\e[40m\e[97mS\e[0m":
                                cellChar = "\e[97m|\e[0m";
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

            output.Append("\e[K\n");
        }

        output.Append(" \e[40m\e[97mS\e[0m  \e[101m\e[97mB\e[0m  \e[104m\e[97mA\e[0m  \e[40m\e[97mT\e[0m  \e[40m\e[97mT\e[0m \e[K\n");
        

        foreach (GyroState gyro in Gyros)
        {
            output.AppendFormat("Gyro {0}: Toppled: {1}, Spin timer: {2}\e[K\n", gyro.Number + 1, gyro.Toppled, gyro.SpinTimer);
        }
        
        output.Append("Pressed buttons: ");
        if (APressed) output.Append("A ");
        if (BPressed) output.Append("B ");
        output.Append("\e[K\n");

        return output.ToString();
    }
}