using System;

namespace SchoolERP.Services
{
    public class FingerprintLogEntry
    {
        public int FingerprintId { get; set; }
        public DateTime CapturedAt { get; set; }
        public string DeviceSerial { get; set; }
        public bool IsSuccessfulScan { get; set; } = true;
    }
}
