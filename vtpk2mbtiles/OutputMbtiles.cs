﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using static System.FormattableString;


namespace vtpk2mbtiles {


	public class OutputMbtiles : IOutput {


		private string _dbFile;
		private static SQLiteConnection _conn;
		private bool _disposed;


		public OutputMbtiles(string destinationMbtiles, MetaData md) {

			if (File.Exists(destinationMbtiles)) {
				throw new Exception($"destination already exists: [{destinationMbtiles}]");
			}

			_dbFile = Path.GetFullPath(destinationMbtiles);
			string connStr = $"Data Source={_dbFile};";
			_conn = new SQLiteConnection(connStr);
			_conn.Open();

			executeCmd("PRAGMA synchronous=OFF");
			executeCmd("PRAGMA count_changes=OFF");
			executeCmd("PRAGMA journal_mode=MEMORY");
			executeCmd("PRAGMA temp_store=MEMORY");

			string schema;
			if (File.Exists("schema.sql")) {
				schema = File.ReadAllText("schema.sql");
			} else {
				Assembly assembly = Assembly.GetExecutingAssembly();
				using (TextReader tr = new StreamReader(assembly.GetManifestResourceStream("vtpk2mbtiles.schema.sql"))) {
					schema = tr.ReadToEnd();
				}
			}
			if (string.IsNullOrWhiteSpace(schema)) {
				throw new Exception("mbtiles schema not found");
			}

			executeCmd(schema);

			executeCmd(string.Join(
				"; "
				, $"INSERT INTO metadata (name, value) VALUES ('name', '{md.Name}');"
				, $"INSERT INTO metadata (name, value) VALUES ('description', 'Created with vtpk2mbtiles by BergWerk GIS - https://github.com/BergWerkGIS/vtpk2mbtiles');"
				, $"INSERT INTO metadata (name, value) VALUES ('bounds', '{md.Bounds()}');"
				, $"INSERT INTO metadata (name, value) VALUES ('center', '{md.Center()}');"
				, $"INSERT INTO metadata (name, value) VALUES ('minzoom', '{md.MinZoom}');"
				, $"INSERT INTO metadata (name, value) VALUES ('maxzoom', '{md.MaxZoom}');"
				, $"INSERT INTO metadata (name, value) VALUES ('json', '{md.VectorLayers}');"
				, "INSERT INTO metadata (name, value) VALUES ('format', 'pbf');"
			));
		}


		~OutputMbtiles() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources) {
			if (!_disposed) {
				if (disposeManagedResources) {
					closeDb();
				}
				_disposed = true;
			}
		}


		public bool Write(TileId tid, byte[] data) {
			try {

				using (SQLiteCommand cmdInsert = _conn.CreateCommand()) {
					cmdInsert.CommandType = CommandType.Text;
					string guid = Guid.NewGuid().ToString("N");
					cmdInsert.CommandText = string.Join("; ",
						$"INSERT INTO map (zoom_level, tile_column, tile_row, tile_id) VALUES ({tid.z}, {tid.x}, {tid.TmsY}, '{guid}')",
						$"INSERT INTO images (tile_data, tile_id) VALUES (@img, '{guid}');"
					);
					cmdInsert.Parameters.AddWithValue("@img", data);
					int rowsAffected = cmdInsert.ExecuteNonQuery();
					if (2 != rowsAffected) {
						Console.WriteLine($"unexpexted error inserting tile, rowsAffected[{rowsAffected}]: {tid}");
						return false;
					}
				}

				return true;
			}
			catch (Exception ex) {
				Console.WriteLine($"unexpected error writing tile {tid}{Environment.NewLine}{ex}");
				return false;
			}
		}


		private void closeDb() {
			try {
				double sizeBefore = getFileSize(_dbFile);
				Console.WriteLine("closing mbtiles ...");
				if (null != _conn) {
					// not necessary since we are just inserting
					//executeCmd("VACUUM;");
					_conn.Close();
					_conn.Dispose();
					_conn = null;
				}
				double sizeAfter = getFileSize(_dbFile);
				//Console.WriteLine(Invariant($"SIZE    : {sizeBefore:0.00}GB => {sizeAfter:0.00}GB"));
			}
			catch (Exception ex) {
				Console.WriteLine($"unexpected error closing mbtiles connection.{Environment.NewLine}{ex}");
			}
		}

		private double getFileSize(string fileName) {
			FileInfo fi = new FileInfo(fileName);
			return ((double)fi.Length / 1024.0d / 1024.0d / 1024.0d);
		}


		private bool executeCmd(string cmdSql) {

			try {

				using (SQLiteCommand cmd = _conn.CreateCommand()) {

					cmd.CommandType = CommandType.Text;
					cmd.CommandText = cmdSql.ToString();

					int rowsAffected = cmd.ExecuteNonQuery();
				}

				return true;
			}
			catch (Exception ex) {
				Console.WriteLine($"ERROR executing database command:{Environment.NewLine}{cmdSql}{Environment.NewLine}{ex}");
				return false;
			}
		}



	}
}
