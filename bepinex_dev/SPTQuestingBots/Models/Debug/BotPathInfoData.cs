using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Models.Debug
{
    public class BotPathInfoData
    {
        public BotPathMarkerData TargetMarker { get; set; } = new BotPathMarkerData();

        public bool IsActive => TargetMarker.IsActive;

        public BotPathInfoData()
        {

        }

        public void SetActive(bool state)
        {
            TargetMarker.SetActive(state);
        }

        public void Destroy()
        {
            TargetMarker.Destroy();
        }
    }
}
