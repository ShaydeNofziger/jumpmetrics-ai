#Requires -Modules Pester

BeforeAll {
    $modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\JumpMetrics.psm1'
    Import-Module $modulePath -Force
}

Describe 'JumpMetrics Module' {
    It 'Should import without errors' {
        Get-Module -Name JumpMetrics | Should -Not -BeNullOrEmpty
    }

    It 'Should export expected functions' {
        $commands = Get-Command -Module JumpMetrics
        $commands.Name | Should -Contain 'Import-FlySightData'
        $commands.Name | Should -Contain 'Get-JumpAnalysis'
        $commands.Name | Should -Contain 'Get-JumpMetrics'
        $commands.Name | Should -Contain 'Get-JumpHistory'
        $commands.Name | Should -Contain 'Export-JumpReport'
    }
}

Describe 'Import-FlySightData' {
    It 'Should throw NotImplementedException' {
        { Import-FlySightData -Path $PSCommandPath } | Should -Throw '*not yet implemented*'
    }
}
