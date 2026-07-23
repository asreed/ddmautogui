@echo off
setlocal

set SIZE=54
set COLOR=#FFFFFF

for %%f in (*.svg) do (
    magick -background none -density 1200 "%%f" ^
        -resize %SIZE%x%SIZE% ^
        -fill "%COLOR%" ^
        -colorize 100 ^
        "%%~nf-white.png"
)

echo Complete.