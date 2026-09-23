# Verifica stato attuale (non richiede Admin)
Get-MMAgent | Select-Object MemoryCompression

# Se risulta False, abilita cosi' (richiede Amministratore):
# Enable-MMAgent -MemoryCompression
