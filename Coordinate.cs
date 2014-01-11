using System;
namespace MyTSP
{
    public class Coordinate
    {
        public int Name { get; set; }

        // X-coordinate (from TSPLIB)
        public double X { get; set; }

        // Y-coordinate (from TSPLIB)
        public double Y { get; set; }

        public Coordinate(int name, double x, double y)
        {
            Name = name;
            X = x;
            Y = y;
        }

        // Latitude in radians.
        public double Latitude
        {
            get { return Math.PI * (Math.Truncate(X) + 5 * (X - Math.Truncate(X)) / 3) / 180; }
        }

        // Longitude in radians.
        public double Longitude
        {
            get { return Math.PI * (Math.Truncate(Y) + 5 * (Y - Math.Truncate(Y)) / 3) / 180; }
        }

        // Geographic distance between two points (as an integer).
        public int Distance(Coordinate p)
        {
            double q1 = Math.Cos(Longitude - p.Longitude);
            double q2 = Math.Cos(Latitude - p.Latitude);
            double q3 = Math.Cos(Latitude + p.Latitude);
            // There may rounding difficulties her if the points are close together...just sayin'.
            return (int)(6378.388 * Math.Acos(0.5 * ((1 + q1) * q2 - (1 - q1) * q3)) + 1);
        }
    }
}