﻿using System;
using System.Collections.Generic;
using System.Linq;
using RoadCaptain.Ports;

namespace RoadCaptain.UseCases
{
    public class HandleAvailableTurnsUseCase
    {
        private readonly IGameStateDispatcher _dispatcher;
        private readonly List<TurnDirection> _commands = new();
        private string _currentSegmentId;

        public HandleAvailableTurnsUseCase(IGameStateDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Execute(ZwiftCommandAvailableMessage commandAvailable)
        {
            if ("somethingempty".Equals(commandAvailable.Type, StringComparison.InvariantCultureIgnoreCase) && 
                _commands.Any())
            {
                // Reset available commands by dispatching an empty list.
                // But only when the segment changed because we're seeing SomethingEmpty + new commands repeat a lot
                if (_currentSegmentId != _dispatcher.CurrentSegment.Id)
                {
                    _commands.Clear();
                    _dispatcher.TurnCommandsAvailable(new List<TurnDirection>());
                    return;
                }
            }
            
            // Track changes by simply counting the number of items
            var startCount = _commands.Count;

            switch (commandAvailable.Type.Trim().ToLower())
            {
                case "turnleft":
                    if (!_commands.Contains(TurnDirection.Left))
                    {
                        _commands.Add(TurnDirection.Left);
                    }

                    break;
                case "turnright":
                    if (!_commands.Contains(TurnDirection.Right))
                    {
                        _commands.Add(TurnDirection.Right);
                    }

                    break;
                case "gostraight":
                    if (!_commands.Contains(TurnDirection.GoStraight))
                    {
                        _commands.Add(TurnDirection.GoStraight);
                    }

                    break;
            }

            // Only call the dispatcher when the number of turns changed and
            // there are at least two turns available.
            if (startCount != _commands.Count && _commands.Count >= 2)
            {
                _currentSegmentId = _dispatcher.CurrentSegment.Id;
                _dispatcher.TurnCommandsAvailable(_commands);
            }
        }
    }
}