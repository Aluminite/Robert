using System.Text;

namespace Robert_CLI;

public record StackUpRobotState : RobotState
{
    public required StackUpRobot.Block[][] Blocks { get; init; }
    public required StackUpRobot.Block[] HeldBlocks { get; init; }
    public required StackUpRobot.Block[] ToppledBlocks { get; init; }

    public override string Visualize()
    {
        int rotationInt = (int)Math.Round(Rotation) + 2;
        int heightInt = (int)Math.Round(Height);
        StringBuilder output = new StringBuilder(400);

        output.AppendFormat("L/R: {0:0.000} Height: {1:0.000} Arms: {2:0.000} LED: {3}\e[K\n", Rotation,
            Height, ArmsDistance, LedOn ? "\e[101m\e[97mOn\e[0m" : "Off");

        for (int extraRow = 9; extraRow >= 6; extraRow--)
        {
            for (int col = 0; col <= 4; col++)
            {
                if (rotationInt == col && extraRow <= heightInt + 4)
                {
                    output.Append(' ');
                    output.Append(StackUpRobot.BlockToColoredLetter(HeldBlocks[extraRow - heightInt], " "));
                    output.Append(' ');
                }
                else
                {
                    output.Append("   ");
                }
            }

            output.Append("\e[K\n");
        }

        bool armsOpen = ArmsDistance > 0.7;

        for (int row = 5; row >= 0; row--)
        {
            for (int col = 0; col <= 4; col++)
            {
                bool blockHeldHere = rotationInt == col && row <= heightInt + 4 && row >= heightInt &&
                                     HeldBlocks[row - heightInt] != StackUpRobot.Block.Empty;

                string color = StackUpRobot.BlockToColoredLetter(
                    blockHeldHere ? HeldBlocks[row - heightInt] : Blocks[col][row],
                    "-");

                if (row == heightInt && col == rotationInt)
                {
                    output.Append(armsOpen ? '<' : '>');
                    output.Append(color);
                    output.Append(armsOpen ? '>' : '<');
                }
                else
                {
                    output.Append(' ');
                    output.Append(color);
                    output.Append(' ');
                }
            }

            output.Append("\e[K\n");
        }

        if (ToppledBlocks.Length > 0)
        {
            output.Append("Toppled blocks: ");
            foreach (StackUpRobot.Block toppled in ToppledBlocks)
            {
                output.Append(StackUpRobot.BlockToColoredLetter(toppled, " "));
                output.Append(' ');
            }

            output.Append("\e[K\n");
        }

        return output.ToString();
    }
}