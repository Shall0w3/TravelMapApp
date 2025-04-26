using System;

namespace TravelMapApp
{
    public class VisitedPlace
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime VisitDate { get; set; }
    }
}