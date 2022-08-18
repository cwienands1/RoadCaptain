﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace RoadCaptain.GameStates
{
    public class CompletedRouteState : GameState
    {
        public CompletedRouteState(
            uint riderId, 
            ulong activityId, 
            TrackPoint currentPosition,
            PlannedRoute plannedRoute)
        {
            RiderId = riderId;
            ActivityId = activityId;
            CurrentPosition = currentPosition;
            Route = plannedRoute;
            Route.Complete();
        }

        [JsonProperty]
        public sealed override uint RiderId { get; }

        [JsonProperty]
        public ulong ActivityId { get; }
        
        [JsonProperty]
        public TrackPoint CurrentPosition { get; }

        [JsonProperty]
        public Segment CurrentSegment { get; }

        [JsonProperty]
        public SegmentDirection Direction { get; private set; } = SegmentDirection.Unknown;

        public double ElapsedDistance { get; private set; }

        public double ElapsedDescent { get; private set; }

        public double ElapsedAscent { get; private set; }

        [JsonProperty]
        public PlannedRoute Route { get; private set; }
        
        public override GameState EnterGame(uint riderId, ulong activityId)
        {
            throw InvalidStateTransitionException.AlreadyInGame(GetType());
        }

        public override GameState LeaveGame()
        {
            return new ConnectedToZwiftState();
        }

        public override GameState UpdatePosition(TrackPoint position, List<Segment> segments, PlannedRoute plannedRoute)
        {
            return new CompletedRouteState(RiderId, ActivityId, position, plannedRoute);
        }

        public override GameState TurnCommandAvailable(string type)
        {
            return this;
        }
    }
}