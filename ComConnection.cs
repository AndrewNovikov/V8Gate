using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace V8Gate {

	internal class ComConnection : IDisposable {
	
		private object m_comObject;
		private bool m_disposed = false;

		public ComConnection(object o) {
			this.m_comObject = null;
			this.m_comObject = o;
		}

		[System.Diagnostics.DebuggerHidden]
		public object comObject {
			get {
				return this.m_comObject;
			}
		}

    public TimeSpan TimeOut { get; set; }
    public bool IsDirty { get; set; }

		protected virtual void Dispose(bool disposing) {
			if(!m_disposed){
				if (disposing){
					//пусто, нет управляемых компонент
				}
				V8A.ReleaseComObject(this.m_comObject);
				this.m_comObject = null;
			}
			m_disposed = true;
		}

		public virtual void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		~ComConnection() {
			this.Dispose(false);
		}
	}
}
