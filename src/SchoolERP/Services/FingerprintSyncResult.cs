namespace SchoolERP.Services
{
    public class FingerprintSyncResult
    {
        public int ReceivedLogs { get; set; }
        public int InsertedAttendanceRows { get; set; }
        public int SkippedLogs { get; set; }
        public string Message { get; set; }
    }
}
