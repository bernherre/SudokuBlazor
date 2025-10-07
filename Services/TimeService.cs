using System;

namespace Sudoku.Web.Services
{
    public class TimerService : IDisposable
    {
        // Especifica explícitamente que quieres el de System.Timers
        private System.Timers.Timer? _timer;
        public int Seconds { get; private set; }
        public event Action? OnTick;

        public void Start()
        {
            Seconds = 0;
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (_, __) =>
            {
                Seconds++;
                OnTick?.Invoke();
            };
            _timer.AutoReset = true;
            _timer.Start();
        }

        public void Stop() => _timer?.Stop();

        public void Dispose() => _timer?.Dispose();
    }
}
