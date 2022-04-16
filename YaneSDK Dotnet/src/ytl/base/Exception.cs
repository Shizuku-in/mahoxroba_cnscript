using System;
using SRS = System.Runtime.Serialization;

namespace Yanesdk.Ytl
{
	/// <summary>
	/// 例外発生時に使用するクラス。
	/// </summary>
	/// <example>
	/// エラーの時に投げる例外
	///	例)
	///	<code>
    ///		throw new YanesdkException(this,"エラーだぴょん");
	/// </code>
	/// </example>
    [Serializable]
	public class YanesdkException : Exception
	{
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public YanesdkException() { }
        public YanesdkException(string message, Exception innerException) : base(message, innerException) { }
        protected YanesdkException(SRS.SerializationInfo info, SRS.StreamingContext context) { }

		/// <summary>
		/// 文字列をそのまま例外として投げる
		/// </summary>
		/// <param name="msg"></param>
		public YanesdkException(string msg) 
			: base(msg) {}
		/// <summary>
        /// 文字列をそのまま例外として投げる
		/// </summary>
		/// <param name="o"></param>
		/// <param name="msg"></param>
		public YanesdkException(object o, string msg) 
			: base(o.GetType().Name + ":" + msg) {}

		/// <summary>
        /// YanesdkResultをそのままエラーメッセージとするタイプ。
		/// </summary>
		/// <param name="msg"></param>
        public YanesdkException(YanesdkResult msg) 
			: this(msg.ToString()) {}

		/// <summary>
        /// 例外の発生したオブジェクトとYanesdkResultをそのままエラーメッセージとするタイプ。
		/// </summary>
		/// <param name="o"></param>
		/// <param name="msg"></param>
        public YanesdkException(object o, YanesdkResult msg) 
			: this(o, msg.ToString()) {}
	}
}
