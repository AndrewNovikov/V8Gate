using System;
using System.ComponentModel;
using System.Data;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace V8Gate {

	[Serializable]
	public class ������������ {

		public string ���;
		public object ����;

		public ������������(string ����, object �����) {
			��� = ����;
			���� = �����;
		}
	}

	public sealed class SkipAttribute : Attribute {
	}

	public sealed class IsV8FieldAttribute : Attribute {
	}

	public sealed class IsV8FieldTablePartAttribute : Attribute {
	}
	
	public sealed class IsV8PropAttribute : Attribute {
	}

	public sealed class IsV8PropTablePartAttribute:Attribute {
	}

//	public class V8_MetaData{
//
//		public static Dictionary<string, Type> Dic=new Dictionary<string,Type>();
//		//static V8_MetaData() {
//		//  V8_MetaData.Dic.Add("CatalogRef.�������������",typeof(���������_�������������));
//		//  Dic.Add("CatalogRef.�����������", typeof(���������_�����������));
//		//  Dic.Add("EnumRef.���������", typeof(�����_���������));
//		//  Dic.Add("DocumentRef.����������", typeof(���������_����������));
//		//}
//	}

	public static class ObjectCache {	

		private static Dictionary<ObjectRef, WeakReference> ObjCache = new Dictionary<ObjectRef, WeakReference>();

		public static void ��������(ObjectRef ������, V8Object obj) {
			lock (ObjCache) {
				WeakReference WR = new WeakReference(obj);
				ObjCache.Add(obj.�����������, WR);
			}
		}

		private static void _��������(ObjectRef ������, V8Object obj) {
			WeakReference WR = new WeakReference(obj);
			ObjCache.Add(obj.�����������, WR);
		}

		public static ���������� ����������<����������>(ObjectRef �������) where ���������� : V8Object, new() {
			WeakReference WR;
			V8Object Obj=null;
			if (�������.IsEmpty()) {
				Obj = new ����������();
			}else {
				lock (ObjCache) {
					if (ObjectCache.ObjCache.TryGetValue(�������, out WR)){
						if (WR.IsAlive) {
							Obj = (V8Object)WR.Target;
//              System.Diagnostics.Trace.WriteLine("�����");
						}else{
							Obj = new ����������();		//����������� � ���������� �� ���
							Obj.����������� = �������;
							WR.Target=Obj;
						}
					}else {
						Obj = new ����������();		//����������� � ���������� �� ���
						Obj.����������� = �������;
						ObjectCache._��������(�������, Obj);
					}
				}
			}
			return (����������)Obj;		 //�� ������ �� ���� return ������ lock???
		}

		//		public static V8Object ����������(V8Object obj){
		//			Type ���������� = obj.GetType(); 
		//			ObjectRef ������ = obj.������;
		//			V8Object Obj;
		//			if (������.IsEmpty()) {
		//				Obj = (V8Object)Activator.CreateInstance(����������);
		//			} else if (!ObjectCache.ObjCache.TryGetValue(������, out Obj)) {
		//				Obj = (V8Object)Activator.CreateInstance(����������);
		//				Obj.������ = ������;
		//				ObjectCache.ObjCache.Add(������, Obj);
		//			}
		//			return Obj;
		//		}

		public static V8Object ����������(ObjectRef oRef) {
			WeakReference WR;
			Type ���������� = oRef.����������();
			ObjectRef ������ = oRef;
			V8Object Obj;
			//����� ��������� ����� ��������� ������, ���� ������ ������ �������� � ��������� ������? �Andrew
			if (������.IsEmpty()) {
				Obj = (V8Object)Activator.CreateInstance(����������);
			} else {
				lock (ObjCache) {
					if (ObjectCache.ObjCache.TryGetValue(������, out WR)) {
						if (WR.IsAlive) {
							Obj = (V8Object)WR.Target;
//              System.Diagnostics.Trace.WriteLine("�����");
						}
						else {
							Obj = (V8Object)Activator.CreateInstance(����������);
							Obj.����������� = ������;
							WR.Target = Obj;
						}
					} else {
						Obj = (V8Object)Activator.CreateInstance(����������);
						Obj.����������� = ������;
						ObjectCache._��������(������, Obj);
					}                                    
				}
			}
			return Obj;							  //�� ������ �� ���� return ������ lock???
		}
	}

	
	//	public static class ���{
	//		public static T ���������<T>(ObjectRef ������) where T : new() {
	//			V8Object Obj;
	//			if (!ObjectCache.ObjCache.TryGetValue(������, out Obj)) {
	//				Obj = new T(������);
	//				ObjectCache.ObjCache.Add(this, Obj);
	//			}
	//			return (T)Obj;
	//		}
	//	}

	interface IV8TablePart {
		//bool �������();
		Type �����������������������();
		object ��������();  //�������� ����� ������, ������� ��� � 1�
		void CopyTo(object obj);
	}

  [Serializable]
	public class TabList<T> : List<T>, ICloneable, IV8TablePart, ISerializable where T : ���������, new() {
		//private bool _������� = false;

		#region "constructors"
		public TabList() {
		}

		public TabList(SerializationInfo info, StreamingContext context) {
			foreach (SerializationEntry field in info) {
				//this[int.Parse(field.Name)] = field.Value as T;
			}
		}
		#endregion

		public void GetObjectData(SerializationInfo info, StreamingContext context) {
			for (int i = 0; i < this.Count; i++) {
				info.AddValue(i.ToString(), this[i]);
			}
		}

		public object Clone() {
			TabList<T> result = new TabList<T>();
			foreach (T item in this) {
				result.Add((T)item.Clone());
			}
			return result;
		}

		public void CopyTo(object obj) {
			TabList<T> remObj = obj as TabList<T>;
			if (remObj == null) throw new ArgumentException("����. ����� �� ���� ���� ��� null");

			T[] values = new T[this.Count];
			base.CopyTo(values);
			remObj.Clear();
			remObj.AddRange(values);
			//PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			//for (int j = 0; j < ����List.Count; j++) {
			//  object ���������� = ����List[j];
			//  object ����������� = IA.��������();
			//  foreach (PropertyInfo ������� in �������) {
			//    object �������� = �������.GetValue(����������, null);
			//    �������.SetValue(�����������, ��������, null);
			//  }
			//}
		}

		public override bool Equals(object obj) {
			if (object.ReferenceEquals(obj, this)) return true;
			TabList<T> that = obj as TabList<T>;
			if (that == null || this.Count != that.Count) return false;
			for (int i = 0; i < this.Count; i++) {
				if (!object.Equals(this[i], that[i])) return false;
			}
			return true;
		}

		public override int GetHashCode() {
			if (this.Count == 0) return base.GetHashCode();
			int result = 0;
			foreach (T item in this) {
				if (item != null) result = result ^ item.GetHashCode();
			}
			return result;
		}

		//bool IV8TablePart.�������() { return _�������; }
		Type IV8TablePart.�����������������������() { return typeof(T); }
		object IV8TablePart.��������() {
			T ����������� = new T();
			this.Add(�����������);
			return �����������;
		}

		public static implicit operator TabList<T>(Collection<T> list) {
			TabList<T> ��������� = new TabList<T>();
			foreach (T var in list) {
				���������.Add(var);
			}
			return ���������;
		}

		//		public bool ������� {
		//			get { return _�������; }
		//			set { _������� = value; }
		//		}
	}

	public class ��������� : INotifyPropertyChanged, ICloneable, ISerializable {
		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(String info) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		#region "constructors"
		public ���������() {
		}

		public ���������(SerializationInfo info, StreamingContext context) {
			JsonHelper.DeserializeV8(this, info, context);
		}
		#endregion

		public void GetObjectData(SerializationInfo info, StreamingContext context) {
			foreach (FieldInfo fi in this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)) {
				object value = fi.GetValue(this);
				if (value != null) {
					info.AddValue(fi.Name.Remove(0, 1), value);
				}
			}
		}

		public object Clone() {
			��������� result = (���������)this.MemberwiseClone();
			//Type resultType = result.GetType();
			//FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			//foreach (FieldInfo field in fields) {
			//  if (!field.FieldType.IsValueType) {
			//    ICloneable fieldValue = field.GetValue(this) as ICloneable;
			//    object newValue = fieldValue == null ? null : fieldValue.Clone();
			//    resultType.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SetValue(result, newValue);
			//  }
			//}
			return result;
		}

		//�Andrew
		public override bool Equals(object obj) {
			if (object.ReferenceEquals(obj, this)) return true;
			��������� that = obj as ���������;
			if (that == null) return false;

			foreach (FieldInfo field in GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)) {
				if (!object.Equals(field.GetValue(this), field.GetValue(that))) return false;
			}
			//PropertyInfo[] properties = this.GetType().GetProperties();
			//for (int i = 0; i < properties.Length; i++) {
			//  object thisPropVal = properties[i].GetValue(this, null);
			//  object thatPropVal = properties[i].GetValue(that, null);
			//  if (!object.Equals(thisPropVal, thatPropVal)) return false;
			//}
			return true;
		}

		public override int GetHashCode() {
			int result = 0;
			//PropertyInfo[] properties = this.GetType().GetProperties();
			//if (properties.Length == 0) return base.GetHashCode();
			//for (int i = 0; i < properties.Length; i++) {
			//  object thisPropVal = properties[i].GetValue(this, null);
			//  if (thisPropVal != null) result = result ^ thisPropVal.GetHashCode();
			//}
			foreach (FieldInfo field in GetType().GetFields()) {
				object value = field.GetValue(this);
				if (value != null) result = result ^ value.GetHashCode();
			}

			return result;
		}
	}

	public interface I���<type> {
		type ��� {
			get;
			set;
		}
	}

	public interface I�������� : I���<decimal> {
	}

	public interface I��������� : I���<string> {
	}

	public interface I�����<type> {
		type ����� {
			get;
			set;
		}
	}

	public interface I���������� : I�����<decimal> {
	}

	public interface I����������� : I�����<string> {
	}

	public interface I������������ {
		string ������������ {
			get;
			set;
		}
	}

	public class CancelEvArgs: EventArgs {
		private bool _cancel;

		public bool Cancel {
			get {
				return _cancel;
			}
			set {
				_cancel = value;
			}
		}

		public CancelEvArgs() {
			_cancel = false;
		}
	}

	public abstract class V8Object:ISerializable, IDeserializationCallback, INotifyPropertyChanged, ICloneable {
		public event PropertyChangedEventHandler PropertyChanged;
		private SerializationInfo _sinfo;
		private StreamingContext _scontext;
		public event EventHandler �����������;
		public event EventHandler<CancelEvArgs> ������������; //�Andrew
		public event EventHandler �������������;

//    ~V8Object() {
//      System.Diagnostics.Trace.WriteLine("������ "+this.GetType().Name+" ��������� ��������� ������");
//    }

		[IsV8Field]
		internal ObjectRef _������;

//		[IsV8Prop]
		internal ObjectRef ����������� {
			[System.Diagnostics.DebuggerHidden]
			get { return _������; }
			[System.Diagnostics.DebuggerHidden]
			set { _������ = value; }
		}

		//begin------------------------�Andrew-----------------------------------------
		[IsV8Field]
		internal bool? _���������������;

		[IsV8Prop]
		public bool ��������������� {
			get {
				if (_��������������� == null) {
					��������������� = DAL.Instance.������������<bool>(�����������, "���������������");
				}
				return _���������������.Value;
			}
			set {
				if (_��������������� != value) {
					_��������������� = value;
					NotifyPropertyChanged("���������������");
				}
			}
		}
		//end------------------------�Andrew-----------------------------------------

		//~V8Object(){
		//  string d=this.GetType().ToString()+" ";
		//  if (this is ��������){
		//    d=d+((��������)this).����.ToString();
		//  }else if (this is ����������) {
		//    d=d+((����������)this).������������;
		//  }
		//  System.Diagnostics.Trace.WriteLine("������� " + d);
		//}

		public object Clone() {
			V8Object result = (V8Object)this.MemberwiseClone();

			foreach (FieldInfo field in result.�������������) {
				ICloneable fieldValue = field.GetValue(result) as ICloneable;
				field.SetValue(result, fieldValue != null ? fieldValue.Clone() : null);
			}
			//Type resultType = result.GetType();
			//FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			//foreach (FieldInfo field in fields) {
			//  if (!field.FieldType.IsValueType) {
			//    ICloneable fieldValue = field.GetValue(this) as ICloneable;
			//    object newValue = fieldValue == null ? null : fieldValue.Clone();
			//    resultType.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SetValue(result, newValue);
			//  }
			//}
			return result;
		}

		public void CopyTo(V8Object obj) {
			Type objType = obj.GetType();
			for (int i = 0; i < this.���������.Length; i++) {
				obj.���������[i].SetValue(obj, this.���������[i].GetValue(this));
			}
			foreach (FieldInfo ���� in this.�������������) {
				IV8TablePart thisValue = ����.GetValue(this) as IV8TablePart;
				if (thisValue != null) {
					object difValue = Activator.CreateInstance(����.FieldType);
					thisValue.CopyTo(difValue);
					����.SetValue(obj, difValue);
				} else {
					����.SetValue(obj, null);
				}
			}
			//PropertyInfo[] �������� = this.�����������������;
			//PropertyInfo[] ���������� = obj.�����������������;
			//for (int i=0; i < ��������.Length; i++) {
			//  PropertyInfo ��������=��������[i];
			//  PropertyInfo ����������=����������[i];
			//  IList ����List = (IList)��������.GetValue(this, null);
			//  IList �����List = (IList)����������.GetValue(obj, null);
			//  IV8TablePart IA = (IV8TablePart)�����List;
			//  �����List.Clear();
			//  PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			//  for (int j = 0; j < ����List.Count; j++) {
			//    object ���������� =����List[j];
			//    object ����������� = IA.��������();
			//    foreach (PropertyInfo ������� in �������) {
			//      object ��������=�������.GetValue(����������, null);
			//      �������.SetValue(�����������, ��������, null);
			//    }
			//  }
			//}
		}

		public override bool Equals(object obj) {
			if (object.ReferenceEquals(obj, this)) return true;
			V8Object that = obj as V8Object;
			if (that == null) return false;

			foreach (FieldInfo field in ���������) {
				if (!object.Equals(field.GetValue(this), field.GetValue(that))) return false;
			}
			foreach (FieldInfo field in �������������) {
				if (!object.Equals(field.GetValue(this), field.GetValue(that))) return false;
			}
			//PropertyInfo[] properties = new PropertyInfo[this.�������������.Length + this.�����������������.Length];
			//this.�������������.CopyTo(properties, 0);
			//this.�����������������.CopyTo(properties, this.�������������.Length);
			//for (int i = 0; i < properties.Length; i++) {
			//  object thisPropVal = properties[i].GetValue(this, null);
			//  object thatPropVal = properties[i].GetValue(that, null);
			//  if (!object.Equals(thisPropVal, thatPropVal)) return false;
			//}
			return true;
		}

		public override int GetHashCode() {
			int result = 0;
			foreach (FieldInfo field in ���������) {
				object value = field.GetValue(this);
				if (value != null) result = result ^ value.GetHashCode();
			}
			foreach (FieldInfo field in �������������) {
				object value = field.GetValue(this);
				if (value != null) result = result ^ value.GetHashCode();
			}
			//PropertyInfo[] properties = new PropertyInfo[this.�������������.Length + this.�����������������.Length];
			//this.�������������.CopyTo(properties, 0);
			//this.�����������������.CopyTo(properties, this.�������������.Length);
			//if (properties.Length == 0) return base.GetHashCode();

			//for (int i = 0; i < properties.Length; i++) {
			//  object value = properties[i].GetValue(this, null);
			//  if (value != null) result = result ^ value.GetHashCode();
			//}

			return result;
		}

		public static FieldInfo[] �������������<���, �������>() {
			FieldInfo[] fi2 = typeof(���).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			List<FieldInfo> ����������� = new List<FieldInfo>(fi2.Length);
			foreach (FieldInfo f in fi2) {
				if (f.IsDefined(typeof(�������), false)) {
					�����������.Add(f);
				}
			}
			FieldInfo[] res=new FieldInfo[�����������.Count];
			�����������.CopyTo(res);
			return res;
		}

		public static PropertyInfo[] �����������������<���, �������>() {
			PropertyInfo[] fi2 = typeof(���).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			List<PropertyInfo> ����������� = new List<PropertyInfo>(fi2.Length);
			foreach (PropertyInfo f in fi2) {
				if (f.IsDefined(typeof(�������), false)) {
					�����������.Add(f);
				}
			}
			PropertyInfo[] res = new PropertyInfo[�����������.Count];
			�����������.CopyTo(res);
			return res;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context) {
			foreach (FieldInfo fi in this.���������) {
				object value = fi.GetValue(this);
				if (value != null) {
					//info.AddValue(fi.Name.Substring(1), fi.GetValue(this));
					//Remove(0, 1) - ������� _ � ����� ����
					info.AddValue(fi.Name.Remove(0, 1), value);
				}
			}
			foreach (FieldInfo fi in �������������) {
				object value = fi.GetValue(this);
				if (value != null) {
					//info.AddValue(fi.Name.Remove(0, 1), value);
					//Remove(0, 1) - ������� _ � ����� ����
					info.AddValue(fi.Name.Remove(0, 1), value);
				}
			}
			//foreach (PropertyInfo T in this.�����������������) {
			//  info.AddValue(T.Name, T.GetValue(this, null));
			//}
		}

		public V8Object(SerializationInfo info, StreamingContext context) {
			_sinfo = info;
			_scontext = context;
			//foreach (PropertyInfo fi in this) {
			//  fi.SetValue(this, info.GetValue(fi.Name, fi.PropertyType), null);
			//}
			//foreach (SerializationEntry fi in info) {
			//  PropertyInfo pi = this.GetType().GetProperty(fi.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			//  pi.SetValue(this, fi.Value, null);
			//}
			OnDeserialization(this);
		}

		public void OnDeserialization(object sender) {
			//foreach (FieldInfo fi in this) {
			//  fi.SetValue(this, _sinfo.GetValue(fi.Name, fi.FieldType));
			//}
			//foreach (SerializationEntry var in _sinfo) {
			//  PropertyInfo fi = this.GetType().GetProperty(var.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			//  fi.SetValue(this, var.Value, null);
			//}

			//foreach (SerializationEntry field in _sinfo) {
			//  Type fieldType = this.GetType();
			//  FieldInfo fi = fieldType.GetField('_' + field.Name, BindingFlags.Instance | BindingFlags.NonPublic);
			//  PropertyInfo pi = fieldType.GetProperty(field.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			//  if (fi != null && pi != null) {
			//    object value;

			//    if (!JsonHelper.TryDeserializeJson(field.Value as JToken, pi.PropertyType, out value)) {
			//      value = field.Value;
			//    }

			//    fi.SetValue(this, value);
			//  }
			//}
			JsonHelper.DeserializeV8(this, _sinfo, _scontext);
		}

		protected void NotifyPropertyChanged(String info) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		
		//public T ������������<T>(string aName) {
		//  DbConnections pool = DbConnections.Instance;
		//  DbConnection con = pool.ConnectV8();
		//  object V8Ref = V8A.Reference(������, con);
		//  object result = V8A.Get(V8Ref, aName);
		//  object value = V8A.ConvertValueV8ToNet(result, con, typeof(T));
		//  pool.ReturnDBConnection(con);
		//  return (T)value;
		//}

		public V8Object() {
		}
		
		public V8Object(ObjectRef �������) {
			if (������� == null) {
				ArgumentException ae = new ArgumentException("������� ������� ������ �� ������ NULL");
			}
			����������� = �������;
		}

		public virtual FieldInfo[] ���������{
			get{
				return null;
			}
		}

		public virtual PropertyInfo[] ������������� {
			get{
				return null;
			}
		}

		public virtual FieldInfo[] ������������� {
			get {
				return null;
			}
		}

		public virtual PropertyInfo[] ����������������� {
			get{
				return null;
			}
		}

		//public IEnumerator<PropertyInfo> GetEnumerator() {
		//  PropertyInfo[] fi2 = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
		//  foreach (PropertyInfo f in fi2) {
		//    if (f.IsDefined(typeof(IsV8PropAttribute), false)) {
		//      yield return f;
		//    }
		//  }
		//}

		//public IEnumerable<PropertyInfo> ��������������() {
		//  PropertyInfo[] fi2 = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
		//  foreach (PropertyInfo f in fi2) {
		//    if (f.IsDefined(typeof(IsV8PropTablePartAttribute), false)) {
		//      IV8TablePart IT=f.GetValue(this, null) as IV8TablePart;
		//      //if (IT.�������()){
		//        yield return f;
		//      //}
		//    }
		//  }
		//}

		//public IEnumerable<FieldInfo> ���������() {
		//  FieldInfo[] fi2 = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
		//  foreach (FieldInfo f in fi2) {
		//    if (f.IsDefined(typeof(IsV8FieldAttribute), false)) {
		//      yield return f;
		//    }
		//  }
		//}

		public void Load(DAL dal) {
			if (this.�����������.IsEmpty()) return;//�Andrew

			V8Object obj = dal.Load(�����������);
			if (!object.ReferenceEquals(obj, this)) { //�Andrew (��� ������ ��������� - obj � this ������������)
				//if (TestDALs.CurDAL is IRemoteDAL) {
				//PropertyInfo[] fi = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				//foreach (PropertyInfo fld in obj.�������������) {
				//  fld.SetValue(this, fld.GetValue(obj, null), null);
				//}
				//foreach (PropertyInfo fld in obj.�����������������) {
				//  IList tp = fld.GetValue(obj, null) as IList;
				//  if (tp == null) {
				//    fld.SetValue(this, fld.GetValue(obj, null), null);
				//  } else {
				//    CopyAll((IList)fld.GetValue(this, null), (IEnumerable)fld.GetValue(obj, null));
				//  }
				//}
				obj.CopyTo(this);
			}
			//}
			if (������������� != null) {
				�������������(this, EventArgs.Empty);
			}
		}

		public void Load() {
			Load(DAL.Instance);
		}

		//		public void Load() {
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object V8Ref = V8A.Reference(������, con);
		//
		//			foreach (FieldInfo fi in this) {
		//				object result = V8A.Get(V8Ref, fi.Name.Substring(1));
		//				object value = V8A.ConvertValueV8ToNet(result, con, fi.FieldType);
		//				fi.SetValue(this, value);
		//			}
		//			foreach (FieldInfo T in ��������������()) {
		//				IList TR = (IList)T.GetValue(this);
		//				IV8TablePart IA = (IV8TablePart)TR;
		//				if (IA.�������()) {
		//					TR.Clear();
		//					FieldInfo[] ������� = IA.�����������������������().GetFields(BindingFlags.Public | BindingFlags.Instance);
		//					object �������������� = V8A.Get(V8Ref, T.Name.Substring(1));
		//					int ���������� = (int)V8A.Get(��������������, "����������()");
		//					//��� ������ ������ ��������
		//					for (int i = 0; i < ����������; i++) {
		//						object ��������������� = V8A.Get(��������������, "��������()", i);
		//						object �����������=IA.��������();
		//						foreach (FieldInfo ������� in �������) {
		//							object �������� = V8A.ConvertValueV8ToNet(V8A.Get(���������������, �������.Name), con, �������.FieldType);
		//							�������.SetValue(�����������, ��������);
		//						}
		//					}
		//				}
		//			}
		//
		//			pool.ReturnDBConnection(con);
		//		}

		public void Load2(DAL dal) {
			if (this.�����������.IsEmpty()) return; //�Andrew

			V8Object obj = dal.Load2(this.�����������);
			if (!object.ReferenceEquals(obj, this)) { //�Andrew (��� ������ ��������� - obj � this ������������)
				//if (TestDALs.CurDAL is IRemoteDAL) {
				//PropertyInfo[] fi = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				//foreach (PropertyInfo fld in obj.�������������) {
				//  fld.SetValue(this, fld.GetValue(obj, null), null);
				//}
				//foreach (PropertyInfo fld in obj.�����������������) {
				//  IList tp = fld.GetValue(obj, null) as IList;
				//  if (tp == null) {
				//    fld.SetValue(this, fld.GetValue(obj, null), null);
				//  } else {
				//    CopyAll((IList)fld.GetValue(this, null), (IEnumerable)fld.GetValue(obj, null));
				//  }
				//}
				obj.CopyTo(this);
			}
			//}
			if (������������� != null) {
				�������������(this, EventArgs.Empty);
			}
		}

		public void Load2() {
			Load2(DAL.Instance);
		}

		//�Andrew
		private void CopyAll(IList dest, IEnumerable source) {
			dest.Clear();
			foreach(object item in source) {
				dest.Add(item);
			}
		}

		//		public void Load2() {
		//			string[] ����������=������.GetType().Name.Split(new char[] { '_' });
		//			string �������������� = ����������[0];  //����� ��� ��� ���
		//			string ����������=����������[1];
		//			if (��������������.StartsWith("���")) {
		//				��������������="����������.";
		//			} else if (��������������.StartsWith("���")) {
		//				��������������="��������.";
		//			}
		//			StringBuilder ������������ = new StringBuilder("������� "+���������� + ".�������������", 2000);
		//			foreach (FieldInfo fi in this) {
		//				Type ������� = fi.FieldType;
		//				string fldName = fi.Name.Substring(1);
		//				������������.Append("," + ���������� + "." + fldName);
		//				if (�������.IsSubclassOf(typeof(ObjectRef))) {
		//					������������.Append(","+����������+"."+fldName+".�������������");
		//				}
		//			}
		//
		//			foreach (FieldInfo T in ��������������()) {
		//				������������.Append("," + ���������� + "." + T.Name.Substring(1) + ".(");
		//				IList TR = (IList)T.GetValue(this);
		//				IV8TablePart IA = (IV8TablePart)TR;
		//				if (IA.�������()) {
		//					string �����������2 = "";
		//					FieldInfo[] ������� = IA.�����������������������().GetFields(BindingFlags.Public | BindingFlags.Instance);
		//					foreach (FieldInfo ������� in �������) {
		//						string fldName = �������.Name;
		//						������������.Append(�����������2 + fldName);
		//						�����������2 = ",";
		//						if (�������.FieldType.IsSubclassOf(typeof(ObjectRef))) {
		//							������������.Append("," + fldName + ".�������������");
		//						}
		//					}
		//				}
		//				������������.Append(")");
		//			}
		//			������������.Append(" �� " + �������������� + ���������� + " ��� " + ����������);
		//			������������.Append(" ���	" + ���������� + ".������ = &������");
		//
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object V8Ref = V8A.Reference(������, con);
		//			������������[] ��� = new ������������[] { new ������������("������", V8Ref) };
		//			object result = V8A.������������������������(con, ������������.ToString(), ���);
		//			object ������� = V8A.Get(result, "�������()");
		//			if ((bool)V8A.Get(�������, "���������()")) { //������ ������ ������� ����� 1 ������
		//				int i=0;
		//				bool ��������� = true;
		//				foreach (FieldInfo fi in this) {
		//					if (���������) { //������ ��� ������������� ��� ������. ���� ������ �������� �������
		//						��������� = false;
		//						������.������������� = (string)V8A.ConvertValueV8ToNet(V8A.Get(�������, "��������()", i++), con, typeof(string));
		//					}
		//					object �������� = V8A.ConvertValueV8ToNet(V8A.Get(�������, "��������()", i++), con, fi.FieldType);
		//					fi.SetValue(this, ��������);
		//					ObjectRef oref = �������� as ObjectRef;
		//					if (oref != null) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
		//						oref.������������� = (string)V8A.ConvertValueV8ToNet(V8A.Get(�������, "��������()", i++), con, typeof(string));
		//					}
		//				}
		//				foreach (FieldInfo T in ��������������()) {
		//					IList TR = (IList)T.GetValue(this);
		//					IV8TablePart IA = (IV8TablePart)TR;
		//					if (IA.�������()) {
		//						TR.Clear();
		//						FieldInfo[] ������� = IA.�����������������������().GetFields(BindingFlags.Public | BindingFlags.Instance);
		//						object ������������������ = V8A.Get(�������, "��������()", i++);
		//						object ����������� = V8A.Get(������������������, "�������()");
		//						while ((bool)V8A.Get(�����������, "���������()")) {
		//							object �����������=IA.��������();
		//							int j = 0;
		//							foreach (FieldInfo ������� in �������) {
		//								object �������� = V8A.ConvertValueV8ToNet(V8A.Get(�����������, "��������()", j++), con, �������.FieldType);
		//								�������.SetValue(�����������, ��������);
		//								ObjectRef oref = �������� as ObjectRef;
		//								if (oref != null) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
		//									oref.������������� = (string)V8A.ConvertValueV8ToNet(V8A.Get(�����������, "��������()", j++), con, typeof(string));
		//								}
		//							}
		//						}
		//					}
		//				}
		//			}
		//			pool.ReturnDBConnection(con);
		//		}

		//�Andrew
		protected bool On������������() {
			if (������������ != null) {
				CancelEvArgs _cancelEventArgs = new CancelEvArgs();
				������������(this, _cancelEventArgs);
				return _cancelEventArgs.Cancel;
			}
			return false;
		}

		protected void On�����������() {
			if (����������� != null) {
				�����������(this, EventArgs.Empty);
			}
		}
	
	}
	[Serializable]
	public abstract class ����������:V8Object {

		//[IsV8Field]
		//internal bool? _���������;

		//[IsV8Prop]
		//public bool ��������� {
		//  get {
		//    if (_��������� == null) {
		//      _��������� = TestDALs.CurDAL.������������<bool>(������, "���������");
		//    }
		//    return _���������.Value;
		//  }
		//  set { _��������� = value; }
		//}

		//[IsV8Field]
		//internal string _������������=null;

		//[IsV8Prop]
		//public string ������������ {
		//  get {
		//    if (_������������ == null) {
		//      ������������ = DAL.Instance.CurDAL.������������<string>(������, "������������");
		//    }
		//    return _������������;
		//  }
		//  set {
		//    if (_������������ != value) {
		//      _������������ = value;
		//      NotifyPropertyChanged("������������");
		//    }
		//  }
		//}

		public ����������(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}

		public ����������(ObjectRef �������)
			: base(�������) {
		}

		public ����������(){
		}

		public void ��������(DAL dal) {
			if (!On������������()) {//�Andrew
				this.����������� = dal.��������(this);
				//V8Object obj = TestDALs.CurDAL.Load2(this);
				//if (DAL.Instance.CurDAL is IRemoteDAL) {
				//  this.������ = ������;
				//}
				On�����������();
			}
		}

		public void ��������() {
			��������(DAL.Instance);
		}

	}

	public abstract class ��������:V8Object {
		[IsV8Field]
		internal DateTime? _����;

		[IsV8Prop]
		public DateTime ���� {
			get {
				if (_���� == null) {
					���� = DAL.Instance.������������<DateTime>(�����������, "����");
				}
				return _����.Value;
			}
			set {
				if (_���� != value) {
					_���� = value;
					NotifyPropertyChanged("����");
				}
			}
		}

		public ��������(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	
		public ��������()
			: base() {
		}

		public ��������(ObjectRef �������)
			: base(�������) {
		}

		public void ��������(DAL dal, �������������������� ����������, ������������������������ ��������) {
			if (!On������������()) {//�Andrew
				this.����������� = dal.����������������(this, ����������, ��������);
				//V8Object obj = TestDALs.CurDAL.Load2(this);
				//if (DAL.Instance.CurDAL is IRemoteDAL) {
				//  this.������ = ������;
				//}
				On�����������();
			}
		}

		public void ��������(�������������������� ����������, ������������������������ ��������) {
			��������(DAL.Instance, ����������, ��������);
		}
	}

	[System.Serializable()]
	public abstract class ObjectRef:IComparable, ISerializable, ICloneable
 {
		#region Fields
		private Guid m_uuid;
		private string _�������������=string.Empty;

		//public abstract string ����������������� { get; }
		#endregion

		#region Properties
		public string ������������� {
			[System.Diagnostics.DebuggerHidden]
			get { return _�������������; }
			[System.Diagnostics.DebuggerHidden]
			set { _������������� = value; }
		}

		[System.Xml.Serialization.XmlTextAttribute()]
		public Guid UUID {
			get {
				return this.m_uuid;
			}
			set {
				this.m_uuid = value;
			}
		}
		#endregion

		#region Constructors
		public ObjectRef(SerializationInfo info, StreamingContext context) {
			//object uuid = info.GetValue("G", typeof(object));
			//object ������ = info.GetValue("�", typeof(object));
			//if (uuid is JValue) {
			//  uuid = ((JValue)uuid).Value;
			//}
			//if (uuid is JValue) {
			//  ������ = ((JValue)������).Value;
			//}

			//if (uuid is Guid) {
			//  this.m_uuid = (Guid)uuid;
			//} else if (uuid is string) {
			//  this.m_uuid = new Guid((string)uuid);
			//} else {
			//  throw new ArgumentException("Guid � ������ �� ����� ���� ���� " + uuid.GetType().FullName);
			//}
			//this._������������� = ������.ToString();
			this.m_uuid = (Guid)JsonHelper.DeserializeValue(info.GetValue("G", typeof(object)), typeof(Guid));
			this._������������� = info.GetString("�");
			//this.m_uuid = (Guid)info.GetValue("G", typeof(Guid));
			//this._������������� = info.GetString("�");

			//foreach (SerializationEntry field in info) {
			//  Type fieldType = this.GetType();
			//  FieldInfo fi = fieldType.GetField('_' + field.Name, BindingFlags.Instance | BindingFlags.NonPublic);
			//  if (fi != null) {
			//    JToken jValue = field.Value as JToken;
			//    if (jValue != null && jValue.HasValues && typeof(ISerializable).IsAssignableFrom(fi.FieldType)) {
			//      SerializationInfo serializationInfo = new SerializationInfo(fieldType, new System.Runtime.Serialization.FormatterConverter());
			//      foreach (JToken token in jValue.Children()) {
			//        JProperty prop = token as JProperty;
			//        if (prop != null) {
			//          serializationInfo.AddValue(prop.Name, prop.Value);
			//        } else {
			//          throw new ArgumentException("JToken appeared to be not property");
			//        }
			//      }
			//    } else {
			//      fi.SetValue(this, field.Value);
			//    }
			//  }
			//}

		}

		public ObjectRef() {
			this.m_uuid = Guid.Empty;
		}

		public ObjectRef(Guid uuid) {
			this.m_uuid = uuid;
		}
		#endregion

		#region Methods
		public abstract string ���������������������();
		//public virtual string ���������������������() {
		//  return DAL.Instance.������������<string>(this, this.�����������������);
		//}

		public virtual Type ����������() {
			return typeof(V8Object);
		}

		public override string ToString() {
			if (this.IsEmpty()) {
				return string.Empty;
			}else if (string.IsNullOrEmpty(this.�������������)) {
				return this.UUID.ToString();
			} else {
				return this.�������������;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("G", this.m_uuid);
			info.AddValue("�", this._�������������);
			info.AddValue("T", this.GetType().AssemblyQualifiedName);
		}

		public virtual int CompareTo(object obj) {
			int i;
			Guid theGuid;
			if (obj != null) {
				if (object.ReferenceEquals(this, obj)) {
					return 0;
				} else {
					i = base.GetType().Name.CompareTo(obj.GetType().Name);
					if (i != 0) {
						return i;
					} else {
						theGuid = this.UUID;
						return theGuid.CompareTo(((ObjectRef)obj).UUID);
					}
				}
			}
			throw new ArgumentException("err_NotObjectRef");	 //!!! ���� ��������� �������������� ������
		}

		public static bool operator ==(ObjectRef first, ObjectRef second) {
			if (ObjectRef.ReferenceEquals(first, null))
				return ObjectRef.ReferenceEquals(second, null);
			return first.Equals(second);
		}

		public static bool operator !=(ObjectRef first, ObjectRef second) {
			return !(first == second);
		}

		public override bool Equals(object obj) {
			if (obj == null) {
				return false;
			} else if (object.ReferenceEquals(this, obj)) {
				return true;
			} else if (object.ReferenceEquals(base.GetType(), obj.GetType())) {
				return this.UUID.Equals(((ObjectRef)obj).UUID);
			} else {
				return false;
			}
		}

		[System.Diagnostics.DebuggerHidden]
		public override int GetHashCode() {
			return this.UUID.GetHashCode();
		}

		[System.Diagnostics.DebuggerHidden]
		public bool IsEmpty() {
			return (this.m_uuid == Guid.Empty);
		}

		public object Clone() {
			return this.MemberwiseClone();
		}
		//			public string Presentation(object connection)
		//			{
		//				object[] theObjectArray;
		//				bool theBoolean = (connection.State == ConnectionState.Closed);
		//				if(theBoolean)
		//				{
		//					connection.Open();
		//				}
		//				try
		//				{
		//					using(ComObject theComObject = this.Reference(connection))
		//					{
		//						theObjectArray = new object[] { theComObject.comObject };
		//						return ((string) V81.Call(connection.Connection, "String", theObjectArray));
		//					}
		//				}
		//				finally
		//				{
		//					if(theBoolean)
		//					{
		//						connection.Close();
		//					}
		//				}
		//			}

		//			public ComObject Reference(V8DbConnection connection)
		//			
		//			{
		//				ComObject theComObject;
		//				bool theBoolean = (connection.State == ConnectionState.Closed);
		//				if(theBoolean)
		//				{
		//					connection.Open();
		//				}
		//				try
		//				{
		//					theComObject = this.Reference(connection.Connection);
		//				}
		//				finally
		//				{
		//					if(theBoolean)
		//					{
		//						connection.Close();
		//					}
		//				}
		//				return theComObject;
		//			}

		//public string ToInvariantString()
		//{
		//  return V81.ToInvariantString(this.m_uuid);
		//}
		#endregion
	}

	[Serializable]
	public abstract class CatalogRef : ObjectRef {

		public CatalogRef(SerializationInfo info, StreamingContext context)
			:base(info, context) {
		}
		
		public CatalogRef()
			: base() {
		}

		public CatalogRef(Guid uuid)
			: base(uuid) {
		}

//		public void �������������������(string �����) {
//			CatalogRef obj = TestDALs.CurDAL.�������������������(�����);
//			this.UUID = obj.UUID;
//		}
	}

	public abstract class DocumentRef:ObjectRef {

		public DocumentRef(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}

		public DocumentRef()
			: base() {
		}

		public DocumentRef(Guid uuid)
			: base(uuid) {
		}

		public override string ���������������������() {
			������������� = DAL.Instance.�����������������������������(this);
			return �������������;
		}

	}

	public abstract class StrNumDocRef:DocumentRef {

		public StrNumDocRef(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}

		public StrNumDocRef()
			: base() {
		}

		public StrNumDocRef(Guid uuid)
			: base(uuid) {
		}

		//		public void �������������(string ������, DateTime �����) {
		//			StrNumDocRef obj = TestDALs.CurDAL.�������������(this, ������, �����);
		//			this.UUID = obj.UUID;
		//		}
	}

	public abstract class IntNumDocRef:DocumentRef {

		public IntNumDocRef(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}

		public IntNumDocRef()
			: base() {
		}

		public IntNumDocRef(Guid uuid)
			: base(uuid) {
		}

		//		public void �������������(int ������, DateTime �����) {
		//			IntNumDocRef obj = TestDALs.CurDAL.�������������(this, ������, �����);
		//			this.UUID = obj.UUID;
		//		}
	}

	[Serializable]
	public abstract class IntNumCatRef : CatalogRef {

		public IntNumCatRef(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}

		public IntNumCatRef()
			: base() {
		}

		public IntNumCatRef(Guid uuid)
			: base(uuid) {
		}

		//		public void �����������(int ����) {
		//			CatalogRef obj = TestDALs.CurDAL.�����������(this, ����);
		//			this.UUID = obj.UUID;
		//		}

		//			string[] ���������� = this.GetType().Name.Split(new char[] { '_' });
		//			string ���������� = ����������[1];
		//
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object V8Ref = V8A.Reference(this, con);
		//			ObjectRef res=(ObjectRef)V8A.ConvertValueV8ToNet(V8A.Get(con, "�����������."+����������+".�����������()", ����), con, this.GetType());
		//			this.UUID=res.UUID;
		//			pool.ReturnDBConnection(con);
		//		}

	}

	public abstract class StrNumCatRef : CatalogRef {
		public StrNumCatRef(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}

		public StrNumCatRef()
			: base() {
		}

		public StrNumCatRef(Guid uuid)
			: base(uuid) {
		}

		//		public void �����������(string ����) {
		//			CatalogRef obj = TestDALs.CurDAL.�����������(this, ����);
		//			this.UUID = obj.UUID;
		//		}

		//		public void �����������(string ����) {
		//			string[] ���������� = this.GetType().Name.Split(new char[] { '_' });
		//			string ���������� = ����������[1];
		//
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object V8Ref = V8A.Reference(this, con);
		//			ObjectRef res = (ObjectRef)V8A.ConvertValueV8ToNet(V8A.Get(con, "�����������." + ���������� + ".�����������()", ����), con, this.GetType());
		//			this.UUID = res.UUID;
		//			pool.ReturnDBConnection(con);
		//		}
	}

}

