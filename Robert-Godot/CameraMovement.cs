using System;
using Godot;

namespace Robert;

public partial class CameraMovement : Camera3D
{
    private float _rho = 0.6f;
    private float _theta = MathF.PI / 4.0f;
    private float _phi = MathF.PI / 3.0f;
    
    private const float Sensitivity = 0.001f * MathF.PI;
    private const float MaxZoom = 0.2f;
    private const float MinZoom = 1.0f;
    private readonly Vector3 _pivot = new Vector3(0, 0.12f, 0);

    public override void _Ready()
    {
        CameraMove(Vector2.Zero);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionPressed("Camera Grab") && @event is InputEventMouseMotion eventMouseMotion)
        {
            CameraMove(eventMouseMotion.Relative);
        }
        if (Input.IsActionPressed("Zoom In"))
        {
            _rho = Mathf.Max(MaxZoom, _rho - 0.1f);
            CameraMove(Vector2.Zero);
        }

        if (Input.IsActionPressed("Zoom Out"))
        {
            _rho = Mathf.Min(MinZoom, _rho + 0.1f);
            CameraMove(Vector2.Zero);
        }
    }

    private void CameraMove(Vector2 movement)
    {
        _theta = Mathf.Wrap(_theta + (-movement.X * Sensitivity), -MathF.PI, MathF.PI);
        _phi = Mathf.Clamp(_phi + (-movement.Y * Sensitivity), MathF.PI * 0.05f, MathF.PI * 0.95f);

        float newX = _pivot.X + _rho * MathF.Sin(_phi) * MathF.Sin(_theta); 
        float newY = _pivot.Y + _rho * MathF.Cos(_phi);
        float newZ = _pivot.Z + _rho * MathF.Sin(_phi) * MathF.Cos(_theta);

        this.Position = new Vector3(newX, newY, newZ);

        this.LookAt(_pivot);
    }
}