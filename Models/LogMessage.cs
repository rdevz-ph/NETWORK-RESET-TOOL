using System;

namespace NetworkResetTool.Models
{
    public class LogMessage
    {
        public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss.fff");
        public string Level { get; set; } = "INFO";
        public string Message { get; set; } = string.Empty;

        public string LevelBrush
        {
            get
            {
                return Level switch
                {
                    "INFO" => "#B0B0B0",      // Medium Gray
                    "CMD" => "#03DAC6",       // Cyan/Teal
                    "OUT" => "#E0E0E0",       // Off-White/Light Gray
                    "SUCCESS" => "#4CAF50",   // Green
                    "ERROR" => "#F44336",     // Red
                    _ => "#FFFFFF"
                };
            }
        }
    }
}
