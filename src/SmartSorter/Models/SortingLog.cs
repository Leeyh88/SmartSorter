using System;
using System.Collections.Generic;
using System.Text;

namespace SmartSorter.Models
{
    public class SortingLog
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public BoxColor DetectedColor { get; set; }
        public int TargetServoAngle { get; set; }
        public OperationMode Mode { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
