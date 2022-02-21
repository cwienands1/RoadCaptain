﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using RoadCaptain.Adapters;
using RoadCaptain.UseCases;
using Xunit;

namespace RoadCaptain.Tests.Unit
{
    public class WhenNavigating
    {
        private readonly NavigationUseCase _useCase;
        private readonly InMemoryMessageReceiver _inMemoryMessageReceiver;
        private readonly InMemoryGameStateDispatcher _gameStateDispatcher;
        private readonly FieldInfo _plannedRouteFieldInfo;

        public WhenNavigating()
        {
            var monitoringEvents = new NopMonitoringEvents();
            _gameStateDispatcher = new InMemoryGameStateDispatcher(monitoringEvents);
            var plannedRoute = FixedForTesting();

            _inMemoryMessageReceiver = new InMemoryMessageReceiver();
            _useCase = new NavigationUseCase(
                _gameStateDispatcher,
                monitoringEvents,
                _inMemoryMessageReceiver,
                _gameStateDispatcher);
            

            // We need to use reflection here because sending the route
            // through the dispatcher does a serialize/deserialize which
            // means we don't have a reference to the planed route anymore.
            _plannedRouteFieldInfo = _useCase.GetType()
                .GetField("_plannedRoute", BindingFlags.Instance | BindingFlags.NonPublic);
            _gameStateDispatcher.EnterGame(1);
            _gameStateDispatcher.RouteSelected(plannedRoute);
        }

        private PlannedRoute CurrentRoute => _plannedRouteFieldInfo.GetValue(_useCase) as PlannedRoute;

        public static PlannedRoute FixedForTesting()
        {
            var route = new SegmentSequenceBuilder()
                .StartingAt("seg-1")
                .TurningLeftTo("seg-2")
                .GoingStraightTo("seg-3")
                .TurningRightTo("seg-4")
                .GoingStraightTo("seg-5")
                .GoingStraightTo("seg-6")
                .TurningRightTo("seg-7")
                .EndingAt("seg-7")
                .Build();

            return route;
        }

        [Fact]
        public void GivenNotStartedRouteAndRiderIsOnStartingSegment_PlannedRouteIsStarted()
        {
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-1" });

            WhenHandlingNavigation();

            CurrentRoute
                .CurrentSegmentId
                .Should()
                .Be("seg-1");
        }

        [Fact]
        public void GivenNotStartedRouteAndRiderIsNotOnStartingSegment_CurrentSegmentRemainsEmpty()
        {
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-NOT-ON-SEG" });

            WhenHandlingNavigation();
            
            CurrentRoute
                .CurrentSegmentId
                .Should()
                .BeNull();
        }

        [Fact]
        public void GivenStartedRouteAndRiderEntersNextExpectedSegment_CurrentSegmentIsUpdated()
        {
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-1" });
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-2" });

            WhenHandlingNavigation();
            
            CurrentRoute
                .CurrentSegmentId
                .Should()
                .Be("seg-2");
        }

        [Fact]
        public void GivenStartedRouteAndRiderEntersNextExpectedSegment_NextSegmentIsUpdated()
        {
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-1" });
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-2" });

            WhenHandlingNavigation();
            
            CurrentRoute
                .NextSegmentId
                .Should()
                .Be("seg-3");
        }

        [Fact]
        public void GivenStartedRouteAndRiderEntersNextExpectedSegment_ExpectedTurnIsUpdated()
        {
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-1" });
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-2" });

            WhenHandlingNavigation();
            
            CurrentRoute
                .TurnToNextSegment
                .Should()
                .Be(TurnDirection.GoStraight);
        }

        [Fact]
        public void GivenStartedRouteOnSegmentThreeAndLeftAndGoStraightCommandsAvailable_NoCommandIsSent()
        {
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-1" });
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-2" });
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-3" });
            _gameStateDispatcher.TurnCommandsAvailable(new List<TurnDirection> { TurnDirection.Left, TurnDirection.GoStraight });

            WhenHandlingNavigation();

            _inMemoryMessageReceiver
                .SentCommands
                .Should()
                .BeEmpty();
        }

        [Fact]
        public void GivenStartedRouteOnSegmentThreeAndLeftAndRightCommandsAvailable_TurnRightCommandIsSent()
        {
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-1" });
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-2" });
            _gameStateDispatcher.SegmentChanged(new Segment { Id = "seg-3" });
            _gameStateDispatcher.TurnCommandsAvailable(new List<TurnDirection> { TurnDirection.Left, TurnDirection.Right });

            WhenHandlingNavigation();

            _inMemoryMessageReceiver
                .SentCommands
                .Should()
                .Contain(TurnDirection.Right.ToString())
                .And
                .HaveCount(1);
        }

        [Fact]
        public void GivenStartedRouteOnLastSegment_NextSegmentIsEmpty()
        {
            var plannedRoute = FixedForTesting();

            plannedRoute.EnteredSegment("seg-1");
            plannedRoute.EnteredSegment("seg-2");
            plannedRoute.EnteredSegment("seg-3");
            plannedRoute.EnteredSegment("seg-4");
            plannedRoute.EnteredSegment("seg-5");
            plannedRoute.EnteredSegment("seg-6");
            plannedRoute.EnteredSegment("seg-7");

            plannedRoute
                .NextSegmentId
                .Should()
                .BeNull();
        }

        [Fact]
        public void GivenStartedRouteOnSegmentThreeAndEnteringSegmentFive_ArgumentExceptionIsThrown()
        {
            var plannedRoute = FixedForTesting();
            plannedRoute.EnteredSegment("seg-1");
            plannedRoute.EnteredSegment("seg-2");
            plannedRoute.EnteredSegment("seg-3");

            Action act = () => plannedRoute.EnteredSegment("seg-5");

            act.Should()
                .Throw<ArgumentException>();

            plannedRoute
                .NextSegmentId
                .Should()
                .Be("seg-4");
        }

        [Fact]
        public void GivenStartedRouteOnLastSegment_TurnToNextSegmentIsNone()
        {
            var plannedRoute = FixedForTesting();
            plannedRoute.EnteredSegment("seg-1");
            plannedRoute.EnteredSegment("seg-2");
            plannedRoute.EnteredSegment("seg-3");
            plannedRoute.EnteredSegment("seg-4");
            plannedRoute.EnteredSegment("seg-5");
            plannedRoute.EnteredSegment("seg-6");
            plannedRoute.EnteredSegment("seg-7");

            plannedRoute
                .TurnToNextSegment
                .Should()
                .Be(TurnDirection.None);
        }

        [Fact]
        public void GivenStartedRouteOnLastSegmentAndEnteringSegment_ArgumentExceptionIsThrown()
        {
            var plannedRoute = FixedForTesting();

            plannedRoute.EnteredSegment("seg-1");
            plannedRoute.EnteredSegment("seg-2");
            plannedRoute.EnteredSegment("seg-3");
            plannedRoute.EnteredSegment("seg-4");
            plannedRoute.EnteredSegment("seg-5");
            plannedRoute.EnteredSegment("seg-6");
            plannedRoute.EnteredSegment("seg-7");

            Action act = () => plannedRoute.EnteredSegment("seg-5");

            act
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Route has already completed, can't enter new segment");
        }

        private void WhenHandlingNavigation()
        {
            var tokenSource = Debugger.IsAttached
                ? new CancellationTokenSource()
                : new CancellationTokenSource(100);

            try
            {
                _useCase.Execute(tokenSource.Token);
            }
            finally
            {
                tokenSource.Cancel();
            }
        }
    }
}