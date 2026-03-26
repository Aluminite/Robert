using System.Collections.Generic;
using Godot;
using Robert.RobotLogic;

namespace Robert;

public partial class RobotVisual : Node3D
{
    private Node3D _robotRotation;
    private Node3D _robotHeight;
    private float _minHeight;
    private Node3D[] _robotArms;
    private StandardMaterial3D _ledMaterial;

    private Node3D[] _blocks;
    private Node3D _blocksParent;
    private Node3D _heldParent;
    private Node3D _blockHolders;

    private Node3D[] _gyros;
    private Node3D _gyrosParent;
    private Node3D _gyroHolders;

    private readonly Vector3 _baseBlockPosition = new Vector3(0.13f, 0.084f, 0f);
    private readonly Vector3 _baseGyroPosition = new Vector3(0.13f, 0.072f, 0f);

    public override void _Ready()
    {
        _robotRotation = GetNode<Node3D>("Rotation");
        _robotHeight = GetNode<Node3D>("Rotation/Height");
        _minHeight = _robotHeight.Position.Y - 0.07f;
        _robotArms = new Node3D[2];
        _robotArms[0] = GetNode<Node3D>("Rotation/Height/Arms/LeftArm");
        _robotArms[1] = GetNode<Node3D>("Rotation/Height/Arms/RightArm");
        _ledMaterial = (StandardMaterial3D)GetNode<MeshInstance3D>("BaseAndHead/LED").Mesh.SurfaceGetMaterial(0);

        _blocks = new Node3D[5];
        _blocks[4] = GetNode<Node3D>("Blocks/Red");
        _blocks[3] = GetNode<Node3D>("Blocks/White");
        _blocks[2] = GetNode<Node3D>("Blocks/Blue");
        _blocks[1] = GetNode<Node3D>("Blocks/Yellow");
        _blocks[0] = GetNode<Node3D>("Blocks/Green");

        _blocksParent = GetNode<Node3D>("Blocks");
        _heldParent = GetNode<Node3D>("Rotation/Height/Arms/HeldObject");
        _blockHolders = GetNode<Node3D>("BlockHolders");

        _gyros = new Node3D[2];
        _gyros[0] = GetNode<Node3D>("Gyros/Gyro1");
        _gyros[1] = GetNode<Node3D>("Gyros/Gyro2");
        _gyrosParent = GetNode<Node3D>("Gyros");
        _gyroHolders = GetNode<Node3D>("GyroHolders");
    }

    private Node3D BlockEnumToNode(StackUpRobot.Block block)
    {
        return block switch
        {
            StackUpRobot.Block.Red => _blocks[4],
            StackUpRobot.Block.White => _blocks[3],
            StackUpRobot.Block.Blue => _blocks[2],
            StackUpRobot.Block.Yellow => _blocks[1],
            StackUpRobot.Block.Green => _blocks[0],
            _ => null
        };
    }

    public void ApplyState(RobotState state, double delta)
    {
        // Move the robot itself
        _robotRotation.RotationDegrees = new Vector3(0, (float)-state.Rotation * 60, 0);
        _robotHeight.Position = new Vector3(0, (float)state.Height * (7f / 5f) * 0.01f + _minHeight, 0);
        _robotArms[0].RotationDegrees = new Vector3(0, (float)state.ArmsDistance * 10, 0);
        _robotArms[1].RotationDegrees = new Vector3(0, (float)state.ArmsDistance * -10, 0);
        _ledMaterial.EmissionEnabled = state.LedOn;

        if (state is StackUpRobotState stackup)
        {
            foreach (Node3D gyro in _gyros)
            {
                gyro.Visible = false;
            }

            _gyroHolders.Visible = false;

            _blockHolders.Visible = true;
            HashSet<Node3D> seen = new HashSet<Node3D>();

            // Deal with the placed blocks
            for (int col = 0; col < stackup.Blocks.Length; col++)
            {
                for (int height = 0; height < stackup.Blocks[0].Length; height++)
                {
                    if (stackup.Blocks[col][height] != StackUpRobot.Block.Empty)
                    {
                        Node3D blockNode = BlockEnumToNode(stackup.Blocks[col][height]);

                        if (blockNode != null)
                        {
                            seen.Add(blockNode);
                            blockNode.Reparent(_blocksParent);
                            blockNode.Visible = true;
                            blockNode.TopLevel = true;
                            blockNode.Position =
                                (_baseBlockPosition + new Vector3(0, 0.014f * height, 0)).Rotated(Vector3.Up,
                                    Mathf.DegToRad(-60 * (col - 2)));
                        }
                    }
                }
            }

            // Deal with the held blocks (if any)
            for (int height = 0; height < stackup.HeldBlocks.Length; height++)
            {
                if (stackup.HeldBlocks[height] == StackUpRobot.Block.Empty) break;

                Node3D blockNode = BlockEnumToNode(stackup.HeldBlocks[height]);
                seen.Add(blockNode);
                blockNode.Reparent(_heldParent);
                blockNode.Visible = true;
                blockNode.TopLevel = false;
                blockNode.Position = new Vector3(0, height * 0.014f, 0);
            }

            // Clean up blocks not seen yet
            foreach (Node3D blockNode in _blocks)
            {
                if (!seen.Contains(blockNode))
                {
                    blockNode.Visible = false;
                }
            }
        }
        else if (state is GyromiteRobotState gyromite)
        {
            foreach (Node3D block in _blocks)
            {
                block.Visible = false;
            }

            _blockHolders.Visible = false;

            _gyroHolders.Visible = true;

            foreach (GyromiteRobotState.GyroState gyro in gyromite.Gyros)
            {
                Node3D gyroNode = _gyros[gyro.Number];
                gyroNode.Visible = !gyro.Toppled;
                if (gyromite.HeldItem == gyro)
                {
                    gyroNode.Reparent(_heldParent);
                    gyroNode.TopLevel = false;
                    gyroNode.Position = new Vector3(0f, -0.012f, 0f);
                }
                else
                {
                    gyroNode.Reparent(_gyrosParent);
                    gyroNode.TopLevel = true;
                    gyroNode.Position = (_baseGyroPosition + new Vector3(0, 0.014f * gyro.Height, 0)).Rotated(
                        Vector3.Up, Mathf.DegToRad(-60 * (gyro.Column - 2)));
                }

                // I estimated from the audio of a gyro spinning that the gyro spins at 50 rev/sec at full speed.
                // Is this wrong? Probably. But to be honest it doesn't really matter
                // as anything this fast is a complete blur anyway.
                float rotationSpeedDegreesPerSec = (float)(gyro.SpinTimer / GyromiteRobot.Gyro.MaxSpinTime) * 50 * 360;
                float newGyroRotation =
                    Mathf.Wrap((float)(gyroNode.RotationDegrees.Y + rotationSpeedDegreesPerSec * delta), -180, 180);
                gyroNode.RotationDegrees = new Vector3(0, newGyroRotation, 0);
            }
        }
        else
        {
            foreach (Node3D block in _blocks)
            {
                block.Visible = false;
            }

            _blockHolders.Visible = false;

            foreach (Node3D gyro in _gyros)
            {
                gyro.Visible = false;
            }

            _gyroHolders.Visible = false;
        }
    }
}