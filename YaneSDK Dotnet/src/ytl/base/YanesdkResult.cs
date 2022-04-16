using System;

namespace Yanesdk.Ytl
{
	/// <summary>
	/// ライブラリ全体で用いる、エラーを表す列挙体。
	/// </summary>
	// [CLSCompliant(true)]
	public enum YanesdkResult {
		/// <summary>
		/// エラーなし。
		/// </summary>
		NoError=0,
		/// <summary>
		/// 未実装。
		/// </summary>
		NotImplemented,
		/// <summary>
		/// quitすべき。
		/// </summary>
		ShouldBeQuit, 
		/// <summary>
		/// 完了できない(前提条件が守られていないため)。
		/// </summary>
		PreconditionError,
		/// <summary>
		/// すでに完了している。
		/// </summary>
		AlreadyDone,
		/// <summary>
		/// 何らかのエラーが発生した。
		/// </summary>
		HappenSomeError,
		/// <summary>
		/// ファイルが見つからない。
		/// </summary>
		FileNotFound,
		/// <summary>
		/// ファイルの読み込み、解読上のエラー。
		/// </summary>
		FileReadError,
		/// <summary>
		/// ファイルが書き込めない。
		/// </summary>
		FileWriteError,
		/// <summary>
		/// パラメータが不正。
		/// </summary>
		InvalidParameter,
		/// <summary>
		/// Win32のAPI関連のエラー。
		/// </summary>
		Win32apiError,
		/// <summary>
		/// SDL関連のエラー。
		/// </summary>
		/// <example>
		/// 例)
		/// <code>
        /// return SDLsomefunc() ? YanesdkResult.SdlError:YanesdkResult.NoError;
		/// </code>
		/// </example>
		SdlError,
		/// <summary>
		/// zlib上のエラー。
		/// </summary>
		ZlibError,
	}
}
