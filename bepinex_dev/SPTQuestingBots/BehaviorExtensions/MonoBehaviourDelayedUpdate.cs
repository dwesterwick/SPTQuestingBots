using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BehaviorExtensions
{
    public abstract class MonoBehaviourDelayedUpdate : MonoBehaviour
    {
        public int UpdateInterval { get; set; } = 100;

        private Stopwatch updateTimer = Stopwatch.StartNew();

        protected bool canUpdate()
        {
            if (updateTimer.ElapsedMilliseconds < UpdateInterval)
            {
                return false;
            }

            updateTimer.Restart();
            return true;
        }
    }
}
