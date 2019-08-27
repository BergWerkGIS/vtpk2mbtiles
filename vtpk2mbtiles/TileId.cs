using System;
using System.Collections.Generic;
using System.Text;

namespace vtpk2mbtiles {
	public class TileId {
		public int z { get; set; }
		public long x { get; set; }
		public long y { get; set; }

		public long TmsY { get { return ((1 << z) - y - 1); } }

		public override string? ToString() {
			return $"z:{z} row/y:{y} col/x:{x}";
		}
	}
}
