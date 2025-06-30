namespace FaceApi.Models
{
    public class VisitRecord
    {
        public required string FaceId { get; set; }
        public required string ExternalImageId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
