using System;
using System.Collections.Generic;
using System.Text;

namespace vtpk2mbtiles {


	public interface IOutput : IDisposable {

		public bool Write(TileId tid, byte[] data);
	}
}
