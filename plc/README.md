# SmartSorter PLC Subsystem & Hardware Spec

SmartSorter 시스템의 PLC(XGB) 제어 프로그램, 입출력(I/O) 맵, 시리얼 통신(Modbus RTU) 사양 및 하드웨어 배선 가이드입니다.

---

## 1. 시스템 제어 개요

C# WPF 대시보드 및 비전 카메라 시스템과 RS-485(Modbus RTU) 통신을 수행하며, 컨베이어 제어, 포토센서 물체 감지, 포토커플러 신호 중계 및 오류 알람을 처리합니다.

```text
[ C# WPF Dashboard ]
        │ (Modbus RTU / RS-485)
        ▼
[ LS XGB PLC (XBC-DN32H) ]
   ├── [입력] 포토센서, 비상정지(E-Stop), 시작/정지 스위치
   └── [출력] 컨베이어 모터, 경고등(Patlite), 포토커플러(24V-5V) ──> [아두이노 서보 제어기]
```

---

## 2. PLC I/O 할당표 (I/O Map)

### Digital Input (입력 맵)
| 주소 (Address) | 기호 (Symbol) | 명칭 (Name) | 신호 극성 / 비고 |
| :--- | :--- | :--- | :--- |
| **`P0000`** | `IN_START` | 시스템 시작 스위치 (Start Switch) | NO 접점 (NPN / $0	ext{V}$ 입력) |
| **`P0001`** | `IN_STOP` | 시스템 정지 스위치 (Stop Switch) | NC 접점 |
| **`P0002`** | `IN_ESTOP` | 비상 정지 스위치 (E-Stop Button) | NC 접점 (A1 알람 트리거) |
| **`P0003`** | `IN_SENSOR_DETECT` | 물품 투입 감지 포토센서 | NPN 센서 ($0	ext{V}$ 감지 시 ON) |
| **`P0004`** | `IN_RESET` | 알람 리셋 스위치 (Alarm Reset) | NO 접점 |

### Digital Output (출력 맵 - 릴레이 출력)
| 주소 (Address) | 기호 (Symbol) | 명칭 (Name) | 연결 장치 및 설명 |
| :--- | :--- | :--- | :--- |
| **`P0020`** | `OUT_CONVEYOR` | 컨베이어 구동 릴레이 | 컨베이어 모터 메인 콘택터 제어 |
| **`P0021`** | `OUT_SIGNAL_RED` | 포토커플러 CH1 (RED 분류) | $24	ext{V}$ 신호 출력 $
ightarrow$ 포토커플러 IN1 |
| **`P0022`** | `OUT_SIGNAL_GREEN` | 포토커플러 CH2 (GREEN 분류) | $24	ext{V}$ 신호 출력 $
ightarrow$ 포토커플러 IN2 |
| **`P0023`** | `OUT_SIGNAL_BLUE` | 포토커플러 CH3 (BLUE 분류) | $24	ext{V}$ 신호 출력 $
ightarrow$ 포토커플러 IN3 |
| **`P0024`** | `OUT_SIGNAL_YELLOW` | 포토커플러 CH4 (YELLOW 분류) | $24	ext{V}$ 신호 출력 $
ightarrow$ 포토커플러 IN4 |
| **`P002A`** | `OUT_LAMP_GREEN` | 운전 상태 표시등 (Green) | 시스템 정상 가동 중 점등 |
| **`P002B`** | `OUT_TOWER_BUZZER` | 경고 경풍 및 알람 부저 | 비상정지 및 통신 에러 발생 시 |

---

## 3. 하드웨어 배선 및 극성 가이드 (Wiring Specs)

### 1️⃣ 입력부 배선 (NPN Sink 방식)
- **PLC 입력 COM**: 외부 $24	ext{V}$ SMPS의 **`+24V`** 연결.
- **포토센서 / 스위치**: 동작 시 **`0V (24G)`** 신호를 PLC 입력 단자로 인가하여 도통(Loop 형성) 감지.

### 2️⃣ 출력부 배선 (릴레이 출력 $
ightarrow$ 포토커플러 중계)
- **PLC 출력 COM**: 외부 $24	ext{V}$ SMPS의 **`+24V`** 연결. (스위칭 시 단자에서 $+24	ext{V}$ 출력)
- **포토커플러 모듈 입력측**:
  - `IN1 ~ IN4`: PLC 출력 단자(`P0021 ~ P0024`)에서 들어오는 **`+24V`** 연결.
  - `포토커플러 입력 COM`: 외부 $24	ext{V}$ SMPS의 **`0V (24G)`** 연결.
- **포토커플러 모듈 출력측 (아두이노 $5	ext{V}$ 회로 절연)**:
  - `VCC` / `GND`: 아두이노의 $5	ext{V}$ / **`GND`** 연결.
  - `OUT1 ~ OUT4`: 아두이노 디지털 입력 핀 연결.

```text
[ SMPS +24V ] ───> [ PLC 출력 COM ]
                         │ (릴레이 접점 ON)
                         ▼
                   [ PLC 출력 P21 ] ──(+24V)──> [ 포토커플러 IN1 ]
                                                  │ (내부 LED)
[ SMPS 24G  ] ────────────────────────────────> [ 포토커플러 입력 COM ]
```

---

## 4. Modbus RTU 시리얼 통신 사양

- **통신 규격**: RS-485 (2-wire)
- **통신 속도 (Baud Rate)**: `9600 bps` / **Data Bit**: `8` / **Stop Bit**: `1` / **Parity**: `None`
- **국번 (Station ID)**: `1`

### Modbus 비트/워드 메모리 맵
| 구분 | Modbus 주소 | PLC 내부 메모리 | 역할 및 설명 |
| :--- | :--- | :--- | :--- |
| **Coil (0x)** | `00001` | `M0000` | 원격 자동 운전 시작 (Start Command) |
| **Coil (0x)** | `00002` | `M0001` | 원격 운전 정지 (Stop Command) |
| **Coil (0x)** | `00005` | `M0004` | 원격 알람 리셋 (Reset Command) |
| **Discrete Input (1x)** | `10001` | `P0003` | 물체 투입 포토센서 상태 (0: OFF, 1: ON) |
| **Holding Reg (4x)** | `40001` | `D0000` | 색상 분류 명령 코드 ($1=	ext{RED}$, $2=	ext{GREEN}$, $3=	ext{BLUE}$, $4=	ext{YELLOW}$) |
| **Holding Reg (4x)** | `40002` | `D0001` | 누적 처리 물품 수량 카운터 |

---

## 5. 프로그램 다운로드 및 동작 테스트 (XG5000)

1. **XG5000** 실행 후 프로젝트(`plc/SmartSorter_PLC.xgp`)를 열어줍니다.
2. 접속 설정에서 **RS-232C/USB** 또는 통신 모듈을 선택하여 PLC에 접속합니다.
3. `온라인` $
ightarrow$ `쓰기(W)`를 실행하여 래더 프로그램 및 통신 파라미터를 PLC에 전송합니다.
4. PLC 상태를 **RUN** 모드로 전환 후, 대시보드 애플리케이션과 통신 연동 및 입출력 모니터링을 진행합니다.

---
