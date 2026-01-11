using System.Collections.ObjectModel;

namespace Robert_CLI;

public class GyromiteRobot : Robot
{
    // Gyros are assumed to be properly placed in their specified column when Gyro.Toppled is false
    // and they're not being held.
    private class Gyro
    {
        public static readonly TimeSpan SpinUpTime = TimeSpan.FromSeconds(23);
        public static readonly TimeSpan MaxSpinTime = TimeSpan.FromSeconds(315);
        public bool Toppled;
        public int Column;
        public TimeSpan SpinTimer;

        public Gyro(int column)
        {
            Toppled = false;
            Column = column;
            SpinTimer = TimeSpan.Zero;
        }
    }

    private int RotationInt => (int)Math.Round(Rotation) + 2;
    private int HeightInt => (int)Math.Round(Height);
    private int GyroTopHeight => RotationInt == 0 ? 3 : 1;

    private readonly ReadOnlyCollection<Gyro> _gyros = new([new Gyro(3), new Gyro(4)]);

    private Gyro? _heldItem;

    private int GyroHeight(Gyro gyro)
    {
        if (_heldItem == gyro) return HeightInt;
        if (gyro.Toppled) return int.MinValue;
        return gyro.Column == 0 ? 3 : 1;
    }

    public override RobotState CurrentState
    {
        get
        {
            lock (Lock)
            {
                GyromiteRobotState.GyroState[] gyroStates = new GyromiteRobotState.GyroState[_gyros.Count];
                GyromiteRobotState.GyroState? heldGyro = null;
                for (int i = 0; i < gyroStates.Length; i++)
                {
                    Gyro gyro = _gyros[i];
                    gyroStates[i] = new GyromiteRobotState.GyroState()
                    {
                        Column = gyro.Column, Height = GyroHeight(gyro), Number = i, SpinTimer = gyro.SpinTimer,
                        Toppled = gyro.Toppled
                    };
                    if (_heldItem == gyro) heldGyro = gyroStates[i];
                }

                (bool aPressed, bool bPressed) = ButtonStates;

                return new GyromiteRobotState()
                {
                    Height = Height, Rotation = Rotation, ArmsDistance = ArmsDistance, LedOn = LedOn,
                    CurrentAction = CurrentAction, APressed = aPressed, BPressed = bPressed, Gyros = gyroStates,
                    HeldItem = heldGyro
                };
            }
        }
    }

    public override void Tick()
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
            GyroTick();
            SinceLastTick.Restart();
        }
    }

    private void GyroTick()
    {
        _heldItem?.Column = RotationInt;

        foreach (Gyro gyro in _gyros)
        {
            if (gyro.Toppled)
            {
                gyro.SpinTimer = TimeSpan.Zero;
                return;
            }

            switch (gyro.Column)
            {
                case 0 when _heldItem != gyro || Height <= 3: // Spinner
                    gyro.SpinTimer += Gyro.MaxSpinTime * (SinceLastTick.Elapsed / Gyro.SpinUpTime);
                    if (gyro.SpinTimer > Gyro.MaxSpinTime)
                    {
                        gyro.SpinTimer = Gyro.MaxSpinTime;
                    }

                    break;
                case 3 or 4 when _heldItem != gyro || Height <= 1: // Black trays
                    gyro.SpinTimer = TimeSpan.Zero;
                    break;
                default:
                    gyro.SpinTimer -= SinceLastTick.Elapsed;
                    if (gyro.SpinTimer < TimeSpan.Zero)
                    {
                        gyro.SpinTimer = TimeSpan.Zero;
                    }

                    if (_heldItem != gyro && gyro.Column is 1 or 2 && gyro.SpinTimer <= TimeSpan.Zero)
                    {
                        gyro.Toppled = true;
                    }

                    break;
            }
        }
    }

    protected override void CloseArmsTick()
    {
        ArmsDistance = Math.Max(ArmsDistance - ArmsTickIncrement, 0.0);

        // Try to grab a gyro.
        if (ArmsDistance <= 0.3)
        {
            foreach (Gyro gyro in _gyros)
            {
                if (gyro.Column == RotationInt && GyroHeight(gyro) == HeightInt)
                {
                    // Successfully grabbed.
                    _heldItem = gyro;
                    ArmsDistance = 0.3;
                    CurrentAction = Action.Waiting;
                }
            }
        }

        if (ArmsDistance <= 0.0)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected override void OpenArmsTick()
    {
        ArmsDistance = Math.Min(ArmsDistance + ArmsTickIncrement, 1.0);

        if (ArmsDistance > 0.3 && _heldItem != null)
        {
            _heldItem.Column = RotationInt;
            foreach (Gyro otherGyro in _gyros)
            {
                if (_heldItem != otherGyro && !otherGyro.Toppled && otherGyro.Column == _heldItem.Column)
                {
                    _heldItem.Toppled = true;
                }
            }

            _heldItem = null;
        }

        if (ArmsDistance >= 1.0)
        {
            CurrentAction = Action.Waiting;
        }
    }

    private void RotationTopple()
    {
        foreach (Gyro gyro in _gyros)
        {
            if (gyro != _heldItem && gyro.Column == RotationInt &&
                HeightInt <= GyroHeight(gyro) + (_heldItem != null ? 3 : 0))
            {
                gyro.Toppled = true;
            }
        }
    }

    protected override void RotateLeftTick()
    {
        Rotation = Math.Max(Rotation - RotateTickIncrement, MovementTarget);
        if (Rotation <= MovementTarget)
        {
            RotationTopple();
            CurrentAction = Action.Waiting;
        }
    }

    protected override void RotateRightTick()
    {
        Rotation = Math.Min(Rotation + RotateTickIncrement, MovementTarget);
        if (Rotation >= MovementTarget)
        {
            RotationTopple();
            CurrentAction = Action.Waiting;
        }
    }

    private bool GyroBlockingDownMovement()
    {
        // If holding a gyro, an extra 4 units of clearance is required.
        if (MovementTarget < GyroTopHeight + (_heldItem == null ? 0 : 4))
        {
            foreach (Gyro gyro in _gyros)
            {
                if (!gyro.Toppled && gyro.Column == RotationInt && _heldItem != gyro)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override bool StartAction(Command command)
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
                    // Only move down if there isn't a gyro in the way.
                    if (!GyroBlockingDownMovement())
                    {
                        // This doesn't allow the robot to go to Height = 0 (or 2 in the spinner column) in Gyromite mode.
                        // Technically an inaccuracy, but better for the player experience.
                        if (MovementTarget >= GyroTopHeight)
                        {
                            CurrentAction = Action.MovingDown2;
                        }
                    }

                    break;
                case Command.MoveDown2:
                    MovementTarget = Math.Max((int)Math.Round(Height - 2), 0);
                    // Only move down if there isn't a gyro in the way.
                    if (!GyroBlockingDownMovement())
                    {
                        // This doesn't allow the robot to go to Height = 0 (or 2 in the spinner column) in Gyromite mode.
                        // Technically an inaccuracy, but better for the player experience.
                        if (MovementTarget >= GyroTopHeight)
                        {
                            CurrentAction = Action.MovingDown2;
                        }
                    }

                    break;
                case Command.RotateLeft:
                    MovementTarget = Math.Max((int)Math.Round(Rotation - 1), -2);

                    // Don't move if in the bottom rows and holding a gyro.
                    // Also don't move into the spinner.
                    if (!(_heldItem != null && HeightInt <= GyroTopHeight + 1) &&
                        !(MovementTarget == -2 && Height < 3 + (_heldItem == null ? 0 : 2)))
                    {
                        // Check if it's actually going to move first.
                        if (MovementTarget != RotationInt - 2)
                        {
                            RotationTopple();
                            CurrentAction = Action.RotatingLeft;
                        }
                    }

                    break;
                case Command.RotateRight:
                    MovementTarget = Math.Min((int)Math.Round(Rotation + 1), 2);

                    // Don't move if in the bottom row and holding a gyro, or if in the spinner and holding a gyro.
                    if (!(_heldItem != null && HeightInt <= GyroTopHeight + 1))
                    {
                        // Check if it's actually going to move first.
                        if (MovementTarget != RotationInt - 2)
                        {
                            RotationTopple();
                            CurrentAction = Action.RotatingRight;
                        }
                    }

                    break;
                case Command.CloseArms:
                    // Don't close the arms more if something is being held.
                    if (_heldItem == null)
                    {
                        CurrentAction = Action.ClosingArms;
                    }

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

    private (bool A, bool B) ButtonStates
    {
        get
        {
            bool aPressed = false;
            bool bPressed = false;

            foreach (Gyro gyro in _gyros)
            {
                if (GyroHeight(gyro) == 1)
                {
                    switch (gyro.Column)
                    {
                        case 1:
                            bPressed = true;
                            break;
                        case 2:
                            aPressed = true;
                            break;
                    }
                }
            }

            return (aPressed, bPressed);
        }
    }

    public bool ReplaceToppled(int gyroNumber, int column)
    {
        if (gyroNumber is not (0 or 1)) throw new ArgumentException("Gyro number must be 0 or 1");
        if (column is < 0 or > 4) throw new ArgumentException("Column number must be between 0 and 4");

        Gyro gyro = _gyros[gyroNumber];
        int otherGyroNumber = gyroNumber == 1 ? 0 : 1;
        Gyro otherGyro = _gyros[otherGyroNumber];

        int columnHeight = column == 0 ? 3 : 1;
        if (gyro.Toppled &&
            !(!otherGyro.Toppled && otherGyro.Column == column &&
              GyroHeight(otherGyro) == columnHeight) && // make sure other gyro isn't in spot
            !(RotationInt == column && ArmsDistance <= 0.3 &&
              HeightInt <= columnHeight + (_heldItem == null ? 0 : 3))) // make sure robot arms/held gyro aren't in way
        {
            gyro.Column = column;
            gyro.Toppled = false;
            return true;
        }

        return false;
    }
}