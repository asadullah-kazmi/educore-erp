// TODO: Replace stub with ZKemKeeper COM SDK or ZKAIO SDK calls when device arrives.
// NuGet: zkemkeeper or use P/Invoke.
using System;
using System.Collections.Generic;
using System.Windows;

namespace SchoolERP.Services
{
    public class ZkFingerprintDeviceClient : IFingerprintDeviceClient
    {
        private readonly string _ipAddress;
        private readonly int _port;

        public ZkFingerprintDeviceClient(string ipAddress = "192.168.1.201", int port = 4370)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public bool Connect()
        {
            MessageBox.Show("ZK device not yet configured", "Biometric Device", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        public IReadOnlyList<FingerprintLogEntry> GetAttendanceLogs()
        {
            return new List<FingerprintLogEntry>();
        }

        public void Disconnect()
        {
            // No-op for now
        }
    }
}
