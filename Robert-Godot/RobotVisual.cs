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

    private readonly Vector3 _baseBlockPosition = new Vector3(0.13f, 0.084f, 0f);

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

    public void ApplyState(RobotState state)
    {
        _robotRotation.RotationDegrees = new Vector3(0, (float)-state.Rotation * 60, 0);
        _robotHeight.Position = new Vector3(0, (float)state.Height * (7f / 5f) * 0.01f + _minHeight, 0);
        _robotArms[0].RotationDegrees = new Vector3(0, (float)state.ArmsDistance * 10, 0);
        _robotArms[1].RotationDegrees = new Vector3(0, (float)state.ArmsDistance * -10, 0);
        _ledMaterial.EmissionEnabled = state.LedOn;

        if (state is StackUpRobotState stackup)
        {
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
        else
        {
            foreach (Node3D block in _blocks)
            {
                block.Visible = false;
            }

            _blockHolders.Visible = false;
        }
    }
}