// Copyright (c) 2022 Sander van Vliet
// Licensed under Artistic License 2.0
// See LICENSE or https://choosealicense.com/licenses/artistic-2.0/

using System;
using System.Drawing;

namespace RoadCaptain.App.Runner
{
    public class DummyUserPreferences : IUserPreferences
    {
        public string? DefaultSport { get; set; } = "Cycling";
        public string? LastUsedFolder { get; set; }
        public string? Route { get; set; }
        public CapturedWindowLocation? InGameWindowLocation { get; set; }
        public bool EndActivityAtEndOfRoute { get; set; }
        public Version LastOpenedVersion { get; set; } = typeof(DummyUserPreferences).Assembly.GetName().Version;
        public byte[]? ConnectionSecret { get; }
        public CapturedWindowLocation? RouteBuilderLocation { get; set; }

        public void Load()
        {
        }

        public void Save()
        {
        }
    }
}
