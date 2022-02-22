﻿namespace RoadCaptain.GameStates
{
    public class PositionedState : InGameState
    {
        public TrackPoint CurrentPosition { get; }

        public PositionedState(int activityId, TrackPoint currentPosition)
            : base(activityId)
        {
            CurrentPosition = currentPosition;
        }
    }
}