using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Ghost
{
    [Serializable]
    public struct GhostFrame
    {
        public float time;
        public Vector3 position;
        public Quaternion rotation;

        public GhostFrame(float time, Vector3 position, Quaternion rotation)
        {
            this.time = time;
            this.position = position;
            this.rotation = rotation;
        }
    }

    [Serializable]
    public class GhostLapData
    {
        public string mapId;
        public float lapTime;
        public List<GhostFrame> frames = new();

        public GhostLapData(string mapId, float lapTime, List<GhostFrame> frames)
        {
            this.mapId = mapId;
            this.lapTime = lapTime;
            this.frames = frames;
        }
    }
}