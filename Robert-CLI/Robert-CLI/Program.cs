using System.Text;
using System.Text.Json;

namespace Robert_CLI;

class Program
{
    private static readonly Robot Rob = new Robot();

    private static void Main()
    {
        const string configFile = "robert-config.json";
        Config? config = null;
        try
        {
            string jsonString = File.ReadAllText(configFile);
            config = JsonSerializer.Deserialize<Config>(jsonString)!;
        }
        catch (Exception e) when (e is FileNotFoundException or JsonException)
        {
            if (e is JsonException)
            {
                Console.WriteLine("Invalid configuration file.");
            }
        }

        if (config is not null)
        {
            Console.Write("Edit config (y/N)? ");
            if (Console.ReadLine()!.ToLower() == "y")
            {
                config = Config.GenerateConfig();
            }
        }
        else
        {
            config = Config.GenerateConfig();
        }

        string newJsonString = JsonSerializer.Serialize(config);
        File.WriteAllText(configFile, newJsonString);

        IRobInterface iface = Config.ReadConfig(config);

        iface.Connect();

        Console.CancelKeyPress += delegate { iface.Disconnect(); };

        Thread ticker = new Thread(RobTicker);
        ticker.Start();

        try
        {
            while (iface.Active)
            {
                byte? cmd = iface.GetCommand();
                if (cmd != null)
                {
                    switch (cmd)
                    {
                        case 12:
                            Rob.StartAction(Robot.Command.MoveUp1);
                            break;
                        case 5:
                            Rob.StartAction(Robot.Command.MoveUp2);
                            break;
                        case 2:
                            Rob.StartAction(Robot.Command.MoveDown1);
                            break;
                        case 13:
                            Rob.StartAction(Robot.Command.MoveDown2);
                            break;
                        case 4:
                            Rob.StartAction(Robot.Command.RotateLeft);
                            break;
                        case 8:
                            Rob.StartAction(Robot.Command.RotateRight);
                            break;
                        case 10:
                            Rob.StartAction(Robot.Command.OpenArms);
                            break;
                        case 6:
                            Rob.StartAction(Robot.Command.CloseArms);
                            break;
                        case 9:
                            Rob.StartAction(Robot.Command.LEDOn);
                            break;
                        case 0:
                            Rob.StartAction(Robot.Command.BlinkLED);
                            break;
                        case 1:
                            Rob.StartAction(Robot.Command.Reset);
                            break;
                    }
                }

                StringBuilder output = new StringBuilder(200);
                output.Append("\e[H\e[J");
                output.AppendFormat("L/R: {0:0.000} Height: {1:0.000} Arms: {2:0.000} LED: {3}\n", Rob.Rotation,
                    Rob.Height, Rob.ArmsDistance, Rob.LedOn ? "On " : "Off");

                int robRotation = (int)Math.Round(Rob.Rotation) + 2;
                int robHeight = (int)Math.Round(Rob.Height);
                for (int row = 5; row >= 0; row--)
                {
                    for (int col = 4; col >= 0; col--)
                    {
                        if (row == robHeight && col == robRotation)
                        {
                            bool armsOpen = Rob.ArmsDistance >= 0.5;
                            output.Append(armsOpen ? 'O' : 'C');
                        }
                        else
                        {
                            output.Append('-');
                        }
                    }

                    output.Append('\n');
                }

                Console.Write(output);
                Thread.Sleep(50);
            }
        }
        finally
        {
            iface.Disconnect();
        }
    }

    private static void RobTicker()
    {
        while (true)
        {
            Rob.Tick();

            // This time doesn't have to be exact.
            // However, the robot slows down a lot if ticks happen too fast and gets janky if they happen too slow.
            // Somewhere between 100 and 1000Hz is likely a good value.
            Thread.Sleep(1);
        }
        // ReSharper disable once FunctionNeverReturns
    }
}