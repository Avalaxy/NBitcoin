﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class DiskBlockPosRange
	{
		private static DiskBlockPosRange _All = new DiskBlockPosRange(DiskBlockPos.Begin, DiskBlockPos.End);

		public static DiskBlockPosRange All
		{
			get
			{
				return DiskBlockPosRange._All;
			}
		}

		/// <summary>
		/// Represent a disk block range
		/// </summary>
		/// <param name="begin">Beginning of the range (included)</param>
		/// <param name="end">End of the range (excluded)</param>
		public DiskBlockPosRange(DiskBlockPos begin = null, DiskBlockPos end = null)
		{
			if(begin == null)
				begin = DiskBlockPos.Begin;
			if(end == null)
				end = DiskBlockPos.End;
			_Begin = begin;
			_End = end;
			if(end <= begin)
				throw new ArgumentException("End should be more than begin");
		}
		private readonly DiskBlockPos _Begin;
		public DiskBlockPos Begin
		{
			get
			{
				return _Begin;
			}
		}
		private readonly DiskBlockPos _End;
		public DiskBlockPos End
		{
			get
			{
				return _End;
			}
		}

		public bool InRange(DiskBlockPos pos)
		{
			return Begin <= pos && pos < End;
		}
		public override string ToString()
		{
			return Begin + " <= x < " + End;
		}
	}
	public class DiskBlockPos : IBitcoinSerializable
	{
		private static DiskBlockPos _Begin = new DiskBlockPos(0, 0);

		public static DiskBlockPos Begin
		{
			get
			{
				return DiskBlockPos._Begin;
			}
		}

		private static DiskBlockPos _End = new DiskBlockPos(uint.MaxValue, uint.MaxValue);
		public static DiskBlockPos End
		{
			get
			{
				return DiskBlockPos._End;
			}
		}

		public DiskBlockPos()
		{

		}
		public DiskBlockPos(uint file, uint position)
		{
			_File = file;
			_Position = position;
			UpdateHash();
		}
		private uint _File;
		public uint File
		{
			get
			{
				return _File;
			}
		}
		private uint _Position;
		public uint Position
		{
			get
			{
				return _Position;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWriteAsCompactVarInt(ref _File);
			stream.ReadWriteAsCompactVarInt(ref _Position);
			if(!stream.Serializing)
				UpdateHash();
		}

		private void UpdateHash()
		{
			_Hash = ToString().GetHashCode();
		}

		int _Hash;

		#endregion

		public override bool Equals(object obj)
		{
			DiskBlockPos item = obj as DiskBlockPos;
			if(item == null)
				return false;
			return _Hash.Equals(item._Hash);
		}
		public static bool operator ==(DiskBlockPos a, DiskBlockPos b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._Hash == b._Hash;
		}

		public static bool operator !=(DiskBlockPos a, DiskBlockPos b)
		{
			return !(a == b);
		}

		public static bool operator <(DiskBlockPos a, DiskBlockPos b)
		{
			if(a.File < b.File)
				return true;
			if(a.File == b.File && a.Position < b.Position)
				return true;
			return false;
		}
		public static bool operator <=(DiskBlockPos a, DiskBlockPos b)
		{
			return a == b || a < b;
		}
		public static bool operator >(DiskBlockPos a, DiskBlockPos b)
		{
			if(a.File > b.File)
				return true;
			if(a.File == b.File && a.Position > b.Position)
				return true;
			return false;
		}
		public static bool operator >=(DiskBlockPos a, DiskBlockPos b)
		{
			return a == b || a > b;
		}
		public override int GetHashCode()
		{
			return _Hash.GetHashCode();
		}


		public DiskBlockPos OfFile(uint file)
		{
			return new DiskBlockPos(file, Position);
		}

		public override string ToString()
		{
			return "f:" + File + "p:" + Position;
		}

		static readonly Regex _Reg = new Regex("f:([0-9]*)p:([0-9]*)", RegexOptions.Compiled);
		public static DiskBlockPos Parse(string data)
		{
			var match = _Reg.Match(data);
			if(!match.Success)
				throw new FormatException("Invalid position string : " + data);
			return new DiskBlockPos(uint.Parse(match.Groups[1].Value), uint.Parse(match.Groups[2].Value));
		}
	}
	public class StoredBlock : StoredItem<Block>
	{
		public bool ParseSkipBlockContent
		{
			get;
			set;
		}

		public StoredBlock(DiskBlockPos position)
			: base(position)
		{
		}
		public StoredBlock(uint magic, Block block, DiskBlockPos blockPosition)
			: base(magic, block, blockPosition)
		{
		}


		#region IBitcoinSerializable Members


		protected override void ReadWriteItem(BitcoinStream stream, ref Block item)
		{
			if(!ParseSkipBlockContent)
				stream.ReadWrite(ref item);
			else
			{
				var beforeReading = stream.Inner.Position;
				BlockHeader header = item == null ? null : item.Header;
				stream.ReadWrite(ref header);
				if(!stream.Serializing)
					item = new Block(header);
				stream.Inner.Position = beforeReading + Header.ItemSize;
			}
		}


		#endregion

		public static IEnumerable<StoredBlock> EnumerateFile(string file, uint fileIndex=0, DiskBlockPosRange range =null)
		{
			return new BlockStore(Path.GetDirectoryName(file), Network.Main).EnumerateFile(file, fileIndex, range);
		}

		public static IEnumerable<StoredBlock> EnumerateFolder(string folder, DiskBlockPosRange range =null)
		{
			return new BlockStore(folder, Network.Main).EnumerateFolder(range);
		}
	}
}