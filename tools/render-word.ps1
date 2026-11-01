param([Parameter(Mandatory)][string]$InputPath, [Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
# 本机未配置随运行时捆绑的LibreOffice时，使用Word自身导出以核对作者原排版。
$renderInput = (Resolve-Path -LiteralPath $InputPath).Path
$renderOutput = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $renderOutput | Out-Null
$renderPdf = Join-Path $renderOutput (([IO.Path]::GetFileNameWithoutExtension($renderInput)) + '.pdf')
$renderWord = $null; $renderDoc = $null
try {
    $renderWord = New-Object -ComObject Word.Application
    $renderWord.Visible = $false; $renderWord.DisplayAlerts = 0
    $renderDoc = $renderWord.Documents.Open($renderInput, $false, $true)
    $renderDoc.Repaginate()
    $renderPages = $renderDoc.ComputeStatistics(2)
    $renderDoc.ExportAsFixedFormat($renderPdf, 17)
    Write-Output ("WORD_RENDER_OK pages=" + $renderPages + " pdf=" + $renderPdf)
} finally {
    if ($renderDoc) { $renderDoc.Close(0); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($renderDoc) }
    if ($renderWord) { $renderWord.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($renderWord) }
}
