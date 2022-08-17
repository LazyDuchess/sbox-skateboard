using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Skateboard.Tricks
{
	public partial class TrickScoreHolder : BaseNetworkable
	{
		private bool _failed = false;
		private bool _finished = false;
		private int maxTricks = 10;

		[Net] public int TotalScore { get; set; }
		[Net] public List<TrickScoreEntry> Entries { get; set; }
		[Net] public bool Failed
		{
			get
			{
				return _failed;
			}
			set
			{
				if ( _finished )
					return;
				_finished = value;
				_failed = value;
			}
		}
		[Net] public bool Finished
		{
			get
			{
				return _finished;
			}
			set
			{
				if (value && !_finished)
				{
					TotalScore = Score * Multiplier;
				}
				_finished = value;
				if ( !_finished )
					_failed = false;
			}
		}
		public bool Empty
		{
			get
			{
				return Entries.Count == 0;
			}
		}
		public int Multiplier
		{
			get
			{
				var result = 0;
				foreach(var element in Entries)
				{
					result += element.Multiplier;
				}
				return result;
			}
		}
		public int Score
		{
			get
			{
				var result = 0;
				foreach ( var element in Entries )
				{
					result += element.Score;
				}
				return result;
			}
		}
		public string String
		{
			get
			{
				var resultString = "";
				var amount = 0;
				for(var i=0;i<Entries.Count;i++ )
				{
					var last = false;
					if ( i == Entries.Count - 1 || i == maxTricks - 1 )
						last = true;
					resultString += Entries[i].Name;
					if ( !last )
						resultString += " + ";
					else
						return resultString;
				}
				return resultString;
			}
		}
		public void Reset()
		{
			Finished = false;
			Entries.Clear();
		}
		public void Add(TrickScoreEntry entry)
		{
			if ( _finished )
				Reset();
			_finished = false;
			Entries.Add( entry );
		}
	}
}
