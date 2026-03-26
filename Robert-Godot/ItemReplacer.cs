using Godot;
using Robert.RobotLogic;

namespace Robert;

public partial class ItemReplacer : Node
{
    private RobotController _controller;
    private Control _fallenItemsGroup;
    private MenuButton _whiteButton;
    private MenuButton _redButton;
    private MenuButton _yellowButton;
    private MenuButton _greenButton;
    private MenuButton _blueButton;
    private MenuButton _gyro1Button;
    private MenuButton _gyro2Button;

    public override void _Ready()
    {
        _controller = GetNode<RobotController>("../RobotController");
        _fallenItemsGroup = GetNode<Control>("../FoldableContainer/ScrollContainer/VBoxContainer/FallenItems");

        Node buttonContainer = _fallenItemsGroup.GetNode("ScrollContainer/FallenItemsContainer");
        _redButton = buttonContainer.GetNode<MenuButton>("RedBlock");
        _yellowButton = buttonContainer.GetNode<MenuButton>("YellowBlock");
        _greenButton = buttonContainer.GetNode<MenuButton>("GreenBlock");
        _blueButton = buttonContainer.GetNode<MenuButton>("BlueBlock");
        _whiteButton = buttonContainer.GetNode<MenuButton>("WhiteBlock");
        _gyro1Button = buttonContainer.GetNode<MenuButton>("Gyro1");
        _gyro2Button = buttonContainer.GetNode<MenuButton>("Gyro2");

        // I don't understand why Godot doesn't allow you to make these connections in the editor.
        Callable redCallable = new Callable(this, MethodName._replaceRedBlock);
        Callable yellowCallable = new Callable(this, MethodName._replaceYellowBlock);
        Callable greenCallable = new Callable(this, MethodName._replaceGreenBlock);
        Callable blueCallable = new Callable(this, MethodName._replaceBlueBlock);
        Callable whiteCallable = new Callable(this, MethodName._replaceWhiteBlock);
        Callable gyro1Callable = new Callable(this, MethodName._replaceGyro1);
        Callable gyro2Callable = new Callable(this, MethodName._replaceGyro2);

        _redButton.GetPopup().Connect("id_pressed", redCallable);
        _yellowButton.GetPopup().Connect("id_pressed", yellowCallable);
        _greenButton.GetPopup().Connect("id_pressed", greenCallable);
        _blueButton.GetPopup().Connect("id_pressed", blueCallable);
        _whiteButton.GetPopup().Connect("id_pressed", whiteCallable);
        _gyro1Button.GetPopup().Connect("id_pressed", gyro1Callable);
        _gyro2Button.GetPopup().Connect("id_pressed", gyro2Callable);
    }

    public void UpdateShownButtons(RobotState state)
    {
        if (state is StackUpRobotState stackup)
        {
            _fallenItemsGroup.Visible = true;
            _gyro1Button.Visible = false;
            _gyro2Button.Visible = false;

            bool redSeen = false;
            bool yellowSeen = false;
            bool greenSeen = false;
            bool blueSeen = false;
            bool whiteSeen = false;

            foreach (StackUpRobot.Block block in stackup.ToppledBlocks)
            {
                switch (block)
                {
                    case StackUpRobot.Block.Red:
                        redSeen = true;
                        break;
                    case StackUpRobot.Block.Yellow:
                        yellowSeen = true;
                        break;
                    case StackUpRobot.Block.Green:
                        greenSeen = true;
                        break;
                    case StackUpRobot.Block.Blue:
                        blueSeen = true;
                        break;
                    case StackUpRobot.Block.White:
                        whiteSeen = true;
                        break;
                }
            }

            _redButton.Visible = redSeen;
            _yellowButton.Visible = yellowSeen;
            _greenButton.Visible = greenSeen;
            _blueButton.Visible = blueSeen;
            _whiteButton.Visible = whiteSeen;
        }
        else if (state is GyromiteRobotState gyromite)
        {
            _fallenItemsGroup.Visible = true;
            _redButton.Visible = false;
            _yellowButton.Visible = false;
            _greenButton.Visible = false;
            _blueButton.Visible = false;
            _whiteButton.Visible = false;

            _gyro1Button.Visible = gyromite.Gyros[0].Toppled;
            _gyro2Button.Visible = gyromite.Gyros[1].Toppled;
        }
        else
        {
            _fallenItemsGroup.Visible = false;
        }
    }

    private void _replaceBlock(StackUpRobot.Block color, int col)
    {
        if (_controller.Robot is StackUpRobot stackup)
        {
            stackup.ReplaceToppled(color, col);
        }
    }

    private void _replaceRedBlock(int col)
    {
        _replaceBlock(StackUpRobot.Block.Red, col);
    }

    private void _replaceYellowBlock(int col)
    {
        _replaceBlock(StackUpRobot.Block.Yellow, col);
    }

    private void _replaceGreenBlock(int col)
    {
        _replaceBlock(StackUpRobot.Block.Green, col);
    }

    private void _replaceBlueBlock(int col)
    {
        _replaceBlock(StackUpRobot.Block.Blue, col);
    }

    private void _replaceWhiteBlock(int col)
    {
        _replaceBlock(StackUpRobot.Block.White, col);
    }

    private void _replaceGyro1(int col)
    {
        if (_controller.Robot is GyromiteRobot gyromite)
        {
            gyromite.ReplaceToppled(0, col);
        }
    }

    private void _replaceGyro2(int col)
    {
        if (_controller.Robot is GyromiteRobot gyromite)
        {
            gyromite.ReplaceToppled(1, col);
        }
    }
}