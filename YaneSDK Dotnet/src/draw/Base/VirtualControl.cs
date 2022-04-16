using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// Control(System.Windows.Forms.Control)ベースでアプリを構築していくとき
	/// 子Control(親のControlのなかに貼り付けられたControl)は、HWndを持つため、
	/// DirectXやOpenGLで親Controlに描画しようとしたときにclippingされてしまう。
	/// (もちろん、子ControlのBGにtransparentを指定しても)
	/// 
	/// これでは、Controlベースのアプリが開発できない。そこで、子Controlは
	/// Visible = falseにしておき、子Controlの持つControlを親Controlに仮想的に
	/// 移動させてしまう。こうすることにより、あたかもControlを扱うかのような
	/// 感覚でIDEで編集することが出来、かつ、clippingもされないで済む。
	/// 
	/// このクラスは、それを実現するためのヘルパクラスである。
	/// </summary>
	/** <example>
	public partial class RoomConfigControl : UserControl , ITextureGUI , IDisposable
	{
		public RoomConfigControl(UserControl parentControl)
		{
			InitializeComponent();

			// 親コントロールに貼り付け。
			virtualControl.InitControl(parentControl , this);
		}

		public virtual void OnInit(ControlContext cc)
		{
			loader = cc.SmartTextureLoader.LoadDefFile("hoge/def.txt");
		}
		TextureLoader loader;
		VirtualControl virtualControl = new VirtualControl();

		public virtual void OnPaint(Yanesdk.Draw.Screen scr , ControlContext cc , int x , int y)
		{
			virtualControl.Location = new System.Drawing.Point(x , y);

			scr.Blt(loader.GetTexture(0) , x , y);
		}

		public new void Dispose()
		{
			virtualControl.Dispose();
			base.Dispose();
		}

	}
	 * </example>
	*/
	public class VirtualControl : IDisposable
	{

		public VirtualControl()
		{
			// Windows専用である
			if (System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);
		}

		/// <summary>
		/// 親のコントロールに子の持つコントロールを移動させる。
		/// このとき、VirtualControl内に移動させたコントロールを
		/// 記録しておき、あとで移動させたコントロールに対してのみ
		/// Visible = false / trueのようなことが出来る。
		/// 
		/// child.Visible = false; もおまけで行なう。
		/// /// </summary>
		/// <param name="parent"></param>
		/// <param name="child"></param>
		public void InitControl(Control parent , Control child)
		{
			Control.ControlCollection cc = child.Controls;
			list.Clear();
			visibleFlag.Clear();

			while ( cc.Count != 0 )
			{
				// Addすると、こちらのControlから自動的に除去される
				// これは実装上のバグか？
				// 念のため事前にRemoveしておく。
				Control c = cc[0];
				child.Controls.Remove(c);
				parent.Controls.Add(c);

				list.Add(c);
				visibleFlag.Add(true);
			}

			child.Visible = false;
			this.parent = parent;
			this.child = child;

			this.visible = true;
			
			this.Location = child.Location;
		}

		private Control parent;
		private Control child;

		/// <summary>
		/// Moveで移動させたので保持しているControls
		/// </summary>
		private List<Control> list = new List<Control>();
		private List<bool> visibleFlag = new List<bool>();

		// 仮想controlを描画するべき座標
		private global::System.Drawing.Point point;

		/// <summary>
		/// 仮想controlを描画すべき座標を親Control内で指定する
		/// </summary>
		public global::System.Drawing.Point Location
		{
			set
			{
				if ( value != point )
				{
					// 座標が違うので一大事。移動させるべし
					//	point - valueのぶんだけ相対で移動させる
				//	foreach ( Control c in parent.Controls )
				//	{
				//		if (list.Contains(c))
				//		{
					// これは我が突っ込んだControlである
					foreach ( Control c in list )
					{
							c.Location = new global::System.Drawing.Point(
								// value - point ぶんだけなのだけど
								// 減算operatorが設定されていない(´ω`)
									c.Location.X + value.X - point.X,
									c.Location.Y + value.Y - point.Y
								);
								// Location.Offsetだと、Pointがstructなので設定できない(´ω`)
								// しょぼぼーん	
					}
				//		}
				//	}


					point = value;
				}
			}
			get
			{
				return point;
			}
		}

		/// <summary>
		/// InitControlで親Controlに移動させたControlの可視/不可視を設定する。
		/// Visible = falseのあとtrueにした場合、falseにする前の各ControlのVisibleプロパティの値を
		/// 再現する。
		/// </summary>
		public bool Visible
		{
			get { return visible; }
			set
			{
				if ( visible != value )
				{
					if ( value )
					{
						// 可視にするのか
						for ( int i = 0 ; i < list.Count ; ++i )
						{
							Control c = list[i];
							c.Visible = visibleFlag[i];
						}
					}
					else
					{
						// 不可視にするのか
						for ( int i = 0 ; i < list.Count ; ++i )
						{
							Control c = list[i];
							visibleFlag[i] = c.Visible;
							c.Visible = false;
						}
					}

					visible = value;
				}
			}

		}
		private bool visible;

		/// <summary>
		/// InitControlで移動させたControlを親Controlから除去
		/// </summary>
		public void Dispose()
		{
			if ( parent != null )
			{
				foreach(Control c in list)
					parent.Controls.Remove(c);
			}
		}

	}
}
