using System.ComponentModel.DataAnnotations.Schema;

namespace TravelMapApp
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public int? VisitedPlaceId { get; set; }
        [ForeignKey("VisitedPlaceId")]
        public VisitedPlace? VisitedPlace { get; set; }
        public int? PlannedPlaceId { get; set; }
        [ForeignKey("PlannedPlaceId")]
        public PlannedPlace? PlannedPlace { get; set; }
    }
}