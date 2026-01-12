using System.Diagnostics;

namespace Robert_CLI;

public class Robot
{
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
        BlinkLed,
        LedOn,
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
                return Command.LedOn;
            case 0:
                return Command.BlinkLed;
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

    protected readonly Stopwatch SinceLastTick = new Stopwatch();

    protected readonly object Lock = new object();

    private double VerticalTickIncrement => SinceLastTick.Elapsed / _verticalMovementTime;

    protected double RotateTickIncrement => SinceLastTick.Elapsed / _rotateMovementTime;

    protected double ArmsTickIncrement => SinceLastTick.Elapsed / _armsTime;

    protected Action CurrentAction = Action.Resetting;

    protected bool LedOn;

    protected double Height = 3.0;

    protected double Rotation;

    protected double ArmsDistance = 1.0;

    private double _ledBlinkTimer;

    protected double LedBlinkCommandTimer;

    private const double LedCommandTimeout = 250.0;

    protected LedState CurrentLedState = LedState.Off;

    protected bool ResetReachedRight;

    protected int MovementTarget;

    public virtual RobotState CurrentState
    {
        get
        {
            lock (Lock)
            {
                return new RobotState()
                {
                    Height = Height, Rotation = Rotation, ArmsDistance = ArmsDistance, LedOn = LedOn,
                    CurrentAction = CurrentAction
                };
            }
        }
    }

    public Robot()
    {
        SinceLastTick.Start();
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

            double elapsedMs = SinceLastTick.Elapsed.TotalMilliseconds;
            _ledBlinkTimer = (_ledBlinkTimer + elapsedMs) % 500;
            LedBlinkCommandTimer += elapsedMs;
        }
    }

    public virtual void Tick()
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
            SinceLastTick.Restart();
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
                case Command.BlinkLed:
                    LedBlinkCommandTimer = 0.0;
                    CurrentLedState = LedState.Blinking;
                    break;
                case Command.LedOn:
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
            Height = Math.Min(Height + VerticalTickIncrement, 5.0);
        }

        if (ArmsDistance < 1.0)
        {
            ArmsDistance = Math.Min(ArmsDistance + ArmsTickIncrement, 1.0);
        }

        if (!ResetReachedRight && Rotation < 2.0)
        {
            Rotation = Math.Min(Rotation + RotateTickIncrement, 2.0);
            if (Rotation >= 2.0)
            {
                ResetReachedRight = true;
            }
        }
        else if (ResetReachedRight && Rotation > 0.0)
        {
            Rotation = Math.Max(Rotation - RotateTickIncrement, 0.0);
        }


        if (Height >= 5.0 && Rotation <= 0.0)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected void MoveUpTick()
    {
        Height = Math.Min(Height + VerticalTickIncrement, MovementTarget);
        if (Height >= MovementTarget)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected void MoveDownTick()
    {
        Height = Math.Max(Height - VerticalTickIncrement, MovementTarget);
        if (Height <= MovementTarget)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected virtual void RotateLeftTick()
    {
        Rotation = Math.Max(Rotation - RotateTickIncrement, MovementTarget);
        if (Rotation <= MovementTarget)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected virtual void RotateRightTick()
    {
        Rotation = Math.Min(Rotation + RotateTickIncrement, MovementTarget);
        if (Rotation >= MovementTarget)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected virtual void CloseArmsTick()
    {
        ArmsDistance = Math.Max(ArmsDistance - ArmsTickIncrement, 0.0);
        if (ArmsDistance <= 0.0)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected virtual void OpenArmsTick()
    {
        ArmsDistance = Math.Min(ArmsDistance + ArmsTickIncrement, 1.0);
        if (ArmsDistance >= 1.0)
        {
            CurrentAction = Action.Waiting;
        }
    }
}