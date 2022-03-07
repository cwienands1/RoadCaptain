﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using RoadCaptain.Adapters;
using RoadCaptain.RouteBuilder.Annotations;
using SkiaSharp;

namespace RoadCaptain.RouteBuilder.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private Segment _selectedSegment;

        public MainViewModel()
        {
            Route = new RouteViewModel();
            Route.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Route));

            Segments = new SegmentStore().LoadSegments();
        }

        public List<Segment> Segments { get; }

        public RouteViewModel Route { get; set; }

        public Dictionary<string, SKPath> SegmentPaths { get; } = new();
        public Dictionary<string, SKRect> SegmentPathBounds { get; } = new();

        public Segment SelectedSegment
        {
            get => _selectedSegment;
            private set
            {
                _selectedSegment = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SelectSegment(string segmentId)
        {
            var newSelectedSegment = Segments.Single(s => s.Id == segmentId);

            // 1. Figure out if this is the first segment on the route, if so add it to the route and set the selection to the new segment
            if (!Route.Sequence.Any())
            {
                Route.StartOn(newSelectedSegment);

                SelectedSegment = newSelectedSegment;
            }
            // 2. Figure out if the newly selected segment is reachable from the last segment
            var lastSegment = Segments.Single(s => s.Id == Route.Last.SegmentId);
            var fromA = lastSegment.NextSegmentsNodeA.SingleOrDefault(t => t.SegmentId == newSelectedSegment.Id);
            var fromB = lastSegment.NextSegmentsNodeB.SingleOrDefault(t => t.SegmentId == newSelectedSegment.Id);

            if (fromA != null && fromB == null)
            {
                var newSegmentDirection = SegmentDirection.BtoA;

                if (newSelectedSegment.NextSegmentsNodeA.Any(s => s.SegmentId == lastSegment.Id))
                {
                    newSegmentDirection = SegmentDirection.AtoB;
                }

                Route.NextStep(fromA.Direction, fromA.SegmentId, newSelectedSegment, SegmentDirection.BtoA, newSegmentDirection);

                SelectedSegment = newSelectedSegment;
            }
            else if (fromA == null && fromB != null)
            {
                var newSegmentDirection = SegmentDirection.BtoA;

                if (newSelectedSegment.NextSegmentsNodeA.Any(s => s.SegmentId == lastSegment.Id))
                {
                    newSegmentDirection = SegmentDirection.AtoB;
                }

                Route.NextStep(fromB.Direction, fromB.SegmentId, newSelectedSegment, SegmentDirection.AtoB, newSegmentDirection);

                SelectedSegment = newSelectedSegment;
            }
        }

        public void ClearSelectedSegment()
        {
            SelectedSegment = null;
        }

        public void CreatePathsForSegments(float width)
        {
            SegmentPaths.Clear();
            SegmentPathBounds.Clear();

            var segmentsWithOffsets = Segments
                .Select(seg => new
                {
                    Segment = seg,
                    GameCoordinates = seg.Points.Select(point =>
                        TrackPoint.LatLongToGame(point.Longitude, -point.Latitude, point.Altitude)).ToList()
                })
                .Select(x => new
                {
                    x.Segment,
                    x.GameCoordinates,
                    Offsets = new Offsets(width, x.GameCoordinates)
                })
                .ToList();

            var overallOffsets = Offsets.From(segmentsWithOffsets.Select(s => s.Offsets).ToList());
            
            foreach (var segment in segmentsWithOffsets)
            {
                var skiaPathFromSegment = SkiaPathFromSegment(overallOffsets, segment.GameCoordinates);
                skiaPathFromSegment.GetTightBounds(out var bounds);

                SegmentPaths.Add(segment.Segment.Id, skiaPathFromSegment);
                SegmentPathBounds.Add(segment.Segment.Id, bounds);
            }
        }

        private static SKPath SkiaPathFromSegment(Offsets offsets, List<TrackPoint> data)
        {
            var path = new SKPath();

            path.AddPoly(
                data
                    .Select(point => ScaleAndTranslate(point, offsets))
                    .Select(point => new SKPoint(point.X, point.Y))
                    .ToArray(),
                false);

            return path;
        }

        private static PointF ScaleAndTranslate(TrackPoint point, Offsets offsets)
        {
            var translatedX = offsets.OffsetX + (float)point.Latitude;
            var translatedY = offsets.OffsetY + (float)point.Longitude;

            var scaledX = translatedX * offsets.ScaleFactor;
            var scaledY = translatedY * offsets.ScaleFactor;

            return new PointF(scaledX, scaledY);
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}