using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace vtpk2mbtiles {


	public class OutputFiles : IOutput {


		private string _destDir;
		private bool _disposed;


		public OutputFiles(string destDir) {
			_destDir = destDir;
			if (!Directory.Exists(_destDir)) { Directory.CreateDirectory(_destDir); }
		}

		OutputFiles() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources) {
			if (!_disposed) {
				if (disposeManagedResources) { }
				_disposed = true;
			}
		}


		public bool Write(TileId tid, byte[] data) {
			try {

				string tileDir = Path.Combine(_destDir, $"{tid.z}", $"{tid.x}");
				if (!Directory.Exists(tileDir)) { Directory.CreateDirectory(tileDir); }
				string tilePath = Path.Combine(tileDir, $"{tid.y}.pbf");
				File.WriteAllBytes(tilePath, data);


				return true;
			}
			catch (Exception ex) {
				Console.WriteLine($"unexpected error writing tile {tid}{Environment.NewLine}{ex}");
				return false;
			}
		}

	}
}
