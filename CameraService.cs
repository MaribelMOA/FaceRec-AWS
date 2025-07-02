using OpenCvSharp;
using System;

namespace FaceApi.Services
{
    public class CameraService : IDisposable
    {
        private VideoCapture _capture;
        private readonly CascadeClassifier _faceCascade;
        private readonly object _lock = new object();
        public bool IsAvailable { get; private set; }

        public CameraService()
        {
            try
            {
                _capture = new VideoCapture(0);
                IsAvailable = _capture.IsOpened();
            }
            catch
            {
                IsAvailable = false;
            }

            _faceCascade = new CascadeClassifier("haarcascade-frontalface-default.xml");
        }

        // public Mat? CaptureFace()
        public CameraCaptureResult CaptureFace()
        {
            lock (_lock)
            {
                if (!IsAvailable)
                {
                    return new CameraCaptureResult
                    {
                        Success = false,
                        Message = "Camera not available."
                    };
                }

                using var frame = new Mat();
                _capture.Read(frame);

                if (frame.Empty())
                {
                    return new CameraCaptureResult
                    {
                        Success = false,
                        Message = "Unable to capture image."
                    };
                }

                var faces = _faceCascade.DetectMultiScale(frame);
                if (faces.Length == 0)
                {
                    return new CameraCaptureResult
                    {
                        Success = false,
                        Message = "No face detected in image."
                    };
                }

                var biggestFace = faces.OrderByDescending(r => r.Width * r.Height).FirstOrDefault();

                if (biggestFace.Width == 0)
                {
                    return new CameraCaptureResult
                    {
                        Success = false,
                        Message = "No face detected in image."
                    };
                }

                var faceMat = new Mat(frame, biggestFace);
                return new CameraCaptureResult
                {
                    Success = true,
                    Message = "Face detected successfully.",
                    FaceImage = faceMat.Clone()
                };
            }
        }

        public void Dispose()
        {
            _capture?.Release();
            _capture?.Dispose();
            _faceCascade?.Dispose();
        }
    }
}
