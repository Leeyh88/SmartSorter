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
   └── [출력] 컨베이어 모터, 포토커플러(24V-5V) ──> [아두이노 서보 제어기]
```

---

## 2. PLC I/O 할당표 (I/O Map)
### Digital Input (입력 맵)
| 주소 (Address) | 기호 (Symbol) | 명칭 (Name) | 신호 극성 / 비고 |
| :--- | :--- | :--- | :--- |
| **`P0000`** | `IN_START` | 시작 스위치 (Start Switch) | NO 접점 (NPN / $0{V}$ 입력) |
| **`P0001`** | `IN_STOP` | 정지 스위치 (Stop Switch) | NC 접점 |

### Digital Output (출력 맵 - 릴레이 출력)
| 주소 (Address) | 기호 (Symbol) | 명칭 (Name) | 연결 장치 및 설명 |
| :--- | :--- | :--- | :--- |
| **`P0020`** | `OUT_CONVEYOR` | 컨베이어 구동 | 컨베이어 모터 메인 콘택터 제어 |
| **`P0021`** | `OUT_SIGNAL_RED` | 포토커플러 CH1 (RED 분류) | $24{V}$ ➜ 포토커플러 IN1 |
| **`P0022`** | `OUT_SIGNAL_GREEN` | 포토커플러 CH2 (GREEN 분류) | $24{V}$ ➜ 포토커플러 IN2 |
| **`P0023`** | `OUT_SIGNAL_BLUE` | 포토커플러 CH3 (BLUE 분류) | $24{V}$ ➜ 포토커플러 IN3 |
| **`P0024`** | `OUT_SIGNAL_YELLOW` | 포토커플러 CH4 (YELLOW 분류) | $24{V}$ 신호 출력 ➜ 포토커플러 IN4 |

### Internal Relay (내부 비트 메모리 - C# 통신용)
| 주소 (Address) | 기호 (Symbol) | 명칭 (Name) | 통신 방식 및 설명 |
| :--- | :--- | :--- | :--- |
| **`M0010`** | `M_DETECT_RED` | RED 감지 신호 | C# 비전에서 RED 판별 시 펄스(Pulse) 전달 |
| **`M0011`** | `M_DETECT_GREEN` | GREEN 감지 신호 | C# 비전에서 GREEN 판별 시 펄스(Pulse) 전달 |
| **`M0012`** | `M_DETECT_BLUE` | BLUE 감지 신호 | C# 비전에서 BLUE 판별 시 펄스(Pulse) 전달 |
| **`M0013`** | `M_DETECT_YELLOW` | YELLOW 감지 신호 | C# 비전에서 YELLOW 판별 시 펄스(Pulse) 전달 |

### Data Register (데이터 레지스터 - C# 카운트 데이터 바인딩용)
| 주소 (Address) | 기호 (Symbol) | 명칭 (Name) | 데이터 타입 및 설명 |
| :--- | :--- | :--- | :--- |
| **`D0000`** | `CNT_RED` | RED 분류 카운트 | WORD (16-bit) / C# UI 바인딩 및 표시 |
| **`D0001`** | `CNT_GREEN` | GREEN 분류 카운트 | WORD (16-bit) / C# UI 바인딩 및 표시 |
| **`D0002`** | `CNT_BLUE` | BLUE 분류 카운트 | WORD (16-bit) / C# UI 바인딩 및 표시 |
| **`D0003`** | `CNT_YELLOW` | YELLOW 분류 카운트 | WORD (16-bit) / C# UI 바인딩 및 표시 |


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
| **Coil (0x)** | `00016` | `M0010` | RED 감지 신호 |
| **Coil (0x)** | `00017` | `M0011` | GREEN 감지 신호 |
| **Coil (0x)** | `00018` | `M0012` | BLUE 감지 신호 |
| **Coil (0x)** | `00019` | `M0013` | YELLOW 감지 신호 |
| **Holding Reg (4x)** | `40001` | `D0000` | RED 분류 누적 수량 카운터 |
| **Holding Reg (4x)** | `40002` | `D0001` | GREEN분류 누적 수량 카운터 |
| **Holding Reg (4x)** | `40003` | `D0002` | BLUE 분류 누적 수량 카운터 |
| **Holding Reg (4x)** | `40004` | `D00003` | YELLOW 분류 누적 수량 카운터 |
---

