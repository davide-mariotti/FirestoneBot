Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WsCap {
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern bool SetProcessWorkingSetSizeEx(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize, int Flags);
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
    $tp = New-Object WsCap+TOKEN_PRIVILEGES
    $tp.PrivilegeCount = 1
    $tp.Attributes = 2 # SE_PRIVILEGE_ENABLED
    $hTok = [IntPtr]::Zero
    [WsCap]::OpenProcessToken([WsCap]::GetCurrentProcess(), 0x28, [ref]$hTok) | Out-Null
    [WsCap]::LookupPrivilegeValue($null, $name, [ref]$tp.Luid) | Out-Null
    [WsCap]::AdjustTokenPrivileges($hTok, $false, [ref]$tp, 0, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
}

# Se lo script gira NON come Admin, questa riga fallisce silenziosamente e i limiti "hard"
# potrebbero non applicarsi - in quel caso rilancia lo script come Amministratore.
Enable-Privilege "SeIncreaseQuotaPrivilege"

# Hard limit: Windows forza il processo a restare sotto il massimo, non solo un trim una tantum
$QUOTA_LIMITS_HARDWS_MIN_ENABLE = 0x1
$QUOTA_LIMITS_HARDWS_MAX_ENABLE = 0x4

$minWs = [IntPtr]20MB
$maxWs = [IntPtr]60MB

Get-Process steamwebhelper -ErrorAction SilentlyContinue | ForEach-Object {
    $ok = [WsCap]::SetProcessWorkingSetSizeEx($_.Handle, $minWs, $maxWs, ($QUOTA_LIMITS_HARDWS_MIN_ENABLE -bor $QUOTA_LIMITS_HARDWS_MAX_ENABLE))
    Write-Host "PID $($_.Id): $ok"
}
