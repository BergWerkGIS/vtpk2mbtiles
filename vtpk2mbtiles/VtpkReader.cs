using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;



namespace vtpk2mbtiles {

	public class VtpkReader : IDisposable {


		// in the first 'root.json': resourceinfo -> cacheinfo -> storageinfo -> packetsize
		public const int PACKET_SIZE = 128;
		private const int HEADER_SIZE = 64;
		private const int TILE_INDEX_SIZE_INFO = 8;

		private bool _disposed = false;
		private BinaryReader _bundleReader;
		private long _bundleRow;
		private long _bundleCol;
		private bool _unzip;
		private IOutput _outputWriter;


		public VtpkReader(string bundleFileName, bool unzip, IOutput outputWriter) {

			_unzip = unzip;
			_outputWriter = outputWriter;

			string bundleName = Path.GetFileNameWithoutExtension(bundleFileName);
			string rowTxt = bundleName.Substring(1, 4);
			string colTxt = bundleName.Substring(6, 4);
			_bundleRow = Convert.ToInt64(rowTxt, 16);
			_bundleCol = Convert.ToInt64(colTxt, 16);

			Console.WriteLine($"processing bundle row:{_bundleRow} col:{_bundleCol} {bundleFileName}");

			_bundleReader = new BinaryReader(File.OpenRead(bundleFileName));
		}


		#region IDisposable

		~VtpkReader() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources) {
			if (!_disposed) {
				if (disposeManagedResources) {
					if (null != _bundleReader) {
						_bundleReader.Close();
						_bundleReader.Dispose();
						_bundleReader = null;
					}
				}
			}
			_disposed = true;
		}

		#endregion


		public long BundleCol { get { return _bundleCol; } }
		public long BundleRow { get { return _bundleRow; } }

		public HashSet<TileId> FailedTiles { private set; get; } = new HashSet<TileId>();
		public long ProcessedTiles { private set; get; } = 0;


		public bool ReadTileIndex() {
			try {
				_bundleReader.BaseStream.Seek(64, 0);

				for (int i = 0; i < 128 * 128; i++) {
					byte[] tileIndex = _bundleReader.ReadBytes(8);
				}

				return true;
			}
			catch (Exception ex) {
				Console.WriteLine($"error reading tile index: {ex}");
				return false;
			}
		}

		public bool GetTiles(List<TileId> tileIds, CancelObject cancel) {
			try {

				FailedTiles = new HashSet<TileId>();
				ProcessedTiles = 0;

				foreach (TileId tid in tileIds) {

					if (cancel.UserCancelled) {
						Console.WriteLine($"breaking out of tile processing ...");
						return false;
					}

					if (!getTileOffsetAndSize(tid, out var offset, out var size)) {
						FailedTiles.Add(tid);
						continue;
					}

					_bundleReader.BaseStream.Position = offset;
					byte[] oneTile = _bundleReader.ReadBytes((int)size);

					if (null == oneTile || 0 == oneTile.Length) {
						Console.WriteLine($"error: {tid} no tile data read. offset:{offset} size:{size}");
						FailedTiles.Add(tid);
						continue;
					}

					if (_unzip) {
						byte[] decompressed = Compression.Decompress(oneTile);
						if (null == decompressed || oneTile.Length == decompressed.Length) {
							Console.WriteLine($"error: {tid} could not be decompressd.  offset:{offset} size:{size}");
							FailedTiles.Add(tid);
							continue;
						}
						oneTile = decompressed;
					}

					if (!_outputWriter.Write(tid, oneTile)) {
						FailedTiles.Add(tid);
						continue;
					}

					ProcessedTiles++;
				}

				return true;
			}
			catch (Exception ex) {
				Console.WriteLine($"error getting tiles: {ex}");
				return false;
			}
		}


		private bool getTileOffsetAndSize(TileId tid, out long offset, out long size) {

			offset = -1;
			size = -1;

			try {

				long row = tid.y - _bundleRow;
				long col = tid.x - _bundleCol;
				long tileIndexOffset = HEADER_SIZE + TILE_INDEX_SIZE_INFO * (PACKET_SIZE * row + col);

				_bundleReader.BaseStream.Seek(tileIndexOffset, 0);
				byte[] rawBytes = _bundleReader.ReadBytes(8);
				long tileIndexValue = BitConverter.ToInt64(rawBytes);
				long tileOffset = tileIndexValue & ((1L << 40) - 1L);
				long tileSize = (tileIndexValue >> 40) & ((1 << 20) - 1);

				_bundleReader.BaseStream.Seek(tileOffset - 4, 0);
				byte[] sizeBytes = _bundleReader.ReadBytes(4);
				try {
					long tileSize2 = BitConverter.ToUInt32(sizeBytes);

					if (tileSize != tileSize2) {
						Console.WriteLine($"{tid}, relative row:{row} relative col:{col} tileIndexOffset: {tileIndexOffset} / {tileIndexOffset:X}");
						Console.WriteLine($"{tid}, tileDataOffset:{tileOffset} / {tileOffset:X} tileSize:{tileSize} tileSize2:{tileSize2}");
						Console.WriteLine("                                       tilesizes don't match ^^^^");
					}
				}
				catch (Exception ex) {
					Console.WriteLine($"{tid}, error calculating tile size via method2{Environment.NewLine}{ex}");
				}

				offset = tileOffset;
				size = tileSize;

				return true;
			}
			catch (Exception ex) {
				Console.WriteLine($"{tid} error getting offset and size in bundle:{Environment.NewLine}{ex}");
				return false;
			}
		}

	}
}
