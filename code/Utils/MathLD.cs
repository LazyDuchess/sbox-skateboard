using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skateboard.Utils
{
	public static class MathLD
	{
		//I'm a unity slut
		//TODO: Clean up ffs
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
	}
}
