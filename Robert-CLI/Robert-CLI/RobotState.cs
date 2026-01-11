using System.Text;

namespace Robert_CLI;

public record RobotState
{
    public required double Height { get; init; }
    public required double Rotation { get; init; }
    public required double ArmsDistance { get; init; }
    public required bool LedOn { get; init; }
    public required Robot.Action CurrentAction { get; init; }
    
    public virtual string Visualize()
    {
        int rotationInt = (int)Math.Round(Rotation) + 2;
        int heightInt = (int)Math.Round(Height);
        StringBuilder output = new StringBuilder(200);

        output.AppendFormat("L/R: {0:0.000} Height: {1:0.000} Arms: {2:0.000} LED: {3}\e[K\n", Rotation,
            Height, ArmsDistance, LedOn ? "On " : "Off");

        bool armsOpen = ArmsDistance > 0.7;
        for (int row = 5; row >= 0; row--)
        {
            for (int col = 0; col <= 4; col++)
            {
                if (row == heightInt && col == rotationInt)
                {
                    output.Append(armsOpen ? "<->" : ">-<");
                }
                else
                {
                    output.Append(" - ");
                }
            }

            output.Append("\e[K\n");
        }

        return output.ToString();
    }
}