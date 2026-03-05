using System;
using System.Collections.Generic;
using System.Linq;

namespace Robert.RobotLogic;

public class StackUpRobot : Robot
{
    public enum Block
    {
        Empty,
        Red,
        Yellow,
        Green,
        Blue,
        White
    }

    public static string BlockToColoredLetter(Block block, string empty)
    {
        return block switch
        {
            Block.Empty => empty,
            Block.Red => "\x1b[101m\x1b[97mR\x1b[0m",
            Block.Yellow => "\x1b[103m\x1b[30mY\x1b[0m",
            Block.Green => "\x1b[102m\x1b[30mG\x1b[0m",
            Block.Blue => "\x1b[104m\x1b[97mB\x1b[0m",
            Block.White => "\x1b[107m\x1b[30mW\x1b[0m",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    // Data of the currently placed blocks.
    // Goes from (the robot's) left to right, bottom to top.
    private readonly Block[][] _blocks;

    // Currently held blocks, from bottom to top.
    // The one that's actually in the robot's hands is always HeldBlocks[0].
    private readonly Block[] _heldBlocks;

    // Blocks that have been toppled onto the ground. In no particular order.
    private readonly List<Block> _toppledBlocks;

    private int RotationInt => (int)Math.Round(Rotation) + 2;
    private int HeightInt => (int)Math.Round(Height);

    public override RobotState CurrentState
    {
        get
        {
            lock (Lock)
            {
                Block[][] newBlocks = new Block[_blocks.Length][];
                for (int i = 0; i < _blocks.Length; i++)
                {
                    newBlocks[i] = (Block[])_blocks[i].Clone();
                }

                return new StackUpRobotState()
                {
                    Height = Height, Rotation = Rotation, ArmsDistance = ArmsDistance, LedOn = LedOn,
                    CurrentAction = CurrentAction, Blocks = newBlocks, HeldBlocks = (Block[])_heldBlocks.Clone(),
                    ToppledBlocks = _toppledBlocks.ToArray()
                };
            }
        }
    }

    public StackUpRobot()
    {
        Height = 5.0;
        _blocks = new Block[5][];
        _heldBlocks = new Block[5];
        _toppledBlocks = new List<Block>(); // could probably be a standard array but this was easier
        for (int i = 0; i < _blocks.Length; i++)
        {
            _blocks[i] = new Block[6];
        }

        _blocks[2] = [Block.Green, Block.Yellow, Block.Blue, Block.White, Block.Red, Block.Empty];
    }

    public StackUpRobot(Block[][] blocks) : this()
    {
        // validate array shape
        if (blocks.Length != 5) throw new ArgumentException("The blocks array must be a 5x6 rectangular 2D array.");
        if (blocks.Any(column => column.Length != 6))
        {
            throw new ArgumentException("The blocks array must be a 5x6 rectangular 2D array.");
        }

        // Copy the blocks into the internal 2D array while also checking that
        // there are no more than 5 blocks, and blocks are not floating.
        int blockCount = 0;
        for (int column = 0; column < blocks.Length; column++)
        {
            bool emptySeen = false;
            for (int height = 0; height < blocks[column].Length; height++)
            {
                if (blocks[column][height] != Block.Empty)
                {
                    if (emptySeen)
                    {
                        throw new ArgumentException("Blocks cannot be placed on top of empty spaces.");
                    }

                    blockCount++;
                }
                else
                {
                    emptySeen = true;
                }

                _blocks[column][height] = blocks[column][height];
            }
        }

        if (blockCount > 5) throw new ArgumentException("No more than 5 blocks are allowed.");
    }


    // Puts the specified toppled block (if it exists) to the top of the specified column.
    // Returns true if successful.
    public bool ReplaceToppled(Block block, int column)
    {
        if (column is < 0 or > 4)
            throw new IndexOutOfRangeException("Column number must be between 0 and 4");
        if (block == Block.Empty) throw new ArgumentException("Empty block cannot be replaced");
        lock (Lock)
        {
            if (!_toppledBlocks.Contains(block)) return false;

            // Find the top free index in the specified column
            int topFreeSpace = -1;
            for (int i = _blocks[column].Length - 1; i >= 0; i--)
            {
                if (_blocks[column][i] == Block.Empty)
                {
                    topFreeSpace = i;
                }
                else
                {
                    break;
                }
            }

            if (topFreeSpace == -1) return false;

            _blocks[column][topFreeSpace] = block;
            _toppledBlocks.Remove(block);
            return true;
        }
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
                    // Only move down if there isn't a block in the way.
                    if (ArmsDistance >= 1 || _blocks[RotationInt][MovementTarget] == Block.Empty)
                    {
                        CurrentAction = Action.MovingDown1;
                    }

                    break;
                case Command.MoveDown2:
                    MovementTarget = Math.Max((int)Math.Round(Height - 2), 0);
                    if (ArmsDistance >= 1 || _blocks[RotationInt][MovementTarget] == Block.Empty)
                    {
                        // No blocks in the way.
                        CurrentAction = Action.MovingDown2;
                    }
                    else
                    {
                        // Try moving only one block instead if 2 spaces was too much.
                        MovementTarget++;
                        if (_blocks[RotationInt][MovementTarget] == Block.Empty)
                        {
                            CurrentAction = Action.MovingDown1;
                        }
                    }

                    break;
                case Command.RotateLeft:
                    // Don't move if in the bottom row and holding a block.
                    if (!(_heldBlocks[0] != Block.Empty && HeightInt == 0))
                    {
                        MovementTarget = Math.Max((int)Math.Round(Rotation - 1), -2);
                        // Check if it's actually going to move first.
                        if (MovementTarget != RotationInt - 2)
                        {
                            RotationTopple();
                            CurrentAction = Action.RotatingLeft;
                        }
                    }

                    break;
                case Command.RotateRight:
                    // Don't move if in the bottom row and holding a block.
                    if (!(_heldBlocks[0] != Block.Empty && HeightInt == 0))
                    {
                        MovementTarget = Math.Min((int)Math.Round(Rotation + 1), 2);
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
                    if (_heldBlocks[0] == Block.Empty)
                    {
                        CurrentAction = Action.ClosingArms;
                    }

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

    protected override void CloseArmsTick()
    {
        ArmsDistance = Math.Max(ArmsDistance - ArmsTickIncrement, 0.0);

        // If there's a stack of blocks here, grab it.
        if (ArmsDistance <= 0.7 && _blocks[RotationInt][HeightInt] != Block.Empty)
        {
            ArmsDistance = 0.7;
            int rotationInt = RotationInt;
            int heightInt = HeightInt;
            int heldIndex = 0;
            while (heightInt <= 5 && _blocks[rotationInt][heightInt] != Block.Empty)
            {
                _heldBlocks[heldIndex++] = _blocks[rotationInt][heightInt];
                _blocks[rotationInt][heightInt] = Block.Empty;
                heightInt++;
            }

            CurrentAction = Action.Waiting;
        }
        else if (ArmsDistance <= 0.0)
        {
            CurrentAction = Action.Waiting;
        }
    }

    protected override void OpenArmsTick()
    {
        ArmsDistance = Math.Min(ArmsDistance + ArmsTickIncrement, 1.0);

        if (ArmsDistance > 0.7 && _heldBlocks[0] != Block.Empty)
        {
            // Figure out where to put the blocks.
            int topEmptyBlock = HeightInt;
            while (topEmptyBlock > 0 && _blocks[RotationInt][topEmptyBlock - 1] == Block.Empty)
            {
                topEmptyBlock--;
            }

            // Put the blocks down.
            int heightIndex = topEmptyBlock;
            int heldIndex = 0;
            while (heldIndex < _heldBlocks.Length && _heldBlocks[heldIndex] != Block.Empty)
            {
                _blocks[RotationInt][heightIndex++] = _heldBlocks[heldIndex];
                _heldBlocks[heldIndex++] = Block.Empty;
            }
        }

        if (ArmsDistance >= 1.0)
        {
            CurrentAction = Action.Waiting;
        }
    }

    private void ToppleColumn(int column)
    {
        for (int i = 0; i < _blocks[column].Length; i++)
        {
            if (_blocks[column][i] != Block.Empty)
            {
                _toppledBlocks.Add(_blocks[column][i]);
                _blocks[column][i] = Block.Empty;
            }
        }
    }

    private void RotationTopple()
    {
        // If a block is currently held and there's a stack directly below the current position, topple it.
        if (_heldBlocks[0] != Block.Empty && HeightInt >= 1 && _blocks[RotationInt][HeightInt - 1] != Block.Empty)
        {
            ToppleColumn(RotationInt);
        }

        // If moving into a column that has a block at the current height, topple it.
        if (_blocks[RotationInt][HeightInt] != Block.Empty)
        {
            ToppleColumn(RotationInt);
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
}