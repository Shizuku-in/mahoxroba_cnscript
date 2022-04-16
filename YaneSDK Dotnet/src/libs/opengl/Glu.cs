using System;
using System.Runtime.InteropServices;

using GLenum = System.UInt32;
using GLboolean = System.Byte;
using GLbitfield = System.UInt32;
using GLbyte = System.SByte;
using GLshort = System.Int16;
using GLint = System.Int32;
using GLsizei = System.Int32;
using GLubyte = System.Byte;
using GLushort = System.UInt16;
using GLuint = System.UInt32;
using GLfloat = System.Single;
using GLclampf = System.Single;
using GLdouble = System.Double;
using GLclampd = System.Double;

namespace OpenGl
{
	/// <summary>
	/// 
	/// </summary>
	[CLSCompliant(false)]
	public class Glu
	{
//Linux環境ではAssemblyのmappingを利用して読み込む→SDLInitializer
//		const string DLL_GLU = "GLU";

		public const string DLL_GLU = "glu32";

		//	[DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
		//	public static extern GLubyte[] gluErrorString(GLenum errCode);

		// これ↑importした人が間違えてるんだなぁ…
		// .NETでは戻り値が配列のやつは↓のようにIntPtrで受け取って手動でマーシャリングしないといけない。
		// 直接stringで受け取ると、勝手に削除されてしまってまずいようだ。

		public static string gluErrorString(GLenum errCode)
		{
			return Marshal.PtrToStringAnsi(_gluErrorString(errCode));
		}
		public static string gluErrorUnicodeStringEXT(GLenum errCode)
		{
			return Marshal.PtrToStringUni(_gluErrorUnicodeStringEXT(errCode));
		}
		public static string gluGetString(GLenum name)
		{
			return Marshal.PtrToStringAnsi(_gluGetString(name));
		}

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall, EntryPoint = "gluErrorString")]
		private static extern IntPtr _gluErrorString(GLenum errCode);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall, EntryPoint = "gluErrorUnicodeStringEXT")]
		private static extern IntPtr _gluErrorUnicodeStringEXT(GLenum errCode);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall, EntryPoint = "gluGetString")]
		private static extern IntPtr _gluGetString(GLenum name);


		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluOrtho2D (
			GLdouble left,	
			GLdouble right, 
			GLdouble bottom,	
			GLdouble top);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluPerspective (
			GLdouble fovy,	
			GLdouble aspect,	
			GLdouble zNear, 
			GLdouble zFar);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluPickMatrix (
			GLdouble x, 
			GLdouble y, 
			GLdouble width, 
			GLdouble height,	
			GLint[]	viewport);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluLookAt (
			GLdouble eyex,	
			GLdouble eyey,	
			GLdouble eyez,	
			GLdouble centerx,	
			GLdouble centery,	
			GLdouble centerz,	
			GLdouble upx,	
			GLdouble upy,	
			GLdouble upz);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern int gluProject (
			GLdouble		objx,	
			GLdouble		objy,	
			GLdouble		objz,	
			GLdouble[]	modelMatrix,	
			GLdouble[]	projMatrix, 
			GLint[]		viewport,	
			ref GLdouble winx,	
			ref GLdouble winy,	
			ref GLdouble winz);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern int gluUnProject (
			GLdouble	   winx,	
			GLdouble	   winy,	
			GLdouble	   winz,	
			GLdouble[]   modelMatrix, 
			GLdouble[]   projMatrix,	
			GLint[]	   viewport,	
			ref GLdouble objx,	
			ref GLdouble objy,	
			ref GLdouble objz);


		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern int gluScaleImage (
			GLenum		format, 
			GLint		widthin,	
			GLint		heightin,	
			GLenum		typein, 
			IntPtr		datain,	
			GLint		widthout,	
			GLint		heightout,	
			GLenum		typeout,	
			IntPtr		dataout);


		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern int gluBuild1DMipmaps (
			GLenum		target, 
			GLint		components, 
			GLint		width,	
			GLenum		format, 
			GLenum		type,	
			IntPtr		data);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern int gluBuild2DMipmaps (
			GLenum		target, 
			GLint		components, 
			GLint		width,	
			GLint		height, 
			GLenum		format, 
			GLenum		type,	
			IntPtr		data);

		public struct GLUnurbs { }
		public struct GLUquadric { }
		public struct GLUtesselator { }

		/* backwards compatibility: */
		// alias GLUnurbs GLUnurbsObj;
		// alias GLUquadric GLUquadricObj;
		// alias GLUtesselator GLUtesselatorObj;
		// alias GLUtesselator GLUtriangulatorObj;

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern /* GLUquadric* */IntPtr gluNewQuadric ();

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluDeleteQuadric (/* GLUquadric */IntPtr state);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluQuadricNormals (/* GLUquadric* */IntPtr quadObject, GLenum	normals);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluQuadricTexture (/* GLUquadric* */IntPtr quadObject, GLboolean textureCoords);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluQuadricOrientation (/* GLUquadric* */IntPtr quadObject, GLenum	orientation);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluQuadricDrawStyle (/* GLUquadric * */IntPtr quadObject,	GLenum drawStyle);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluCylinder (
			/* GLUquadric* */IntPtr qobj,	
			GLdouble			baseRadius, 
			GLdouble			topRadius,	
			GLdouble			height, 
			GLint				slices, 
			GLint				stacks);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluDisk (
			/* GLUquadric* */IntPtr qobj,	
			GLdouble			innerRadius,	
			GLdouble			outerRadius,	
			GLint				slices, 
			GLint				loops);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluPartialDisk (
			/* GLUquadric* */IntPtr qobj,	
			GLdouble			innerRadius,	
			GLdouble			outerRadius,	
			GLint				slices, 
			GLint				loops,	
			GLdouble			startAngle, 
			GLdouble			sweepAngle);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluSphere (
			/* GLUquadric* */IntPtr qobj,	
			GLdouble			radius, 
			GLint				slices, 
			GLint				stacks);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluQuadricCallback (
			/* GLUquadric* */IntPtr qobj,	
			GLenum				which,	
			GLUCallbackProc		fn);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern /* GLUtesselator* */IntPtr gluNewTess();

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluDeleteTess(/* GLUtesselator* */IntPtr tess );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluTessBeginPolygon(
			/* GLUtesselator* */IntPtr tess, /* void* */IntPtr polygon_data );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluTessBeginContour(/* GLUtesselator* */IntPtr tess );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluTessVertex(		
			/* GLUtesselator* */IntPtr tess, GLdouble[]	coords, /* void	* */IntPtr data );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluTessEndContour(/* GLUtesselator * */IntPtr tess );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluTessEndPolygon(/* GLUtesselator* */IntPtr tess );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluTessProperty(		
			/* GLUtesselator* */IntPtr tess,
			GLenum				which,	
			GLdouble			value );
 
		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluTessNormal(		
			/* GLUtesselator* */IntPtr tess,	
			GLdouble			x,
			GLdouble			y,	
			GLdouble			z );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluTessCallback(		
			/* GLUtesselator* */IntPtr tess,
			GLenum				which,	
			GLUCallbackProc		fn);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void  gluGetTessProperty(	
			/* GLUtesselator* */IntPtr tess,
			GLenum				which,	
			ref GLdouble		value );
 
		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern /* GLUnurbs* */IntPtr gluNewNurbsRenderer ();

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluDeleteNurbsRenderer (/* GLUnurbs* */IntPtr nobj);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluBeginSurface (/* GLUnurbs* */IntPtr nobj);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluBeginCurve (/* GLUnurbs* */IntPtr nobj);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluEndCurve (/* GLUnurbs* */IntPtr nobj);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluEndSurface (/* GLUnurbs* */IntPtr nobj);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluBeginTrim (/* GLUnurbs* */IntPtr nobj);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluEndTrim (/* GLUnurbs* */IntPtr nobj);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluPwlCurve (
			/* GLUnurbs* */IntPtr nobj,	
			GLint				count,	
			/* GLfloat* */IntPtr array, 
			GLint				stride, 
			GLenum				type);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluNurbsCurve (
			/* GLUnurbs* */IntPtr nobj,	
			GLint				nknots, 
			/* GLfloat* */IntPtr knot,	
			GLint				stride, 
			/* GLfloat* */IntPtr ctlarray,	
			GLint				order,	
			GLenum				type);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void 
			gluNurbsSurface(		
			/* GLUnurbs* */IntPtr nobj,	
			GLint				sknot_count,	
			/* float* */IntPtr sknot, 
			GLint				tknot_count,	
			/* GLfloat* */IntPtr tknot, 
			GLint				s_stride,	
			GLint				t_stride,	
			/* GLfloat* */IntPtr ctlarray,	
			GLint				sorder, 
			GLint				torder, 
			GLenum				type);

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void 
			gluLoadSamplingMatrices (
			/* GLUnurbs* */IntPtr nobj,	
			GLfloat[] 	modelMatrix,	
			GLfloat[] 	projMatrix, 
			GLint[]		viewport );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void 
			gluNurbsProperty (
			/* GLUnurbs* */IntPtr nobj,	
			GLenum				property,	
			GLfloat 			value );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void 
			gluGetNurbsProperty (
			/* GLUnurbs* */IntPtr nobj,	
			GLenum				property,	
			ref GLfloat 		value );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluNurbsCallback (
			/* GLUnurbs* */IntPtr nobj,	
			GLenum				which,	
			GLUCallbackProc		fn);


		/****			 function prototypes	****/

		public delegate void GLUCallbackProc();

		/* gluQuadricCallback */
		public delegate void GLUquadricErrorProc(GLenum a);

		/* gluTessCallback */
		public delegate void GLUtessBeginProc(GLenum a);
		public delegate void GLUtessEdgeFlagProc(GLboolean a);
		public delegate void GLUtessVertexProc(IntPtr a);
		public delegate void GLUtessEndProc();
		public delegate void GLUtessErrorProc(GLenum a);
		public delegate void GLUtessCombineProc(GLdouble[/*3*/] a,
		/* void*[4] */IntPtr b, GLfloat[/*4*/] c,/* void** */IntPtr d);
		public delegate void GLUtessBeginDataProc(GLenum a, /* void * */IntPtr b);
		public delegate void GLUtessEdgeFlagDataProc(GLboolean a, /* void * */IntPtr b);
		public delegate void GLUtessVertexDataProc(/* void * */IntPtr a, /* void * */IntPtr b);
		public delegate void GLUtessEndDataProc(/* void * */IntPtr a);
		public delegate void GLUtessErrorDataProc(GLenum a, /* void * */IntPtr b);
		public delegate void GLUtessCombineDataProc(GLdouble[/*3*/] a, /*void*[4]*/IntPtr b, 
		GLfloat[/*4*/] c, /*void** */IntPtr d, /* void* */IntPtr e);

		/* gluNurbsCallback */
		public delegate void GLUnurbsErrorProc(GLenum a);

		/****			Generic constants				****/

		/* Version */
		public const int GLU_VERSION_1_1				= 1;
		public const int GLU_VERSION_1_2				= 1;

		/* Errors: (return value 0 = no error) */
		public const int GLU_INVALID_ENUM		= 100900;
		public const int GLU_INVALID_VALUE 	= 100901;
		public const int GLU_OUT_OF_MEMORY 	= 100902;
		public const int GLU_INCOMPATIBLE_GL_VERSION	= 100903;

		/* StringName */
		public const int GLU_VERSION			= 100800;
		public const int GLU_EXTENSIONS		= 100801;

		/* Boolean */
		public const int GLU_TRUE				= (int)Gl.GL_TRUE;
		public const int GLU_FALSE				= (int)Gl.GL_FALSE;


		/****			Quadric public constants				****/

		/* QuadricNormal */
		public const int GLU_SMOOTH			= 100000;
		public const int GLU_FLAT				= 100001;
		public const int GLU_NONE				= 100002;

		/* QuadricDrawStyle */
		public const int GLU_POINT 			= 100010;
		public const int GLU_LINE				= 100011;
		public const int GLU_FILL				= 100012;
		public const int GLU_SILHOUETTE		= 100013;

		/* QuadricOrientation */
		public const int GLU_OUTSIDE			= 100020;
		public const int GLU_INSIDE			= 100021;

		/*	types: */
		/*		GLU_ERROR				100103 */


		/****			Tesselation public constants			****/

		public const double GLU_TESS_MAX_COORD			 = 1.0e150;

		/* TessProperty */
		public const int GLU_TESS_WINDING_RULE 		= 100140;
		public const int GLU_TESS_BOUNDARY_ONLY		= 100141;
		public const int GLU_TESS_TOLERANCE			= 100142;

		/* TessWinding */
		public const int GLU_TESS_WINDING_ODD			= 100130;
		public const int GLU_TESS_WINDING_NONZERO		= 100131;
		public const int GLU_TESS_WINDING_POSITIVE 	= 100132;
		public const int GLU_TESS_WINDING_NEGATIVE 	= 100133;
		public const int GLU_TESS_WINDING_ABS_GEQ_TWO	= 100134;

		/* TessCallback */
		public const int GLU_TESS_BEGIN		= 100100;  /* void (*)(GLenum	 type)	*/
		public const int GLU_TESS_VERTEX		= 100101;  /* void (*)(void 	 *data) */
		public const int GLU_TESS_END			= 100102;  /* void (*)(void)			*/
		public const int GLU_TESS_ERROR		= 100103;  /* void (*)(GLenum	 errno) */
		public const int GLU_TESS_EDGE_FLAG	= 100104;  /* void (*)(GLboolean boundaryEdge)	*/
		public const int GLU_TESS_COMBINE		= 100105;  /* void (*)(GLdouble  coords[3],
															void	  *data[4],
															GLfloat   weight[4],
															void	  **dataOut)	 */
		public const int GLU_TESS_BEGIN_DATA	= 100106;  /* void (*)(GLenum	 type,	
															void	  *polygon_data) */
		public const int GLU_TESS_VERTEX_DATA	= 100107;  /* void (*)(void 	 *data, 
															void	  *polygon_data) */
		public const int GLU_TESS_END_DATA 	= 100108;  /* void (*)(void 	 *polygon_data) */
		public const int GLU_TESS_ERROR_DATA	= 100109;  /* void (*)(GLenum	 errno, 
															void	  *polygon_data) */
		public const int GLU_TESS_EDGE_FLAG_DATA = 100110;  /* void (*)(GLboolean boundaryEdge,
															void	  *polygon_data) */
		public const int GLU_TESS_COMBINE_DATA = 100111; /* void (*)(GLdouble	coords[3],
															void	  *data[4],
															GLfloat   weight[4],
															void	  **dataOut,
															void	  *polygon_data) */

		/* TessError */
		public const int GLU_TESS_ERROR1	= 100151;
		public const int GLU_TESS_ERROR2	= 100152;
		public const int GLU_TESS_ERROR3	= 100153;
		public const int GLU_TESS_ERROR4	= 100154;
		public const int GLU_TESS_ERROR5	= 100155;
		public const int GLU_TESS_ERROR6	= 100156;
		public const int GLU_TESS_ERROR7	= 100157;
		public const int GLU_TESS_ERROR8	= 100158;

		public const int GLU_TESS_MISSING_BEGIN_POLYGON = GLU_TESS_ERROR1;
		public const int GLU_TESS_MISSING_BEGIN_CONTOUR = GLU_TESS_ERROR2;
		public const int GLU_TESS_MISSING_END_POLYGON	 = GLU_TESS_ERROR3;
		public const int GLU_TESS_MISSING_END_CONTOUR	 = GLU_TESS_ERROR4;
		public const int GLU_TESS_COORD_TOO_LARGE		 = GLU_TESS_ERROR5;
		public const int GLU_TESS_NEED_COMBINE_CALLBACK = GLU_TESS_ERROR6;

		/****			NURBS public constants 				****/

		/* NurbsProperty */
		public const int GLU_AUTO_LOAD_MATRIX	 = 100200;
		public const int GLU_CULLING			 = 100201;
		public const int GLU_SAMPLING_TOLERANCE = 100203;
		public const int GLU_DISPLAY_MODE		 = 100204;
		public const int GLU_PARAMETRIC_TOLERANCE		= 100202;
		public const int GLU_SAMPLING_METHOD			= 100205;
		public const int GLU_U_STEP					= 100206;
		public const int GLU_V_STEP					= 100207;

		/* NurbsSampling */
		public const int GLU_PATH_LENGTH				= 100215;
		public const int GLU_PARAMETRIC_ERROR			= 100216;
		public const int GLU_DOMAIN_DISTANCE			= 100217;


		/* NurbsTrim */
		public const int GLU_MAP1_TRIM_2		= 100210;
		public const int GLU_MAP1_TRIM_3		= 100211;

		/* NurbsDisplay */
		/*		GLU_FILL				100012 */
		public const int GLU_OUTLINE_POLYGON	= 100240;
		public const int GLU_OUTLINE_PATCH 	= 100241;

		/* NurbsCallback */
		/*		GLU_ERROR				100103 */

		/* NurbsErrors */
		public const int GLU_NURBS_ERROR1		= 100251;
		public const int GLU_NURBS_ERROR2		= 100252;
		public const int GLU_NURBS_ERROR3		= 100253;
		public const int GLU_NURBS_ERROR4		= 100254;
		public const int GLU_NURBS_ERROR5		= 100255;
		public const int GLU_NURBS_ERROR6		= 100256;
		public const int GLU_NURBS_ERROR7		= 100257;
		public const int GLU_NURBS_ERROR8		= 100258;
		public const int GLU_NURBS_ERROR9		= 100259;
		public const int GLU_NURBS_ERROR10 	= 100260;
		public const int GLU_NURBS_ERROR11 	= 100261;
		public const int GLU_NURBS_ERROR12 	= 100262;
		public const int GLU_NURBS_ERROR13 	= 100263;
		public const int GLU_NURBS_ERROR14 	= 100264;
		public const int GLU_NURBS_ERROR15 	= 100265;
		public const int GLU_NURBS_ERROR16 	= 100266;
		public const int GLU_NURBS_ERROR17 	= 100267;
		public const int GLU_NURBS_ERROR18 	= 100268;
		public const int GLU_NURBS_ERROR19 	= 100269;
		public const int GLU_NURBS_ERROR20 	= 100270;
		public const int GLU_NURBS_ERROR21 	= 100271;
		public const int GLU_NURBS_ERROR22 	= 100272;
		public const int GLU_NURBS_ERROR23 	= 100273;
		public const int GLU_NURBS_ERROR24 	= 100274;
		public const int GLU_NURBS_ERROR25 	= 100275;
		public const int GLU_NURBS_ERROR26 	= 100276;
		public const int GLU_NURBS_ERROR27 	= 100277;
		public const int GLU_NURBS_ERROR28 	= 100278;
		public const int GLU_NURBS_ERROR29 	= 100279;
		public const int GLU_NURBS_ERROR30 	= 100280;
		public const int GLU_NURBS_ERROR31 	= 100281;
		public const int GLU_NURBS_ERROR32 	= 100282;
		public const int GLU_NURBS_ERROR33 	= 100283;
		public const int GLU_NURBS_ERROR34 	= 100284;
		public const int GLU_NURBS_ERROR35 	= 100285;
		public const int GLU_NURBS_ERROR36 	= 100286;
		public const int GLU_NURBS_ERROR37 	= 100287;

		/****			Backwards compatibility for old tesselator			 ****/

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluBeginPolygon(/* GLUtesselator * */IntPtr tess );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluNextContour(/* GLUtesselator * */IntPtr tess, GLenum type );

		[DllImport(DLL_GLU, CallingConvention = CallingConvention.StdCall)]
		public static extern void gluEndPolygon(/* GLUtesselator * */IntPtr tess);

		/* Contours types -- obsolete! */
		public const int GLU_CW		= 100120;
		public const int GLU_CCW		= 100121;
		public const int GLU_INTERIOR	= 100122;
		public const int GLU_EXTERIOR	= 100123;
		public const int GLU_UNKNOWN	= 100124;

		/* Names without "TESS_" prefix */
		public const int GLU_BEGIN 	= GLU_TESS_BEGIN;
		public const int GLU_VERTEX	= GLU_TESS_VERTEX;
		public const int GLU_END		= GLU_TESS_END;
		public const int GLU_ERROR 	= GLU_TESS_ERROR;
		public const int GLU_EDGE_FLAG = GLU_TESS_EDGE_FLAG;
	}
}

