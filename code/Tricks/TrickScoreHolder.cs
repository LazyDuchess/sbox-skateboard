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
		[Net] private bool _failed { get; set; } = false;
		[Net] private bool _finished { get; set; } = false;
		private int maxTricks = 5;

		[Net] public int TotalScore { get; set; }
		[Net] public List<TrickScoreEntry> Entries { get; set; }
		public bool Failed
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
		public bool Finished
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
		public bool VisuallyEmpty
		{
			get
			{
				if ( Empty )
					return true;
				if ( String == "" )
					return true;
				return false;
			}
		}
		/*
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
				var entriesToDisplay = new List<TrickScoreEntry>();
				var n = 0;
				for (var i=Entries.Count-1;i>=0;i-- )
				{
					var entry = Entries[i];
					var entryName = Entries[i].Name;
					if (entryName != "")
					{
						entriesToDisplay.Insert( 0, entry );
					}
					n++;
					if ( n >= maxTricks )
						break;
				}
				for (var i=0;i<entriesToDisplay.Count;i++ )
				{
					resultString += entriesToDisplay[i].Name;
					if ( i < entriesToDisplay.Count - 1 )
						resultString += " + ";
				}
				return resultString;
			}
		}*/
		[Net] public int Multiplier { get; set; } = 0;
		[Net] public int Score { get; set; } = 0;
		[Net] public string String { get; set; } = "";
		public void Reset()
		{
			Finished = false;
			Entries.Clear();
			Refresh();
		}
		void Refresh()
		{
			var resultString = "";
			var entriesToDisplay = new List<TrickScoreEntry>();
			var n = 0;
			for ( var i = Entries.Count - 1; i >= 0; i-- )
			{
				var entry = Entries[i];
				var entryName = Entries[i].Name;
				if ( entryName != "" )
				{
					entriesToDisplay.Insert( 0, entry );
				}
				n++;
				if ( n >= maxTricks )
					break;
			}
			for ( var i = 0; i < entriesToDisplay.Count; i++ )
			{
				resultString += entriesToDisplay[i].Name;
				if ( i < entriesToDisplay.Count - 1 )
					resultString += " + ";
			}
			String = resultString;
			var result = 0;
			foreach ( var element in Entries )
			{
				result += element.Score;
			}
			Score = result;
			result = 0;
			foreach ( var element in Entries )
			{
				result += element.Multiplier;
			}
			Multiplier = result;
		}
		public void Add(TrickScoreEntry entry)
		{
			if ( Local.Pawn != null && Local.Pawn.IsClient )
				return;
			if ( _finished )
				Reset();
			_finished = false;
			Entries.Add( entry );
			Refresh();
		}
	}
}
