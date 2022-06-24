﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FluentAssertions;
using RoadCaptain.Adapters;
using RoadCaptain.App.Runner.Models;
using RoadCaptain.App.Runner.ViewModels;
using RoadCaptain.App.Runner.Views;
using RoadCaptain.GameStates;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace RoadCaptain.App.Runner.Tests.Unit.Views
{
    public class InGameWindowTests
    {
        //[Fact]
        public void GivenRouteInLastSegment_SecondSegmentRowIsNotVisible()
        {
            var segments = new List<Segment>
            {
                new(new List<TrackPoint>()) { Id = "seg-1"},
                new(new List<TrackPoint>()) { Id = "seg-2"},
                new(new List<TrackPoint>()) { Id = "seg-3"},
            };
            var route = new PlannedRoute()
            {
                Name = "TestRoute",
                World = new World { Id = "testworld", Name = "TestWorld" },
                ZwiftRouteName = "Mountain route"
            };
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-1", NextSegmentId = "seg-2", Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-2", NextSegmentId = "seg-3", Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-3", NextSegmentId = null, Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.EnteredSegment("seg-1");

            var model = new InGameWindowModel(segments)
            {
                Route = route
            };

            var viewModel = new InGameNavigationWindowViewModel(model, segments, null);
            var monitoringEvents = new NopMonitoringEvents();
            var window =
                new InGameNavigationWindow(new InMemoryGameStateDispatcher(monitoringEvents), monitoringEvents, new DummyUserPreferences())
                    {
                        ShowActivated = true,
                        DataContext = viewModel
                    };

            window.Show();
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);

            route.EnteredSegment("seg-2");
            viewModel.UpdateGameState(new OnRouteState(1, 2, new TrackPoint(1, 2, 3), segments[1], route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);

            route.EnteredSegment("seg-3");
            viewModel.UpdateGameState(new OnRouteState(1, 2, new TrackPoint(1, 2, 3), segments[2], route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);
            
            var windowContent = (window.Content as Grid);
            //TryScreenshotToClipboardAsync(windowContent).GetAwaiter().GetResult();
            var secondRow = (windowContent.FindControl<Grid>("SecondRow"));

            secondRow
                .IsVisible
                .Should()
                .BeFalse();
        }

        //[Fact]
        public void GivenRouteInLastSegment_PlaceholderIsVisible()
        {
            var segment = new Segment(new List<TrackPoint>
            {
                new TrackPoint(1, 2, 3),
                new TrackPoint(1, 2.1, 5)
            }) { Id = "seg-3"};
            segment.CalculateDistances();
            var segments = new List<Segment>
            {
                new(new List<TrackPoint>()) { Id = "seg-1"},
                new(new List<TrackPoint>()) { Id = "seg-2"},
                segment,
            };
            var route = new PlannedRoute()
            {
                Name = "TestRoute",
                World = new World { Id = "testworld", Name = "TestWorld" },
                ZwiftRouteName = "Mountain route"
            };
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-1", NextSegmentId = "seg-2", Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-2", NextSegmentId = "seg-3", Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-3", NextSegmentId = null, Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.EnteredSegment("seg-1");

            var model = new InGameWindowModel(segments)
            {
                Route = route
            };

            var viewModel = new InGameNavigationWindowViewModel(model, segments, null);
            var monitoringEvents = new NopMonitoringEvents();
            var window =
                new InGameNavigationWindow(new InMemoryGameStateDispatcher(monitoringEvents), monitoringEvents, new DummyUserPreferences())
                    {
                        ShowActivated = true,
                        DataContext = viewModel
                    };
            viewModel.Model.TotalDescent = 123;
            viewModel.Model.TotalAscent = 78;
            viewModel.Model.TotalDistance = 25;
            viewModel.Model.ElapsedDescent = 33;
            viewModel.Model.ElapsedAscent = 12;
            viewModel.Model.ElapsedDistance = 25;

            window.Show();
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);

            route.EnteredSegment("seg-2");
            viewModel.UpdateGameState(new OnRouteState(1, 2, new TrackPoint(1, 2, 3), segments[1], route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);

            route.EnteredSegment("seg-3");
            viewModel.UpdateGameState(new OnRouteState(1, 2, new TrackPoint(1, 2, 3) { DistanceOnSegment = 12}, segments[2], route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);
            
            var windowContent = (window.Content as Grid);
            TryScreenshotToClipboardAsync(windowContent).GetAwaiter().GetResult();
            var secondRow = (windowContent.FindControl<Grid>("Placeholder"));

            secondRow
                .IsVisible
                .Should()
                .BeTrue();
        }

        //[Fact]
        public void GivenRouteHasCompleted_FinishFlagIsVisible()
        {
            var segment = new Segment(new List<TrackPoint>
            {
                new TrackPoint(1, 2, 3),
                new TrackPoint(1, 2.1, 5)
            }) { Id = "seg-3"};
            segment.CalculateDistances();
            var segments = new List<Segment>
            {
                new(new List<TrackPoint>()) { Id = "seg-1"},
                new(new List<TrackPoint>()) { Id = "seg-2"},
                segment,
            };
            var route = new PlannedRoute()
            {
                Name = "TestRoute",
                World = new World { Id = "testworld", Name = "TestWorld" },
                ZwiftRouteName = "Mountain route"
            };
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-1", NextSegmentId = "seg-2", Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-2", NextSegmentId = "seg-3", Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-3", NextSegmentId = null, Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.EnteredSegment("seg-1");

            var model = new InGameWindowModel(segments)
            {
                Route = route
            };

            var viewModel = new InGameNavigationWindowViewModel(model, segments, null);
            var monitoringEvents = new NopMonitoringEvents();
            var window =
                new InGameNavigationWindow(new InMemoryGameStateDispatcher(monitoringEvents), monitoringEvents, new DummyUserPreferences())
                    {
                        ShowActivated = true,
                        DataContext = viewModel
                    };
            viewModel.Model.TotalDescent = 123;
            viewModel.Model.TotalAscent = 78;
            viewModel.Model.TotalDistance = 25;
            viewModel.Model.ElapsedDescent = 33;
            viewModel.Model.ElapsedAscent = 12;
            viewModel.Model.ElapsedDistance = 25;

            window.Show();
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);

            route.EnteredSegment("seg-2");
            viewModel.UpdateGameState(new OnRouteState(1, 2, new TrackPoint(1, 2, 3), segments[1], route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);

            route.EnteredSegment("seg-3");
            viewModel.UpdateGameState(new OnRouteState(1, 2, new TrackPoint(1, 2, 3) { DistanceOnSegment = 12}, segments[2], route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);

            viewModel.UpdateGameState(new CompletedRouteState(1, 2, new TrackPoint(1, 2, 3) { DistanceOnSegment = 12}, route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);
            
            var windowContent = (window.Content as Grid);
            TryScreenshotToClipboardAsync(windowContent).GetAwaiter().GetResult();
            var secondRow = (windowContent.FindControl<StackPanel>("FinishFlag"));

            secondRow
                .IsVisible
                .Should()
                .BeTrue();
        }

        //[Fact]
        public void GivenRouteInLastSegmentButNotYetCompleted_FinishFlagIsNotVisible()
        {
            var segment = new Segment(new List<TrackPoint>
            {
                new TrackPoint(1, 2, 3),
                new TrackPoint(1, 2.1, 5)
            }) { Id = "seg-3"};
            segment.CalculateDistances();
            var segments = new List<Segment>
            {
                new(new List<TrackPoint>()) { Id = "seg-1"},
                new(new List<TrackPoint>()) { Id = "seg-2"},
                segment,
            };
            var route = new PlannedRoute()
            {
                Name = "TestRoute",
                World = new World { Id = "testworld", Name = "TestWorld" },
                ZwiftRouteName = "Mountain route"
            };
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-1", NextSegmentId = "seg-2", Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-2", NextSegmentId = "seg-3", Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.RouteSegmentSequence.Add(new SegmentSequence { SegmentId = "seg-3", NextSegmentId = null, Direction = SegmentDirection.AtoB, TurnToNextSegment = TurnDirection.GoStraight });
            route.EnteredSegment("seg-1");

            var model = new InGameWindowModel(segments)
            {
                Route = route
            };

            var viewModel = new InGameNavigationWindowViewModel(model, segments, null);
            var monitoringEvents = new NopMonitoringEvents();
            var window =
                new InGameNavigationWindow(new InMemoryGameStateDispatcher(monitoringEvents), monitoringEvents, new DummyUserPreferences())
                    {
                        ShowActivated = true,
                        DataContext = viewModel
                    };
            viewModel.Model.TotalDescent = 123;
            viewModel.Model.TotalAscent = 78;
            viewModel.Model.TotalDistance = 25;
            viewModel.Model.ElapsedDescent = 33;
            viewModel.Model.ElapsedAscent = 12;
            viewModel.Model.ElapsedDistance = 25;

            window.Show();
            
            Dispatcher.UIThread.InvokeAsync(async () => { }, DispatcherPriority.SystemIdle);

            route.EnteredSegment("seg-2");
            viewModel.UpdateGameState(new OnRouteState(1, 2, new TrackPoint(1, 2, 3), segments[1], route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);

            route.EnteredSegment("seg-3");
            viewModel.UpdateGameState(new OnRouteState(1, 2, new TrackPoint(1, 2, 3) { DistanceOnSegment = 12}, segments[2], route));
            Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.SystemIdle);
            
            var windowContent = (window.Content as Grid);
            TryScreenshotToClipboardAsync(windowContent).GetAwaiter().GetResult();
            var secondRow = (windowContent.FindControl<StackPanel>("FinishFlag"));

            secondRow
                .IsVisible
                .Should()
                .BeFalse();
        }

        public async Task<bool> TryScreenshotToClipboardAsync(Avalonia.Controls.Control frameworkElement)
        {
            //frameworkElement.ClipToBounds = true; // Can remove if everything still works when the screen is maximised.
            //Rect relativeBounds = frameworkElement.Bounds;
            //double areaWidth = frameworkElement.Width; // Cannot use relativeBounds.Width as this may be incorrect if a window is maximised.
            //double areaHeight = frameworkElement.Height; // Cannot use relativeBounds.Height for same reason.
            //double XLeft = relativeBounds.X;
            //double XRight = XLeft + areaWidth;
            //double YTop = relativeBounds.Y;
            //double YBottom = YTop + areaHeight;
            //new RenderTargetBitmap(new PixelSize((int)Math.Round(XRight, MidpointRounding.AwayFromZero),
            //        (int)Math.Round(YBottom, MidpointRounding.AwayFromZero)),
            //    new Vector(96, 96));

            //// Render framework element to a bitmap. This works better than any screen-pixel-scraping methods which will pick up unwanted
            //// artifacts such as the taskbar or another window covering the current window.
            //using (DrawingContext ctx = dv.RenderOpen())
            //{
            //    var vb = new VisualBrush(frameworkElement);
            //    ctx.DrawRectangle(vb, null, new Rect(new Point(XLeft, YTop), new Point(XRight, YBottom)));
            //}
            //bitmap.Render(dv);
            
            //return await TryCopyBitmapToClipboard(bitmap);

            return false;
        }
    }
}