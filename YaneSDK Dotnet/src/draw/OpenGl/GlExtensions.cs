using System;
using System.Collections.Generic;

using OpenGl;
using Yanesdk.Ytl;
using System.Diagnostics;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 特定のOpenGL Extensionsを有するかどうかを調べる。
	/// singleton objectである Instanceメンバ経由で扱う。
	/// </summary>
	/// <remarks>
	/// 例)
	///		if (GlExtensions.Instance.IsAvaliable(GL_ARB_texture_non_power_of_two))
	///               ....
	/// </remarks>
	public class GlExtensions
	{
		/// <summary>
		/// このクラスのsingleton instance
		/// </summary>
		public static GlExtensions Instance
		{
			get
			{
				return Singleton<GlExtensions>.Instance;
			}
		}

		public GlExtensions()
		{
			// コンストラクタでGL Extensionのリストを取得しておく。

			/*
			string ext = Gl.glGetString(Gl.GL_EXTENSIONS);
			string[] exts = ext.Split(' ');

			// 利用可能なextensionをhashtableにmarkしておく
			foreach (string s in exts)
				extlist.Add(s, null);
			*/
			// ↑このコードだと同一のGL拡張を返されると例外で落ちる。
			// (そういうビデオカードのドライバがある)

			//	また、セパレータとして\tや\nなどが入る可能性も考慮。
			// (そんなドライバは無いかも知れないが)

			string ext = Gl.glGetString(Gl.GL_EXTENSIONS);
			string[] exts = ext.Split(new char[] { ' ', '\t', '\n', '\r' },
				 StringSplitOptions.RemoveEmptyEntries);

			// 利用可能なextensionをhashtableにmarkしておく
			foreach (string s in exts)
			{
				if (!extlist.ContainsKey(s))
					extlist.Add(s, null);
			}
		}
		
		/// <summary>
		/// あるExtensionが利用可能であるかを返す
		/// </summary>
		/// <remarks>
		/// 例)
		///		if (GlExtensions.Instance.IsAvaliable(GL_ARB_texture_non_power_of_two))
		///			....
		/// </remarks>
		/// <param name="extName"></param>
		/// <returns></returns>
		public bool IsAvailable(string extName)
		{
			return extlist.ContainsKey(extName);
		}

		/// <summary>
		/// NPOT(GL_ARB_texture_non_power_of_two)が使えるかどうかのフラグ。
		/// GeForce系では遅くならないが、
		/// Radeon系(ATI)では遅くなって使い物にならないらしい。
		/// なので、ATIならば、NPOTを使わないようにする。
		/// 他にも遅くなって使い物にならないベンダーがあれば登録します。
		/// </summary>
		public bool NPOT
		{
			get
			{
				if (!npot.HasValue)
					CheckNpot();

				return npot.Value;
			}
		}
		private bool? npot = false;

		/// <summary>
		/// NPOTが使えるかどうか。
		/// </summary>
		private void CheckNpot()
		{
			{
				bool npot = IsAvailable("GL_ARB_texture_non_power_of_two");

				// ATI系だと大変遅いので、ATIの場合、使えないとして扱う。

				string vendor = Gl.glGetString(Gl.GL_VENDOR);

				// ATIならば"ATI Technologies Inc."という文字列が戻るのだけども
				// サードパーティ製ドライバのこともあるので小文字化して、
				// Containsで比較すべき。
				if (vendor.ToLower().Contains("ati technologies"))
				// ↑ATI以外にも糞ドライバがあればこの条件に追加してちょうだい。
				{
					npot = false;
				}

				this.npot = npot;
			}
		}

		/// <summary>
		/// GL_XXX_texture_rectangleが使えるかどうかの判定。
		/// </summary>
		/// <param name="width">作成するテクスチャの横幅</param>
		/// <param name="height">作成するテクスチャの縦幅</param>
		public bool IsAvailableRectTexture(int width, int height)
		{
			if (isAvailableRectTexture == null)
			{
				isAvailableRectTexture = 
					IsAvailable("GL_NV_texture_rectangle")
				||	IsAvailable("GL_EXT_texture_rectangle")
				||	IsAvailable("GL_ARB_texture_rectangle");

				// この３つは、定数の値や使い方が同じなので、ひとまとめに扱って
				// 構わない。

				// 利用可能であるならば、最大矩形を得ておく。
				if (isAvailableRectTexture.Value)
				{
					int max;
					unsafe
					{
						Gl.glGetIntegerv(Gl.GL_MAX_RECTANGLE_TEXTURE_SIZE_ARB,
							new IntPtr(&max));
					}
					rectTextureMax = max;
				}
			}

			// texture_rectangleに対応していなければ
			if (! isAvailableRectTexture.Value)
				return false;
			
			// 作成するサイズが大きすぎたらfalse
			return (width <= rectTextureMax && height <= rectTextureMax);
		}

		/// <summary>
		///		GL_NV_texture_rectangle
		///		GL_EXT_texture_rectangle
		///		GL_ARB_texture_rectangle
		/// のいずれかに対応しているか。
		/// </summary>
		/// <remarks>
		/// 未調査ならばnull
		/// </remarks>
		private bool? isAvailableRectTexture = null;

		/// <summary>
		/// texture_rectangleの最大値
		/// </summary>
		private int rectTextureMax = 0;

		/// <summary>
		/// GL Extensionの名前リスト
		/// </summary>
		private Dictionary<string, object> extlist = new Dictionary<string,object>();
	}
}
