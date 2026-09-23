# Richiede PowerShell come Amministratore
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class MemTrim {
    [DllImport("ntdll.dll")]
    public static extern int NtSetSystemInformation(int InfoClass, IntPtr Info, int Length);
    [DllImport("advapi32.dll", SetLastError=true)]
    public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);
    [DllImport("advapi32.dll", SetLastError=true)]
    public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out long lpLuid);
    [DllImport("advapi32.dll", SetLastError=true)]
    public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES {
        public int PrivilegeCount;
        public long Luid;
        public int Attributes;
    }
}
"@

function Enable-Privilege($name) {
    $tp = New-Object MemTrim+TOKEN_PRIVILEGES
    $tp.PrivilegeCount = 1
    $tp.Attributes = 2 # SE_PRIVILEGE_ENABLED
    $hTok = [IntPtr]::Zero
    [MemTrim]::OpenProcessToken([MemTrim]::GetCurrentProcess(), 0x28, [ref]$hTok) | Out-Null
    [MemTrim]::LookupPrivilegeValue($null, $name, [ref]$tp.Luid) | Out-Null
    [MemTrim]::AdjustTokenPrivileges($hTok, $false, [ref]$tp, 0, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
}

Enable-Privilege "SeProfileSingleProcessPrivilege"
Enable-Privilege "SeIncreaseQuotaPrivilege"

# SystemMemoryListInformation = 80, MemoryPurgeStandbyList = 4
$ptr = [Runtime.InteropServices.Marshal]::AllocHGlobal(4)
[Runtime.InteropServices.Marshal]::WriteInt32($ptr, 4)
$result = [MemTrim]::NtSetSystemInformation(80, $ptr, 4)
[Runtime.InteropServices.Marshal]::FreeHGlobal($ptr)

Write-Host "NtSetSystemInformation result: $result (0 = successo)"
