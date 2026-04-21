# PowerShell script to compile MonoGame content
# This script compiles spritefont files to .xnb format

$mgcbPath = "mgcb"

# Get the directory where the script is located (TicTacToe folder)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Define paths using full paths
$contentDir = Join-Path $scriptDir "Content"
$outputDir = Join-Path $scriptDir "bin\Debug\net10.0\Content"

# Create output directory if it doesn't exist
if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# Compile all fonts in a single mgcb call
# Using single quotes for delimiters to avoid escaping issues
& $mgcbPath /platform:Windows /importer:FontDescriptionImporter /processor:FontDescriptionProcessor /build:"$contentDir\Arial.spritefont;$outputDir\Arial.xnb" /build:"$contentDir\Arial14.spritefont;$outputDir\Arial14.xnb" /build:"$contentDir\Arial16.spritefont;$outputDir\Arial16.xnb" /build:"$contentDir\Arial18.spritefont;$outputDir\Arial18.xnb" /build:"$contentDir\Arial24.spritefont;$outputDir\Arial24.xnb" /build:"$contentDir\Roboto.spritefont;$outputDir\Roboto.xnb"
