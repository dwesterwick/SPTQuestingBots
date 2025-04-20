using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class OverlayData
    {
        public ActorDataStruct Data { get; set; }
        public GUIContent GuiContent { get; set; }
        public Rect GuiRect { get; set; }
        public Vector3 Position { get; set; }
        public string StaticText { get; set; } = "";

        private Stopwatch updateTimer = Stopwatch.StartNew();

        public long LastUpdateElapsedTime => updateTimer.ElapsedMilliseconds;

        public OverlayData()
        {
            GuiContent = new GUIContent();
            GuiRect = new Rect();
        }

        public OverlayData(Vector3 _position) : this()
        {
            Position = _position;
        }

        public OverlayData(Vector3 _position, string _staticText) : this(_position)
        {
            StaticText = _staticText;
        }

        public void ResetUpdateTime()
        {
            updateTimer.Restart();
        }
    }
}
