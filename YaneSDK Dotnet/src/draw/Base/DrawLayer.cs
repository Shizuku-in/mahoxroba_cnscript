using System;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 描画Layerクラスの基底クラス。
	/// </summary>
	public interface IDrawLayer
	{
		///	<summary>(x,y)の座標に描画する。</summary>
		void OnDraw(Screen2DGl context,int x,int y);
	}
}
