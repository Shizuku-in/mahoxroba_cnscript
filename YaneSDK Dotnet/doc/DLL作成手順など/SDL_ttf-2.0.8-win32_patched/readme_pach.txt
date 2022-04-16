Win32でコンパイルの仕方

１．以下のソースコードを入手。http://jcatki.no-ip.org/SDL_ttf/
　　freetype-2.2.1
　　SDL_ttf-2.0.8
　　SDL-1.2.11
　　
２．MicroSoft_PlattformSDK+Cコンパイラ

３．//FreeTypeのftoption.hに以下追加	
#undef TT_CONFIG_OPTION_EMBEDDED_BITMAPS
#undef FT_CONFIG_OPTION_OLD_INTERNALS	

４．SDLとfreetypeをコンパイル

５．出来たものを参照してSDL_ttfをコンパイル



で、出来たものが
↓
http://homepage3.nifty.com/azi/SDL_ttf-2.0.8-win32_patched.zip