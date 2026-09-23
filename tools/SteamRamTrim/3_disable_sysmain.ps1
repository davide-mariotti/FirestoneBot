# Richiede PowerShell come Amministratore
Stop-Service SysMain -Force -ErrorAction SilentlyContinue
Set-Service SysMain -StartupType Disabled
Write-Host "SysMain disabilitato. Per ripristinare:"
Write-Host '  Set-Service SysMain -StartupType Automatic; Start-Service SysMain'
