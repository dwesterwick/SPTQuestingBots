using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class BotPathMarkerData
    {
        public GameObject Marker { get; set; } = null;
        public OverlayData Overlay { get; set; } = new OverlayData();

        public bool HasMarker => Marker != null;
        public bool IsActive => HasMarker && Marker.activeSelf;

        public BotPathMarkerData()
        {

        }

        public void SetActive(bool state)
        {
            if (Marker != null)
            {
                Marker.SetActive(state);
            }
        }

        public void Destroy()
        {
            if (Marker != null)
            {
                UnityEngine.Object.Destroy(Marker);
                Marker = null;
            }
        }
    }
}
