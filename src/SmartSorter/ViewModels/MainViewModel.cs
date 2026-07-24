using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SmartSorter.Models;
using SmartSorter.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;

namespace SmartSorter.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SimulationService _simulationService;
        private readonly DatabaseService _dbService;
        private readonly PlcService _plcService;
        private readonly CameraService _cameraService;
        private readonly DispatcherTimer _simTimer;

        // --- [환경설정 바인딩 프로퍼티] ---
        private string _comPort = "COM9";
        public string ComPort
        {
            get => _comPort;
            set => SetProperty(ref _comPort, value);
        }

        private string _baudRate = "9600";
        public string BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        private string _stationId = "1";
        public string StationId
        {
            get => _stationId;
            set => SetProperty(ref _stationId, value);
        }

        private int _redServoAngle = 45;
        public int RedServoAngle
        {
            get => _redServoAngle;
            set => SetProperty(ref _redServoAngle, value);
        }

        private int _greenServoAngle = 90;
        public int GreenServoAngle
        {
            get => _greenServoAngle;
            set => SetProperty(ref _greenServoAngle, value);
        }

        private int _blueServoAngle = 135;
        public int BlueServoAngle
        {
            get => _blueServoAngle;
            set => SetProperty(ref _blueServoAngle, value);
        }

        private int _yellowServoAngle = 180;
        public int YellowServoAngle
        {
            get => _yellowServoAngle;
            set => SetProperty(ref _yellowServoAngle, value);
        }

        // --- [AI 비전 감시 세부 설정 프로퍼티 (DB 저장 연동)] ---
        private int _detectionIntervalMs = 1000; // 인식 간격 (기본 1000ms = 1초)
        public int DetectionIntervalMs
        {
            get => _detectionIntervalMs;
            set
            {
                if (SetProperty(ref _detectionIntervalMs, value))
                {
                    _cameraService.DetectionIntervalMs = value;
                }
            }
        }

        private int _roiWidth = 120; // 박스 가로 크기
        public int RoiWidth
        {
            get => _roiWidth;
            set
            {
                if (SetProperty(ref _roiWidth, value))
                {
                    _cameraService.RoiWidth = value;
                }
            }
        }

        private int _roiHeight = 120; // 박스 세로 크기
        public int RoiHeight
        {
            get => _roiHeight;
            set
            {
                if (SetProperty(ref _roiHeight, value))
                {
                    _cameraService.RoiHeight = value;
                }
            }
        }

        private int _roiOffsetX = 0; // X축 오프셋 (-왼쪽, +오른쪽)
        public int RoiOffsetX
        {
            get => _roiOffsetX;
            set
            {
                if (SetProperty(ref _roiOffsetX, value))
                {
                    _cameraService.RoiOffsetX = value;
                }
            }
        }

        private int _roiOffsetY = 0; // Y축 오프셋 (-위, +아래)
        public int RoiOffsetY
        {
            get => _roiOffsetY;
            set
            {
                if (SetProperty(ref _roiOffsetY, value))
                {
                    _cameraService.RoiOffsetY = value;
                }
            }
        }

        // --- [상태 프로퍼티 및 수동 모드 연동] ---
        private OperationMode _currentMode = OperationMode.Auto; // 기본값: 자동 모드
        public OperationMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (SetProperty(ref _currentMode, value))
                {
                    // 💡 CurrentMode가 변경될 때 XAML의 IsEnabled="{Binding IsManualMode}" 에 변경 알림 전송
                    OnPropertyChanged(nameof(IsManualMode));
                }
            }
        }

        // 💡 XAML 수동 제어 패널 IsEnabled 바인딩 전용 프로퍼티
        public bool IsManualMode => CurrentMode == OperationMode.Manual;

        private string _statusMessage = "시스템이 준비되었습니다. (자동 모드)";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isPlcConnected = false;
        public bool IsPlcConnected
        {
            get => _isPlcConnected;
            set => SetProperty(ref _isPlcConnected, value);
        }

        private bool _isConveyorRunning = false;
        public bool IsConveyorRunning
        {
            get => _isConveyorRunning;
            set => SetProperty(ref _isConveyorRunning, value);
        }

        private string _cameraStatusText = "미연결";
        public string CameraStatusText
        {
            get => _cameraStatusText;
            set => SetProperty(ref _cameraStatusText, value);
        }

        private string _cameraSource = "0"; // 기본 웹캠 0번 (IP RTSP 입력 가능)
        public string CameraSource
        {
            get => _cameraSource;
            set => SetProperty(ref _cameraSource, value);
        }

        // --- [화면 탭] ---
        private string _currentTab = "Dashboard";
        public string CurrentTab
        {
            get => _currentTab;
            set => SetProperty(ref _currentTab, value);
        }

        // --- [비전 스트림 이미지] ---
        private BitmapSource? _currentVisionImage;
        public BitmapSource? CurrentVisionImage
        {
            get => _currentVisionImage;
            set => SetProperty(ref _currentVisionImage, value);
        }

        // --- [수동 제어 서보 각도] ---
        private int _servoAngle1 = 90;
        public int ServoAngle1
        {
            get => _servoAngle1;
            set => SetProperty(ref _servoAngle1, value);
        }

        private int _servoAngle2 = 90;
        public int ServoAngle2
        {
            get => _servoAngle2;
            set => SetProperty(ref _servoAngle2, value);
        }

        // --- [실시간 수량 통계] ---
        private int _redCount = 0;
        public int RedCount
        {
            get => _redCount;
            set { if (SetProperty(ref _redCount, value)) OnPropertyChanged(nameof(TotalCount)); }
        }

        private int _greenCount = 0;
        public int GreenCount
        {
            get => _greenCount;
            set { if (SetProperty(ref _greenCount, value)) OnPropertyChanged(nameof(TotalCount)); }
        }

        private int _blueCount = 0;
        public int BlueCount
        {
            get => _blueCount;
            set { if (SetProperty(ref _blueCount, value)) OnPropertyChanged(nameof(TotalCount)); }
        }

        private int _yellowCount = 0;
        public int YellowCount
        {
            get => _yellowCount;
            set { if (SetProperty(ref _yellowCount, value)) OnPropertyChanged(nameof(TotalCount)); }
        }

        public int TotalCount => RedCount + GreenCount + BlueCount + YellowCount;

        // --- [이력 조회 필터링] ---
        private DateTime _searchStartDate = DateTime.Now.AddDays(-7);
        public DateTime SearchStartDate
        {
            get => _searchStartDate;
            set => SetProperty(ref _searchStartDate, value);
        }

        private DateTime _searchEndDate = DateTime.Now;
        public DateTime SearchEndDate
        {
            get => _searchEndDate;
            set => SetProperty(ref _searchEndDate, value);
        }

        private string _selectedColorFilter = "전체";
        public string SelectedColorFilter
        {
            get => _selectedColorFilter;
            set => SetProperty(ref _selectedColorFilter, value);
        }

        private string _selectedModeFilter = "전체";
        public string SelectedModeFilter
        {
            get => _selectedModeFilter;
            set => SetProperty(ref _selectedModeFilter, value);
        }


        // --- [이력 통계] ---
        private int _historyTotalCount = 0;
        public int HistoryTotalCount
        {
            get => _historyTotalCount;
            set => SetProperty(ref _historyTotalCount, value);
        }

        private int _historyRedCount = 0;
        public int HistoryRedCount
        {
            get => _historyRedCount;
            set => SetProperty(ref _historyRedCount, value);
        }

        private int _historyGreenCount = 0;
        public int HistoryGreenCount
        {
            get => _historyGreenCount;
            set => SetProperty(ref _historyGreenCount, value);
        }

        private int _historyBlueCount = 0;
        public int HistoryBlueCount
        {
            get => _historyBlueCount;
            set => SetProperty(ref _historyBlueCount, value);
        }

        private int _historyYellowCount = 0;
        public int HistoryYellowCount
        {
            get => _historyYellowCount;
            set => SetProperty(ref _historyYellowCount, value);
        }

        // --- [컬렉션] ---
        public ObservableCollection<SortingLog> Logs { get; } = new ObservableCollection<SortingLog>();

        private ObservableCollection<SortingLog> _dbLogs = new ObservableCollection<SortingLog>();
        public ObservableCollection<SortingLog> DbLogs
        {
            get => _dbLogs;
            set => SetProperty(ref _dbLogs, value);
        }

        // --- [생성자] ---
        public MainViewModel()
        {
            _simulationService = new SimulationService();
            _dbService = new DatabaseService();
            _plcService = new PlcService();
            _cameraService = new CameraService();

            // 1. PLC 이벤트 구독
            _plcService.OnStatusChanged += (success, message) =>
            {
                App.Current?.Dispatcher.Invoke(() =>
                {
                    IsPlcConnected = success;
                    StatusMessage = message;
                    AddLog(message, CurrentMode, success);
                });
            };

            _plcService.OnDataRead += (coils, registers) =>
            {
                App.Current?.Dispatcher.Invoke(() =>
                {
                    if (registers != null && registers.Length >= 4)
                    {
                        RedCount = registers[0];     // D0
                        GreenCount = registers[1];   // D1
                        BlueCount = registers[2];    // D2
                        YellowCount = registers[3];  // D3
                    }
                });
            };

            // 2. 비전 실시간 카메라 프레임 수신
            _cameraService.OnFrameReceived += (bitmap) =>
            {
                App.Current?.Dispatcher.Invoke(() =>
                {
                    CurrentVisionImage = bitmap;
                });
            };

            // 3. AI 비전 실시간 색상 감지 처리
            _cameraService.OnColorDetected += async (colorName) =>
            {
                App.Current?.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"[AI 비전] 색상 실시간 감지됨: {colorName}";
                });

                if (CurrentMode == OperationMode.Auto)
                {
                    BoxColor detectedColorEnum = BoxColor.None;
                    int targetCoil = -1;

                    switch (colorName.ToUpper())
                    {
                        case "RED":
                            RedCount++;
                            detectedColorEnum = BoxColor.Red;
                            targetCoil = 16; // PLC 접점 M10 (Modbus Coil 16)
                            break;
                        case "GREEN":
                            GreenCount++;
                            detectedColorEnum = BoxColor.Green;
                            targetCoil = 17; // PLC 접점 M11 (Modbus Coil 17)
                            break;
                        case "BLUE":
                            BlueCount++;
                            detectedColorEnum = BoxColor.Blue;
                            targetCoil = 18; // PLC 접점 M12 (Modbus Coil 18)
                            break;
                        case "YELLOW":
                            YellowCount++;
                            detectedColorEnum = BoxColor.Yellow;
                            targetCoil = 19; // PLC 접점 M13 (Modbus Coil 19)
                            break;
                    }

                    if (IsPlcConnected && targetCoil != -1)
                    {
                        await _plcService.WriteCoilAsync(targetCoil, true);
                        await Task.Delay(200);
                        await _plcService.WriteCoilAsync(targetCoil, false);
                    }

                    string logMsg = $"[AI 비전 분류] {colorName} 감지 ➔ PLC 접점 M{targetCoil} 출력";
                    AddLog(logMsg, CurrentMode, true, detectedColorEnum);

                    var logItem = new SortingLog
                    {
                        Timestamp = DateTime.Now,
                        Mode = OperationMode.Auto,
                        DetectedColor = detectedColorEnum,
                        Message = logMsg
                    };
                    Task.Run(() => _dbService.SaveSortingLog(logItem));
                }
            };

            _simulationService.OnBoxDetected += OnBoxDetectedInSimulation;

            // 4. 시뮬레이션 타이머
            _simTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _simTimer.Tick += (s, e) =>
            {
                if (CurrentMode == OperationMode.Simulation)
                {
                    _simulationService.TriggerVirtualBoxDetection();
                }
            };

            AddLog("애플리케이션이 시작되었습니다. (자동 모드)", OperationMode.Auto, true);

            // DB에서 저장된 설정을 로드합니다.
            LoadSettingsFromDb();
        }

        // --- [시뮬레이션 이벤트] ---
        private void OnBoxDetectedInSimulation(BoxColor color, BitmapImage imageFrame)
        {
            CurrentVisionImage = imageFrame;

            int targetAngle = 0;
            switch (color)
            {
                case BoxColor.Red:
                    RedCount++;
                    targetAngle = RedServoAngle;
                    ServoAngle1 = targetAngle;
                    break;
                case BoxColor.Green:
                    GreenCount++;
                    targetAngle = GreenServoAngle;
                    ServoAngle1 = targetAngle;
                    break;
                case BoxColor.Blue:
                    BlueCount++;
                    targetAngle = BlueServoAngle;
                    ServoAngle2 = targetAngle;
                    break;
                case BoxColor.Yellow:
                    YellowCount++;
                    targetAngle = YellowServoAngle;
                    ServoAngle2 = targetAngle;
                    break;
            }

            StatusMessage = $"[시뮬레이션] {color} 박스 감지 ➔ 서보 각도 {targetAngle}° 제어";
            AddLog(StatusMessage, OperationMode.Simulation, true, color);

            var logItem = new SortingLog
            {
                Timestamp = DateTime.Now,
                Mode = OperationMode.Simulation,
                DetectedColor = color,
                TargetServoAngle = targetAngle,
                Message = StatusMessage
            };

            Task.Run(() => _dbService.SaveSortingLog(logItem));
        }

        // --- [커맨드 구현] ---

        // 📷 [카메라 시작 / 종료 커맨드 (비동기)]
        [RelayCommand]
        private async Task ToggleCameraAsync()
        {
            try
            {
                if (_cameraService.IsRunning)
                {
                    _cameraService.StopCamera();
                    CameraStatusText = "미연결";
                    CurrentVisionImage = null;
                    AddLog("카메라 연결 해제됨", CurrentMode, false);
                }
                else
                {
                    CameraStatusText = "연결 시도 중...";
                    await Task.Run(() => _cameraService.StartCamera(CameraSource));

                    CameraStatusText = $"연결됨 ({CameraSource})";
                    AddLog($"카메라 연결 성공 (Source: {CameraSource})", CurrentMode, true);
                }
            }
            catch (Exception ex)
            {
                CameraStatusText = "연결 실패";
                StatusMessage = $"카메라 오류: {ex.Message}";
                AddLog(StatusMessage, CurrentMode, false);
            }
        }

        // 🟢 [컨베이어 ON/OFF 제어 커맨드]
        [RelayCommand]
        private async Task StartConveyorAsync()
        {
            if (!IsPlcConnected)
            {
                StatusMessage = "PLC가 연결되어 있지 않습니다. 먼저 PLC를 연결해 주세요.";
                AddLog(StatusMessage, CurrentMode, false);
                return;
            }

            try
            {
                IsConveyorRunning = !IsConveyorRunning;
                await _plcService.WriteCoilAsync(0, IsConveyorRunning);

                string stateText = IsConveyorRunning ? "가동 (ON)" : "정지 (OFF)";
                StatusMessage = $"[PLC 제어] 컨베이어 {stateText} 명령 전송 (M0: {IsConveyorRunning})";
                AddLog(StatusMessage, CurrentMode, true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"[PLC 제어 실패] 컨베이어 제어 중 오류: {ex.Message}";
                AddLog(StatusMessage, CurrentMode, false);
            }
        }

        // 🤖 [서보 모터 수동 동작 (M10, M11 비트 펄스 전송)]
        [RelayCommand]
        private async Task SendServoAngleAsync(string servoNo)
        {
            int angle = servoNo == "1" ? ServoAngle1 : ServoAngle2;

            if (IsPlcConnected)
            {
                int coilAddress = servoNo == "1" ? 10 : 11;
                await _plcService.WriteCoilAsync(coilAddress, true);
                await Task.Delay(200);
                await _plcService.WriteCoilAsync(coilAddress, false);
            }

            StatusMessage = $"[수동 제어] 서보 {servoNo}번 동작 명령 전송 (각도: {angle}°)";
            AddLog(StatusMessage, CurrentMode, true);
        }

        // 모드 변경
        [RelayCommand]
        private void ChangeMode(string modeStr)
        {
            if (Enum.TryParse(modeStr, out OperationMode newMode))
            {
                CurrentMode = newMode;

                if (CurrentMode == OperationMode.Simulation)
                    _simTimer.Start();
                else
                    _simTimer.Stop();

                StatusMessage = $"작동 모드가 [{CurrentMode}] (으)로 변경되었습니다.";
                AddLog(StatusMessage, CurrentMode, true);
            }
        }

        // 탭 이동
        [RelayCommand]
        private void Navigate(string tabName)
        {
            CurrentTab = tabName;
            if (tabName == "History") RefreshDbLogs();
            else if (tabName == "Settings") LoadSettingsFromDb();
        }

        // DB 이력 새로고침
        [RelayCommand]
        private async Task RefreshDbLogsAsync()
        {
            var logs = await Task.Run(() => _dbService.GetSortingLogs());

            App.Current?.Dispatcher.Invoke(() =>
            {
                DbLogs.Clear();
                foreach (var item in logs)
                {
                    DbLogs.Add(item);
                }
            });
        }

        private void RefreshDbLogs() => _ = RefreshDbLogsAsync();

        // 실시간 UI 로그 추가 (최대 100개 유지)
        public void AddLog(string msg, OperationMode mode, bool success, BoxColor color = BoxColor.None)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, new SortingLog
                {
                    Timestamp = DateTime.Now,
                    Message = msg,
                    Mode = mode,
                    IsSuccess = success,
                    DetectedColor = color,
                    TargetServoAngle = (color == BoxColor.Red) ? RedServoAngle : (color == BoxColor.Green) ? GreenServoAngle : (color == BoxColor.Blue) ? BlueServoAngle : YellowServoAngle
                });

                while (Logs.Count > 100)
                {
                    Logs.RemoveAt(Logs.Count - 1);
                }
            });
        }

        // 📥 [DB에서 시스템 및 카메라 비전 설정값 가져오기]
        public void LoadSettingsFromDb()
        {
            Task.Run(() =>
            {
                var settings = _dbService.GetSettings();

                App.Current?.Dispatcher.Invoke(() =>
                {
                    if (settings != null)
                    {
                        if (settings.TryGetValue("ComPort", out var port)) ComPort = port;
                        if (settings.TryGetValue("BaudRate", out var baud)) BaudRate = baud;
                        if (settings.TryGetValue("StationId", out var station)) StationId = station;

                        if (settings.TryGetValue("RedServoAngle", out var red) && int.TryParse(red, out var r)) RedServoAngle = r;
                        if (settings.TryGetValue("GreenServoAngle", out var green) && int.TryParse(green, out var g)) GreenServoAngle = g;
                        if (settings.TryGetValue("BlueServoAngle", out var blue) && int.TryParse(blue, out var b)) BlueServoAngle = b;
                        if (settings.TryGetValue("YellowServoAngle", out var yellow) && int.TryParse(yellow, out var y)) YellowServoAngle = y;

                        if (settings.TryGetValue("CameraSource", out var camSource)) CameraSource = camSource;
                        if (settings.TryGetValue("DetectionIntervalMs", out var interval) && int.TryParse(interval, out var ms)) DetectionIntervalMs = ms;
                        if (settings.TryGetValue("RoiWidth", out var rw) && int.TryParse(rw, out var w)) RoiWidth = w;
                        if (settings.TryGetValue("RoiHeight", out var rh) && int.TryParse(rh, out var h)) RoiHeight = h;
                        if (settings.TryGetValue("RoiOffsetX", out var rx) && int.TryParse(rx, out var ox)) RoiOffsetX = ox;
                        if (settings.TryGetValue("RoiOffsetY", out var ry) && int.TryParse(ry, out var oy)) RoiOffsetY = oy;
                    }
                });
            });
        }

        // 💾 [시스템 및 카메라 비전 설정값 DB 저장]
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            await Task.Run(() =>
            {
                _dbService.SaveSetting("ComPort", ComPort);
                _dbService.SaveSetting("BaudRate", BaudRate);
                _dbService.SaveSetting("StationId", StationId);

                _dbService.SaveSetting("RedServoAngle", RedServoAngle.ToString());
                _dbService.SaveSetting("GreenServoAngle", GreenServoAngle.ToString());
                _dbService.SaveSetting("BlueServoAngle", BlueServoAngle.ToString());
                _dbService.SaveSetting("YellowServoAngle", YellowServoAngle.ToString());

                _dbService.SaveSetting("CameraSource", CameraSource);
                _dbService.SaveSetting("DetectionIntervalMs", DetectionIntervalMs.ToString());
                _dbService.SaveSetting("RoiWidth", RoiWidth.ToString());
                _dbService.SaveSetting("RoiHeight", RoiHeight.ToString());
                _dbService.SaveSetting("RoiOffsetX", RoiOffsetX.ToString());
                _dbService.SaveSetting("RoiOffsetY", RoiOffsetY.ToString());
            });

            StatusMessage = "시스템 및 AI 비전 설정이 성공적으로 DB에 저장되었습니다.";
            AddLog(StatusMessage, CurrentMode, true);
        }

        // 조건 검색 커맨드
        [RelayCommand]
        private async Task SearchDbLogsAsync()
        {
            string colorFilter = ParseFilterString(SelectedColorFilter);
            string modeFilter = ParseFilterString(SelectedModeFilter);

            var logs = await Task.Run(() => _dbService.GetFilteredSortingLogs(SearchStartDate, SearchEndDate, colorFilter, modeFilter));

            App.Current?.Dispatcher.Invoke(() =>
            {
                DbLogs.Clear();
                int red = 0, green = 0, blue = 0, yellow = 0;

                foreach (var item in logs)
                {
                    DbLogs.Add(item);
                    switch (item.DetectedColor)
                    {
                        case BoxColor.Red: red++; break;
                        case BoxColor.Green: green++; break;
                        case BoxColor.Blue: blue++; break;
                        case BoxColor.Yellow: yellow++; break;
                    }
                }

                HistoryTotalCount = logs.Count;
                HistoryRedCount = red;
                HistoryGreenCount = green;
                HistoryBlueCount = blue;
                HistoryYellowCount = yellow;
            });
        }

        // 📁 CSV 내보내기 커맨드
        [RelayCommand]
        private async Task ExportToCsvAsync()
        {
            try
            {
                string colorFilter = ParseFilterString(SelectedColorFilter);
                string modeFilter = ParseFilterString(SelectedModeFilter);

                var logs = await Task.Run(() => _dbService.GetFilteredSortingLogs(SearchStartDate, SearchEndDate, colorFilter, modeFilter));

                if (logs == null || logs.Count == 0)
                {
                    MessageBox.Show("선택한 조건(기간/운전모드/색상)에 해당하는 이력 데이터가 없습니다.",
                                    "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV 파일 (*.csv)|*.csv",
                    FileName = $"SmartSorter_Log_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "분류 이력 CSV 내보내기"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("일시,운전모드,판별 색상,서보 제어 각도,이벤트 상세 내용");

                    foreach (var log in logs)
                    {
                        string time = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                        string mode = log.Mode.ToString();
                        string color = log.DetectedColor.ToString();
                        string angle = log.TargetServoAngle.ToString();
                        string msg = $"\"{log.Message.Replace("\"", "\"\"")}\"";

                        sb.AppendLine($"{time},{mode},{color},{angle},{msg}");
                    }

                    await File.WriteAllTextAsync(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);

                    MessageBox.Show($"총 {logs.Count}건의 데이터가 CSV 파일로 성공적으로 저장되었습니다.\n\n경로: {saveFileDialog.FileName}",
                                    "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSV 내보내기 중 오류가 발생했습니다:\n{ex.Message}",
                                "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ParseFilterString(object? filterObj)
        {
            if (filterObj == null) return "전체";

            string text = filterObj.ToString() ?? "전체";
            if (text.Contains(":"))
            {
                text = text.Split(':')[1].Trim();
            }
            return text;
        }

        // PLC 통신 연결/해제 커맨드
        [RelayCommand]
        private async Task ConnectPlcAsync()
        {
            if (IsPlcConnected)
            {
                _plcService.Disconnect();
                IsPlcConnected = false;
                StatusMessage = "PLC 시리얼 통신 연결이 해제되었습니다.";
                AddLog(StatusMessage, CurrentMode, true);
                return;
            }

            string port = ComPort;
            int.TryParse(BaudRate, out int baud);
            byte.TryParse(StationId, out byte station);

            if (baud == 0) baud = 9600;
            if (station == 0) station = 1;

            StatusMessage = $"PLC 시리얼 연결 시도 중... ({port}, {baud}bps, 국번:{station})";

            bool isSuccess = await _plcService.ConnectSerialAsync(port, baud, station);
            IsPlcConnected = isSuccess;
        }

        // 🖐️ [수동 제어 테스트 커맨드 (Manual 모드 전용)]
        [RelayCommand]
        private async Task ManualTestAsync(string colorName)
        {
            // 1. 수동 모드 체크 (CurrentMode 프로퍼티 이용)
            if (CurrentMode != OperationMode.Manual)
            {
                MessageBox.Show("수동 제어는 '수동 (Manual)' 모드일 때만 실행할 수 있습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Enum.TryParse<BoxColor>(colorName, true, out var selectedColor))
                return;

            int servoAngle = 0;
            int plcAddress = -1;

            switch (selectedColor)
            {
                case BoxColor.Red:
                    servoAngle = RedServoAngle;
                    plcAddress = 16; // M10 (Modbus Coil 16)
                    break;
                case BoxColor.Green:
                    servoAngle = GreenServoAngle;
                    plcAddress = 17; // M11 (Modbus Coil 17)
                    break;
                case BoxColor.Blue:
                    servoAngle = BlueServoAngle;
                    plcAddress = 18; // M12 (Modbus Coil 18)
                    break;
                case BoxColor.Yellow:
                    servoAngle = YellowServoAngle;
                    plcAddress = 19; // M13 (Modbus Coil 19)
                    break;
            }

            try
            {
                // 2. PLC 신호 전송 (WriteCoilAsync 이용 펄스 제어)
                if (IsPlcConnected && plcAddress != -1)
                {
                    await _plcService.WriteCoilAsync(plcAddress, true);
                    await Task.Delay(200); // 펄스 유지
                    await _plcService.WriteCoilAsync(plcAddress, false);
                }

                // 3. 실시간 수량 증가 및 로그 메시지 생성
                string logMsg = $"[수동 제어] {colorName.ToUpper()} 동작 강제 출력 (접점: M{plcAddress - 6}, 각도: {servoAngle}°)";

                // 4. UI 실시간 업데이트 및 하단 로그 반영
                App.Current?.Dispatcher.Invoke(() =>
                {
                    switch (selectedColor)
                    {
                        case BoxColor.Red: RedCount++; break;
                        case BoxColor.Green: GreenCount++; break;
                        case BoxColor.Blue: BlueCount++; break;
                        case BoxColor.Yellow: YellowCount++; break;
                    }
                    AddLog(logMsg, CurrentMode, true, selectedColor);
                });

                // 5. DB 저장
                var logItem = new SortingLog
                {
                    Timestamp = DateTime.Now,
                    Mode = OperationMode.Manual,
                    DetectedColor = selectedColor,
                    TargetServoAngle = servoAngle,
                    Message = logMsg
                };
                await Task.Run(() => _dbService.SaveSortingLog(logItem));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"수동 제어 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}