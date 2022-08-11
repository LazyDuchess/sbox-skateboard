using Sandbox;
using System;

namespace Skateboard.Utils
{
	public static class InputLD
	{
		public const float kDeadzone = 0.75f;
		public static float DigitalForwardInput
		{
			get
			{
				if ( Math.Abs( Input.Forward ) <= kDeadzone )
					return 0f;
				return Math.Sign( Input.Forward );
			}
		}
		public static float DigitalLeftInput
		{
			get
			{
				if ( Math.Abs( Input.Left ) <= kDeadzone )
					return 0f;
				return Math.Sign( Input.Left );
			}
		}
		public static bool ForwardDown
		{
			get
			{
				return DigitalForwardInput > 0f;
			}
		}
		public static bool BackDown
		{
			get
			{
				return DigitalForwardInput < 0f;
			}
		}
	}
}
