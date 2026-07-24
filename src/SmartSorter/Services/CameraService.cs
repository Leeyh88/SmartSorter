using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace SmartSorter.Services
{
    public class CameraService
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cts;
        private bool _isRunning = false;

        // ⏱️ 중복 인식 방지용 마지막 감지 시간
        private DateTime _lastDetectedTime = DateTime.MinValue;

        // --- [MainViewModel에서 바인딩 및 조작할 비전 세부 설정 프로퍼티] ---
        public int DetectionIntervalMs { get; set; } = 1000; // 인식 간격 (기본 1000ms = 1초)
        public int RoiWidth { get; set; } = 120;             // 박스 너비
        public int RoiHeight { get; set; } = 120;            // 박스 높이
        public int RoiOffsetX { get; set; } = 0;             // X축 위치 이동 오프셋 (-왼쪽, +오른쪽)
        public int RoiOffsetY { get; set; } = 0;             // Y축 위치 이동 오프셋 (-위, +아래)

        // 프레임 갱신 이벤트 (UI Image 컨트롤 바인딩용)
        public event Action<BitmapSource>? OnFrameReceived;

        // 색상 감지 이벤트 (감지된 색상명: "RED", "GREEN", "BLUE", "YELLOW")
        public event Action<string>? OnColorDetected;

        public bool IsRunning => _isRunning;

        /// <summary>
        /// 문자열(Index 번호 "0", "1" 또는 IP카메라 RTSP 주소)을 받아서 카메라 시작
        /// </summary>
        public void StartCamera(string cameraSource, int width = 640, int height = 480)
        {
            if (int.TryParse(cameraSource, out int index))
            {
                StartCamera(index, width, height);
            }
            else
            {
                StartCameraRtsp(cameraSource, width, height);
            }
        }

        /// <summary>
        /// 정수형 카메라 인덱스로 시작 (웹캠 / USB 비전)
        /// </summary>
        public void StartCamera(int cameraIndex = 0, int width = 640, int height = 480)
        {
            if (_isRunning) StopCamera(); // 기존에 켜져있던 캠이 있다면 종료

            _capture = new VideoCapture(cameraIndex);

            if (!_capture.IsOpened())
            {
                throw new Exception($"카메라(Index: {cameraIndex})를 열 수 없습니다. 장치 연결을 확인해 주세요.");
            }

            _capture.Set(VideoCaptureProperties.FrameWidth, width);
            _capture.Set(VideoCaptureProperties.FrameHeight, height);
            _capture.Set(VideoCaptureProperties.Fps, 30);

            _isRunning = true;
            _cts = new CancellationTokenSource();

            Task.Run(() => CaptureLoop(_cts.Token));
        }

        /// <summary>
        /// IP 카메라 (RTSP / HTTP 스트림 URL) 전용 시작 메서드
        /// </summary>
        private void StartCameraRtsp(string rtspUrl, int width = 640, int height = 480)
        {
            if (_isRunning) StopCamera();

            _capture = new VideoCapture(rtspUrl);

            if (!_capture.IsOpened())
            {
                throw new Exception($"IP 카메라 스트림({rtspUrl})에 연결할 수 없습니다.");
            }

            _capture.Set(VideoCaptureProperties.FrameWidth, width);
            _capture.Set(VideoCaptureProperties.FrameHeight, height);

            _isRunning = true;
            _cts = new CancellationTokenSource();

            Task.Run(() => CaptureLoop(_cts.Token));
        }

        private void CaptureLoop(CancellationToken token)
        {
            using (Mat frame = new Mat())
            using (Mat hsvFrame = new Mat())
            {
                while (!token.IsCancellationRequested && _isRunning && _capture != null)
                {
                    if (!_capture.Read(frame) || frame.Empty())
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    // 1. BGR -> HSV 색상 공간 변환
                    Cv2.CvtColor(frame, hsvFrame, ColorConversionCodes.BGR2HSV);

                    // 2. ROI 영역 색상 판별
                    string? detectedColor = DetectColor(hsvFrame, frame);

                    // 🛑 [3. 인식 간격(DetectionIntervalMs) 쿨타임 제어]
                    if (!string.IsNullOrEmpty(detectedColor))
                    {
                        if ((DateTime.Now - _lastDetectedTime).TotalMilliseconds >= DetectionIntervalMs)
                        {
                            _lastDetectedTime = DateTime.Now; // 감지 시각 갱신
                            OnColorDetected?.Invoke(detectedColor);
                        }
                    }

                    // 4. Mat -> WPF BitmapSource 변환 후 UI 이벤트 발생
                    BitmapSource bitmapSource = frame.ToWriteableBitmap();
                    bitmapSource.Freeze(); // UI 스레드 접근을 위해 Freeze

                    OnFrameReceived?.Invoke(bitmapSource);

                    Thread.Sleep(33); // 약 30 FPS
                }
            }
        }

        // HSV 영역 기반 색상 판별 로직 (ROI 박스 가변 및 쿨타임 로직 적용)
        private string? DetectColor(Mat hsv, Mat originalFrame)
        {
            // 📐 ROI 위치 및 크기 계산 (오프셋 및 화면 이탈 방지 처리)
            int cx = (hsv.Width / 2) + RoiOffsetX;
            int cy = (hsv.Height / 2) + RoiOffsetY;

            int x = Math.Max(0, Math.Min(cx - (RoiWidth / 2), hsv.Width - RoiWidth));
            int y = Math.Max(0, Math.Min(cy - (RoiHeight / 2), hsv.Height - RoiHeight));

            Rect roi = new Rect(x, y, RoiWidth, RoiHeight);

            // 🎨 쿨타임 상태에 따라 화면 박스 테두리 색상 변경 (쿨타임 중: 주황색, 감지 대기 중: 초록색)
            bool isCoolingDown = (DateTime.Now - _lastDetectedTime).TotalMilliseconds < DetectionIntervalMs;
            Scalar boxColor = isCoolingDown ? Scalar.FromRgb(255, 165, 0) : Scalar.FromRgb(0, 255, 0);

            // 화면에 ROI 감지 가이드 박스 그리기
            Cv2.Rectangle(originalFrame, roi, boxColor, 2);

            using (Mat hsvRoi = new Mat(hsv, roi))
            {
                Scalar meanHsv = Cv2.Mean(hsvRoi);
                double hue = meanHsv[0];        // 色 (Hue: 0~180)
                double saturation = meanHsv[1]; // 彩 (Saturation: 0~255)
                double value = meanHsv[2];      // 明 (Value: 0~255)

                // 채도/명도가 기준치보다 낮으면 무채색/어두움 처리하여 패스
                if (saturation < 35 || value < 35) return null;

                // HSV 색상 범위 판별
                if ((hue >= 0 && hue <= 12) || (hue >= 165 && hue <= 180)) return "RED";
                if (hue >= 35 && hue <= 85) return "GREEN";
                if (hue >= 95 && hue <= 135) return "BLUE";
                if (hue >= 18 && hue <= 34) return "YELLOW";
            }

            return null;
        }

        public void StopCamera()
        {
            _isRunning = false;
            _cts?.Cancel();
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
        }
    }
}