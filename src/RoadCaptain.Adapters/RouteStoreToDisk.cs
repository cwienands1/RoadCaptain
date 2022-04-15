// Copyright (c) 2022 Sander van Vliet
// Licensed under Artistic License 2.0
// See LICENSE or https://choosealicense.com/licenses/artistic-2.0/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RoadCaptain.Ports;

namespace RoadCaptain.Adapters
{
    internal class RouteStoreToDisk : IRouteStore
    {
        private static readonly JsonSerializerSettings RouteSerializationSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(new CamelCaseNamingStrategy())
            }
        };

        private readonly ISegmentStore _segmentStore;

        private List<Segment> _segments;
        private readonly IWorldStore _worldStore;

        public RouteStoreToDisk(ISegmentStore segmentStore, IWorldStore worldStore)
        {
            _segmentStore = segmentStore;
            _worldStore = worldStore;
        }

        public PlannedRoute LoadFrom(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            var serialized = File.ReadAllText(path);
            var parsed = JObject.Parse(serialized);

            if (parsed.ContainsKey("version"))
            {
                var schemaVersion = parsed["version"]?.Value<string>() ?? "0";

                if (schemaVersion == PersistedRouteVersion1.Version)
                {
                    var deserialized = JsonConvert.DeserializeObject<PersistedRouteVersion1>(
                        serialized,
                        RouteSerializationSettings);

                    deserialized.Route.World = _worldStore.LoadWorldById(deserialized.Route.WorldId);

                    // ReSharper disable once PossibleNullReferenceException
                    return deserialized.Route;
                }
            }

            var deserializeObject = JsonConvert.DeserializeObject<PersistedRouteVersion0>(
                serialized,
                RouteSerializationSettings);
            
            // ReSharper disable once PossibleNullReferenceException
            return deserializeObject.AsRoute();
        }

        public void Store(PlannedRoute route, string path)
        {
            var serialized = path.EndsWith(".gpx", StringComparison.InvariantCultureIgnoreCase)
                ? SerializeAsGpx(route)
                : SerializeAsJson(route);

            File.WriteAllText(path, serialized);
        }

        private static string SerializeAsJson(PlannedRoute route)
        {
            var versionedRoute = new PersistedRouteVersion1
            {
                Route = route
            };

            return JsonConvert.SerializeObject(versionedRoute, Formatting.Indented, RouteSerializationSettings);
        }

        private string SerializeAsGpx(PlannedRoute route)
        {
            var trackSegments = string.Join(
                Environment.NewLine,
                route.RouteSegmentSequence.Select(seg => "<trkseg>" + SegmentAsTrackPointList(seg) + "</trkseg>"));

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(4) ?? "0.0.0.0";
            var routeBuilderVersion = $"RoadCaptain:RouteBuilder {version}";

            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                   $"<gpx creator=\"{routeBuilderVersion}\" version=\"1.1\" xmlns=\"http://www.topografix.com/GPX/1/1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd http://www.garmin.com/xmlschemas/TrackPointExtension/v1 http://www.garmin.com/xmlschemas/TrackPointExtensionv1.xsd http://www.garmin.com/xmlschemas/GpxExtensions/v3 http://www.garmin.com/xmlschemas/GpxExtensionsv3.xsd\" xmlns:gpxtpx=\"http://www.garmin.com/xmlschemas/TrackPointExtension/v1\" xmlns:gpxx=\"http://www.garmin.com/xmlschemas/GpxExtensions/v3\">" +
                   "<trk>" +
                   $"<name>{route.Name}</name>" +
                   $"<desc>RoadCaptain route starting at {route.ZwiftRouteName}</desc>" +
                   "<type>RoadCaptain route</type>" +
                   trackSegments +
                   "</trk>" +
                   "</gpx>";
        }

        private string SegmentAsTrackPointList(SegmentSequence segment)
        {
            var actualSegment = GetSegmentById(segment.SegmentId);

            return string.Join(
                Environment.NewLine,
                actualSegment
                    .Points
                    .Select(x =>
                        $"<trkpt lat=\"{x.Latitude.ToString(CultureInfo.InvariantCulture)}\" lon=\"{x.Longitude.ToString(CultureInfo.InvariantCulture)}\"><ele>{x.Altitude.ToString(CultureInfo.InvariantCulture)}</ele></trkpt>")
                    .ToList());
        }

        private Segment GetSegmentById(string segmentId)
        {
            _segments ??= _segmentStore.LoadSegments(new World { Id = "watopia", Name = "Watopia" });

            return _segments.Single(s => s.Id == segmentId);
        }
    }
}