using System.Text;

namespace Robert_CLI;

class Program
{
    static Robot _rob = new Robot();

    private static void Main()
    {
        IRobInterface iface = new EmuInterface("127.0.0.1", 8012);
        //IRobInterface iface = new HardwareInterface("COM4", 57600);
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
                            _rob.StartAction(Robot.Command.MoveUp1);
                            break;
                        case 5:
                            _rob.StartAction(Robot.Command.MoveUp2);
                            break;
                        case 2:
                            _rob.StartAction(Robot.Command.MoveDown1);
                            break;
                        case 13:
                            _rob.StartAction(Robot.Command.MoveDown2);
                            break;
                        case 4:
                            _rob.StartAction(Robot.Command.RotateLeft);
                            break;
                        case 8:
                            _rob.StartAction(Robot.Command.RotateRight);
                            break;
                        case 10:
                            _rob.StartAction(Robot.Command.OpenArms);
                            break;
                        case 6:
                            _rob.StartAction(Robot.Command.CloseArms);
                            break;
                        case 9:
                            _rob.StartAction(Robot.Command.LEDOn);
                            break;
                        case 0:
                            _rob.StartAction(Robot.Command.BlinkLED);
                            break;
                        case 1:
                            _rob.StartAction(Robot.Command.Reset);
                            break;
                    }
                }

                StringBuilder output = new StringBuilder(200);
                output.Append("\e[H");
                output.AppendFormat("L/R: {0:0.000} Height: {1:0.000} Arms: {2:0.000} LED: {3}\n", _rob.Rotation,
                    _rob.Height, _rob.ArmsDistance, _rob.LedOn ? "On " : "Off");

                int robRotation = (int)Math.Round(_rob.Rotation) + 2;
                int robHeight = (int)Math.Round(_rob.Height);
                for (int row = 5; row >= 0; row--)
                {
                    for (int col = 4; col >= 0; col--)
                    {
                        if (row == robHeight && col == robRotation)
                        {
                            bool armsOpen = _rob.ArmsDistance >= 0.5;
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
            _rob.Tick();
            
            // This time doesn't have to be exact.
            // However, the robot slows down a lot if ticks happen too fast and gets janky if they happen too slow.
            // Somewhere between 100 and 1000Hz is likely a good value.
            Thread.Sleep(1);
        }
        // ReSharper disable once FunctionNeverReturns
    }
}