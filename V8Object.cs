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
	public class ПарамЗапроса {

		public string Имя;
		public object Знач;

		public ПарамЗапроса(string аИмя, object аЗнач) {
			Имя = аИмя;
			Знач = аЗнач;
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
//		//  V8_MetaData.Dic.Add("CatalogRef.ФорматыЛистов",typeof(СпрСсылка_ФорматыЛистов));
//		//  Dic.Add("CatalogRef.ФорматыКниг", typeof(СпрСсылка_ФорматыКниг));
//		//  Dic.Add("EnumRef.Переплеты", typeof(Переч_Переплеты));
//		//  Dic.Add("DocumentRef.ДоговорОДП", typeof(ДокСсылка_ДоговорОДП));
//		//}
//	}

	public static class ObjectCache {	

		private static Dictionary<ObjectRef, WeakReference> ObjCache = new Dictionary<ObjectRef, WeakReference>();

		public static void Добавить(ObjectRef Ссылка, V8Object obj) {
			lock (ObjCache) {
				WeakReference WR = new WeakReference(obj);
				ObjCache.Add(obj.СсылкаВнутр, WR);
			}
		}

		private static void _Добавить(ObjectRef Ссылка, V8Object obj) {
			WeakReference WR = new WeakReference(obj);
			ObjCache.Add(obj.СсылкаВнутр, WR);
		}

		public static ТипОбъекта НайтиВКеше<ТипОбъекта>(ObjectRef аСсылка) where ТипОбъекта : V8Object, new() {
			WeakReference WR;
			V8Object Obj=null;
			if (аСсылка.IsEmpty()) {
				Obj = new ТипОбъекта();
			}else {
				lock (ObjCache) {
					if (ObjectCache.ObjCache.TryGetValue(аСсылка, out WR)){
						if (WR.IsAlive) {
							Obj = (V8Object)WR.Target;
//              System.Diagnostics.Trace.WriteLine("Попал");
						}else{
							Obj = new ТипОбъекта();		//конструктор с параметром не жрёт
							Obj.СсылкаВнутр = аСсылка;
							WR.Target=Obj;
						}
					}else {
						Obj = new ТипОбъекта();		//конструктор с параметром не жрёт
						Obj.СсылкаВнутр = аСсылка;
						ObjectCache._Добавить(аСсылка, Obj);
					}
				}
			}
			return (ТипОбъекта)Obj;		 //не должен ли быть return внутри lock???
		}

		//		public static V8Object НайтиВКеше(V8Object obj){
		//			Type ТипОбъекта = obj.GetType(); 
		//			ObjectRef Ссылка = obj.Ссылка;
		//			V8Object Obj;
		//			if (Ссылка.IsEmpty()) {
		//				Obj = (V8Object)Activator.CreateInstance(ТипОбъекта);
		//			} else if (!ObjectCache.ObjCache.TryGetValue(Ссылка, out Obj)) {
		//				Obj = (V8Object)Activator.CreateInstance(ТипОбъекта);
		//				Obj.Ссылка = Ссылка;
		//				ObjectCache.ObjCache.Add(Ссылка, Obj);
		//			}
		//			return Obj;
		//		}

		public static V8Object НайтиВКеше(ObjectRef oRef) {
			WeakReference WR;
			Type ТипОбъекта = oRef.ТипОбъекта();
			ObjectRef Ссылка = oRef;
			V8Object Obj;
			//Зачем создавать новый экземпляр класса, если пустая ссылка передана в аргументе метода? ©Andrew
			if (Ссылка.IsEmpty()) {
				Obj = (V8Object)Activator.CreateInstance(ТипОбъекта);
			} else {
				lock (ObjCache) {
					if (ObjectCache.ObjCache.TryGetValue(Ссылка, out WR)) {
						if (WR.IsAlive) {
							Obj = (V8Object)WR.Target;
//              System.Diagnostics.Trace.WriteLine("Попал");
						}
						else {
							Obj = (V8Object)Activator.CreateInstance(ТипОбъекта);
							Obj.СсылкаВнутр = Ссылка;
							WR.Target = Obj;
						}
					} else {
						Obj = (V8Object)Activator.CreateInstance(ТипОбъекта);
						Obj.СсылкаВнутр = Ссылка;
						ObjectCache._Добавить(Ссылка, Obj);
					}                                    
				}
			}
			return Obj;							  //не должен ли быть return внутри lock???
		}
	}

	
	//	public static class Кеш{
	//		public static T Проверить<T>(ObjectRef ссылка) where T : new() {
	//			V8Object Obj;
	//			if (!ObjectCache.ObjCache.TryGetValue(ссылка, out Obj)) {
	//				Obj = new T(ссылка);
	//				ObjectCache.ObjCache.Add(this, Obj);
	//			}
	//			return (T)Obj;
	//		}
	//	}

	interface IV8TablePart {
		//bool Активна();
		Type ПолучитьСтуктуруКолонок();
		object Добавить();  //добавить новую строку, названо как в 1С
		void CopyTo(object obj);
	}

  [Serializable]
	public class TabList<T> : List<T>, ICloneable, IV8TablePart, ISerializable where T : ТаблЧасть, new() {
		//private bool _Активна = false;

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
			if (remObj == null) throw new ArgumentException("Табл. часть не того типа или null");

			T[] values = new T[this.Count];
			base.CopyTo(values);
			remObj.Clear();
			remObj.AddRange(values);
			//PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			//for (int j = 0; j < СвойList.Count; j++) {
			//  object СвояСтрока = СвойList[j];
			//  object НоваяСтрока = IA.Добавить();
			//  foreach (PropertyInfo Колонка in Колонки) {
			//    object Значение = Колонка.GetValue(СвояСтрока, null);
			//    Колонка.SetValue(НоваяСтрока, Значение, null);
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

		//bool IV8TablePart.Активна() { return _Активна; }
		Type IV8TablePart.ПолучитьСтуктуруКолонок() { return typeof(T); }
		object IV8TablePart.Добавить() {
			T НоваяСтрока = new T();
			this.Add(НоваяСтрока);
			return НоваяСтрока;
		}

		public static implicit operator TabList<T>(Collection<T> list) {
			TabList<T> ТаблЧасть = new TabList<T>();
			foreach (T var in list) {
				ТаблЧасть.Add(var);
			}
			return ТаблЧасть;
		}

		//		public bool Активна {
		//			get { return _Активна; }
		//			set { _Активна = value; }
		//		}
	}

	public class ТаблЧасть : INotifyPropertyChanged, ICloneable, ISerializable {
		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(String info) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		#region "constructors"
		public ТаблЧасть() {
		}

		public ТаблЧасть(SerializationInfo info, StreamingContext context) {
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
			ТаблЧасть result = (ТаблЧасть)this.MemberwiseClone();
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

		//©Andrew
		public override bool Equals(object obj) {
			if (object.ReferenceEquals(obj, this)) return true;
			ТаблЧасть that = obj as ТаблЧасть;
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

	public interface IКод<type> {
		type Код {
			get;
			set;
		}
	}

	public interface IКодЧисло : IКод<decimal> {
	}

	public interface IКодСтрока : IКод<string> {
	}

	public interface IНомер<type> {
		type Номер {
			get;
			set;
		}
	}

	public interface IНомерЧисло : IНомер<decimal> {
	}

	public interface IНомерСтрока : IНомер<string> {
	}

	public interface IНаименование {
		string Наименование {
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
		public event EventHandler ПослеЗаписи;
		public event EventHandler<CancelEvArgs> ПередЗаписью; //©Andrew
		public event EventHandler ПослеЗагрузки;

//    ~V8Object() {
//      System.Diagnostics.Trace.WriteLine("Объект "+this.GetType().Name+" уничтожен сборщиком мусора");
//    }

		[IsV8Field]
		internal ObjectRef _Ссылка;

//		[IsV8Prop]
		internal ObjectRef СсылкаВнутр {
			[System.Diagnostics.DebuggerHidden]
			get { return _Ссылка; }
			[System.Diagnostics.DebuggerHidden]
			set { _Ссылка = value; }
		}

		//begin------------------------©Andrew-----------------------------------------
		[IsV8Field]
		internal bool? _ПометкаУдаления;

		[IsV8Prop]
		public bool ПометкаУдаления {
			get {
				if (_ПометкаУдаления == null) {
					ПометкаУдаления = DAL.Instance.ПолучитьПоле<bool>(СсылкаВнутр, "ПометкаУдаления");
				}
				return _ПометкаУдаления.Value;
			}
			set {
				if (_ПометкаУдаления != value) {
					_ПометкаУдаления = value;
					NotifyPropertyChanged("ПометкаУдаления");
				}
			}
		}
		//end------------------------©Andrew-----------------------------------------

		//~V8Object(){
		//  string d=this.GetType().ToString()+" ";
		//  if (this is Документ){
		//    d=d+((Документ)this).Дата.ToString();
		//  }else if (this is Справочник) {
		//    d=d+((Справочник)this).Наименование;
		//  }
		//  System.Diagnostics.Trace.WriteLine("Удалено " + d);
		//}

		public object Clone() {
			V8Object result = (V8Object)this.MemberwiseClone();

			foreach (FieldInfo field in result.ПоляТаблЧасти) {
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
			for (int i = 0; i < this.ПоляШапки.Length; i++) {
				obj.ПоляШапки[i].SetValue(obj, this.ПоляШапки[i].GetValue(this));
			}
			foreach (FieldInfo поле in this.ПоляТаблЧасти) {
				IV8TablePart thisValue = поле.GetValue(this) as IV8TablePart;
				if (thisValue != null) {
					object difValue = Activator.CreateInstance(поле.FieldType);
					thisValue.CopyTo(difValue);
					поле.SetValue(obj, difValue);
				} else {
					поле.SetValue(obj, null);
				}
			}
			//PropertyInfo[] МоиТаблЧ = this.СвойстваТаблЧасти;
			//PropertyInfo[] ЧужиеТаблЧ = obj.СвойстваТаблЧасти;
			//for (int i=0; i < МоиТаблЧ.Length; i++) {
			//  PropertyInfo МояТаблЧ=МоиТаблЧ[i];
			//  PropertyInfo ЧужаяТаблЧ=ЧужиеТаблЧ[i];
			//  IList СвойList = (IList)МояТаблЧ.GetValue(this, null);
			//  IList ЧужойList = (IList)ЧужаяТаблЧ.GetValue(obj, null);
			//  IV8TablePart IA = (IV8TablePart)ЧужойList;
			//  ЧужойList.Clear();
			//  PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			//  for (int j = 0; j < СвойList.Count; j++) {
			//    object СвояСтрока =СвойList[j];
			//    object НоваяСтрока = IA.Добавить();
			//    foreach (PropertyInfo Колонка in Колонки) {
			//      object Значение=Колонка.GetValue(СвояСтрока, null);
			//      Колонка.SetValue(НоваяСтрока, Значение, null);
			//    }
			//  }
			//}
		}

		public override bool Equals(object obj) {
			if (object.ReferenceEquals(obj, this)) return true;
			V8Object that = obj as V8Object;
			if (that == null) return false;

			foreach (FieldInfo field in ПоляШапки) {
				if (!object.Equals(field.GetValue(this), field.GetValue(that))) return false;
			}
			foreach (FieldInfo field in ПоляТаблЧасти) {
				if (!object.Equals(field.GetValue(this), field.GetValue(that))) return false;
			}
			//PropertyInfo[] properties = new PropertyInfo[this.СвойстваШапки.Length + this.СвойстваТаблЧасти.Length];
			//this.СвойстваШапки.CopyTo(properties, 0);
			//this.СвойстваТаблЧасти.CopyTo(properties, this.СвойстваШапки.Length);
			//for (int i = 0; i < properties.Length; i++) {
			//  object thisPropVal = properties[i].GetValue(this, null);
			//  object thatPropVal = properties[i].GetValue(that, null);
			//  if (!object.Equals(thisPropVal, thatPropVal)) return false;
			//}
			return true;
		}

		public override int GetHashCode() {
			int result = 0;
			foreach (FieldInfo field in ПоляШапки) {
				object value = field.GetValue(this);
				if (value != null) result = result ^ value.GetHashCode();
			}
			foreach (FieldInfo field in ПоляТаблЧасти) {
				object value = field.GetValue(this);
				if (value != null) result = result ^ value.GetHashCode();
			}
			//PropertyInfo[] properties = new PropertyInfo[this.СвойстваШапки.Length + this.СвойстваТаблЧасти.Length];
			//this.СвойстваШапки.CopyTo(properties, 0);
			//this.СвойстваТаблЧасти.CopyTo(properties, this.СвойстваШапки.Length);
			//if (properties.Length == 0) return base.GetHashCode();

			//for (int i = 0; i < properties.Length; i++) {
			//  object value = properties[i].GetValue(this, null);
			//  if (value != null) result = result ^ value.GetHashCode();
			//}

			return result;
		}

		public static FieldInfo[] ЗапроситьПоля<Тип, Атрибут>() {
			FieldInfo[] fi2 = typeof(Тип).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			List<FieldInfo> СписокПолей = new List<FieldInfo>(fi2.Length);
			foreach (FieldInfo f in fi2) {
				if (f.IsDefined(typeof(Атрибут), false)) {
					СписокПолей.Add(f);
				}
			}
			FieldInfo[] res=new FieldInfo[СписокПолей.Count];
			СписокПолей.CopyTo(res);
			return res;
		}

		public static PropertyInfo[] ЗапроситьСвойства<Тип, Атрибут>() {
			PropertyInfo[] fi2 = typeof(Тип).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			List<PropertyInfo> СписокПолей = new List<PropertyInfo>(fi2.Length);
			foreach (PropertyInfo f in fi2) {
				if (f.IsDefined(typeof(Атрибут), false)) {
					СписокПолей.Add(f);
				}
			}
			PropertyInfo[] res = new PropertyInfo[СписокПолей.Count];
			СписокПолей.CopyTo(res);
			return res;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context) {
			foreach (FieldInfo fi in this.ПоляШапки) {
				object value = fi.GetValue(this);
				if (value != null) {
					//info.AddValue(fi.Name.Substring(1), fi.GetValue(this));
					//Remove(0, 1) - убираем _ в имени поля
					info.AddValue(fi.Name.Remove(0, 1), value);
				}
			}
			foreach (FieldInfo fi in ПоляТаблЧасти) {
				object value = fi.GetValue(this);
				if (value != null) {
					//info.AddValue(fi.Name.Remove(0, 1), value);
					//Remove(0, 1) - убираем _ в имени поля
					info.AddValue(fi.Name.Remove(0, 1), value);
				}
			}
			//foreach (PropertyInfo T in this.СвойстваТаблЧасти) {
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
		
		//public T ПолучитьПоле<T>(string aName) {
		//  DbConnections pool = DbConnections.Instance;
		//  DbConnection con = pool.ConnectV8();
		//  object V8Ref = V8A.Reference(Ссылка, con);
		//  object result = V8A.Get(V8Ref, aName);
		//  object value = V8A.ConvertValueV8ToNet(result, con, typeof(T));
		//  pool.ReturnDBConnection(con);
		//  return (T)value;
		//}

		public V8Object() {
		}
		
		public V8Object(ObjectRef аСсылка) {
			if (аСсылка == null) {
				ArgumentException ae = new ArgumentException("Попытка создать объект из ссылки NULL");
			}
			СсылкаВнутр = аСсылка;
		}

		public virtual FieldInfo[] ПоляШапки{
			get{
				return null;
			}
		}

		public virtual PropertyInfo[] СвойстваШапки {
			get{
				return null;
			}
		}

		public virtual FieldInfo[] ПоляТаблЧасти {
			get {
				return null;
			}
		}

		public virtual PropertyInfo[] СвойстваТаблЧасти {
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

		//public IEnumerable<PropertyInfo> ТабличныеЧасти() {
		//  PropertyInfo[] fi2 = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
		//  foreach (PropertyInfo f in fi2) {
		//    if (f.IsDefined(typeof(IsV8PropTablePartAttribute), false)) {
		//      IV8TablePart IT=f.GetValue(this, null) as IV8TablePart;
		//      //if (IT.Активна()){
		//        yield return f;
		//      //}
		//    }
		//  }
		//}

		//public IEnumerable<FieldInfo> ПоляШапки() {
		//  FieldInfo[] fi2 = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
		//  foreach (FieldInfo f in fi2) {
		//    if (f.IsDefined(typeof(IsV8FieldAttribute), false)) {
		//      yield return f;
		//    }
		//  }
		//}

		public void Load(DAL dal) {
			if (this.СсылкаВнутр.IsEmpty()) return;//©Andrew

			V8Object obj = dal.Load(СсылкаВнутр);
			if (!object.ReferenceEquals(obj, this)) { //©Andrew (при прямом обращении - obj и this эквивалентны)
				//if (TestDALs.CurDAL is IRemoteDAL) {
				//PropertyInfo[] fi = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				//foreach (PropertyInfo fld in obj.СвойстваШапки) {
				//  fld.SetValue(this, fld.GetValue(obj, null), null);
				//}
				//foreach (PropertyInfo fld in obj.СвойстваТаблЧасти) {
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
			if (ПослеЗагрузки != null) {
				ПослеЗагрузки(this, EventArgs.Empty);
			}
		}

		public void Load() {
			Load(DAL.Instance);
		}

		//		public void Load() {
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object V8Ref = V8A.Reference(Ссылка, con);
		//
		//			foreach (FieldInfo fi in this) {
		//				object result = V8A.Get(V8Ref, fi.Name.Substring(1));
		//				object value = V8A.ConvertValueV8ToNet(result, con, fi.FieldType);
		//				fi.SetValue(this, value);
		//			}
		//			foreach (FieldInfo T in ТабличныеЧасти()) {
		//				IList TR = (IList)T.GetValue(this);
		//				IV8TablePart IA = (IV8TablePart)TR;
		//				if (IA.Активна()) {
		//					TR.Clear();
		//					FieldInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetFields(BindingFlags.Public | BindingFlags.Instance);
		//					object ТабличнаяЧасть = V8A.Get(V8Ref, T.Name.Substring(1));
		//					int Количество = (int)V8A.Get(ТабличнаяЧасть, "Количество()");
		//					//для каждой строки ТабЧасти
		//					for (int i = 0; i < Количество; i++) {
		//						object СтрокаТаблЧасти = V8A.Get(ТабличнаяЧасть, "Получить()", i);
		//						object НоваяСтрока=IA.Добавить();
		//						foreach (FieldInfo Колонка in Колонки) {
		//							object Значение = V8A.ConvertValueV8ToNet(V8A.Get(СтрокаТаблЧасти, Колонка.Name), con, Колонка.FieldType);
		//							Колонка.SetValue(НоваяСтрока, Значение);
		//						}
		//					}
		//				}
		//			}
		//
		//			pool.ReturnDBConnection(con);
		//		}

		public void Load2(DAL dal) {
			if (this.СсылкаВнутр.IsEmpty()) return; //©Andrew

			V8Object obj = dal.Load2(this.СсылкаВнутр);
			if (!object.ReferenceEquals(obj, this)) { //©Andrew (при прямом обращении - obj и this эквивалентны)
				//if (TestDALs.CurDAL is IRemoteDAL) {
				//PropertyInfo[] fi = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				//foreach (PropertyInfo fld in obj.СвойстваШапки) {
				//  fld.SetValue(this, fld.GetValue(obj, null), null);
				//}
				//foreach (PropertyInfo fld in obj.СвойстваТаблЧасти) {
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
			if (ПослеЗагрузки != null) {
				ПослеЗагрузки(this, EventArgs.Empty);
			}
		}

		public void Load2() {
			Load2(DAL.Instance);
		}

		//©Andrew
		private void CopyAll(IList dest, IEnumerable source) {
			dest.Clear();
			foreach(object item in source) {
				dest.Add(item);
			}
		}

		//		public void Load2() {
		//			string[] ЧастьИмени=Ссылка.GetType().Name.Split(new char[] { '_' });
		//			string ПрефиксТаблицы = ЧастьИмени[0];  //здесь Спр или Док
		//			string ИмяТаблицы=ЧастьИмени[1];
		//			if (ПрефиксТаблицы.StartsWith("Спр")) {
		//				ПрефиксТаблицы="Справочник.";
		//			} else if (ПрефиксТаблицы.StartsWith("Док")) {
		//				ПрефиксТаблицы="Документ.";
		//			}
		//			StringBuilder ТекстЗапроса = new StringBuilder("ВЫБРАТЬ "+ИмяТаблицы + ".Представление", 2000);
		//			foreach (FieldInfo fi in this) {
		//				Type ТипПоля = fi.FieldType;
		//				string fldName = fi.Name.Substring(1);
		//				ТекстЗапроса.Append("," + ИмяТаблицы + "." + fldName);
		//				if (ТипПоля.IsSubclassOf(typeof(ObjectRef))) {
		//					ТекстЗапроса.Append(","+ИмяТаблицы+"."+fldName+".Представление");
		//				}
		//			}
		//
		//			foreach (FieldInfo T in ТабличныеЧасти()) {
		//				ТекстЗапроса.Append("," + ИмяТаблицы + "." + T.Name.Substring(1) + ".(");
		//				IList TR = (IList)T.GetValue(this);
		//				IV8TablePart IA = (IV8TablePart)TR;
		//				if (IA.Активна()) {
		//					string Разделитель2 = "";
		//					FieldInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetFields(BindingFlags.Public | BindingFlags.Instance);
		//					foreach (FieldInfo Колонка in Колонки) {
		//						string fldName = Колонка.Name;
		//						ТекстЗапроса.Append(Разделитель2 + fldName);
		//						Разделитель2 = ",";
		//						if (Колонка.FieldType.IsSubclassOf(typeof(ObjectRef))) {
		//							ТекстЗапроса.Append("," + fldName + ".Представление");
		//						}
		//					}
		//				}
		//				ТекстЗапроса.Append(")");
		//			}
		//			ТекстЗапроса.Append(" ИЗ " + ПрефиксТаблицы + ИмяТаблицы + " КАК " + ИмяТаблицы);
		//			ТекстЗапроса.Append(" ГДЕ	" + ИмяТаблицы + ".Ссылка = &Ссылка");
		//
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object V8Ref = V8A.Reference(Ссылка, con);
		//			ПарамЗапроса[] Пар = new ПарамЗапроса[] { new ПарамЗапроса("Ссылка", V8Ref) };
		//			object result = V8A.ПолучитьРезультатЗапроса(con, ТекстЗапроса.ToString(), Пар);
		//			object Выборка = V8A.Get(result, "Выбрать()");
		//			if ((bool)V8A.Get(Выборка, "Следующий()")) { //запрос должен вернуть ровно 1 строку
		//				int i=0;
		//				bool ПервыйРаз = true;
		//				foreach (FieldInfo fi in this) {
		//					if (ПервыйРаз) { //первым идёт Представление для ссылки. Сама ссылка известна заранее
		//						ПервыйРаз = false;
		//						Ссылка.Представление = (string)V8A.ConvertValueV8ToNet(V8A.Get(Выборка, "Получить()", i++), con, typeof(string));
		//					}
		//					object Значение = V8A.ConvertValueV8ToNet(V8A.Get(Выборка, "Получить()", i++), con, fi.FieldType);
		//					fi.SetValue(this, Значение);
		//					ObjectRef oref = Значение as ObjectRef;
		//					if (oref != null) {//за ссылочным типом в следующем поле запроса должно быть Представление
		//						oref.Представление = (string)V8A.ConvertValueV8ToNet(V8A.Get(Выборка, "Получить()", i++), con, typeof(string));
		//					}
		//				}
		//				foreach (FieldInfo T in ТабличныеЧасти()) {
		//					IList TR = (IList)T.GetValue(this);
		//					IV8TablePart IA = (IV8TablePart)TR;
		//					if (IA.Активна()) {
		//						TR.Clear();
		//						FieldInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetFields(BindingFlags.Public | BindingFlags.Instance);
		//						object ВложРезультЗапроса = V8A.Get(Выборка, "Получить()", i++);
		//						object ВложВыборка = V8A.Get(ВложРезультЗапроса, "Выбрать()");
		//						while ((bool)V8A.Get(ВложВыборка, "Следующий()")) {
		//							object НоваяСтрока=IA.Добавить();
		//							int j = 0;
		//							foreach (FieldInfo Колонка in Колонки) {
		//								object Значение = V8A.ConvertValueV8ToNet(V8A.Get(ВложВыборка, "Получить()", j++), con, Колонка.FieldType);
		//								Колонка.SetValue(НоваяСтрока, Значение);
		//								ObjectRef oref = Значение as ObjectRef;
		//								if (oref != null) {//за ссылочным типом в следующем поле запроса должно быть Представление
		//									oref.Представление = (string)V8A.ConvertValueV8ToNet(V8A.Get(ВложВыборка, "Получить()", j++), con, typeof(string));
		//								}
		//							}
		//						}
		//					}
		//				}
		//			}
		//			pool.ReturnDBConnection(con);
		//		}

		//©Andrew
		protected bool OnПередЗаписью() {
			if (ПередЗаписью != null) {
				CancelEvArgs _cancelEventArgs = new CancelEvArgs();
				ПередЗаписью(this, _cancelEventArgs);
				return _cancelEventArgs.Cancel;
			}
			return false;
		}

		protected void OnПослеЗаписи() {
			if (ПослеЗаписи != null) {
				ПослеЗаписи(this, EventArgs.Empty);
			}
		}
	
	}
	[Serializable]
	public abstract class Справочник:V8Object {

		//[IsV8Field]
		//internal bool? _ЭтоГруппа;

		//[IsV8Prop]
		//public bool ЭтоГруппа {
		//  get {
		//    if (_ЭтоГруппа == null) {
		//      _ЭтоГруппа = TestDALs.CurDAL.ПолучитьПоле<bool>(Ссылка, "ЭтоГруппа");
		//    }
		//    return _ЭтоГруппа.Value;
		//  }
		//  set { _ЭтоГруппа = value; }
		//}

		//[IsV8Field]
		//internal string _Наименование=null;

		//[IsV8Prop]
		//public string Наименование {
		//  get {
		//    if (_Наименование == null) {
		//      Наименование = DAL.Instance.CurDAL.ПолучитьПоле<string>(Ссылка, "Наименование");
		//    }
		//    return _Наименование;
		//  }
		//  set {
		//    if (_Наименование != value) {
		//      _Наименование = value;
		//      NotifyPropertyChanged("Наименование");
		//    }
		//  }
		//}

		public Справочник(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}

		public Справочник(ObjectRef аСсылка)
			: base(аСсылка) {
		}

		public Справочник(){
		}

		public void Записать(DAL dal) {
			if (!OnПередЗаписью()) {//©Andrew
				this.СсылкаВнутр = dal.Записать(this);
				//V8Object obj = TestDALs.CurDAL.Load2(this);
				//if (DAL.Instance.CurDAL is IRemoteDAL) {
				//  this.Ссылка = Ссылка;
				//}
				OnПослеЗаписи();
			}
		}

		public void Записать() {
			Записать(DAL.Instance);
		}

	}

	public abstract class Документ:V8Object {
		[IsV8Field]
		internal DateTime? _Дата;

		[IsV8Prop]
		public DateTime Дата {
			get {
				if (_Дата == null) {
					Дата = DAL.Instance.ПолучитьПоле<DateTime>(СсылкаВнутр, "Дата");
				}
				return _Дата.Value;
			}
			set {
				if (_Дата != value) {
					_Дата = value;
					NotifyPropertyChanged("Дата");
				}
			}
		}

		public Документ(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	
		public Документ()
			: base() {
		}

		public Документ(ObjectRef аСсылка)
			: base(аСсылка) {
		}

		public void Записать(DAL dal, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
			if (!OnПередЗаписью()) {//©Andrew
				this.СсылкаВнутр = dal.ЗаписатьДокумент(this, аРежЗаписи, аРежПров);
				//V8Object obj = TestDALs.CurDAL.Load2(this);
				//if (DAL.Instance.CurDAL is IRemoteDAL) {
				//  this.Ссылка = Ссылка;
				//}
				OnПослеЗаписи();
			}
		}

		public void Записать(РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
			Записать(DAL.Instance, аРежЗаписи, аРежПров);
		}
	}

	[System.Serializable()]
	public abstract class ObjectRef:IComparable, ISerializable, ICloneable
 {
		#region Fields
		private Guid m_uuid;
		private string _Представление=string.Empty;

		//public abstract string ПолеПредставления { get; }
		#endregion

		#region Properties
		public string Представление {
			[System.Diagnostics.DebuggerHidden]
			get { return _Представление; }
			[System.Diagnostics.DebuggerHidden]
			set { _Представление = value; }
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
			//object предст = info.GetValue("П", typeof(object));
			//if (uuid is JValue) {
			//  uuid = ((JValue)uuid).Value;
			//}
			//if (uuid is JValue) {
			//  предст = ((JValue)предст).Value;
			//}

			//if (uuid is Guid) {
			//  this.m_uuid = (Guid)uuid;
			//} else if (uuid is string) {
			//  this.m_uuid = new Guid((string)uuid);
			//} else {
			//  throw new ArgumentException("Guid у ссылки не может быть типа " + uuid.GetType().FullName);
			//}
			//this._Представление = предст.ToString();
			this.m_uuid = (Guid)JsonHelper.DeserializeValue(info.GetValue("G", typeof(object)), typeof(Guid));
			this._Представление = info.GetString("П");
			//this.m_uuid = (Guid)info.GetValue("G", typeof(Guid));
			//this._Представление = info.GetString("П");

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
		public abstract string ПолучитьПредставление();
		//public virtual string ПолучитьПредставление() {
		//  return DAL.Instance.ПолучитьПоле<string>(this, this.ПолеПредставления);
		//}

		public virtual Type ТипОбъекта() {
			return typeof(V8Object);
		}

		public override string ToString() {
			if (this.IsEmpty()) {
				return string.Empty;
			}else if (string.IsNullOrEmpty(this.Представление)) {
				return this.UUID.ToString();
			} else {
				return this.Представление;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("G", this.m_uuid);
			info.AddValue("П", this._Представление);
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
			throw new ArgumentException("err_NotObjectRef");	 //!!! было получение локализованной строки
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

//		public void НайтиПоНаименованию(string аНаим) {
//			CatalogRef obj = TestDALs.CurDAL.НайтиПоНаименованию(аНаим);
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

		public override string ПолучитьПредставление() {
			Представление = DAL.Instance.ПолучитьПредставлениеЗапросом(this);
			return Представление;
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

		//		public void НайтиПоНомеру(string аНомер, DateTime аДата) {
		//			StrNumDocRef obj = TestDALs.CurDAL.НайтиПоНомеру(this, аНомер, аДата);
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

		//		public void НайтиПоНомеру(int аНомер, DateTime аДата) {
		//			IntNumDocRef obj = TestDALs.CurDAL.НайтиПоНомеру(this, аНомер, аДата);
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

		//		public void НайтиПоКоду(int аКод) {
		//			CatalogRef obj = TestDALs.CurDAL.НайтиПоКоду(this, аКод);
		//			this.UUID = obj.UUID;
		//		}

		//			string[] ЧастьИмени = this.GetType().Name.Split(new char[] { '_' });
		//			string ИмяТаблицы = ЧастьИмени[1];
		//
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object V8Ref = V8A.Reference(this, con);
		//			ObjectRef res=(ObjectRef)V8A.ConvertValueV8ToNet(V8A.Get(con, "Справочники."+ИмяТаблицы+".НайтиПоКоду()", аКод), con, this.GetType());
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

		//		public void НайтиПоКоду(string аКод) {
		//			CatalogRef obj = TestDALs.CurDAL.НайтиПоКоду(this, аКод);
		//			this.UUID = obj.UUID;
		//		}

		//		public void НайтиПоКоду(string аКод) {
		//			string[] ЧастьИмени = this.GetType().Name.Split(new char[] { '_' });
		//			string ИмяТаблицы = ЧастьИмени[1];
		//
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object V8Ref = V8A.Reference(this, con);
		//			ObjectRef res = (ObjectRef)V8A.ConvertValueV8ToNet(V8A.Get(con, "Справочники." + ИмяТаблицы + ".НайтиПоКоду()", аКод), con, this.GetType());
		//			this.UUID = res.UUID;
		//			pool.ReturnDBConnection(con);
		//		}
	}

}

