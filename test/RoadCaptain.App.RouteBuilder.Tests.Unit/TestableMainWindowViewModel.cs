// Copyright (c) 2023 Sander van Vliet
// Licensed under Artistic License 2.0
// See LICENSE or https://choosealicense.com/licenses/artistic-2.0/

using System.Diagnostics;
using System.Threading.Tasks;
using RoadCaptain.App.RouteBuilder.Services;
using RoadCaptain.App.RouteBuilder.ViewModels;
using RoadCaptain.App.Shared.Commands;
using RoadCaptain.Ports;

namespace RoadCaptain.App.RouteBuilder.Tests.Unit
{
    public class TestableMainWindowViewModel : MainWindowViewModel
    {
        public TestableMainWindowViewModel(IRouteStore routeStore, ISegmentStore segmentStore,
            IVersionChecker versionChecker,
            IWindowService windowService, IWorldStore worldStore, IUserPreferences userPreferences,
            IApplicationFeatures applicationFeatures, IStatusBarService statusBarService)
            : base(routeStore, segmentStore, versionChecker, windowService, worldStore, userPreferences, applicationFeatures, statusBarService)
        {
        }

        [DebuggerStepThrough]
        public async Task<CommandResult> CallAddSegmentToRoute(Segment segment)
        {
             BuildRouteViewModel.SelectSegmentCommand.Execute(segment);
             return CommandResult.Success();
        }
    }
}
