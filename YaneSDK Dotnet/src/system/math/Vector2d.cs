using System;

namespace Yanesdk.Math {
	/// <summary>
	/// 2次元vectorライブラリ
	/// </summary>
	public struct Vector2D
	{


		/// <summary>
		/// 保持しているベクター(x,y)
		/// </summary>
		public double X,Y;

		/// <summary>
		/// 変数x,yを初期化するコンストラクタ
		/// </summary>
		/// <param name="x_"></param>
		/// <param name="y_"></param>
		public Vector2D(double x_,double y_) {
			X = x_; Y = y_; }

		/// <summary>
		/// 他のvectorで初期化するためのコンストラクタ
		/// </summary>
		/// <param name="v"></param>
		public Vector2D(Vector2D v) {
			X = v.X; Y = v.Y; }

		/// <summary>
		/// (x,y)を設定するsetter
		/// </summary>
		/// <param name="x_"></param>
		/// <param name="y_"></param>
		public void SetVector(double x_,double y_) {
			X = x_; Y = y_; }

		/// <summary>
		/// 内積を求める
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public double InnerProduct(Vector2D v) {
			return X * v.X + Y * v.Y;
		}

		/// <summary>
		/// 外積を求める(2次元の外積の結果は定数)
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public double OuterProduct(Vector2D v) {
			return X * v.Y - Y * v.X;
		}

		/// <summary>
		/// 他のベクトルを加算する
		/// </summary>
		/// <param name="v"></param>
		public void Add(Vector2D v) {
			X += v.X;
			Y += v.Y;
		}

		/// <summary>
		/// 他のベクトルを減算する
		/// </summary>
		/// <param name="v"></param>
		public void Sub(Vector2D v) {
			X -= v.X;
			Y -= v.Y;
		}

		/// <summary>
		/// スカラー値を乗算する
		/// </summary>
		/// <param name="a"></param>
		public void Mul(double a) {
			X *= a;
			Y *= a;
		}

		/// <summary>
		/// スカラー値で除算する
		/// </summary>
		/// <param name="a"></param>
		public void Div(double a) {
			X /= a;
			Y /= a;
		}

		/// <summary>
		/// 絶対値を求める(sqrtが入るので遅い)
		/// </summary>
		/// <returns></returns>
		public double Size {
			get { return global::System.Math.Sqrt(X * X + Y * Y); }
		}

		/// <summary>
		/// 絶対値の二乗を求める(大小比較をする目的ならこちらで十分)
		/// </summary>
		/// <returns></returns>
		public double SizeSquare() {
			return X * X + Y * Y;
		}

		/// <summary>
		/// 近似による距離(絶対値)の算出。sqrtを使っていない分だけ速い
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public double Distance(Vector2D v) {
			double ax = global::System.Math.Abs(X - v.X);
			double ay = global::System.Math.Abs(Y - v.Y);
			if ( ax > ay ) {
				return ax + ay / 2;
			} else {
				return ay + ax / 2;
			}
		}

		#region	以下は、各種オペレータ

		/// <summary>
		/// +
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector2D operator+(Vector2D a, Vector2D b) {
			a.Add(b); return a;}

		/// <summary>
		/// -
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector2D operator-(Vector2D a, Vector2D b) {
			a.Sub(b); return a;}

		/// <summary>
		/// * 内積
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static double operator*(Vector2D a, Vector2D b) {
			return a.InnerProduct(b); }

		/// <summary>
		/// / 外積
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static double operator/(Vector2D a, Vector2D b) {
			return a.OuterProduct(b); }

		/// <summary>
		/// * 定数倍
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector2D operator*(Vector2D a, double b) {
			a.Mul(b); return a;}

		/// <summary>
		/// / 定数割
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector2D operator/(Vector2D a, double b) {
			a.Div(b); return a;}

		/// <summary>
		/// operator ==
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator==(Vector2D a, Vector2D b) { return (a.X==b.X) && (a.Y==b.Y); }

		/// <summary>
		/// operator != 
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator!=(Vector2D a, Vector2D b) { return (a.X!=b.X) || (a.Y!=b.Y); }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			Vector2D v = (Vector2D) obj;
			return (X==v.X) && (Y==v.Y);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode();
		}

		#endregion

		/// <summary>
		/// 「(5,10)」のようにベクトルを文字列化して返す
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return "(" + X + "," + Y + ")";
		}

		/// <summary>
		/// デバッグ用に標準出力に値を表示する→「(5,10)」のように出力される。
		/// </summary>
		void output() {
			Console.Write(ToString());
		}
	}
}
