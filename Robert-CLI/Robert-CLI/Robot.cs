using System.Diagnostics;
using System.Text;

namespace Robert_CLI;

public class Robot
{
    // ReSharper disable once MemberCanBePrivate.Global
    public enum Action
    {
        MovingDown1,
        MovingDown2,
        MovingUp1,
        MovingUp2,
        RotatingLeft,
        RotatingRight,
        ClosingArms,
        OpeningArms,
        Waiting,
        Resetting
    }

    public enum Command
    {
        MoveDown1,
        MoveDown2,
        MoveUp1,
        MoveUp2,
        RotateLeft,
        RotateRight,
        CloseArms,
        OpenArms,

        // ReSharper disable once InconsistentNaming
        BlinkLED,

        // ReSharper disable once InconsistentNaming
        LEDOn,
        Reset
    }

    public static Command? CommandByteToEnum(byte command)
    {
        switch (command)
        {
            case 12:
                return Command.MoveUp1;
            case 5:
                return Command.MoveUp2;
            case 2:
                return Command.MoveDown1;
            case 13:
                return Command.MoveDown2;
            case 4:
                return Command.RotateLeft;
            case 8:
                return Command.RotateRight;
            case 10:
                return Command.OpenArms;
            case 6:
                return Command.CloseArms;
            case 9:
                return Command.LEDOn;
            case 0:
                return Command.BlinkLED;
            case 1:
                return Command.Reset;
            default:
                return null;
        }
    }

    protected enum LedState
    {
        Off,
        Normal,
        Blinking
    }

    private readonly TimeSpan _verticalMovementTime = TimeSpan.FromMilliseconds(1750);

    private readonly TimeSpan _rotateMovementTime = TimeSpan.FromMilliseconds(2000);

    private readonly TimeSpan _armsTime = TimeSpan.FromMilliseconds(2500);

    private readonly Stopwatch _sinceLastTick = new Stopwatch();

    protected readonly object Lock = new object();

    private double VerticalTickIncrement => _sinceLastTick.Elapsed / _verticalMovementTime;

    protected double RotateTickIncrement => _sinceLastTick.Elapsed / _rotateMovementTime;

    protected double ArmsTickIncrement => _sinceLastTick.Elapsed / _armsTime;

    public Action CurrentAction { get; protected set; } = Action.Resetting;

    public bool LedOn { get; private set; }

    public double Height { get; protected set; } = 3.0;

    public double Rotation { get; protected set; }

    public double ArmsDistance { get; protected set; } = 1.0;

    private double _ledBlinkTimer;

    protected double LedBlinkCommandTimer;

    private const double LedCommandTimeout = 250.0;

    protected LedState CurrentLedState = LedState.Off;

    protected bool ResetReachedRight;

    protected int MovementTarget;

    public Robot()
    {
        _sinceLastTick.Start();
    }

    protected void LedTick()
    {
        if (CurrentLedState == LedState.Off)
        {
            LedOn = false;
        }
        else if (CurrentLedState == LedState.Normal)
        {
            LedOn = CurrentAction == Action.Waiting;
        }
        else if (CurrentLedState == LedState.Blinking)
        {
            if (LedBlinkCommandTimer > LedCommandTimeout)
            {
                // The next blink command took too long, exit blinking mode
                CurrentLedState = LedState.Off;
                LedOn = false;
                _ledBlinkTimer = 0.0;
                LedBlinkCommandTimer = 0.0;
                return;
            }

            LedOn = _ledBlinkTimer < (1000.0 / 3.0);

            double elapsedMs = _sinceLastTick.Elapsed.TotalMilliseconds;
            _ledBlinkTimer = (_ledBlinkTimer + elapsedMs) % 500;
            LedBlinkCommandTimer += elapsedMs;
        }
    }

    public void Tick()
    {
        lock (Lock)
        {
            switch (CurrentAction)
            {
                case Action.Resetting:
                    ResetTick();
                    break;
                case Action.MovingUp1:
                case Action.MovingUp2:
                    MoveUpTick();
                    break;
                case Action.MovingDown1:
                case Action.MovingDown2:
                    MoveDownTick();
                    break;
                case Action.RotatingLeft:
                    RotateLeftTick();
                    break;
                case Action.RotatingRight:
                    RotateRightTick();
                    break;
                case Action.ClosingArms:
                    CloseArmsTick();
                    break;
                case Action.OpeningArms:
                    OpenArmsTick();
                    break;
            }

            LedTick();
            _sinceLastTick.Restart();
        }
    }

    // Returns true if the action was successfully started.
    public virtual bool StartAction(Command command)
    {
        lock (Lock)
        {
            if (CurrentAction != Action.Waiting) return false;
            switch (command)
            {
                case Command.Reset:
                    ResetReachedRight = false;
                    CurrentAction = Action.Resetting;
                    break;
                case Command.MoveUp1:
                    MovementTarget = Math.Min((int)Math.Round(Height + 1), 5);
                    CurrentAction = Action.MovingUp1;
                    break;
                case Command.MoveUp2:
                    MovementTarget = Math.Min((int)Math.Round(Height + 2), 5);
                    CurrentAction = Action.MovingUp2;
                    break;
                case Command.MoveDown1:
                    MovementTarget = Math.Max((int)Math.Round(Height - 1), 0);
                    CurrentAction = Action.MovingDown1;
                    break;
                case Command.MoveDown2:
                    MovementTarget = Math.Max((int)Math.Round(Height - 2), 0);
                    CurrentAction = Action.MovingDown2;
                    break;
                case Command.RotateLeft:
                    MovementTarget = Math.Max((int)Math.Round(Rotation - 1), -2);
                    CurrentAction = Action.RotatingLeft;
                    break;
                case Command.RotateRight:
                    MovementTarget = Math.Min((int)Math.Round(Rotation + 1), 2);
                    CurrentAction = Action.RotatingRight;
                    break;
                case Command.CloseArms:
                    CurrentAction = Action.ClosingArms;
                    break;
                case Command.OpenArms:
                    CurrentAction = Action.OpeningArms;
                    break;
                case Command.BlinkLED:
                    LedBlinkCommandTimer = 0.0;
                    CurrentLedState = LedState.Blinking;
                    break;
                case Command.LEDOn:
                    CurrentLedState = LedState.Normal;
                    break;
                default:
                    return false;
            }

            return true;
        }
    }

    protected void ResetTick()
    {
        if (Height < 5.0)
        {
            Height += VerticalTickIncrement;
        }

        if (ArmsDistance < 1.0)
        {
            ArmsDistance += ArmsTickIncrement;
        }

        if (!ResetReachedRight && Rotation < 2.0)
        {
            Rotation += RotateTickIncrement;
        }
        else if (ResetReachedRight && Rotation > 0.0)
        {
            Rotation -= RotateTickIncrement;
        }

        if (!ResetReachedRight && Rotation >= 2.0)
        {
            ResetReachedRight = true;
        }

        if (Height >= 5.0 && Rotation <= 0.0)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected void MoveUpTick()
    {
        if (Height >= MovementTarget)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            Height += VerticalTickIncrement;
        }
    }

    protected void MoveDownTick()
    {
        if (Height <= MovementTarget)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            Height -= VerticalTickIncrement;
        }
    }

    protected virtual void RotateLeftTick()
    {
        if (Rotation <= MovementTarget)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            Rotation -= RotateTickIncrement;
        }
    }

    protected virtual void RotateRightTick()
    {
        if (Rotation >= MovementTarget)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            Rotation += RotateTickIncrement;
        }
    }

    protected virtual void CloseArmsTick()
    {
        if (ArmsDistance <= 0.0)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            ArmsDistance -= ArmsTickIncrement;
        }
    }

    protected virtual void OpenArmsTick()
    {
        if (ArmsDistance >= 1.0)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            ArmsDistance += ArmsTickIncrement;
        }
    }

    public virtual string Visualize()
    {
        int rotationInt = (int)Math.Round(Rotation) + 2;
        int heightInt = (int)Math.Round(Height);
        StringBuilder output = new StringBuilder(100);

        for (int row = 5; row >= 0; row--)
        {
            for (int col = 0; col <= 4; col++)
            {
                if (row == heightInt && col == rotationInt)
                {
                    bool armsOpen = ArmsDistance >= 0.5;
                    output.Append(armsOpen ? "<->" : ">-<");
                }
                else
                {
                    output.Append(" - ");
                }
            }

            output.Append('\n');
        }

        return output.ToString();
    }
}