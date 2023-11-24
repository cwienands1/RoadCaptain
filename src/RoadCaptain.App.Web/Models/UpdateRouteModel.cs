// Copyright (c) 2023 Sander van Vliet
// Licensed under Artistic License 2.0
// See LICENSE or https://choosealicense.com/licenses/artistic-2.0/

namespace RoadCaptain.App.Web.Models
{
    public class UpdateRouteModel
    {
        public string? Serialized { get; set; }
        public decimal Distance { get; set; }
        public decimal Descent { get; set; }
        public decimal Ascent { get; set; }
        public bool IsLoop { get; set; }
        public string? ZwiftRouteName { get; set; }
        public string? Name { get; set; }
    }
}
