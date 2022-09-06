﻿using System;
using System.IO;
using RoadCaptain.Ports;

namespace RoadCaptain.App.Runner.Tests.Unit
{
    public class StubRouteStore : IRouteStore
    {
        public PlannedRoute LoadFrom(string path)
        {
            if (path == "someroute.json")
            {
                return new PlannedRoute
                {
                    World = new World { Id = "watopia"},
                    Sport = SportType.Cycling
                };
            }

            if (path.Contains("RebelRoute-"))
            {
                return new PlannedRoute
                {
                    Name = "rebel-route-stub",
                    World = new World { Id = "watopia"},
                    Sport = SportType.Cycling
                };
            }

            throw new FileNotFoundException();
        }

        public void Store(PlannedRoute route, string path)
        {
            throw new NotImplementedException();
        }
    }
}