# SchoolERP Biometric Integration Guide

## How to Switch from Mock to Real Device
To switch from the mock fingerprint device to the real ZK Technology device, update `src/SchoolERP/App.config`:
```xml
<add key="FingerprintDevice:UseMock" value="false"/>
```
You can also customize the IP address and port in the same file:
```xml
<add key="FingerprintDevice:IP" value="YOUR_DEVICE_IP"/>
<add key="FingerprintDevice:Port" value="4370"/>
```

## ZKemKeeper COM SDK Approach
1. **Register the ZKemKeeper COM Library**
   - Obtain zkemkeeper.dll from ZK Technology
   - Register the DLL (as admin):
     ```cmd
     regsvr32 "path\to\zkemkeeper.dll"
     ```

2. **Add COM Reference**
   - In Visual Studio, right-click SchoolERP project → Add → Reference
   - Go to COM tab → Find and select "zkemkeeper"
   - Click OK

3. **Update ZkFingerprintDeviceClient.cs**
   Replace the stub with actual SDK calls:
   - Connect: Use `Connect_Net()`
   - GetAttendanceLogs: Use `ReadGeneralLogData()` + `GetGeneralLogData()`
   - Disconnect: Use `Disconnect()`

## Required SDK Calls
| Function | Description |
|----------|-------------|
| `Connect_Net(ipAddress, port)` | Connects to the ZK device over TCP/IP |
| `ReadGeneralLogData(machineNumber)` | Reads all attendance logs from the device |
| `GetNextGeneralLogData(...)` | Iterates over the logs returned by ReadGeneralLogData |
| `Disconnect()` | Closes the connection to the device |

## FingerprintID Mapping
The `FingerprintID` column in the `dbo.Teachers` table must match the **Enrollment Number** configured for each teacher on the ZK device.
