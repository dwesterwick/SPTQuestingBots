using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class BotPathData : StaticPathData
    {
        public float DistanceToTarget => Vector3.Distance(bot.Position, TargetPosition);

        private BotOwner bot;
        
        public BotPathData(BotOwner botOwner) : base()
        {
            bot = botOwner;
        }

        public bool Update(Vector3 target, float reachDistance = 0.5f, bool force = false)
        {
            bool requiresUpdate = force;

            if (bot.Mover.LastPathSetTime != LastSetTime)
            {
                requiresUpdate = true;
            }

            if ((target != TargetPosition) || (reachDistance != ReachDistance))
            {
                TargetPosition = target;
                ReachDistance = reachDistance;
                requiresUpdate = true;
            }

            requiresUpdate |= Corners.Length == 0;

            if (requiresUpdate)
            {
                StartPosition = bot.Position;
                Status = CreatePathSegment(bot.Position, target, out Vector3[] corners);
                Corners = corners;
                LastSetTime = Time.time;
            }

            return requiresUpdate;
        }

        public float GetDistanceToFinalPoint()
        {
            if (Corners.Length == 0)
            {
                return float.NaN;
            }

            return Vector3.Distance(bot.Position, Corners.Last());
        }
    }
}
