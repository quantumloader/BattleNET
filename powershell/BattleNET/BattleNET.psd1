@{
    RootModule        = 'BattleNET.psm1'
    ModuleVersion     = '2.1.0'
    GUID              = 'a7c3e9d1-4b2f-4e8a-9c1d-6f5e2a8b0d41'
    Author            = 'quantumloader'
    CompanyName       = 'quantumloader'
    Copyright         = '(c) 2026 quantumloader. LGPL-3.0-or-later.'
    Description       = 'BattlEye RCon client for PowerShell (BattleNET)'
    PowerShellVersion = '5.1'
    FunctionsToExport = @(
        'Connect-BattlEye'
        'Disconnect-BattlEye'
        'Send-BattlEyeCommand'
        'Send-BattlEyeCommandAndWait'
        'New-BattlEyeClient'
        'New-BattlEyeLoginPacket'
        'New-BattlEyeCommandPacket'
        'New-BattlEyeAcknowledgePacket'
        'ConvertFrom-BattlEyePacket'
        'Get-BattlEyeCrc32'
    )
    AliasesToExport   = @()
    CmdletsToExport   = @()
    VariablesToExport = @()
}
