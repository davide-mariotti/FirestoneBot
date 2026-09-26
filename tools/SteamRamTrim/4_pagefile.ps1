# Richiede PowerShell come Amministratore. Effetto dopo il RIAVVIO del PC.
#
# Causa dei blocchi con 17 istanze (diagnosi 2026-09-26): esaurimento del COMMIT LIMIT
# (RAM + pagefile), non della RAM fisica. Con pagefile "gestito automaticamente" Windows lo
# limita a 1/8 del volume: su C: da 232 GB = ~29 GB -> commit limit 32 + 29 = 61 GB.
# Con 17 Steam + 17 Firestone servono ~65 GB (Firestone ~2.4 GB ciascuno, steamwebhelper ~13 GB
# in totale): le allocazioni falliscono -> dwm.exe crasha con c00001ad (schermo bloccato),
# Chromium/Electron (VS Code, steamwebhelper) muoiono con 0xE0000008 (OOM), evento 2004
# "memoria virtuale insufficiente" ogni 5 minuti nel log di Sistema.
#
# Il trim del working set (trim_ram.ps1) NON aiuta: sposta pagine su pagefile ma il commit
# resta identico. Serve alzare il limite con un pagefile fisso piu' grande.

param(
    [int]$SizeMB = 57344   # 56 GB -> commit limit ~88 GB. Lascia ~24 GB liberi su C:
)

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { Write-Error "Rilancia PowerShell come Amministratore."; exit 1 }

$drive = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
$current = Get-CimInstance Win32_PageFileUsage | Where-Object Name -like 'C:*'
$availableMB = [int]($drive.FreeSpace / 1MB) + [int]$current.AllocatedBaseSize
if ($SizeMB -gt $availableMB - 10240) {
    Write-Error "Spazio insufficiente su C: (disponibili ~$availableMB MB incluso il pagefile attuale, servono $SizeMB + 10 GB di margine)."
    exit 1
}

# Disattiva la gestione automatica, poi imposta un pagefile fisso (iniziale = massimo:
# niente crescita dinamica, che e' lenta e fallisce proprio nei picchi).
$cs = Get-CimInstance Win32_ComputerSystem
if ($cs.AutomaticManagedPagefile) {
    Set-CimInstance -InputObject $cs -Property @{ AutomaticManagedPagefile = $false }
}

# Scrittura diretta nel registro (e' cio' che Windows legge al boot). Via WMI la prima versione
# lasciava "C:\pagefile.sys 0 0" (= dimensione gestita dal sistema, di nuovo limitata a 1/8 disco).
$mmKey = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'
Set-ItemProperty -Path $mmKey -Name PagingFiles -Type MultiString -Value @("C:\pagefile.sys $SizeMB $SizeMB")

$written = (Get-ItemProperty $mmKey).PagingFiles
Write-Host "PagingFiles = $written"
if ($written -ne "C:\pagefile.sys $SizeMB $SizeMB") { Write-Error "Scrittura nel registro non riuscita."; exit 1 }
Write-Host "Fatto. RIAVVIA il PC per applicare. Dopo il riavvio verifica con:"
Write-Host "  (Get-CimInstance Win32_OperatingSystem).TotalVirtualMemorySize / 1MB   # atteso ~88 (GB)"
