namespace FaceApi.Services
{
    public class CameraCaptureResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public OpenCvSharp.Mat? FaceImage { get; set; }
    }
}
