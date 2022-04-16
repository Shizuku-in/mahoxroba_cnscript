using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// カラーキーの種類
	/// </summary>
	public enum ColorKeyType
	{
		/// <summary>
		/// カラーキー設定されておらず
		/// </summary>
		None ,

		/// <summary>
		/// 色指定型カラーキー
		/// </summary>
		ColorKeyRGB,

		/// <summary>
		/// 場所指定型カラーキー
		/// </summary>
		ColorKeyPos
	}

	/// <summary>
	/// 抜き色(カラーキー)の情報を保持する構造体
	/// </summary>
	public class ColorKey
	{
		public ColorKey()
		{
			colorKeyType = ColorKeyType.None;
		}

		/// <summary>
		/// ColorKeyを設定する。
		/// 
		/// このTextureLoaderが生成して画像を読み込んだときに
		/// TextureTexture.SetColorKeyを呼び出すなら、そのときに
		/// 必要なパラメータ。
		/// 
		/// Loadする前に行なわないと効果が無い。
		/// 
		/// 転送時の転送元カラーキー(抜き色)を指定します。
		/// これを指定して、loadを行なうと、そのサーフェースの該当色の部分の
		/// α値が0(透過)になる。(ただしαサーフェース作成時のみ)
		/// </para>
		/// <para>
		/// r,g,bはそれぞれ0～255。
		/// </para>
		/// <para>
		/// また、画像を読み込む(load)するより以前に、この関数を
		/// 呼び出しておかなければならない。(loadするときに抜き色部分の
		/// α値を0に書き換えるので)
		/// 
		/// その後の画像読み込み、解放によっては設定したカラーキーは
		/// 無効にはならない。
		/// </summary>
		public void SetColorKey(int r, int g, int b)
		{
			this.R = r; this.G = g; this.B = b;
			colorKeyType = ColorKeyType.ColorKeyRGB;
		}

		/// <summary>
		/// 座標指定型のColorKey。
		/// 指定した座標の色が抜き色となる。
		/// 
		/// このTextureLoaderが生成して画像を読み込んだときに
		/// TextureTexture.SetColorKeyを呼び出すなら、そのときに
		/// 必要なパラメータ。
		/// 
		/// Loadする前に行なわないと効果が無い。
		/// </summary>
		public void SetColorKey(int cx, int cy)
		{
			this.CX = cx;
			this.CY = cy;
			colorKeyType = ColorKeyType.ColorKeyPos;
		}

		/// <summary>
		///	カラーキーの取り消し
		/// ColorKeyのresetしてdefault状態へ。
		/// ちなみにdefaultでは ColorKey は 無効。
		/// 定義ファイルをLoadする前に行なわないと効果が無い。
		///	SetColorKey/SetColorKeyPos で設定した情報を無効化します。
		/// </summary>
		public void ResetColorKey()
		{
			R = G = B = CX = CY = 0;
			colorKeyType = ColorKeyType.None;
		}

		/// <summary>
		/// カラーキーが設定されているか。
		/// </summary>
		public ColorKeyType ColorKeyType
		{
			get { return colorKeyType; }
		//	set { colorKeyType = value; }
		}
		private ColorKeyType colorKeyType;

		public int R, G, B;			//	カラーキー
		public int CX, CY;			//	位置指定型カラーキーのcx,cy
	}
}
