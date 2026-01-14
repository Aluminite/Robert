using System.Text.Json;
using static Robert_CLI.StackUpRobot;

namespace Robert_CLI;

class Program
{
    private static bool _visualizerPause;
    private static bool _stop;

    private static void RobTicker(Robot robot)
    {
        while (!_stop)
        {
            robot.Tick();

            // This time doesn't have to be exact.
            // However, the robot slows down a lot if ticks happen too fast.
            // Somewhere below 1000Hz is likely a good value.
            Thread.Sleep(5);
        }
    }

    private static void RobStateReader(Robot robot, IRobInterface iface)
    {
        while (!_stop)
        {
            RobotState state = robot.CurrentState;
            if (state is GyromiteRobotState gyromiteState && iface.Active)
            {
                iface.SetA(gyromiteState.APressed);
                iface.SetB(gyromiteState.BPressed);
            }

            if (!_visualizerPause)
            {
                Console.Write("\e[H");
                Console.Write(state.Visualize());
            }

            Thread.Sleep(50);
        }
    }

    private static void InterfaceReceiver(IRobInterface iface, Robot robot)
    {
        while (!_stop && iface.Active)
        {
            byte cmd = iface.GetCommand();

            Robot.Command? decodedCmd = Robot.CommandByteToEnum(cmd);
            if (decodedCmd is not null)
            {
                robot.StartAction(decodedCmd.Value);
            }
        }
    }

    public static void Main()
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

        Robot rob;
        while (true)
        {
            Console.Write("Robot mode? (N)one, (S)tack-Up, (G)yromite: ");
            string response = Console.ReadLine()!.ToLower();
            if (response == "n")
            {
                rob = new Robot();
                break;
            }

            if (response == "s")
            {
                rob = new StackUpRobot();
                break;
            }

            if (response == "g")
            {
                rob = new GyromiteRobot();
                break;
            }
        }
        
        Console.Write("\e[H\e[J");

        Thread robotReader = new Thread(() => RobStateReader(rob, iface));
        Thread ticker = new Thread(() => RobTicker(rob));
        Thread receiver = new Thread(() => InterfaceReceiver(iface, rob));
        
        void Stop()
        {
            _stop = true;
            if (iface.Active)
            {
                iface.Disconnect();
            }

            robotReader.Join();
            ticker.Join();
            receiver.Join();
        }
        
        iface.Connect();
        
        robotReader.Start();
        ticker.Start();
        receiver.Start();

        Console.CancelKeyPress += delegate
        {
            Stop();
        };

        while (!_stop)
        {
            Console.ReadKey(true);
            _visualizerPause = true;
            Console.WriteLine("Options:");
            if (rob is StackUpRobot)
            {
                Console.WriteLine("r to replace toppled blocks");
            }
            if (rob is GyromiteRobot)
            {
                Console.WriteLine("r to replace toppled gyros");
            }
            Console.WriteLine("e to exit");

            char response = char.ToLower(Console.ReadKey(true).KeyChar);
            switch (response)
            {
                case 'r' when rob is StackUpRobot stackup:
                    try
                    {
                        Console.Write("Block color? (first letter): ");
                        string color = Console.ReadLine()!.ToLower();
                        Block block = color switch
                        {
                            "r" => Block.Red,
                            "y" => Block.Yellow,
                            "g" => Block.Green,
                            "b" => Block.Blue,
                            "w" => Block.White,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        Console.Write("Column number to place in? (1-5): ");
                        int column = int.Parse(Console.ReadLine()!);

                        stackup.ReplaceToppled(block, column - 1);
                    }
                    catch (Exception e) when (e is FormatException or ArgumentOutOfRangeException)
                    {
                    }

                    break;
                case 'r' when rob is GyromiteRobot gyromite:
                    try
                    {
                        Console.Write("Which gyro? (1 or 2): ");
                        int gyroNumber = int.Parse(Console.ReadLine()!);

                        Console.Write("Column number to place in? (1-5): ");
                        int column = int.Parse(Console.ReadLine()!);

                        gyromite.ReplaceToppled(gyroNumber - 1, column - 1);
                    }
                    catch (Exception e) when (e is FormatException or ArgumentOutOfRangeException)
                    {
                    }

                    break;
                case 'e':
                {
                    Stop();
                    break;
                }
            }

            Console.Write("\e[H\e[J");
            _visualizerPause = false;
        }
    }
}