using System.Diagnostics;

namespace Robert_CLI;

public class Action
{
    private Stopwatch _stopwatch;
    private TimeSpan _start;
    private TimeSpan _duration;
    
    public Action(Stopwatch stopwatch, TimeSpan start, TimeSpan duration)
    {
        _stopwatch = stopwatch;
        _start = start;
        _duration = duration;
    }

    public Action(Stopwatch stopwatch, TimeSpan duration)
    {
        _stopwatch = stopwatch;
        _start = stopwatch.Elapsed;
        _duration = duration;
    }

    public bool Done => _stopwatch.Elapsed > _start + _duration;

    public double Progress
    {
        get
        {
            if (Done) return 1.0;
            return (_stopwatch.Elapsed - _start) / _duration;
        }
    }
}