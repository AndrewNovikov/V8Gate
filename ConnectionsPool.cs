using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

//using V8Gate;

namespace V8Gate {	 //комментарий 1

	//internal class Transaction {
	//  //public long ID { public get; private set; }
	//  ComConnection _con;
	//  TimeSpan _timeOut;

	//  public Transaction(ComConnection con, TimeSpan timeOut) {
	//    //ID = id;
	//    _con = con;
	//    _timeOut = timeOut;
	//  }

	//  //public override bool Equals(object obj) {
	//  //  Transaction _obj = obj as Transaction;
	//  //  return _obj != null && _obj.ID.Equals(ID);
	//  //}

	//  //public override int GetHashCode() {
	//  //  return ID.GetHashCode();
	//  //}
	//  //public class ID {
	//  //  int _lockID;
	//  //  int _id;

	//  //  public ID(int id) {
	//  //    _lockID = -1;
	//  //    _id = id;
	//  //  }

	//  //  public override bool Equals(object obj) {
	//  //    ID _obj = obj as ID;
	//  //    if (_obj == null) {
	//  //      return false;
	//  //    } else {
	//  //      return _id.Equals(_obj._id) && _lockID.Equals(_obj._lockID);
	//  //    }
	//  //  }
	//  //}
	//}

	internal class DbConnection: IDisposable {
		//private static V8.COMConnector conr = new V8.COMConnector();
		//private static object _initLock = new object();
		private ComConnection _connection;
		private DbConnections _parent;
		private bool _disposed = false;
		//Transaction _transaction;
		private Guid _transactionID;
		//private TimeSpan _timeOut;

		#region "Properties"
		public ComConnection Connection {
			[System.Diagnostics.DebuggerHidden]
			get {
				if (this._disposed) {
					throw new ObjectDisposedException("DbConnection");
				}
				return _connection;
			}
			[System.Diagnostics.DebuggerHidden]
			set {
				if (this._disposed) {
					throw new ObjectDisposedException("DbConnection");
				}
				_connection = value;
			}
		}
		#endregion

		#region "Constructors"
		internal DbConnection(DbConnections parent, Guid transactionID) {
			_parent = parent;
			_transactionID = transactionID;
			_connection = _parent.GetObjectFromPool(_transactionID);
			//_transactionID = transactionID;
			//_connection = _parent.GetObjectFromPool();
			////Trace.WriteLine("Ќачало DbConnection €-" + Thread.CurrentThread.Name);
			//V81.COMConnector conr = new V81.COMConnector();
			////conr.PoolTimeout = 1;
			////Trace.WriteLine("начало коннекта");
			//_connection = new ComObject(conr.Connect(aConnectionString));
			////Trace.WriteLine("конец коннекта");
			//Marshal.ReleaseComObject(conr);
			////Trace.WriteLine(" онец DbConnection €-" + Thread.CurrentThread.Name);
		}
		#endregion

		//private static int comconcount = 0;
		//internal static ComObject CreateComConnection(string aConnectionString) {
		//  int _count = ++comconcount;
		//  Trace.WriteLine("--- creating connection " + _count + " ---");
		//  V81.COMConnector conr = new V81.COMConnector();
		//  ComObject result = new ComObject(conr.Connect(aConnectionString));
		//  Marshal.ReleaseComObject(conr);
		//  Trace.WriteLine("--- connection " + _count + " is created ---");
		//  return result;
		//}

		#region "implementing IDisposable"
		protected virtual void Dispose(bool disposing) {
			if (!this._disposed) {
				if (disposing) {
					this._parent.ReturnObjectToPool(_transactionID);
					this._connection = null;
					this._parent = null;
				}
			}
			_disposed = true;
		}

		public virtual void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		~DbConnection() {
			this.Dispose(false);
		}
		#endregion

		public object XML«начение(object а“ип, string —трокаXML) {
			//return V8A.Invoke(connection.comObject, "XML«начение", BindingFlags.InvokeMethod, new object[] { а“ип, —трокаXML });
			return V8A.Call(Connection, Connection.comObject, "XML«начение()", а“ип, —трокаXML);
		}

		public string XML—трока(object а«начение) {
			//return (string)V8A.Invoke(connection.comObject, "XML—трока", BindingFlags.InvokeMethod, new object[] { а«начение });
			return (string)V8A.Call(Connection, Connection.comObject, "XML—трока()", а«начение);
		}

		public object XML“ип(object а“ип) {
			//return V8A.Invoke(connection.comObject, "XML“ип", BindingFlags.InvokeMethod, new object[] { а“ип });
			return V8A.Call(Connection, Connection.comObject, "XML“ип()", а“ип);
		}

		public object XML“ип«нч(object а«начение) {
			//return V8A.Invoke(connection.comObject, "XML“ип«нч", BindingFlags.InvokeMethod, new object[] { а«начение });
			return V8A.Call(Connection, Connection.comObject, "XML“ип«нч()", а«начение);
		}

		public object »зXML“ипа(object а“ипXML) {
			//return V8A.Invoke(connection.comObject, "»зXML“ипа", BindingFlags.InvokeMethod, new object[] { а“ипXML });
			return V8A.Call(Connection, Connection.comObject, "»зXML“ипа()", а“ипXML);
		}

		public Guid Ќачать“ранзакцию(TimeSpan timeOut) {
			if (_transactionID != Guid.Empty) {
				throw new InvalidOperationException("“ранзакци€ уже начата. ¬ложенные транзакции не поддерживаютс€.");
			}

			Connection.TimeOut = timeOut;
			_transactionID = Guid.NewGuid();
			_parent.MoveToTransaction(_transactionID);
			//V8A.Invoke(connection.comObject, "BeginTransaction", BindingFlags.InvokeMethod, null);
			V8A.Call(Connection, Connection.comObject, "BeginTransaction()");
			return _transactionID;
		}

		public void «афиксировать“ранзакцию() {
			//V8A.Invoke(connection.comObject, "«афиксировать“ранзакцию", BindingFlags.InvokeMethod, null);
			V8A.Call(Connection, Connection.comObject, "«афиксировать“ранзакцию()");
			_parent.MoveFromTransaction(_transactionID);
			_transactionID = Guid.Empty;
		}

		public void ќтменить“ранзакцию() {
			//V8A.Invoke(connection.comObject, "ќтменить“ранзакцию", BindingFlags.InvokeMethod, null);
			V8A.Call(Connection, Connection.comObject, "ќтменить“ранзакцию()");
			_parent.MoveFromTransaction(_transactionID);
			_transactionID = Guid.Empty;
			//_parent.ReturnObjectToPool(_transaction); //≈сли _transactionID не указана - будет InvalidOperationException
			//“ран = "ќтменена транзакци€";
		}
	}
	
	public sealed class DbConnections {
		//private const long GARBAGE_INTERVAL = 30000; //5 minutes
		private static readonly object _poolLock = new object();
		private static readonly object _tPoolLock = new object();
		
		private readonly List<ComConnection> unlocked = new List<ComConnection>();//Hashtable of available objects
		private readonly Dictionary<int, ComConnection> locked = new Dictionary<int, ComConnection>(); //Hashtable of the checked-out objects
		
		private readonly Dictionary<Guid, ComConnection> lockedT = new Dictionary<Guid, ComConnection>(); //Hashtable of the checked-out transaction objects
		private readonly Dictionary<Guid, KeyValuePair<DateTime, ComConnection>> unlockedT = new Dictionary<Guid, KeyValuePair<DateTime, ComConnection>>(); //Hashtable of the available transaction objects
		//private internal readonly TransactionIdStack _transactionsID = new TransactionIdStack();

		public string ConnectionString { get; private set; }
		public int ConnectionsCount { get; private set; }
		
		//public int LockedConnectionsCount { get { return locked.Count; } }
		//public int UnlockedConnectionsCount { get { return unlocked.Count; } }
		//public int tLockedConnectionsCount { get { return lockedT.Count; } }
		//public int tUnlockedConnectionsCount { get { return unlockedT.Count; } }
		public ConnectionsInfo GetConnectionsInfo {
			get {
				lock (_poolLock) {
					lock (_tPoolLock) {
						return new ConnectionsInfo(locked.Count, unlocked.Count, lockedT.Count, unlockedT.Count);
					}
				}
			}
		}

		#region singletone
		public static DbConnections Instance {
			get {
				return Nested.instance;
			}
		}

		class Nested {
			internal static readonly DbConnections instance;
			
			// Yoda: Explicit static constructor to tell C# compiler
			// not to mark type as beforefieldinit
			static Nested() {
				instance = new DbConnections();
			}
		}
		#endregion

		#region "constructors"
		private DbConnections() {
			System.Configuration.ConnectionStringSettings css = System.Configuration.ConfigurationManager.ConnectionStrings["1C_Base"];
			if (css == null || string.IsNullOrEmpty(css.ConnectionString)) {
				//достать им€ файла
				throw new Exception("¬ ConnectionStrings не найдена строка 1C_Base в конфиг файле");
			}
			ConnectionString = css.ConnectionString;
			string strConCount = System.Configuration.ConfigurationManager.AppSettings["1C_ConCount"];
			int cc; //= 1; нет смысла, TryParse  измен€ет значение в любом случае
			ConnectionsCount = int.TryParse(strConCount, out cc) ? cc : 0;

			//Trace.WriteLine("internal ObjectPool €-" + Thread.CurrentThread.Name);
			Thread[] t = new Thread[ConnectionsCount];
			for (int i = 0; i < t.Length; i++) {
				t[i] = new Thread(new ThreadStart(AddConnect));
				//t[i].Name = "Tr" + i.ToString();
				t[i].Start();
			}
			for (int i = 0; i < t.Length; i++) {
				//Trace.WriteLine("Ќачал ждать " + i.ToString() + " €-" + Thread.CurrentThread.Name);
				t[i].Join();
			}

			//System.Timers.Timer gcTimer = new System.Timers.Timer();
			//gcTimer.Interval = GARBAGE_INTERVAL;
			//gcTimer.Elapsed += new System.Timers.ElapsedEventHandler(CollectGarbage);
			//gcTimer.Enabled = true;
		}
		#endregion

		private void AddConnect() {
			//Trace.WriteLine("Ќачало AddConnect "+Thread.CurrentThread.Name);
			lock (_poolLock) {
				unlocked.Add(CreateComConnect());
			}
			//Trace.WriteLine(" онец AddConnect " + Thread.CurrentThread.Name);
		}

		private ComConnection CreateComConnect() {
			V83.COMConnector conr = new V83.COMConnector();
			object con = conr.Connect(ConnectionString);
			ComConnection result = new ComConnection(con);
			Marshal.ReleaseComObject(conr);
			return result;
		} 

		internal ComConnection GetObjectFromPool(Guid transactionID) {
			ComConnection o = null;

			if (transactionID != Guid.Empty) { //Transaction

				lock (_tPoolLock) {
					if (!unlockedT.ContainsKey(transactionID)) {
						throw new NoTransactionException("No transaction connection in the connections pool. unlockedT.Count=" + unlockedT.Count + " lockedT.Count=" + lockedT.Count);
					}
					o = unlockedT[transactionID].Value;
					unlockedT.Remove(transactionID);
					lockedT.Add(transactionID, o);
				}

			} else {                // Non-transaction
				bool needToCreate = false;

				lock (_poolLock) {
					needToCreate = unlocked.Count == 0;
					if (!needToCreate) {
						o = unlocked[0];
						unlocked.RemoveAt(0);
						if (o.IsDirty) {
							o.Dispose();
							needToCreate = true;
						} else {
							locked.Add(Thread.CurrentThread.ManagedThreadId, o);
						}
					}
				}

				if (needToCreate) {
					o = CreateComConnect();

					lock (_poolLock) {
						locked.Add(Thread.CurrentThread.ManagedThreadId, o);
					}
				}

				//if (unlocked.Count > 0) {

				//  lock (_poolLock) {
				//    o = unlocked[0];
				//    unlocked.RemoveAt(0);
				//    if (o.IsDirty) {
				//      o.Dispose();
				//      o = CreateComConnect();
				//    }
				//    locked.Add(Thread.CurrentThread.ManagedThreadId, o);
				//  }

				//} else {
				//  o = CreateComConnect();

				//  lock (_poolLock) {
				//    locked.Add(Thread.CurrentThread.ManagedThreadId, o);
				//  }
				//}

			}
			return o;
		}

		internal void ReturnObjectToPool(Guid transactionID) {
			if (transactionID != Guid.Empty) { //Transaction
				lock (_tPoolLock) {
					ComConnection o = lockedT[transactionID];
					lockedT.Remove(transactionID);
					unlockedT.Add(transactionID, new KeyValuePair<DateTime, ComConnection>(DateTime.Now, o));
				}
			} else {                // Non-transaction
				int uid = Thread.CurrentThread.ManagedThreadId;
				lock (_poolLock) {
					ComConnection o = locked[uid];
					locked.Remove(uid);
					unlocked.Add(o);
				}
			}
		}

		internal void MoveToTransaction(Guid transactionID) {
			//long result;
			int uid = Thread.CurrentThread.ManagedThreadId;
			lock (_poolLock) {
				lock (_tPoolLock) {
					//result = DateTime.Now.Ticks;
					ComConnection o = locked[uid];
					locked.Remove(uid);
					lockedT.Add(transactionID, o);
					//_transactionsID.Push(id);
				}
			}
			//return result;
		}

		internal void MoveFromTransaction(Guid transactionID) {
			int uid = Thread.CurrentThread.ManagedThreadId;
			lock (_poolLock) {
				lock (_tPoolLock) {
					ComConnection o = lockedT[transactionID];
					lockedT.Remove(transactionID);
					//_transactionsID.Pop();
					locked.Add(uid, o);
				}
			}
		}

		//internal DbConnection ConnectV8() {
		//  return new DbConnection(this, _transactionsID.Peek());
		//}

		internal DbConnection ConnectV8(Guid transactionID) {
			return new DbConnection(this, transactionID);
		}

		internal void DisconnectV8(DbConnection con) {
			con.Dispose();
		}

		//private void CollectGarbage(object sender, System.Timers.ElapsedEventArgs e) {
		//  if (unlockedT.Count == 0) return;
		//  DateTime toCompare = DateTime.Now;
		//  lock (_tPoolLock) {
		//    foreach (Guid key in unlockedT.Keys) {
		//      KeyValuePair<DateTime, ComConnection> obj = unlockedT[key];
		//      ComConnection con = obj.Value;
		//      DateTime objTime = obj.Key + con.TimeOut;
		//      if (objTime < toCompare) {
		//        RollBackTransaction(key);
		//      }
		//    }
		//  }

		//  //List<long> expired = new List<long>(unlockedT.Count);
		//  //lock (_tPoolLock) {
		//  //  foreach (long key in unlockedT.Keys) {
		//  //    KeyValuePair<DateTime, ComConnection> obj = unlockedT[key];
		//  //    ComConnection con = obj.Value;
		//  //    DateTime objTime = obj.Key + con.TimeOut;
		//  //    if (objTime < toCompare) {
		//  //      expired.Add(key);
		//  //    }
		//  //  }
		//  //  Trace.WriteLine("expired.Count is " + expired.Count);
		//  //  foreach (long key in expired) {
		//  //    Trace.WriteLine("expired key is " + key);
		//  //    DAL.Instance.RollbackTransaction(key);
		//  //    //unlockedT[key].Value.Dispose();
		//  //    //unlockedT.Remove(key);
		//  //  }
		//  //}
		//  //Trace.WriteLine("locked.Count is " + locked.Count);
		//  //Trace.WriteLine("unlocked.Count is " + unlocked.Count);
		//  //Trace.WriteLine("lockedT.Count is " + lockedT.Count);
		//  //Trace.WriteLine("unlockedT.Count is " + unlockedT.Count);
		//  //Trace.WriteLine("Garbage collected");
		//  //Trace.WriteLine("-------------------------------------------");
		//}

		private void RollBackTransaction(Guid transactionId) {
			using (DbConnection con1 = Instance.ConnectV8(transactionId)) {
				con1.ќтменить“ранзакцию();
			}

		}

		public void Clear() {
			lock (_poolLock) {
				foreach (ComConnection con in locked.Values) {
					con.Dispose();
				}
				locked.Clear();
				foreach (ComConnection con in unlocked) {
					con.Dispose();
				}
				unlocked.Clear();
			}
			lock (_tPoolLock) {
				foreach (ComConnection con in lockedT.Values) {
					con.Dispose();
				}
				lockedT.Clear();
				foreach (KeyValuePair<DateTime, ComConnection> con in unlockedT.Values) {
					con.Value.Dispose();
				}
				unlockedT.Clear();
			}
			//GC.Collect();
		}
	}
}
