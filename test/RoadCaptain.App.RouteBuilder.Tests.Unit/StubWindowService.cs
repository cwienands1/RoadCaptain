// Copyright (c) 2022 Sander van Vliet
// Licensed under Artistic License 2.0
// See LICENSE or https://choosealicense.com/licenses/artistic-2.0/

using Autofac;
using JetBrains.Annotations;

namespace RoadCaptain.App.RouteBuilder.Tests.Unit
{
    public class StubWindowService : WindowService
    {
        public StubWindowService([NotNull] IComponentContext componentContext,
            [NotNull] MonitoringEvents monitoringEvents) : base(componentContext, monitoringEvents)
        {
        }
    }
}
