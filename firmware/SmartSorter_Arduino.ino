#include <Servo.h>

// =========================================================================
// 1. 핀 정의 (Pin Definitions)
// =========================================================================
// PLC 24V -> 24V-5V 포토커플러 모듈 -> 아두이노 디지털 입력 핀
const int PIN_PLC_P21 = 2; // RED (P21)
const int PIN_PLC_P22 = 3; // GREEN (P22)
const int PIN_PLC_P23 = 4; // BLUE (P23)
const int PIN_PLC_P24 = 5; // YELLOW (P24)

// 서보모터 PWM 제어 핀
const int PIN_SERVO_1 = 9;  // Servo 1 (RED, GREEN 제어용)
const int PIN_SERVO_2 = 10; // Servo 2 (BLUE, YELLOW 제어용)

// =========================================================================
// 2. 각도 설정 (Angle Configurations)
// =========================================================================
const int SERVO_HOME_ANGLE = 0; // 대기 모드 원점 각도 (0도)

// 동작 각도 조건
const int RED_ANGLE    = 30;  // P21: RED    -> Servo1 (30도)
const int GREEN_ANGLE  = 120; // P22: GREEN  -> Servo1 (120도)
const int BLUE_ANGLE   = 30;  // P23: BLUE   -> Servo2 (30도)
const int YELLOW_ANGLE = 120; // P24: YELLOW -> Servo2 (120도)

// 동작 지속 시간 (서보모터가 해당 각도를 유지하는 시간, 단위: ms)
const unsigned long ACTION_HOLD_TIME = 1500; 

// 서보모터 객체 생성
Servo servo1;
Servo servo2;

// =========================================================================
// 3. 초기화 (Setup)
// =========================================================================
void setup() {
  // 포토커플러 입력 핀 설정 (NPN/PNP 출력 특성에 따라 INPUT 또는 INPUT_PULLUP)
  pinMode(PIN_PLC_P21, INPUT);
  pinMode(PIN_PLC_P22, INPUT);
  pinMode(PIN_PLC_P23, INPUT);
  pinMode(PIN_PLC_P24, INPUT);

  // 서보모터 핀 연결
  servo1.attach(PIN_SERVO_1);
  servo2.attach(PIN_SERVO_2);

  // 원점(0도) 복귀 초기화
  resetServosToHome();

  // 디버깅용 시리얼 통신 설정 (9600 bps)
  Serial.begin(9600);
  Serial.println("=========================================");
  Serial.println("  SmartSorter Servo Controller Started   ");
  Serial.println("=========================================");
}

// =========================================================================
// 4. 메인 루프 (Main Loop)
// =========================================================================
void loop() {
  // PLC 접점 신호 읽기 (HIGH = 접점 동작 ON)
  bool isP21 = digitalRead(PIN_PLC_P21); // RED
  bool isP22 = digitalRead(PIN_PLC_P22); // GREEN
  bool isP23 = digitalRead(PIN_PLC_P23); // BLUE
  bool isP24 = digitalRead(PIN_PLC_P24); // YELLOW

  // [P21] RED -> Servo1 (30도)
  if (isP21 == HIGH) {
    Serial.println("[PLC P21 DETECTED] -> RED: Servo 1 -> 30 deg");
    servo1.write(RED_ANGLE);
    delay(ACTION_HOLD_TIME);
    resetServosToHome();
  }
  // [P22] GREEN -> Servo1 (120도)
  else if (isP22 == HIGH) {
    Serial.println("[PLC P22 DETECTED] -> GREEN: Servo 1 -> 120 deg");
    servo1.write(GREEN_ANGLE);
    delay(ACTION_HOLD_TIME);
    resetServosToHome();
  }
  // [P23] BLUE -> Servo2 (30도)
  else if (isP23 == HIGH) {
    Serial.println("[PLC P23 DETECTED] -> BLUE: Servo 2 -> 30 deg");
    servo2.write(BLUE_ANGLE);
    delay(ACTION_HOLD_TIME);
    resetServosToHome();
  }
  // [P24] YELLOW -> Servo2 (120도)
  else if (isP24 == HIGH) {
    Serial.println("[PLC P24 DETECTED] -> YELLOW: Servo 2 -> 120 deg");
    servo2.write(YELLOW_ANGLE);
    delay(ACTION_HOLD_TIME);
    resetServosToHome();
  }

  // 짧은 대기 주기
  delay(20);
}

// =========================================================================
// 5. 원점 복귀 함수 (Reset Servos to Home Position)
// =========================================================================
void resetServosToHome() {
  servo1.write(SERVO_HOME_ANGLE);
  servo2.write(SERVO_HOME_ANGLE);
}