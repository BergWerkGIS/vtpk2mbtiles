using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static System.FormattableString;


namespace vtpk2mbtiles {

	public class MetaData {

		public string Name { get; set; }
		public double FullExtXMin { get; set; }
		public double FullExtYMin { get; set; }
		public double FullExtXMax { get; set; }
		public double FullExtYMax { get; set; }

		public int MinZoom => 1;
		public int MaxZoom => 16; // wrong value '23' in the root.json

		public string VectorLayers { get; set; }

		public string Bounds() {
			return Invariant($"{mercX2lng(FullExtXMin):0.0},{mercY2lat(FullExtYMin):0.0},{mercX2lng(FullExtXMax):0.0},{mercY2lat(FullExtYMax):0.0}");
		}

		public string Center() {
			return Invariant($"{mercX2lng(((FullExtXMin + FullExtXMax) / 2.0d)):0.0},{mercY2lat(((FullExtYMin + FullExtYMax) / 2.0d)):0.0},8");
		}

		public override string ToString() {
			return string.Join(
				Environment.NewLine
				, Name
				, $"minzoom: {MinZoom}"
				, $"maxzoom: {MaxZoom}"
				, Bounds()
				, Center()
				, VectorLayers
			);
		}


		private double mercY2lat(double y) {
			return 180 * (2 * Math.Atan(Math.Exp(y / 6378137)) - (Math.PI / 2)) / Math.PI;
		}

		private double mercX2lng(double x) {
			return 180.0d * (x / 6378137.0d) / Math.PI;
		}
	}
}
