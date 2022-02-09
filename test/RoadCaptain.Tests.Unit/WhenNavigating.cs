﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using RoadCaptain.UseCases;
using Xunit;

namespace RoadCaptain.Tests.Unit
{
    public class WhenNavigating
    {
        private readonly InMemoryGameStateReceiver _gameStateReceiver;
        private readonly NavigationUseCase _useCase;
        private readonly InMemoryMessageReceiver _inMemoryMessageReceiver;
        private readonly FieldInfo _plannedRouteFieldInfo;

        public WhenNavigating()
        {
            var monitoringEvents = new NopMonitoringEvents();
            _gameStateReceiver = new InMemoryGameStateReceiver(monitoringEvents);

            var plannedRoute = FixedForTesting();

            _inMemoryMessageReceiver = new InMemoryMessageReceiver();
            _useCase = new NavigationUseCase(
                _gameStateReceiver,
                monitoringEvents,
                _inMemoryMessageReceiver);

            _gameStateReceiver.Enqueue("routeSelected", plannedRoute);

            // We need to use reflection here because sending the route
            // through the dispatcher does a serialize/deserialize which
            // means we don't have a reference to the planed route anymore.
            _plannedRouteFieldInfo = _useCase.GetType()
                .GetField("_plannedRoute", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private PlannedRoute PlannedRoute => _plannedRouteFieldInfo.GetValue(_useCase) as PlannedRoute;

        public static PlannedRoute FixedForTesting()
        {
            var route = new SegmentSequenceBuilder()
                .StartingAt("seg-1")
                .TuringLeftTo("seg-2")
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
            _gameStateReceiver.Enqueue("segmentChanged", "seg-1");

            WhenHandlingNavigation();

            PlannedRoute
                .CurrentSegmentId
                .Should()
                .Be("seg-1");
        }

        [Fact]
        public void GivenNotStartedRouteAndRiderIsNotOnStartingSegment_CurrentSegmentRemainsEmpty()
        {
            _gameStateReceiver.Enqueue("segmentChanged", "seg-NOT-ON-SEG");

            WhenHandlingNavigation();

            PlannedRoute
                .CurrentSegmentId
                .Should()
                .BeNull();
        }

        [Fact]
        public void GivenStartedRouteAndRiderEntersNextExpectedSegment_CurrentSegmentIsUpdated()
        {
            _gameStateReceiver.Enqueue("segmentChanged", "seg-1");
            _gameStateReceiver.Enqueue("segmentChanged", "seg-2");

            WhenHandlingNavigation();

            PlannedRoute
                .CurrentSegmentId
                .Should()
                .Be("seg-2");
        }

        [Fact]
        public void GivenStartedRouteAndRiderEntersNextExpectedSegment_NextSegmentIsUpdated()
        {
            _gameStateReceiver.Enqueue("segmentChanged", "seg-1");
            _gameStateReceiver.Enqueue("segmentChanged", "seg-2");

            WhenHandlingNavigation();

            PlannedRoute
                .NextSegmentId
                .Should()
                .Be("seg-3");
        }

        [Fact]
        public void GivenStartedRouteAndRiderEntersNextExpectedSegment_ExpectedTurnIsUpdated()
        {
            _gameStateReceiver.Enqueue("segmentChanged", "seg-1");
            _gameStateReceiver.Enqueue("segmentChanged", "seg-2");

            WhenHandlingNavigation();

            PlannedRoute
                .TurnToNextSegment
                .Should()
                .Be(TurnDirection.GoStraight);
        }

        [Fact]
        public void GivenStartedRouteOnSegmentThreeAndLeftAndGoStraightCommandsAvailable_NoCommandIsSent()
        {
            _gameStateReceiver.Enqueue("segmentChanged", "seg-1");
            _gameStateReceiver.Enqueue("segmentChanged", "seg-2");
            _gameStateReceiver.Enqueue("segmentChanged", "seg-3");
            _gameStateReceiver.Enqueue("turnCommandsAvailable", new List<TurnDirection> { TurnDirection.Left, TurnDirection.GoStraight });

            WhenHandlingNavigation();

            _inMemoryMessageReceiver
                .SentCommands
                .Should()
                .BeEmpty();
        }

        [Fact]
        public void GivenStartedRouteOnSegmentThreeAndLeftAndRightCommandsAvailable_TurnRightCommandIsSent()
        {
            _gameStateReceiver.Enqueue("segmentChanged", "seg-1");
            _gameStateReceiver.Enqueue("segmentChanged", "seg-2");
            _gameStateReceiver.Enqueue("segmentChanged", "seg-3");
            _gameStateReceiver.Enqueue("turnCommandsAvailable", new List<TurnDirection> { TurnDirection.Left, TurnDirection.Right });

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