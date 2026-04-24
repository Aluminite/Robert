using System;
using System.Diagnostics;

namespace Robert.RobotLogic;

public class SpeedModifier
{
    private Stopwatch _stopwatch = new Stopwatch();
    public double Modifier;
    
    public SpeedModifier(double modifier)
    {
        Modifier = modifier;
    }

    public TimeSpan Elapsed => _stopwatch.Elapsed * Modifier;

    public void Start()
    {
        _stopwatch.Start();
    }

    public void Restart()
    {
        _stopwatch.Restart();
    }
}