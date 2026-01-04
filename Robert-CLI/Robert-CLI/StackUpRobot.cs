using System.Collections.ObjectModel;
using System.Text;

namespace Robert_CLI;

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
            Block.Red => "\e[101m\e[97mR\e[0m",
            Block.Yellow => "\e[103m\e[30mY\e[0m",
            Block.Green => "\e[102m\e[30mG\e[0m",
            Block.Blue => "\e[104m\e[97mB\e[0m",
            Block.White => "\e[107m\e[30mW\e[0m",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    // Data of the currently placed blocks.
    // Goes from (the robot's) left to right, bottom to top.
    private readonly Block[][] _blocks;
    public ReadOnlyCollection<Block[]> Blocks => _blocks.AsReadOnly();

    // Currently held blocks, from bottom to top.
    // The one that's actually in the robot's hands is always HeldBlocks[0].
    private readonly Block[] _heldBlocks;
    public ReadOnlyCollection<Block> HeldBlocks => _heldBlocks.AsReadOnly();

    // Blocks that have been toppled onto the ground. In no particular order.
    private readonly List<Block> _toppledBlocks;
    public ReadOnlyCollection<Block> ToppledBlocks => _toppledBlocks.AsReadOnly();

    private int RotationInt => (int)Math.Round(Rotation) + 2;
    private int HeightInt => (int)Math.Round(Height);

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
        if (column > _blocks.Length)
            throw new IndexOutOfRangeException("Column index is greater than number of columns");
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
                        RotationStartTopple();

                        CurrentAction = Action.RotatingLeft;
                    }

                    break;
                case Command.RotateRight:
                    // Don't move if in the bottom row and holding a block.
                    if (!(_heldBlocks[0] != Block.Empty && HeightInt == 0))
                    {
                        MovementTarget = Math.Min((int)Math.Round(Rotation + 1), 2);
                        RotationStartTopple();

                        CurrentAction = Action.RotatingRight;
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

    protected override void CloseArmsTick()
    {
        if (ArmsDistance <= 0.0)
        {
            CurrentAction = Action.Waiting;
        }
        // If there's a stack of blocks here, grab it.
        else if (ArmsDistance < 0.5 && _blocks[RotationInt][HeightInt] != Block.Empty)
        {
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
        else
        {
            ArmsDistance -= ArmsTickIncrement;
        }
    }

    protected override void OpenArmsTick()
    {
        if (ArmsDistance >= 1.0)
        {
            CurrentAction = Action.Waiting;
        }
        else
        {
            if (ArmsDistance >= 0.5 && _heldBlocks[0] != Block.Empty)
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

            ArmsDistance += ArmsTickIncrement;
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

    private void RotationStartTopple()
    {
        // Check if it's actually going to move first.
        if (MovementTarget != RotationInt - 2)
        {
            // Topple the current stack if a block is being held directly above.
            if (HeightInt > 0 && _heldBlocks[0] != Block.Empty &&
                _blocks[RotationInt][HeightInt - 1] != Block.Empty)
            {
                ToppleColumn(RotationInt);
            }

            // Topple the current stack if there is a block at the current height.
            if (_blocks[RotationInt][HeightInt] != Block.Empty)
            {
                ToppleColumn(RotationInt);
            }
        }
    }

    private void RotationFinishTopple()
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
        if (Rotation <= MovementTarget)
        {
            RotationFinishTopple();
            CurrentAction = Action.Waiting;
        }
        else
        {
            Rotation -= RotateTickIncrement;
        }
    }

    protected override void RotateRightTick()
    {
        if (Rotation >= MovementTarget)
        {
            RotationFinishTopple();
            CurrentAction = Action.Waiting;
        }
        else
        {
            Rotation += RotateTickIncrement;
        }
    }

    public override string Visualize()
    {
        int rotationInt = RotationInt;
        int heightInt = HeightInt;
        StringBuilder output = new StringBuilder(400);

        for (int extraRow = 9; extraRow >= 6; extraRow--)
        {
            for (int col = 0; col <= 4; col++)
            {
                if (rotationInt == col && extraRow <= heightInt + 4)
                {
                    output.Append(' ');
                    output.Append(BlockToColoredLetter(_heldBlocks[extraRow - heightInt], " "));
                    output.Append(' ');
                }
                else
                {
                    output.Append("   ");
                }
            }

            output.Append("\e[K\n");
        }

        for (int row = 5; row >= 0; row--)
        {
            for (int col = 0; col <= 4; col++)
            {
                bool armsOpen = ArmsDistance >= 0.5;
                bool blockHeldHere = rotationInt == col && row <= heightInt + 4 && row >= heightInt &&
                                     _heldBlocks[row - heightInt] != Block.Empty;

                string color = BlockToColoredLetter(blockHeldHere ? _heldBlocks[row - heightInt] : _blocks[col][row],
                    "-");

                if (row == heightInt && col == rotationInt)
                {
                    output.Append(armsOpen ? '<' : '>');
                    output.Append(color);
                    output.Append(armsOpen ? '>' : '<');
                }
                else
                {
                    output.Append(' ');
                    output.Append(color);
                    output.Append(' ');
                }
            }

            output.Append("\e[K\n");
        }
        
        if (_toppledBlocks.Count > 0)
        {
            output.Append("Toppled blocks: ");
            for (int i = 0; i < _toppledBlocks.Count; i++)
            {
                output.Append(BlockToColoredLetter(_toppledBlocks[i], " "));
                output.Append(' ');
            }
            output.Append("\e[K\n");
        }
        
        return output.ToString();
    }
}