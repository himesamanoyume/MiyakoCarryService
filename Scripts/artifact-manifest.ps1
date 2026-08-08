function Get-McsArtifacts {
    param(
        [string]$Ver
    )

    return @(
        @{ Name = "Plugin";    ZipFile = "MiyakoCarryService-$Ver.zip";          ReportFile = "plugin_vt_report_url.txt" },
        @{ Name = "Fika";      ZipFile = "MiyakoCarryServiceFika-$Ver.zip";      ReportFile = "fika_vt_report_url.txt" },
        @{ Name = "Assistant"; ZipFile = "MiyakoCarryServiceAssistant-$Ver.zip"; ReportFile = "assistant_vt_report_url.txt" }
    )
}
