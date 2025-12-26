using System.Diagnostics;

namespace Robert_CLI;

class Program
{
    private static void Main()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Robot rob = new Robot(stopwatch);

        IRobInterface iface = new EmuInterface("127.0.0.1", 8012);
        iface.Connect();

        Console.CancelKeyPress += delegate { iface.Disconnect(); };

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
                            rob.MoveUp1();
                            break;
                        case 5:
                            rob.MoveUp2();
                            break;
                        case 2:
                            rob.MoveDown1();
                            break;
                        case 13:
                            rob.MoveDown2();
                            break;
                        case 4:
                            rob.MoveLeft();
                            break;
                        case 8:
                            rob.MoveRight();
                            break;
                        case 10:
                            rob.OpenArms();
                            break;
                        case 6:
                            rob.CloseArms();
                            break;
                        case 9:
                            rob.TurnOnLed();
                            break;
                        case 0:
                            rob.BlinkLed();
                            break;
                        case 1:
                            // Reset not implemented yet
                            rob = new Robot(stopwatch);
                            break;
                    }
                }

                rob.UpdateState();
                Console.Write("L/R: {0:0.000} Height: {1:0.000} Arms: {2:0.000} LED: {3}\r", rob.FractionalHPos,
                    rob.FractionalHeight, rob.FractionalArms, rob.LedOn ? "On" : "Off");
                Thread.Sleep(20);
            }
        }
        finally
        {
            iface.Disconnect();
        }
    }
}
