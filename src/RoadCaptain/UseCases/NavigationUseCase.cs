﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RoadCaptain.Ports;

namespace RoadCaptain.UseCases
{
    public class NavigationUseCase
    {
        private readonly IGameStateReceiver _gameStateReceiver;
        private readonly MonitoringEvents _monitoringEvents;
        private readonly IMessageReceiver _messageReceiver;
        private PlannedRoute _plannedRoute;
        private ulong _lastSequenceNumber;
        private readonly IGameStateDispatcher _dispatcher;

        public NavigationUseCase(
            IGameStateReceiver gameStateReceiver,
            MonitoringEvents monitoringEvents,
            IMessageReceiver messageReceiver, 
            IGameStateDispatcher dispatcher)
        {
            _gameStateReceiver = gameStateReceiver;
            _monitoringEvents = monitoringEvents;
            _messageReceiver = messageReceiver;
            _dispatcher = dispatcher;
        }

        public void Execute(CancellationToken token)
        {
            // Set up handlers
            _gameStateReceiver
                .Register(RouteSelected,
                    LastSequenceNumberUpdated, 
                    null);

            // Start listening for game state updates,
            // the Start() method will block until token
            // is cancelled
            _gameStateReceiver.Start(token);
        }

        private void HandleEnteredGame(ulong obj)
        {
            // Reset the route when the user enters the game
            _plannedRoute.Reset();
        }

        private void LastSequenceNumberUpdated(ulong sequenceNumber)
        {
            _lastSequenceNumber = sequenceNumber;
        }

        private void RouteSelected(PlannedRoute route)
        {
            _plannedRoute = route;
        }

        private void HandleCommandsAvailable(List<TurnDirection> commands)
        {
            if (!_plannedRoute.HasCompleted && !_plannedRoute.HasStarted)
            {
                return;
            }

            if (commands.Any())
            {
                if (CommandsMatchTurnToNextSegment(commands, _plannedRoute.TurnToNextSegment))
                {
                    _monitoringEvents.Information("Executing turn {TurnDirection}", _plannedRoute.TurnToNextSegment);
                    _messageReceiver.SendTurnCommand(_plannedRoute.TurnToNextSegment, _lastSequenceNumber);
                }
                else
                {
                    _monitoringEvents.Error(
                        "Expected turn command {ExpectedTurnCommand} to be present but instead got: {TurnCommands}",
                        _plannedRoute.TurnToNextSegment,
                        string.Join(", ", commands));
                }
            }
        }

        private static bool CommandsMatchTurnToNextSegment(
            List<TurnDirection> commands,
            TurnDirection turnToNextSegemnt)
        {
            return commands.Contains(turnToNextSegemnt);
        }
    }
}