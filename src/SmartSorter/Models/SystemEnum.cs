using System;
using System.Collections.Generic;
using System.Text;

namespace SmartSorter.Models
{
    // 시스템 동작 모드
    public enum OperationMode
    {
        Auto,       // 자동 모드 (웹캠 + Modbus 제어)
        Manual,     // 수동 모드 (슬라이더로 서보 각도 조작)
        Simulation  // 시뮬레이션 모드 (웹캠 없이 가상 테스트)
    }

    // 감지 대상 색상
    public enum BoxColor
    {
        None,
        Red,
        Green,
        Blue,
        Yellow
    }
}
