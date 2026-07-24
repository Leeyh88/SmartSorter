# SmartSorter C# WPF DashboardSmartSorter 프로젝트의 C# .NET / WPF 대시보드 애플리케이션 사양 문서입니다.  
OpenCV(Emgu CV)를 이용한 실시간 비전 인식, PLC와의 Modbus RTU 통신, MSSQL 데이터베이스 연동 및 MVVM 아키텍처 구조를 설명합니다.

---

## 1. 시스템 아키텍처 개요

본 애플리케이션은 **MVVM (Model-View-ViewModel)** 패턴으로 설계되었으며, 비전 영상 처리, 통신 스레드, 데이터베이스 입출력이 독립된 레이어로 분리되어 동작합니다.

```mermaid
graph TD
    CAM[Camera]
    PLC[LS XGB PLC]
    DB[(MSSQL Database)]

    subgraph WPF_APP ["C# WPF Application"]
        subgraph CORE ["Core Services"]
            VE["VisionEngine<br/>(Color Sorting)"]
            MS["ModbusRtuService<br/>(Status Mon / Control)"]
        end

        MVM["MainViewModel<br/>(MVVM Logic & State)"]
        MW["UI View"]
    end

    %% 연결 관계
    CAM -->|"OpenCV Video Frame"| VE
    PLC <-->|"RS-485 / Modbus RTU"| MS

    VE --> MVM
    MS --> MVM
    MVM <-->|"Data Binding"| MW
    MVM -->|"ADO.NET"| DB
---

## 2. 주요 기능 및 모듈 구성

### 1️⃣ UI & MVVM (View / ViewModel)
- **MainWindow.xaml**: 카메라 실시간 프레임, ROI 설정 박스, PLC 통신 상태, 분류 카운터 및 로그 리스트를 보여주는 메인 대시보드.
- **MainViewModel.cs**: Reactive UI 데이터 바인딩, Command 처리, 주기적 상태 갱신 타이머 관리.

### 2️⃣ 비전 인식 엔진 (`Services/VisionEngine.cs`)
- **OpenCV (Emgu CV / OpenCvSharp)** 기반 프레임 캡처 및 HSV 색상 공간 변환.
- 지정된 **ROI( 관심 영역 )** 내부의 픽셀 분포 분석을 통해 `RED`, `GREEN`, `BLUE`, `YELLOW` 색상 실시간 분류.
- 노이즈 제거(Gaussian Blur, Morphological Operations) 및 임계값(Threshold) 처리.

### 3️⃣ PLC 통신 모듈 (`Services/ModbusRtuService.cs`)
- `System.IO.Ports.SerialPort` 기반 Modbus RTU 마스터 프로토콜 구현.
- **주요 레지스터 제어**:
  - `M0000` (Coil 00001): 원격 가동 시작 명령
  - `P0003` (Discrete Input 10001): 감지 센서 상태 폴링
  - `D0000` (Holding Reg 40001): 분류 색상 코드 전달 ($1=	ext{RED}$, $2=	ext{GREEN}$, $3=	ext{BLUE}$, $4=	ext{YELLOW}$)

### 4️⃣ 데이터베이스 연동 (`Repositories/DatabaseRepository.cs`)
- **MSSQL (`SmartSorterDB`)** 연동.
- 분류 발생 시 `SortingHistory` 테이블에 비동기(`async/await`) 로그 저장.
- 시리얼 포트 번호, ROI 위치 정보 등의 환경설정을 `SystemSettings` 테이블에서 로드 및 저장.

---

## 3. 프로젝트 폴더 구조

```text
src/SmartSorter.Wpf/
├── App.xaml / App.xaml.cs          # Application Entry Point
├── MainWindow.xaml                 # 메인 대시보드 View
├── ViewModels/
│   ├── MainViewModel.cs            # 메인 대시보드 ViewModel
│   └── BaseViewModel.cs            # INotifyPropertyChanged 기본 클래스
├── Models/
│   ├── SortingLog.cs               # 분류 이력 데이터 모델
│   └── SystemConfig.cs             # 시스템 설정 모델
├── Services/
│   ├── VisionEngine.cs             # OpenCV 카메라 & 색상 검출 서비스
│   └── ModbusRtuService.cs         # PLC Serial Modbus RTU 통신 서비스
├── Repositories/
│   └── DatabaseRepository.cs       # MSSQL DB CRUD 처리 레이어
└── Helpers/
    └── RelayedCommand.cs           # WPF ICommand 구현체
```

---