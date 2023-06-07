// Copyright (c) 2023 Sander van Vliet
// Licensed under Artistic License 2.0
// See LICENSE or https://choosealicense.com/licenses/artistic-2.0/

using System.Threading.Tasks;
using RoadCaptain.Ports;
using RoadCaptain.UseCases;
using Serilog.Core;

namespace RoadCaptain.App.Runner.ViewModels
{
    public class DesignTimeSelectRouteWindowViewModel : SelectRouteWindowViewModel
    {
        public DesignTimeSelectRouteWindowViewModel() : base(
            new SearchRoutesUseCase(new [] { new StubRouteRepository()}, new MonitoringEventsWithSerilog(Logger.None)),
            new RetrieveRepositoryNamesUseCase(new [] { new StubRouteRepository()}),
            new DesignTimeWindowService(), new StubWorldStore())
        {
            Repositories = new[]
            {
                "All",
                "Local"
            };
        }
    }

    public class StubWorldStore : IWorldStore
    {
        public World[] LoadWorlds()
        {
            return new[]
            {
                new World { Id = "watopia", Name = "Watopia" },
                new World { Id = "makuri_islands", Name = "Makuri Islands" },
            };
        }

        public World? LoadWorldById(string id)
        {
            return null;
        }
    }

    internal class StubRouteRepository : IRouteRepository
    {
        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(true);
        }

        public Task<RouteModel[]> SearchAsync(string? world = null, string? creator = null, string? name = null,
            string? zwiftRouteName = null,
            int? minDistance = null, int? maxDistance = null, int? minAscent = null, int? maxAscent = null,
            int? minDescent = null, int? maxDescent = null, bool? isLoop = null, string[]? komSegments = null,
            string[]? sprintSegments = null)
        {
            return Task.FromResult(new[]
            {
                new RouteModel
                {
                    Name = "Demo route",
                    Ascent = 1500,
                    Descent = 1200,
                    Distance = 100,
                    CreatorName = "Sander van Vliet",
                    ZwiftRouteName = "Muir and the Mountain",
                    IsLoop = false,
                    Id = 1,
                    CreatorZwiftProfileId = "https://roadcaptain.nl"
                }
            });
        }

        public Task<RouteModel> StoreAsync(PlannedRoute plannedRoute, OAuthToken token)
        {
            throw new System.NotImplementedException();
        }

        public string Name => "Local";
    }
}
