using System.Diagnostics;

namespace Robert_CLI;

public class Robot
{
    private readonly Stopwatch _stopwatch;

    public Robot(Stopwatch stopwatch)
    {
        _stopwatch = stopwatch;
        CurrentAction = new Action(stopwatch, TimeSpan.Zero);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public enum State
    {
        MovingDown1,
        MovingDown2,
        MovingUp1,
        MovingUp2,
        MovingLeft,
        MovingRight,
        ClosingArms,
        OpeningArms,
        Waiting,
        Resetting
    }

    private enum LedState
    {
        Off,
        Normal,
        Blinking
    }
    
    public int Height { get; private set; } = 5;
    public int HPos { get; private set; } = 2;
    public bool ArmsOpen { get; private set; } = true;
    public Action CurrentAction { get; private set; }
    public State CurrentState { get; private set; } = State.Waiting;
    
    private LedState _currentLedState = LedState.Off;
    private TimeSpan _ledBlinkStart = TimeSpan.Zero;
    private TimeSpan _lastBlinkCommand = TimeSpan.Zero;
    
    public bool LedOn
    {
        get
        {
            switch (_currentLedState)
            {
                case LedState.Normal:
                    return CurrentState == State.Waiting;
                case LedState.Blinking:
                    TimeSpan blinkingTime = _stopwatch.Elapsed - _ledBlinkStart;
                    TimeSpan blinkInterval = TimeSpan.FromMilliseconds(500);
                    int blinks = (int) (blinkingTime / blinkInterval);
                    TimeSpan currentBlink = blinkingTime - (blinkInterval * blinks);

                    return currentBlink.TotalMilliseconds < 333;
                default:
                    return false;
            }
        }
    }

    public void BlinkLed()
    {
        if (_currentLedState != LedState.Blinking)
        {
            _currentLedState = LedState.Blinking;
            _ledBlinkStart = _stopwatch.Elapsed;
        }

        _lastBlinkCommand = _stopwatch.Elapsed;
    }
    
    public void TurnOnLed()
    {
        _currentLedState = LedState.Normal;
    }

    // Before any field is accessed, this method must be called to make sure the state is up to date
    // with any actions which might have been completed.
    public void UpdateState()
    {
        if (CurrentAction is { Done: true })
        {
            switch (CurrentState)
            {
                case State.MovingDown1:
                    Height = Math.Max(Height - 1, 0);
                    break;
                case State.MovingDown2:
                    Height = Math.Max(Height - 2, 0);
                    break;
                case State.MovingUp1:
                    Height = Math.Min(Height + 1, 5);
                    break;
                case State.MovingUp2:
                    Height = Math.Min(Height + 2, 5);
                    break;
                case State.MovingLeft:
                    HPos = Math.Max(HPos - 1, 0);
                    break;
                case State.MovingRight:
                    HPos = Math.Min(HPos + 1, 4);
                    break;
                case State.ClosingArms:
                    ArmsOpen = false;
                    break;
                case State.OpeningArms:
                    ArmsOpen = true;
                    break;
            }
            
            CurrentState = State.Waiting;
        }

        // Stop the LED blinking if the last blink command was more than 250ms ago.
        if (_currentLedState == LedState.Blinking &&
            _stopwatch.Elapsed - _lastBlinkCommand > TimeSpan.FromMilliseconds(250))
        {
            _currentLedState = LedState.Off;
        }
    }

    // Arm movements take about 2 seconds
    private const int MovementTime = 2; 
    
    public void MoveDown1()
    {
        if (CurrentState != State.Waiting) return;
        if (Height <= 0) return;
        
        CurrentState = State.MovingDown1;
        CurrentAction = new Action(_stopwatch, TimeSpan.FromSeconds(MovementTime));
    }
    
    public void MoveDown2()
    {
        if (CurrentState != State.Waiting) return;
        if (Height <= 1) return;
        
        CurrentState = State.MovingDown2;
        CurrentAction = new Action(_stopwatch, TimeSpan.FromSeconds(MovementTime * 2));
    }

    public void MoveUp1()
    {
        if (CurrentState != State.Waiting) return;
        if (Height >= 5) return;
        
        CurrentState = State.MovingUp1;
        CurrentAction = new Action(_stopwatch, TimeSpan.FromSeconds(MovementTime));
    }
    
    public void MoveUp2()
    {
        if (CurrentState != State.Waiting) return;
        if (Height >= 5) return;
        if (Height == 4)
        {
            MoveUp1();
            return;
        }
        
        CurrentState = State.MovingUp2;
        CurrentAction = new Action(_stopwatch, TimeSpan.FromSeconds(MovementTime * 2));
    }
    
    public double FractionalHeight
    {
        get
        {
            switch (CurrentState)
            {
                case State.MovingDown1:
                    return Height - CurrentAction.Progress;
                case State.MovingDown2:
                    return Height - CurrentAction.Progress * 2;
                case State.MovingUp1:
                    return Height + CurrentAction.Progress;
                case State.MovingUp2:
                    return Height + CurrentAction.Progress * 2;
                default:
                    return Height;
            }
        }
    }

    public void MoveLeft()
    {
        if (CurrentState != State.Waiting) return;
        if (HPos <= 0) return;
        
        CurrentState = State.MovingLeft;
        CurrentAction = new Action(_stopwatch, TimeSpan.FromSeconds(MovementTime));
    }

    public void MoveRight()
    {
        if (CurrentState != State.Waiting) return;
        if (HPos >= 4) return;
        
        CurrentState = State.MovingRight;
        CurrentAction = new Action(_stopwatch, TimeSpan.FromSeconds(MovementTime));
    }

    public double FractionalHPos
    {
        get
        {
            switch (CurrentState)
            {
                case State.MovingLeft:
                    return HPos - CurrentAction.Progress;
                case State.MovingRight:
                    return HPos + CurrentAction.Progress;
                default:
                    return HPos;
            }
        }
    }

    // Opening/closing arms takes about 3 seconds
    private const int ArmsTime = 3;
    
    public void CloseArms()
    {
        if (CurrentState != State.Waiting) return;
        if (!ArmsOpen) return;
        
        CurrentState = State.ClosingArms;
        CurrentAction = new Action(_stopwatch, TimeSpan.FromSeconds(ArmsTime));
    }
    
    public void OpenArms()
    {
        if (CurrentState != State.Waiting) return;
        if (ArmsOpen) return;
        
        CurrentState = State.OpeningArms;
        CurrentAction = new Action(_stopwatch, TimeSpan.FromSeconds(ArmsTime));
    }

    public double FractionalArms
    {
        get
        {
            switch (CurrentState)
            {
                case State.OpeningArms:
                    return CurrentAction.Progress;
                case State.ClosingArms:
                    return 1 - CurrentAction.Progress;
                default:
                    return ArmsOpen ? 1.0 : 0.0;
            }
        }
    }
}