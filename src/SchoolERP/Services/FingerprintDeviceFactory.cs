using System.Configuration;

namespace SchoolERP.Services
{
    public static class FingerprintDeviceFactory
    {
        public static IFingerprintDeviceClient Create()
        {
            var useMockSetting = ConfigurationManager.AppSettings["FingerprintDevice:UseMock"];
            var useMock = !bool.TryParse(useMockSetting, out var parsedUseMock) || parsedUseMock;

            if (useMock)
            {
                return new MockFingerprintDeviceClient();
            }

            var ip = ConfigurationManager.AppSettings["FingerprintDevice:IP"] ?? "192.168.1.201";
            var portSetting = ConfigurationManager.AppSettings["FingerprintDevice:Port"];
            var port = int.TryParse(portSetting, out var parsedPort) ? parsedPort : 4370;

            return new ZkFingerprintDeviceClient(ip, port);
        }
    }
}
