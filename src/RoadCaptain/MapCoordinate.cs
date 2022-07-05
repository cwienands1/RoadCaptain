using System;

namespace RoadCaptain
{
    public readonly struct MapCoordinate : IEquatable<MapCoordinate>
    {
        public double X { get; }
        public double Y { get; }
        public double Altitude { get; }
        public ZwiftWorldId WorldId { get; }

        public MapCoordinate(double x, double y, double altitude, ZwiftWorldId worldId)
        {
            // TODO: Consider whether this is necessary for game coordinates
            // NOTE: It's an optimization for TrackPoint but might not be needed here
            X = Math.Round(x, 5);
            Y = Math.Round(y, 5);
            Altitude = altitude;
            WorldId = worldId;
        }

        public bool Equals(MapCoordinate other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Altitude.Equals(other.Altitude);
        }

        public override bool Equals(object obj)
        {
            return obj is MapCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Altitude);
        }

        public GameCoordinate ToGameCoordinate()
        {
            return new GameCoordinate(X, Y, Altitude, WorldId);
        }

        public TrackPoint ToTrackPoint()
        {
            // NOTE 1:
            // This code has been optimized to be as quick as possible
            // which is why there is a bunch of if-statements instead
            // of switches or pattern matching.
            
            // NOTE 2:
            // Because Zwift game coordinates are flipped you will see
            // that latitude is the negative calculated longitude and
            // longitude the calculated latitude. Ultimately we want to
            // keep as close to real-world lat/lon as possible, hence
            // this conversion here.
            if (WorldId == ZwiftWorldId.Watopia)
            {
                var latitudeAsCentimetersFromOrigin = X + -128809769.40541935;
                var latitude = latitudeAsCentimetersFromOrigin * 9.040388931996475E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + 1824587167.6433601;
                var longitude = longitudeAsCentimetersFromOrigin * 9.15017561017031E-06 * 0.01;

                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.Richmond)
            {
                var latitudeAsCentimetersFromOrigin = X + 416681564.4970093;
                var latitude = latitudeAsCentimetersFromOrigin * 9.0099976736186E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + -684350551.7311095;
                var longitude = longitudeAsCentimetersFromOrigin * 1.1315458228533332E-05 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.London)
            {
                var latitudeAsCentimetersFromOrigin = X + 572999216.4279556;
                var latitude = latitudeAsCentimetersFromOrigin * 8.988093472576876E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + -1165514.8567129374;
                var longitude = longitudeAsCentimetersFromOrigin * 1.4409163767062611E-05 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.NewYork)
            {
                var latitudeAsCentimetersFromOrigin = X + 451904755.49697876;
                var latitude = latitudeAsCentimetersFromOrigin * 9.021199819576003E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + -624888323.3413696;
                var longitude = longitudeAsCentimetersFromOrigin * 1.1838382403428395E-05 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.Innsbruck)
            {
                var latitudeAsCentimetersFromOrigin = X + 525815359.3559265;
                var latitude = latitudeAsCentimetersFromOrigin * 8.990380293086398E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + 85498815.16199112;
                var longitude = longitudeAsCentimetersFromOrigin * 1.3328535060711476E-05 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.Bologna)
            {
                var latitudeAsCentimetersFromOrigin = X + 494915327.2666931;
                var latitude = latitudeAsCentimetersFromOrigin * 8.990380293086398E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + 89998398.77214432;
                var longitude = longitudeAsCentimetersFromOrigin * 1.2603824000201662E-05 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.Yorkshire)
            {
                var latitudeAsCentimetersFromOrigin = X + 600543305.7785034;
                var latitude = latitudeAsCentimetersFromOrigin * 8.990380293086398E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + -10081972.491562366;
                var longitude = longitudeAsCentimetersFromOrigin * 1.529215665285275E-05 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.CritCity)
            {
                var latitudeAsCentimetersFromOrigin = X + -114866743.52011015;
                var latitude = latitudeAsCentimetersFromOrigin * 9.040388931996475E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + 1811999121.6374512;
                var longitude = longitudeAsCentimetersFromOrigin * 9.15017561017031E-06 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.MakuriIslands)
            {
                var latitudeAsCentimetersFromOrigin = X + -118908671.79471876;
                var latitude = latitudeAsCentimetersFromOrigin * 9.040388931996475E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + 1812385336.6892092;
                var longitude = longitudeAsCentimetersFromOrigin * 9.15017561017031E-06 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.France)
            {
                var latitudeAsCentimetersFromOrigin = X + -240220877.27394104;
                var latitude = latitudeAsCentimetersFromOrigin * 9.031302494445749E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + 1719827819.2077637;
                var longitude = longitudeAsCentimetersFromOrigin * 9.663609744784066E-06 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            if (WorldId == ZwiftWorldId.Paris)
            {
                var latitudeAsCentimetersFromOrigin = X + 543554648.5443115;
                var latitude = latitudeAsCentimetersFromOrigin * 8.990380293086398E-06 * 0.01;

                var longitudeAsCentimetersFromOrigin = Y + 16931795.4672575;
                var longitude = longitudeAsCentimetersFromOrigin * 1.366736370221548E-05 * 0.01;
                
                return new TrackPoint(-longitude, latitude, Altitude, WorldId);
            }

            return TrackPoint.Unknown;
        }

        public static MapCoordinate Unknown => new(Double.NaN, Double.NaN, Double.NaN, ZwiftWorldId.Unknown);
    }
}