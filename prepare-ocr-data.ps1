$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$dataDirectory = Join-Path $projectRoot 'tessdata'
New-Item -ItemType Directory -Force -Path $dataDirectory | Out-Null
$baseUrl = 'https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main'
foreach ($language in @('ara', 'eng')) {
    $destination = Join-Path $dataDirectory "$language.traineddata"
    Invoke-WebRequest -UseBasicParsing -Uri "$baseUrl/$language.traineddata" -OutFile $destination
    if ((Get-Item -LiteralPath $destination).Length -lt 100000) {
        throw "The $language OCR model download is incomplete."
    }
}
