@{
    RootModule        = 'JumpMetrics.psm1'
    ModuleVersion     = '0.1.0'
    GUID              = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
    Author            = 'JumpMetrics AI'
    Description       = 'PowerShell module for FlySight 2 GPS data analysis and AI-powered skydiving performance insights.'
    PowerShellVersion = '7.5'

    FunctionsToExport = @(
        'Import-FlySightData'
        'Get-JumpAnalysis'
        'Get-JumpMetrics'
        'Get-JumpHistory'
        'Export-JumpReport'
    )

    CmdletsToExport   = @()
    VariablesToExport  = @()
    AliasesToExport    = @()

    PrivateData = @{
        PSData = @{
            Tags       = @('skydiving', 'FlySight', 'GPS', 'analysis', 'AI')
            ProjectUri = 'https://github.com/yourusername/JumpMetricsAI'
        }
    }
}
