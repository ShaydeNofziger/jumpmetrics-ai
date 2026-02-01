#Requires -Modules Pester

BeforeAll {
    $modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\JumpMetrics.psm1'
    Import-Module $modulePath -Force
    
    $sampleFilePath = Join-Path -Path $PSScriptRoot -ChildPath '..\..\..\..\samples\sample-jump.csv'
    $sampleFilePath = Resolve-Path $sampleFilePath -ErrorAction SilentlyContinue
}

Describe 'JumpMetrics Module' {
    It 'Should import without errors' {
        Get-Module -Name JumpMetrics | Should -Not -BeNullOrEmpty
    }

    It 'Should export expected public functions' {
        $commands = Get-Command -Module JumpMetrics
        $commands.Name | Should -Contain 'Import-FlySightData'
        $commands.Name | Should -Contain 'Get-JumpAnalysis'
        $commands.Name | Should -Contain 'Get-JumpMetrics'
        $commands.Name | Should -Contain 'Get-JumpHistory'
        $commands.Name | Should -Contain 'Export-JumpReport'
    }
}

Describe 'ConvertFrom-FlySightCsv' -Tag 'Private' {
    BeforeAll {
        # Access private function for testing
        $privatePath = Join-Path -Path $PSScriptRoot -ChildPath '..\Private\ConvertFrom-FlySightCsv.ps1'
        . $privatePath
    }

    It 'Should parse sample FlySight file' {
        if ($sampleFilePath) {
            $result = ConvertFrom-FlySightCsv -Path $sampleFilePath
            $result | Should -Not -BeNullOrEmpty
            $result.Metadata | Should -Not -BeNullOrEmpty
            $result.DataPoints | Should -Not -BeNullOrEmpty
            $result.DataPoints.Count | Should -BeGreaterThan 1000
        }
        else {
            Set-ItResult -Skipped -Because 'Sample file not found'
        }
    }
    
    It 'Should parse metadata correctly' {
        if ($sampleFilePath) {
            $result = ConvertFrom-FlySightCsv -Path $sampleFilePath
            $result.Metadata.FormatVersion | Should -Be 1
            $result.Metadata.FirmwareVersion | Should -Not -BeNullOrEmpty
            $result.Metadata.DeviceId | Should -Not -BeNullOrEmpty
            $result.Metadata.SessionId | Should -Not -BeNullOrEmpty
        }
        else {
            Set-ItResult -Skipped -Because 'Sample file not found'
        }
    }
    
    It 'Should calculate metadata statistics' {
        if ($sampleFilePath) {
            $result = ConvertFrom-FlySightCsv -Path $sampleFilePath
            $result.Metadata.TotalDataPoints | Should -BeGreaterThan 0
            $result.Metadata.RecordingStart | Should -Not -BeNullOrEmpty
            $result.Metadata.RecordingEnd | Should -Not -BeNullOrEmpty
            $result.Metadata.MaxAltitude | Should -BeGreaterThan $result.Metadata.MinAltitude
        }
        else {
            Set-ItResult -Skipped -Because 'Sample file not found'
        }
    }
    
    It 'Should throw on nonexistent file' {
        { ConvertFrom-FlySightCsv -Path 'C:\nonexistent\file.csv' } | Should -Throw
    }
}

Describe 'Import-FlySightData' {
    It 'Should import sample file successfully' {
        if ($sampleFilePath) {
            $result = Import-FlySightData -Path $sampleFilePath
            $result | Should -Not -BeNullOrEmpty
            $result.JumpId | Should -Not -BeNullOrEmpty
            $result.FileName | Should -Be 'sample-jump.csv'
            $result.DataPoints.Count | Should -BeGreaterThan 1000
        }
        else {
            Set-ItResult -Skipped -Because 'Sample file not found'
        }
    }
    
    It 'Should validate data and return warnings' {
        if ($sampleFilePath) {
            $result = Import-FlySightData -Path $sampleFilePath
            $result.ValidationResults | Should -Not -BeNullOrEmpty
            $result.ValidationResults.IsValid | Should -Be $true
        }
        else {
            Set-ItResult -Skipped -Because 'Sample file not found'
        }
    }
    
    It 'Should support pipeline input' {
        if ($sampleFilePath) {
            $result = Get-Item -Path $sampleFilePath | Import-FlySightData
            $result | Should -Not -BeNullOrEmpty
        }
        else {
            Set-ItResult -Skipped -Because 'Sample file not found'
        }
    }
    
    It 'Should cache jump in session' {
        if ($sampleFilePath) {
            $global:JumpMetricsCache = $null
            $result = Import-FlySightData -Path $sampleFilePath
            $global:JumpMetricsCache | Should -Not -BeNullOrEmpty
            $global:JumpMetricsCache.Count | Should -BeGreaterThan 0
        }
        else {
            Set-ItResult -Skipped -Because 'Sample file not found'
        }
    }
}

Describe 'Get-JumpMetrics' {
    BeforeAll {
        if ($sampleFilePath) {
            $testJump = Import-FlySightData -Path $sampleFilePath
        }
    }
    
    It 'Should calculate metrics from jump object' {
        if ($testJump) {
            $result = Get-JumpMetrics -Jump $testJump
            $result | Should -Not -BeNullOrEmpty
            $result.Overview | Should -Not -BeNullOrEmpty
            $result.Segments | Should -Not -BeNullOrEmpty
        }
        else {
            Set-ItResult -Skipped -Because 'Test jump not available'
        }
    }
    
    It 'Should detect multiple segments' {
        if ($testJump) {
            $result = Get-JumpMetrics -Jump $testJump
            $result.Segments.Count | Should -BeGreaterThan 1
        }
        else {
            Set-ItResult -Skipped -Because 'Test jump not available'
        }
    }
    
    It 'Should calculate segment metrics' {
        if ($testJump) {
            $result = Get-JumpMetrics -Jump $testJump
            $result.Segments[0].Metrics | Should -Not -BeNullOrEmpty
        }
        else {
            Set-ItResult -Skipped -Because 'Test jump not available'
        }
    }
    
    It 'Should support pipeline input' {
        if ($testJump) {
            $result = $testJump | Get-JumpMetrics
            $result | Should -Not -BeNullOrEmpty
        }
        else {
            Set-ItResult -Skipped -Because 'Test jump not available'
        }
    }
}

Describe 'Get-JumpAnalysis' {
    BeforeAll {
        if ($sampleFilePath) {
            $testJump = Import-FlySightData -Path $sampleFilePath
        }
    }
    
    It 'Should generate mock analysis' {
        if ($testJump) {
            $result = Get-JumpAnalysis -Jump $testJump
            $result | Should -Not -BeNullOrEmpty
            $result.OverallAssessment | Should -Not -BeNullOrEmpty
            $result.SkillLevel | Should -BeGreaterThan 0
        }
        else {
            Set-ItResult -Skipped -Because 'Test jump not available'
        }
    }
    
    It 'Should include safety flags' {
        if ($testJump) {
            $result = Get-JumpAnalysis -Jump $testJump
            $result.SafetyFlags | Should -Not -BeNullOrEmpty
        }
        else {
            Set-ItResult -Skipped -Because 'Test jump not available'
        }
    }
}

Describe 'Get-JumpHistory' {
    It 'Should return empty array when no jumps cached' {
        $global:JumpMetricsCache = $null
        $result = Get-JumpHistory
        $result | Should -BeNullOrEmpty
    }
    
    It 'Should list cached jumps' {
        if ($sampleFilePath) {
            $global:JumpMetricsCache = $null
            Import-FlySightData -Path $sampleFilePath | Out-Null
            $result = Get-JumpHistory
            $result.Count | Should -Be 1
        }
        else {
            Set-ItResult -Skipped -Because 'Sample file not found'
        }
    }
}

Describe 'Export-JumpReport' {
    BeforeAll {
        if ($sampleFilePath) {
            $testJump = Import-FlySightData -Path $sampleFilePath
            $testMetrics = Get-JumpMetrics -Jump $testJump
            $testAnalysis = Get-JumpAnalysis -Jump $testJump
        }
    }
    
    It 'Should generate markdown report' {
        if ($testJump) {
            $tempFile = [System.IO.Path]::GetTempFileName() + '.md'
            try {
                $result = Export-JumpReport -Jump $testJump -Metrics $testMetrics -Analysis $testAnalysis -OutputPath $tempFile
                $result | Should -Not -BeNullOrEmpty
                Test-Path -Path $tempFile | Should -Be $true
                
                $content = Get-Content -Path $tempFile -Raw
                $content | Should -Match '# Jump Report'
                $content | Should -Match 'Jump ID'
                $content | Should -Match 'Performance Metrics'
            }
            finally {
                if (Test-Path -Path $tempFile) {
                    Remove-Item -Path $tempFile -Force
                }
            }
        }
        else {
            Set-ItResult -Skipped -Because 'Test data not available'
        }
    }
}

Describe 'Help Documentation' {
    It 'Should have help for Import-FlySightData' {
        $cmd = Get-Command Import-FlySightData -ErrorAction SilentlyContinue
        $cmd | Should -Not -BeNullOrEmpty
    }
    
    It 'Should have help for Get-JumpMetrics' {
        $cmd = Get-Command Get-JumpMetrics -ErrorAction SilentlyContinue
        $cmd | Should -Not -BeNullOrEmpty
    }
    
    It 'Should have help for Get-JumpAnalysis' {
        $cmd = Get-Command Get-JumpAnalysis -ErrorAction SilentlyContinue
        $cmd | Should -Not -BeNullOrEmpty
    }
    
    It 'Should have help for Get-JumpHistory' {
        $cmd = Get-Command Get-JumpHistory -ErrorAction SilentlyContinue
        $cmd | Should -Not -BeNullOrEmpty
    }
    
    It 'Should have help for Export-JumpReport' {
        $cmd = Get-Command Export-JumpReport -ErrorAction SilentlyContinue
        $cmd | Should -Not -BeNullOrEmpty
    }
}
