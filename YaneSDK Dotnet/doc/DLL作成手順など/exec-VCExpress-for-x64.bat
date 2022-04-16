@echo off

rem call "%VS80COMNTOOLS%vsvars32.bat"
    rem ↑これやってるとややこしい事になりそうな予感がするので念のためコメントアウト。
call "%ProgramFiles%\Microsoft Platform SDK\SetEnv.Cmd" /X64 /RETAIL
    rem ↑コレがミソ。
call "%DXSDK_DIR%Utilities\Bin\dx_setenv.cmd" /AMD64

set INCLUDE=%INCLUDE%;%~dp0freetype-2.1.10\include
set INCLUDE=%INCLUDE%;%~dp0jpeg-6b
set INCLUDE=%INCLUDE%;%~dp0libogg-1.1.3\include
set INCLUDE=%INCLUDE%;%~dp0libpng-1.2.14
set INCLUDE=%INCLUDE%;%~dp0libvorbis-1.1.2\include
set INCLUDE=%INCLUDE%;%~dp0SDL-1.2.11\include
set INCLUDE=%INCLUDE%;%~dp0smpeg-0.4.4
set INCLUDE=%INCLUDE%;%~dp0tiff-3.8.2\libtiff
set INCLUDE=%INCLUDE%;%~dp0zlib-1.2.3

set LIB=%LIB%;%~dp0SDL-1.2.11\VisualC\SDL\release
set LIB=%LIB%;%~dp0SDL-1.2.11\VisualC\SDLmain\release
set LIB=%LIB%;%~dp0smpeg-0.4.4\VisualC\release
set LIB=%LIB%;%~dp0freetype-2.1.10\objs
set LIB=%LIB%;%~dp0libogg-1.1.3\win32\dynamic_release
set LIB=%LIB%;%~dp0zlib-1.2.3\projects\visualc6\win32_dll_release

start "" "%VS80COMNTOOLS%..\IDE\VCExpress.exe" /USEENV

