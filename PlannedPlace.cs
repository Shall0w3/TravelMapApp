using System;

namespace TravelMapApp
{
    public class PlannedPlace
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime PlannedVisitDate { get; set; }
    }
}