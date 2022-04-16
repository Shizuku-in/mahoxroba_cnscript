using System;

namespace Yanesdk.Math {
	/// <summary>
	/// 3次元vectorライブラリ
	/// </summary>
	public struct Vector3D {
		/// <summary>
		/// 保持しているベクター(x,y,z)。
		/// </summary>
		public double X,Y,Z;

		/// <summary>
		/// 変数x,yを初期化するコンストラクタ。
		/// </summary>
		/// <param name="x_"></param>
		/// <param name="y_"></param>
		/// <param name="z_"></param>
		public Vector3D(double x_,double y_,double z_) {
			X = x_; Y = y_; Z = z_; }

		/// <summary>
		/// 他のvectorで初期化するためのコンストラクタ。
		/// </summary>
		/// <param name="v"></param>
		public Vector3D(Vector3D v) {
			X = v.X; Y = v.Y; Z = v.Z; }

		/// <summary>
		/// (x,y,z)を設定するsetter。
		/// </summary>
		/// <param name="x_"></param>
		/// <param name="y_"></param>
		/// <param name="z_"></param>
		public void SetVector(double x_,double y_,double z_) {
			X = x_; Y = y_; Z = z_; }

		/// <summary>
		/// 内積を求める。
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public double InnerProduct(Vector3D v) {
			return X * v.X + Y * v.Y + Z * v.Z;
		}

		/// <summary>
		/// 外積を求める(3次元の外積の結果はベクトル)。
		/// </summary>
		/// <param name="v"></param>
		public void OuterProduct(Vector3D v) {
			double x_,y_,z_;
			x_ = Y * v.Z - Z * v.Y;
			y_ = Z * v.X - X * v.Z;
			z_ = X * v.Y - Y * v.X;
			SetVector(x_,y_,z_);
		}

		/// <summary>
		/// 他のベクトルを加算する。
		/// </summary>
		/// <param name="v"></param>
		public void Add(Vector3D v) {
			X += v.X;
			Y += v.Y;
			Z += v.Z;
		}

		/// <summary>
		/// 他のベクトルを減算する。
		/// </summary>
		/// <param name="v"></param>
		public void Sub(Vector3D v) {
			X -= v.X;
			Y -= v.Y;
			Z -= v.Z;
		}

		/// <summary>
		/// スカラー値を乗算する。
		/// </summary>
		/// <param name="a"></param>
		public void Mul(double a) {
			X *= a;
			Y *= a;
			Z *= a;
		}

		/// <summary>
		/// スカラー値で除算する。
		/// </summary>
		/// <param name="a"></param>
		public void Div(double a) {
			X /= a;
			Y /= a;
			Z /= a;
		}

		/// <summary>
		/// 絶対値を求める(sqrtが入るので遅い)。
		/// </summary>
		/// <returns></returns>
		public double Size {
			get { return global::System.Math.Sqrt(X * X + Y * Y + Z * Z); }
		}

		/// <summary>
		/// 絶対値の二乗を求める(大小比較をする目的ならこちらで十分)。
		/// </summary>
		/// <returns></returns>
		public double SizeSquare {
			get { return X * X + Y * Y + Z * Z; }
		}

		/// <summary>
		/// 近似による距離(絶対値)の算出。sqrtを使っていない分だけ速い。
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public double Distance(Vector3D v) {
			double ax = global::System.Math.Abs(X - v.X);
			double ay = global::System.Math.Abs(Y - v.Y);
			double az = global::System.Math.Abs(Z - v.Z);

			//	一番長い距離 + 二つ目の/2 + ３つ目の/2ぐらいでいいんじゃ?
			if ( ax > ay ) {
				if ( ax > az )
					return ax + ay / 2 + az / 2;
			} else {
				if ( ay > az )
					return ay + ax / 2 + az / 2;
			}
			return az + ax / 2 + ay / 2;
		}

		//	以下は、各種オペレータ

		/// <summary>
		/// + 和。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector3D operator+(Vector3D a, Vector3D b) {
			a.Add(b); return a;}

		/// <summary>
		/// - 差。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector3D operator-(Vector3D a, Vector3D b) {
			a.Sub(b); return a;}

		/// <summary>
		/// * 内積。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static double operator*(Vector3D a, Vector3D b) {
			return a.InnerProduct(b); }

		/// <summary>
		/// / 外積。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector3D operator/(Vector3D a, Vector3D b) {
			a.OuterProduct(b); return a;}

		/// <summary>
		/// * 定数倍。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector3D operator*(Vector3D a, double b) {
			a.Mul(b); return a;}

		/// <summary>
		/// / 定数割。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector3D operator/(Vector3D a, double b) {
			a.Div(b); return a;}

		/// <summary>
		/// operator ==
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator==(Vector3D a, Vector3D b) { return (a.X==b.X) && (a.Y==b.Y) && (a.Z==b.Z); }
		/// <summary>
		/// operator !=
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator!=(Vector3D a, Vector3D b) { return (a.X!=b.X) || (a.Y!=b.Y) || (a.Z!=b.Z); }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			Vector3D v = (Vector3D) obj;
			return (X==v.X) && (Y==v.Y) && (Z==v.Z);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return "(" + X + "," + Y + "," + Z + ")";
		}

		/// <summary>
		/// デバッグ用に標準出力に値を表示する→「(5,10,15)」のように出力される。
		/// </summary>
		void output() {
			Console.Write(ToString());
		}
	}
}
