param([Parameter(Mandatory=$true)][string]$InputPath,[Parameter(Mandatory=$true)][string]$OutputPdf)
$ErrorActionPreference='Stop'
$docxRenderInput=(Resolve-Path -LiteralPath $InputPath).Path
$docxRenderOutput=[IO.Path]::GetFullPath($OutputPdf)
New-Item -ItemType Directory -Force -Path (Split-Path $docxRenderOutput -Parent) | Out-Null
$docxRenderApp=$null
$docxRenderDocument=$null
try {
    $docxRenderApp=New-Object -ComObject Word.Application
    $docxRenderApp.Visible=$false
    $docxRenderApp.DisplayAlerts=0
    $docxRenderApp.AutomationSecurity=3
    $docxRenderApp.Options.UpdateLinksAtOpen=$false
    $docxRenderDocument=$docxRenderApp.Documents.Open($docxRenderInput,$false,$true,$false)
    $docxRenderDocument.Repaginate()
    $docxRenderPages=$docxRenderDocument.ComputeStatistics(2)
    $docxRenderDocument.ExportAsFixedFormat($docxRenderOutput,17)
    Write-Output "WORD_RENDER_OK pages=$docxRenderPages output=$docxRenderOutput"
}
finally {
    if($null -ne $docxRenderDocument){$docxRenderDocument.Close(0);[void][Runtime.InteropServices.Marshal]::ReleaseComObject($docxRenderDocument)}
    if($null -ne $docxRenderApp){$docxRenderApp.Quit(0);[void][Runtime.InteropServices.Marshal]::ReleaseComObject($docxRenderApp)}
}
