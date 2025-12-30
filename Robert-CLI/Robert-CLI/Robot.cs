namespace Robert_CLI;

public class Robot
{
    public const int TickRate = 125;
    private const double VerticalTickIncrement = 1.0 / 1.75 / TickRate;
    private const double RotateTickIncrement = 1.0 / 2 / TickRate;
    private const double ArmsTickIncrement = 1.0 / 2.5 / TickRate;

    public Action CurrentAction { get; private set; } = Action.Resetting;
    public bool LedOn { get; private set; }
    public double Height { get; private set; } = 3.0;
    public double Rotation { get; private set; }
    public double ArmsDistance { get; private set; } = 1.0;

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

    private enum LedState
    {
        Off,
        Normal,
        Blinking
    }

    private int _ledBlinkTimer;
    private int _ledBlinkCommandTimer;
    private LedState _currentLedState = LedState.Off;

    private void LedTick()
    {
        if (_currentLedState == LedState.Off)
        {
            LedOn = false;
        }
        else if (_currentLedState == LedState.Normal)
        {
            LedOn = CurrentAction == Action.Waiting;
        }
        else if (_currentLedState == LedState.Blinking)
        {
            if (_ledBlinkCommandTimer > 1 / 4.0 * TickRate)
            {
                // The next blink command took too long, exit blinking mode
                _currentLedState = LedState.Off;
                LedOn = false;
                _ledBlinkTimer = 0;
                _ledBlinkCommandTimer = 0;
                return;
            }

            double blinkTimerProgress = _ledBlinkTimer * 2.0 / TickRate;
            LedOn = blinkTimerProgress < 2 / 3.0;

            _ledBlinkTimer = (_ledBlinkTimer + 1) % (TickRate / 2);
            _ledBlinkCommandTimer += 1;
        }
    }

    public void Tick()
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
    }

    // Returns true if the action was successfully started.
    public bool StartAction(Command command)
    {
        if (CurrentAction != Action.Waiting) return false;
        switch (command)
        {
            case Command.Reset:
                _resetReachedRight = false;
                CurrentAction = Action.Resetting;
                break;
            case Command.MoveUp1:
                _movementTarget = Math.Min((int)Math.Round(Height + 1), 5);
                CurrentAction = Action.MovingUp1;
                break;
            case Command.MoveUp2:
                _movementTarget = Math.Min((int)Math.Round(Height + 2), 5);
                CurrentAction = Action.MovingUp2;
                break;
            case Command.MoveDown1:
                _movementTarget = Math.Max((int)Math.Round(Height - 1), 0);
                CurrentAction = Action.MovingDown1;
                break;
            case Command.MoveDown2:
                _movementTarget = Math.Max((int)Math.Round(Height - 2), 0);
                CurrentAction = Action.MovingDown2;
                break;
            case Command.RotateLeft:
                _movementTarget = Math.Max((int)Math.Round(Rotation - 1), -2);
                CurrentAction = Action.RotatingLeft;
                break;
            case Command.RotateRight:
                _movementTarget = Math.Min((int)Math.Round(Rotation + 1), 2);
                CurrentAction = Action.RotatingRight;
                break;
            case Command.CloseArms:
                CurrentAction = Action.ClosingArms;
                break;
            case Command.OpenArms:
                CurrentAction = Action.OpeningArms;
                break;
            case Command.BlinkLED:
                _ledBlinkCommandTimer = 0;
                _currentLedState = LedState.Blinking;
                break;
            case Command.LEDOn:
                _currentLedState = LedState.Normal;
                break;
            default:
                return false;
        }

        return true;
    }

    private bool _resetReachedRight;

    private void ResetTick()
    {
        if (Height < 5.0)
        {
            Height += VerticalTickIncrement;
        }

        if (ArmsDistance < 1.0)
        {
            ArmsDistance += ArmsTickIncrement;
        }

        if (!_resetReachedRight && Rotation < 2.0)
        {
            Rotation += RotateTickIncrement;
        }
        else if (_resetReachedRight && Rotation > 0.0)
        {
            Rotation -= RotateTickIncrement;
        }

        if (!_resetReachedRight && Rotation >= 2.0)
        {
            _resetReachedRight = true;
        }

        if (Height >= 5.0 && Rotation <= 0.0)
        {
            CurrentAction = Action.Waiting;
        }
    }

    private int _movementTarget;

    private void MoveUpTick()
    {
        if (Height >= _movementTarget)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            Height += VerticalTickIncrement;
        }
    }

    private void MoveDownTick()
    {
        if (Height <= _movementTarget)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            Height -= VerticalTickIncrement;
        }
    }

    private void RotateLeftTick()
    {
        if (Rotation <= _movementTarget)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            Rotation -= RotateTickIncrement;
        }
    }

    private void RotateRightTick()
    {
        if (Rotation >= _movementTarget)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            Rotation += RotateTickIncrement;
        }
    }

    private void CloseArmsTick()
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

    private void OpenArmsTick()
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
}