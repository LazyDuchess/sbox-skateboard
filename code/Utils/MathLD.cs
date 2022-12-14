using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skateboard.Utils
{
	public static class MathLD
	{
		public static float CalcShortestRot( float from, float to )
		{
			var max = 180f;
			// If from or to is a negative, we have to recalculate them.
			// For an example, if from = -45 then from(-45) + 360 = 315.
			if ( from < 0 )
			{
				from += max;
			}

			if ( to < 0 )
			{
				to += max;
			}

			// Do not rotate if from == to.
			if ( from == to ||
			   from == 0 && to == max ||
			   from == max && to == 0 )
			{
				return 0;
			}

			// Pre-calculate left and right.
			float left = (360 - from) + to;
			float right = from - to;
			// If from < to, re-calculate left and right.
			if ( from < to )
			{
				if ( to > 0 )
				{
					left = to - from;
					right = (max - to) + from;
				}
				else
				{
					left = (max - to) + from;
					right = to - from;
				}
			}

			// Determine the shortest direction.
			return ((left <= right) ? left : (right * -1));
		}
		//I'm a unity slut
		public static Rotation FromToRotation( Vector3 fromDirection, Vector3 toDirection )
		{
			Vector3 axis = Vector3.Cross( fromDirection, toDirection );
			float angle = Vector3.GetAngle( fromDirection, toDirection );
			if ( angle >= 179.9196f )
			{
				var r = Vector3.Cross( fromDirection, Vector3.Right );
				axis = Vector3.Cross( r, fromDirection );
				if ( axis.LengthSquared < 0.000001f )
					axis = Vector3.Up;
			}
			return AngleAxis( angle, axis.Normal );
			//return RotateTowards( Rotation.LookAt( fromDirection ), Rotation.LookAt( toDirection ), float.MaxValue );
		}

		public static Rotation AngleAxis( float aAngle, Vector3 aAxis )
		{
			aAxis = aAxis.Normal;
			float rad = aAngle * (float)(Math.PI / 180.0) * 0.5f;
			aAxis *= (float)Math.Sin( rad );
			return new Rotation( aAxis.x, aAxis.y, aAxis.z, (float)Math.Cos( rad ) );
		}

		public struct NearestPoint
		{
			public Vector3 Point;
			public Vector3 Direction;
			public float Fraction;
			public bool Outside;
		}

		public static NearestPoint NearestPointOnLine( Vector3 point, Vector3 lineStart, Vector3 lineEnd, float threshold = 0f )
		{
			var lineDirection = (lineEnd - lineStart).Normal;
			var v = point - lineStart;
			var d = Vector3.Dot( v, lineDirection );
			var len = Vector3.DistanceBetween( lineStart, lineEnd );
			var outside = false;
			if ( d < threshold || d > len-threshold )
				outside = true;
			d = Math.Min( len, Math.Max( 0f, d ) );
			return new NearestPoint()
			{
				Point = lineStart + lineDirection * d,
				Direction = lineDirection,
				Fraction = d / len,
				Outside = outside
			};
		}
		/*
		//linePnt - point the line passes through
		//lineDir - unit vector in direction of line, either direction works
		//pnt - the point to find nearest on line for
		public static Vector3 NearestPointOnLine( Vector3 linePnt, Vector3 lineDir, Vector3 pnt )
		{
			lineDir.Normalize();//this needs to be a unit vector
			var v = pnt - linePnt;
			var d = Vector3.Dot( v, lineDir );
			return linePnt + lineDir * d;
		}*/
	}
}
