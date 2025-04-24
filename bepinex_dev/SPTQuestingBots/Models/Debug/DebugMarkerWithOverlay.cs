using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class DebugMarkerWithOverlay : IDisposable
    {
        public GameObject Marker { get; set; } = null;
        public DebugOverlay Overlay { get; set; }
        public Vector3 Position { get; set; } = Vector3.negativeInfinity;
        public float MarkerRadius { get; private set; }

        public bool HasMarker => Marker != null;
        public bool IsActive => HasMarker && Marker.activeSelf;

        public DebugMarkerWithOverlay(Func<GUIStyle> _getGuiStyle, float _markerRadius)
        {
            Overlay = new DebugOverlay(_getGuiStyle);
            MarkerRadius = _markerRadius;
        }

        public DebugMarkerWithOverlay(Func<GUIStyle> _getGuiStyle, float _markerRadius, GameObject _marker, DebugOverlay _overlay) : this(_getGuiStyle, _markerRadius)
        {
            Marker = _marker;
            Overlay = _overlay;
        }

        public void Dispose()
        {
            Destroy();
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

        public void DrawOverlay(string text)
        {
            Overlay.Draw(text, Position);
        }
    }
}
