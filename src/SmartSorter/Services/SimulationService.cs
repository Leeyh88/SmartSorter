using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using SmartSorter.Models;

namespace SmartSorter.Services
{
    public class SimulationService
    {
        private readonly Random _random = new Random();
        private readonly BoxColor[] _colors = { BoxColor.Red, BoxColor.Green, BoxColor.Blue, BoxColor.Yellow };

        // 가상 센서/카메라에 박스가 감지되었을 때 발생하는 이벤트
        public event Action<BoxColor, BitmapImage>? OnBoxDetected;

        // 가상 박스 생성 (랜덤 색상의 박스 이미지를 캔버스 형태로 그리기)
        public void TriggerVirtualBoxDetection()
        {
            // 1. 랜덤 색상 선택
            BoxColor selectedColor = _colors[_random.Next(_colors.Length)];

            // 2. 가상 비전 프레임 생성 (Bitmap)
            BitmapImage bitmapImage = GenerateVirtualFrame(selectedColor);

            // 3. 이벤트 발생
            OnBoxDetected?.Invoke(selectedColor, bitmapImage);
        }

        // 가상 비전 프레임(이미지) 생성 헬퍼
        private BitmapImage GenerateVirtualFrame(BoxColor color)
        {
            using (var bitmap = new Bitmap(640, 480))
            using (var g = Graphics.FromImage(bitmap))
            {
                // 배경 (검은색 바탕의 컨베이어 벨트 느낌)
                g.Clear(Color.FromArgb(30, 30, 30));

                // 가상 컨베이어 가이드 라인
                using (var pen = new Pen(Color.Gray, 2))
                {
                    g.DrawLine(pen, 0, 120, 640, 120);
                    g.DrawLine(pen, 0, 360, 640, 360);
                }

                // 박스 색상 지정
                Color boxSystemColor = color switch
                {
                    BoxColor.Red => Color.Red,
                    BoxColor.Green => Color.LimeGreen,
                    BoxColor.Blue => Color.DodgerBlue,
                    BoxColor.Yellow => Color.Gold,
                    _ => Color.Gray
                };

                // 화면 중앙에 감지된 물류 박스 그리기
                using (var brush = new SolidBrush(boxSystemColor))
                {
                    g.FillRectangle(brush, 220, 160, 200, 160);
                }

                // 박스 외곽선 및 바운딩 박스(ROI) 표시
                using (var pen = new Pen(Color.Lime, 3))
                {
                    g.DrawRectangle(pen, 210, 150, 220, 180);
                }

                // 비전 인식 결과 텍스트 오버레이
                using (var font = new Font("Arial", 16, FontStyle.Bold))
                using (var limeBrush = new SolidBrush(Color.Lime)) //  이름을 limeBrush로 변경
                using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                {
                    float centerX = bitmap.Width / 2f;
                    g.DrawString($"[SIMULATION] DETECTED: {color.ToString().ToUpper()}", font, limeBrush, centerX, 115, sf);
                }

                // System.Drawing.Bitmap -> WPF BitmapImage 변환
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // UI 쓰레드 안전 공유

                    return bitmapImage;
                }
            }
        }
    }
}