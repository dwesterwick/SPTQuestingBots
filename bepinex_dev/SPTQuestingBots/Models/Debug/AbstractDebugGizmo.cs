using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public abstract class AbstractDebugGizmo : IDisposable
    {
        public int UpdateInterval { get; set; } = 100;

        private Stopwatch updateTimer = Stopwatch.StartNew();

        public long LastUpdateElapsedTime => updateTimer.ElapsedMilliseconds;

        public AbstractDebugGizmo() { }
        public AbstractDebugGizmo(int _updateInterval) : this() { UpdateInterval = _updateInterval; }

        public abstract GUIStyle UpdateGUIStyle();
        public abstract bool ReadyToDispose();
        public abstract void Draw();
        protected abstract void OnUpdate();
        protected abstract void OnDispose();

        public void Dispose()
        {
            OnDispose();
        }

        public void Update()
        {
            // Don't update the overlay too often or performance and RAM usage will be affected
            if (LastUpdateElapsedTime < UpdateInterval)
            {
                return;
            }

            OnUpdate();

            updateTimer.Restart();
        }
    }
}
