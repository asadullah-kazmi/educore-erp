using System.Collections.Generic;

namespace SchoolERP.Services
{
    public interface IFingerprintDeviceClient
    {
        bool Connect();
        IReadOnlyList<FingerprintLogEntry> GetAttendanceLogs();
        void Disconnect();
    }
}
