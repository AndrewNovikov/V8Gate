using System;
using System.Reflection;
using System.Collections.Generic;
//using V8AData;
//using ConnectionsPool;
//using DAL;

namespace V8Gate {

	public class Proba {
		public Proba() { }
	}

	public class RemoteDAL : AbstractDAL, IRemoteDAL {
		public RemoteDAL() { }

		public override void ClearPool() {
			DAL.Instance.Swap();
			DAL.Instance.CurDAL.ClearPool();
			DAL.Instance.Swap();
		}
		
		public override ObjectRef Записать(V8Object obj) {
			DAL.Instance.Swap();
			obj.Записать();
			DAL.Instance.Swap();
			return obj.Ссылка;
		}

		public override ObjectRef ЗаписатьДокумент(Документ obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
			DAL.Instance.Swap();
			obj.Записать(аРежЗаписи, аРежПров);
			DAL.Instance.Swap();
			return obj.Ссылка;
		}

		public override T ПолучитьПоле<T>(ObjectRef oRef, string aName) {
			DAL.Instance.Swap();
			V8Object obj = ObjectCache.НайтиВКеше(oRef);
			PropertyInfo pi = oRef.ТипОбъекта().GetProperty(aName);
			object value = pi.GetValue(obj, null);
			DAL.Instance.Swap();
			return (T)value;
		}

		public override V8Object Load(ObjectRef oRef) {
			DAL.Instance.Swap();
			V8Object V8obj = ObjectCache.НайтиВКеше(oRef);
			V8obj.Load();
			//ArrayList A = new ArrayList();
			//foreach (FieldInfo fi in V8obj){
			//  A.Add(fi.GetValue(V8obj));
			//}
			//object[] ObjArray=new object[A.Count];
			//A.CopyTo(ObjArray);
			DAL.Instance.Swap();
			return V8obj;
		}

		public override V8Object Load2(ObjectRef oRef) {
			DAL.Instance.Swap();
			V8Object V8obj = ObjectCache.НайтиВКеше(oRef);
			V8obj.Load2();
			DAL.Instance.Swap();
			return V8obj;
		}

//		internal override CatalogRef НайтиПоНаименованию(CatalogRef obj, string аНаим) {
//			DAL.Instance.Swap();
//			obj.НайтиПоНаименованию(аНаим);
//			DAL.Instance.Swap();
//			return obj;
//		}

//		internal override CatalogRef НайтиПоКоду(IntNumCatRef obj, int аКод) {
//			DAL.Instance.Swap();
//			obj.НайтиПоКоду(аКод);
//			DAL.Instance.Swap();
//			return obj;
//		}

//		public override CatalogRef НайтиПоКоду(StrNumCatRef obj, string аКод) {
//			DAL.Instance.Swap();
//			obj.НайтиПоКоду(аКод);
//			DAL.Instance.Swap();
//			return obj;
//		}

//		internal override StrNumDocRef НайтиПоНомеру(StrNumDocRef obj, string аНомер, DateTime аДата) {
//			DAL.Instance.Swap();
//			obj.НайтиПоНомеру(аНомер, аДата);
//			DAL.Instance.Swap();
//			return obj;
//		}

//		internal override IntNumDocRef НайтиПоНомеру(IntNumDocRef obj, int аНомер, DateTime аДата) {
//			DAL.Instance.Swap();
//			obj.НайтиПоНомеру(аНомер, аДата);
//			DAL.Instance.Swap();
//			return obj;
//		}

		public override Тип НайтиПоНаименованию<Тип>(string аНаим) {
			DAL.Instance.Swap();
			Тип result = DAL.Instance.CurDAL.НайтиПоНаименованию<Тип>(аНаим);
			DAL.Instance.Swap();
			return result;
		}

		public override Тип НайтиПоРеквизиту<Тип>(string аИмяРеквизита, object аЗначениеРеквизита, Тип аРодитель, object аВладелец) {
			DAL.Instance.Swap();
			Тип result = DAL.Instance.CurDAL.НайтиПоРеквизиту<Тип>(аИмяРеквизита, аЗначениеРеквизита, аРодитель, аВладелец);
			DAL.Instance.Swap();
			return result;
		}

		public override Тип НайтиПоКоду<Тип>(string аКод) {
			DAL.Instance.Swap();
			Тип result = DAL.Instance.CurDAL.НайтиПоКоду<Тип>(аКод);
			DAL.Instance.Swap();
			return result;
		}

		public override Тип НайтиПоКоду<Тип>(int аКод) {
			DAL.Instance.Swap();
			Тип result = DAL.Instance.CurDAL.НайтиПоКоду<Тип>(аКод);
			DAL.Instance.Swap();
			return result;
		}

		public override Тип НайтиПоНомеру<Тип>(string аНомер, DateTime аДата) {
			DAL.Instance.Swap();
			Тип result = DAL.Instance.CurDAL.НайтиПоНомеру<Тип>(аНомер, аДата);
			DAL.Instance.Swap();
			return result;
		}

		public override Тип НайтиПоНомеру<Тип>(int аНомер, DateTime аДата) {
			DAL.Instance.Swap();
			Тип result = DAL.Instance.CurDAL.НайтиПоНомеру<Тип>(аНомер, аДата);
			DAL.Instance.Swap();
			return result;
		}

		public override object[,] ВыполнитьЗапрос(string аТекстЗапроса, ПарамЗапроса[] аПараметры, 
			Type[] ТипыКолонок, string[] ИменаКолонок) {
			DAL.Instance.Swap();
			object[,] result = DAL.Instance.CurDAL.ВыполнитьЗапрос(аТекстЗапроса, аПараметры, ТипыКолонок, ИменаКолонок);
			DAL.Instance.Swap();
			return result;
			//return V8A.ВыполнитьЗапрос(аТекстЗапроса, аПараметры, ТипыКолонок);
		}
	}

	public static class MyProxy {

		public static List<ТипыКолонок> ВыполнитьЗапрос<ТипыКолонок>(string аТекстЗапроса) where ТипыКолонок : new() {
			return ВыполнитьЗапрос<ТипыКолонок>(аТекстЗапроса, null);
		}

		public static List<ТипыКолонок> ВыполнитьЗапрос<ТипыКолонок>(string аТекстЗапроса, ПарамЗапроса[] аПараметры)
		where ТипыКолонок : new(){
			PropertyInfo[] ВсеПоля = typeof(ТипыКолонок).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			bool[] НужнПоле = new bool[ВсеПоля.Length];
			int ЧислоНужных = 0;
			int i=0;
			foreach (PropertyInfo Поле in ВсеПоля) {
				if (!Attribute.IsDefined(Поле, typeof(SkipAttribute))) {
					НужнПоле[i] = true;
					ЧислоНужных++;
				}
				i++;
			}

			Type[] мТипыКолонок = new Type[ЧислоНужных];
			string[] мИменаКолонок = new string[ЧислоНужных];
			i = 0;
			int col = 0;
			foreach (PropertyInfo Поле in ВсеПоля) {
				if (НужнПоле[i++]) {
					мТипыКолонок[col] = Поле.PropertyType;
					мИменаКолонок[col] = Поле.Name;
					col++;
				}
			}

			object[,] lst2 = DAL.Instance.CurDAL.ВыполнитьЗапрос(аТекстЗапроса, аПараметры, мТипыКолонок, мИменаКолонок);
			int rows = lst2.GetLength(0);
			int cols = lst2.GetLength(1);
			List<ТипыКолонок> lst3 = new List<ТипыКолонок>(rows);
			for (int row = 0; row < rows; row++) {
				//object[] Ряд=lst2[k,*];
				ТипыКолонок Стр = new ТипыКолонок();
				for (col = 0; col < cols; col++) {
					ВсеПоля[col].SetValue(Стр, lst2[row, col], null);
				}
				lst3.Add(Стр);
			}
			return lst3;
		}
		
	}
}
