using System;
using System.Collections.Generic;

namespace SchoolERP.Services
{
    public class MockFingerprintDeviceClient : IFingerprintDeviceClient
    {
        private bool connected;

        public bool Connect()
        {
            connected = true;
            return connected;
        }

        public IReadOnlyList<FingerprintLogEntry> GetAttendanceLogs()
        {
            if (!connected)
            {
                throw new InvalidOperationException("Mock device is not connected.");
            }

            return new List<FingerprintLogEntry>
            {
                new FingerprintLogEntry { FingerprintId = 1001, CapturedAt = DateTime.Now.AddMinutes(-45), DeviceSerial = "MOCK-ZK-001" },
                new FingerprintLogEntry { FingerprintId = 1002, CapturedAt = DateTime.Now.AddMinutes(-40), DeviceSerial = "MOCK-ZK-001" },
                new FingerprintLogEntry { FingerprintId = 1001, CapturedAt = DateTime.Now.AddMinutes(-5), DeviceSerial = "MOCK-ZK-001" }
            };
        }

        public void Disconnect()
        {
            connected = false;
        }
    }
}
