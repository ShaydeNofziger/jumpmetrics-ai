#Requires -Modules Pester

BeforeAll {
    $modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\JumpMetrics.psm1'
    Import-Module $modulePath -Force
    
    # Path to sample data
    $script:sampleDataPath = Join-Path -Path $PSScriptRoot -ChildPath '..\..\..\samples\sample-jump.csv'
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

Describe 'ConvertFrom-FlySightCsv' {
    BeforeAll {
        # ConvertFrom-FlySightCsv is a private function, we need to dot-source it
        . (Join-Path -Path $PSScriptRoot -ChildPath '..\Private\ConvertFrom-FlySightCsv.ps1')
    }

    It 'Should parse valid FlySight 2 CSV file' {
        $result = ConvertFrom-FlySightCsv -Path $script:sampleDataPath
        
        $result | Should -Not -BeNullOrEmpty
        $result.Metadata | Should -Not -BeNullOrEmpty
        $result.DataPoints | Should -Not -BeNullOrEmpty
        $result.DataPoints.Count | Should -BeGreaterThan 0
    }

    It 'Should extract correct metadata' {
        $result = ConvertFrom-FlySightCsv -Path $script:sampleDataPath
        
        $result.Metadata.FormatVersion | Should -Be 1
        $result.Metadata.FirmwareVersion | Should -Not -BeNullOrEmpty
        $result.Metadata.DeviceId | Should -Not -BeNullOrEmpty
        $result.Metadata.SessionId | Should -Not -BeNullOrEmpty
    }

    It 'Should parse 1972 data points from sample file' {
        $result = ConvertFrom-FlySightCsv -Path $script:sampleDataPath
        $result.DataPoints.Count | Should -Be 1972
    }

    It 'Should include computed properties' {
        $result = ConvertFrom-FlySightCsv -Path $script:sampleDataPath
        $dataPoint = $result.DataPoints[0]
        
        $dataPoint.PSObject.Properties['HorizontalSpeed'] | Should -Not -BeNullOrEmpty
        $dataPoint.PSObject.Properties['VerticalSpeed'] | Should -Not -BeNullOrEmpty
    }
}

Describe 'Import-FlySightData' {
    It 'Should parse FlySight CSV file locally' {
        $result = Import-FlySightData -Path $script:sampleDataPath -ErrorAction Stop
        
        $result | Should -Not -BeNullOrEmpty
        $result.metadata | Should -Not -BeNullOrEmpty
        $result.segments | Should -Not -BeNullOrEmpty
    }

    It 'Should return metadata with correct altitude range' {
        $result = Import-FlySightData -Path $script:sampleDataPath -ErrorAction Stop
        
        $result.metadata.maxAltitude | Should -BeGreaterThan 1900
        $result.metadata.minAltitude | Should -BeLessThan 200
    }

    It 'Should error on non-existent file' {
        { Import-FlySightData -Path './nonexistent.csv' -ErrorAction Stop } | Should -Throw
    }
}

Describe 'Export-JumpReport' {
    BeforeAll {
        $script:testJumpData = Import-FlySightData -Path $script:sampleDataPath
        $script:testReportPath = Join-Path -Path $TestDrive -ChildPath 'test-report.md'
    }

    It 'Should generate markdown report from local jump data' {
        $result = Export-JumpReport -JumpData $script:testJumpData -OutputPath $script:testReportPath
        
        $result | Should -Not -BeNullOrEmpty
        Test-Path $script:testReportPath | Should -Be $true
    }

    It 'Should include metadata in report' {
        Export-JumpReport -JumpData $script:testJumpData -OutputPath $script:testReportPath
        $content = Get-Content $script:testReportPath -Raw
        
        $content | Should -Match 'Jump Analysis Report'
        $content | Should -Match 'Jump Metadata'
        $content | Should -Match 'Recording Details'
        $content | Should -Match '1972' # Data points count
    }

    It 'Should include FlySight firmware version in report' {
        Export-JumpReport -JumpData $script:testJumpData -OutputPath $script:testReportPath
        $content = Get-Content $script:testReportPath -Raw
        
        $content | Should -Match 'v2023\.09\.22\.2'
    }
}

Describe 'Get-JumpMetrics' {
    It 'Should accept JumpData from pipeline' {
        $jump = Import-FlySightData -Path $script:sampleDataPath
        { $jump | Get-JumpMetrics -ErrorAction Stop } | Should -Not -Throw
    }

    It 'Should display metrics when available' {
        $jump = Import-FlySightData -Path $script:sampleDataPath
        $result = $jump | Get-JumpMetrics -ErrorAction SilentlyContinue
        $result | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-JumpAnalysis' {
    It 'Should accept JumpData from pipeline' {
        $jump = Import-FlySightData -Path $script:sampleDataPath
        { $jump | Get-JumpAnalysis -WarningAction SilentlyContinue -ErrorAction Stop } | Should -Not -Throw
    }

    It 'Should warn when no AI analysis available' {
        $jump = Import-FlySightData -Path $script:sampleDataPath
        $warnings = @()
        $jump | Get-JumpAnalysis -WarningVariable warnings -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
        $warnings.Count | Should -BeGreaterThan 0
    }
}

Describe 'Get-JumpHistory' {
    It 'Should have StoragePath parameter with default value' {
        $command = Get-Command Get-JumpHistory
        $storagePathParam = $command.Parameters['StoragePath']
        $storagePathParam | Should -Not -BeNullOrEmpty
    }

    It 'Should return empty array when no jumps stored' {
        # Use a non-existent directory
        $tempPath = Join-Path -Path $TestDrive -ChildPath 'emptystorage'
        $result = Get-JumpHistory -StoragePath $tempPath -ErrorAction SilentlyContinue
        $result | Should -BeNullOrEmpty
    }
}
