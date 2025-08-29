REM requires ImageMagick
REM default sizes are 256, 192, 128, 96, 64, 48, 40, 32, 24 and 16px
REM "256,128,64,24,16"

@echo off
for %%f in (*.svg) do (
    magick -density 1200 -background transparent -define icon:auto-resize="64,32,24,16" "%%f" "%%~nf.ico"
)
echo complete