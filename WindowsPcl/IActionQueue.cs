using System;

namespace AdjustSdk.Pcl
{
    public interface IActionQueue
    {
        string Name { get; }
        void Delay(TimeSpan timeSpan, Action action);
        void Enqueue(Action action);
    }
}