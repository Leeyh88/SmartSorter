# Vision & Modbus PLC 기반 스마트 컬러 분류 시스템

WPF 대시보드 UI를 통한 비전 모니터링, Modbus RTU PLC 통신 제어, 데이터베이스 이력 관리 및 아두이노 서보모터 분류 제어 통합 스마트 팩토리 프로젝트입니다.

---
### 📌 바로가기 
- [1. 시스템 개요](#1-시스템-개요)
- [2. 기술 스택](#2-기술-스택-tech-stack)
- [3. 주요 기능](#3-주요-기능-key-features)
- [4. 하드웨어 결선 다이어그램](#4-하드웨어-결선-다이어그램)
- [5. 시퀀스 다이어그램](#5-시퀀스-다이어그램)
- [6. 실행 및 구축 가이드](#6-실행-및-구축-가이드)

> 📂 **파트별 세부 매뉴얼 (Quick Links)**
> - [**C# WPF**](./src) (MVVM 구조, 비전 처리, DB 연동)
> - [**LS ELECTRIC PLC**](./plc) (Modbus I/O 맵, XG5000 래더)
> - [**아두이노 서보모터**](./firmware) (포토커플러 회로, 서보 핀 맵)
> - [**DB MSSQL**](./sql) (SQL 스크립트, DB ERD)
---

## 1. 시스템 개요
본 시스템은 컨베이어 벨트를 통해 이송되는 물품의 색상을 **카메라**로 실시간 감지하고, **Modbus RTU 통신**을 통해 **LS ELECTRIC PLC** 및 **Arduino 서보모터**를 제어하여 색상별로 자동 분류하는 종합 스마트 제어 시스템입니다.
## 2. 기술 스택 (Tech Stack) 
### Software & Framework 
- **Language**: C# (.NET 10.0) 
- **UI Framework**: WPF (MVVM Pattern) 
- **Vision**: OpenCV, CvSharp4 
- **Database**: MSSQL 
- **Firmware**: Arduino IDE (C/C++) 
- **PLC**: LS ELECTRIC XGT PLC (XG5000) 
- **Actuator**: SG90 / MG996R Servo Motors 
- **Protocol**: Modbus RTU (RS-485 Serial) 
- **Interface**: 24V-5V Optocoupler Module 
## 3. 주요 기능 (Key Features) 
### 1️⃣ 운전 모드 관리 (Operation Modes) 
- **Auto (자동 모드)**: AI 비전 카메라가 색상(RED, GREEN, BLUE, YELLOW)을 실시간 판별하여 PLC 접점 명령(P21~P24)을 자동으로 전송합니다. 
- **Manual (수동 모드)**: 수동 제어 패널이 활성화되어 각 색상별 분류 동작을 개별 테스트할 수 있습니다. (Auto/Simulation 모드 시 비활성화 차단 처리) 
- **Simulation (시뮬레이션)**: 가상 데이터 타이머를 통해 실물 장비 연결 없이 전체 분류 시나리오를 테스트합니다. 
### 2️⃣ 실시간 모니터링 & 수량 집계 
- RED / GREEN / BLUE / YELLOW 및 TOTAL 실시간 분류 수량 카운팅 
- PLC Modbus 통신 상태 및 USB 비전 카메라 연결 상태 실시간 표시 
### 3️⃣ 데이터베이스 관리 & CSV 내보내기 (Data Analytics) 
- 모든 분류 이벤트(일시, 운전모드, 색상, 서보각도, 상세 메시지) 실시간 DB(MSSQL) 저장 
- 기간별 / 운전모드별 / 색상별 필터링 조건 검색 지원 
- UTF-8 (BOM) 포맷 **CSV 파일 엑셀 내보내기** 지원 (한글 깨짐 방지)
---
## 4. 하드웨어 결선 다이어그램
```mermaid
graph TD
    subgraph POWER ["⚡ External Power (24V SMPS)"]
        GND24["0V (24G)"]
        VCC24["+24V"]
    end

    subgraph PLC ["⚙️ LS ELECTRIC PLC (Relay Output)"]
        COM["PLC Output COM<br/>(Connected to +24V)"]
        P21["P21 (RED Output)"]
        P22["P22 (GREEN Output)"]
        P23["P23 (BLUE Output)"]
        P24["P24 (YELLOW Output)"]
    end

    subgraph OPTO_IN ["🔌 4-Ch Optocoupler Module (Input Side)"]
        IN_COM["Input COM 단자 (-) <--<br/>0V(24G) 연결"]
        IN1["IN 1 (+) <-- +24V Signal"]
        IN2["IN 2 (+) <-- +24V Signal"]
        IN3["IN 3 (+) <-- +24V Signal"]
        IN4["IN 4 (+) <-- +24V Signal"]
    end

    subgraph OPTO_OUT ["🔌 4-Ch Optocoupler Module (Output Side)"]
        OUT1["OUT 1 (5V Signal)"]
        OUT2["OUT 2 (5V Signal)"]
        OUT3["OUT 3 (5V Signal)"]
        OUT4["OUT 4 (5V Signal)"]
    end

    subgraph ARDUINO ["🤖 Arduino Uno (5V Logic)"]
        D2["Digital Pin D2"]
        D3["Digital Pin D3"]
        D4["Digital Pin D4"]
        D5["Digital Pin D5"]

        LOGIC["Servo Logic & Timer"]

        PWM9["PWM D9"]
        PWM10["PWM D10"]
    end

    subgraph ACTUATOR ["🦾 Servos"]
        SV1["Servo 1 (RED/GREEN)"]
        SV2["Servo 2 (BLUE/YELLOW)"]
    end

    %% 전원 연결
    GND24 --> IN_COM
    VCC24 --> COM

    %% PLC -> 포토커플러 IN
    P21 -->|"P21 (+24V)"| IN1
    P22 -->|"P22 (+24V)"| IN2
    P23 -->|"P23 (+24V)"| IN3
    P24 -->|"P24 (+24V)"| IN4

    %% 포토커플러 IN -> OUT (절연)
    IN1 -.-> OUT1
    IN2 -.-> OUT2
    IN3 -.-> OUT3
    IN4 -.-> OUT4

    %% 포토커플러 OUT -> 아두이노
    OUT1 --> D2
    OUT2 --> D3
    OUT3 --> D4
    OUT4 --> D5

    %% 아두이노 내부 로직 및 서보 출력
    D2 & D3 & D4 & D5 --> LOGIC
    LOGIC --> PWM9
    LOGIC --> PWM10
    PWM9 --> SV1
    PWM10 --> SV2
```
---
## 5. 시퀀스 다이어그램
```mermaid
sequenceDiagram
    autonumber
    PLC->>WPF: Modbus 감지 상태 전달
    WPF->>Camera: 프레임 캡처 & HSV 분석
    WPF->>PLC: 색상 분류 코드 쓰기 (D0)
    PLC->>Photocoupler: 분류 신호 출력 (P21~P24)
    Photocoupler->>Arduino: 5V Trigger (D2~D5)
    Arduino->>Servo Motor: 서보 동작 (각도 제어 & 1.5초 유지)
```
---
## 6. 실행 및 구축 가이드 
### 1️⃣ C# WPF 실행 
1. `src/SmartSorter.sln` 솔루션 파일을 Visual Studio 2022 이상에서 열기. 
2. `Services/DatabaseService.cs` 소스코드 내 MSSQL 접속 문자열(`_connectionString`)을 DB 환경(Server IP, DB명, 계정 정보)에 맞게 수정.
3. NuGet 패키지 복원 후 빌드 및 실행.
### 2️⃣ MSSQL 데이터베이스 구축 
1. SQL Server Management Studio (SSMS) 실행 및 MSSQL 서버 접속 
2. `sql/schema.sql` 스크립트 실행하여 `SmartSorter` 데이터베이스 및 테이블, 인덱스 생성.
### 3️⃣ PLC 프로그램 전송 
1. XG5000 프로그램 실행 후 plc/SmartSorter_PLC.xgp 프로젝트를 열기. 
2. PLC 연결 후 래더 프로그램 쓰기(다운로드). 
### 4️⃣ 아두이노 업로드 
1. firmware/SmartSorter_Arduino/SmartSorter_Arduino.ino 파일을 아두이노 IDE에서 열기. 
2. 아두이노 우노(UNO) 보드에 스케치를 업로드.
