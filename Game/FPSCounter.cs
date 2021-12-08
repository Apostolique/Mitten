using System;
using Apos.Input;
using Microsoft.Xna.Framework;

namespace GameProject {
    class FPSCounter {
        public FPSCounter() {
            TargetElapsedTime = TimeSpan.FromTicks(166667);
        }

        public TimeSpan TargetElapsedTime {
            get => _targetElapsedTime;
            set {
                _targetElapsedTime = value;
                _targetFPS = (int)Math.Round((double)ONE / _targetElapsedTime.Ticks);
            }
        }
        public int UpdatePerSecond { get; private set; } = 0;
        public int FramesPerSecond { get; private set; } = 0;
        public double TimePerUpdate { get; private set; } = 0;
        public double TimePerFrame { get; private set; } = 0;
        public int DroppedFrames { get; set; } = 0;

        public void Update(GameTime gameTime) {
            _updateCounter++;

            if (gameTime.TotalGameTime.Ticks - _lastUpdateTick < ONE) {
                return;
            }

            UpdatePerSecond = _updateCounter;
            _updateCounter = 0;
            _lastUpdateTick = gameTime.TotalGameTime.Ticks;

            TimePerUpdate = Math.Truncate(1000d / UpdatePerSecond * 10000) / 10000;
        }

        public void Draw(GameTime gameTime) {
            _frameCounter++;
            long currentTick = gameTime.TotalGameTime.Ticks;

            if (currentTick - _lastDrawTick < ONE) {
                return;
            }

            FramesPerSecond = _frameCounter;
            _frameCounter = 0;
            _lastDrawTick = currentTick;

            TimePerFrame = Math.Truncate(1000d / FramesPerSecond * 10000) / 10000;

            if (FramesPerSecond < _targetFPS && currentTick > THREE && InputHelper.IsActive) {
                DroppedFrames += _targetFPS - FramesPerSecond;
            }
        }

        private TimeSpan _targetElapsedTime;
        private int _targetFPS;

        private long _lastUpdateTick = 0;
        private long _lastDrawTick = 0;
        private int _frameCounter = 0;
        private int _updateCounter = 0;

        private const long ONE = TimeSpan.TicksPerSecond;
        private const long THREE = TimeSpan.TicksPerSecond * 3;
        private const long SIXTY = TimeSpan.TicksPerSecond * 60;
    }
}
