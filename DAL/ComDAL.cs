using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
//using ConnectionsPool;
//using V8Gate;
//using DAL;

namespace V8Gate {

	public class ComDAL : AbstractDAL {
		public ComDAL() { }

		public override void ClearPool() {
			DbConnections pool = DbConnections.Instance;
			pool.Clear();
		}

		public override V8Object Load(ObjectRef oRef) {
			V8Object obj = ObjectCache.НайтиВКеше(oRef);
			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object ComV8Ref = V8Gate.V8A.Reference(oRef, con);

			foreach (PropertyInfo fi in obj.СвойстваШапки) {
				if (fi.Name != "Ссылка") {
					object ComResult = V8Gate.V8A.Get(ComV8Ref, fi.Name); //.Substring(1));
					object value = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, fi.PropertyType);
					V8Gate.V8A.ReleaseComObject(ComResult);
					fi.SetValue(obj, value, null);
				}
			}
			foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
				IList TR = (IList)T.GetValue(obj, null);
				IV8TablePart IA = (IV8TablePart)TR;
				//if (IA.Активна()) {
				TR.Clear();
				PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				object ТабличнаяЧасть = V8Gate.V8A.Get(ComV8Ref, T.Name);
				V8Gate.V8A.ReleaseComObject(ComV8Ref);
				int Количество = (int)V8Gate.V8A.Get(ТабличнаяЧасть, "Количество()");
				//для каждой строки ТабЧасти
				for (int i = 0; i < Количество; i++) {
					object ComСтрокаТаблЧасти = V8Gate.V8A.Get(ТабличнаяЧасть, "Получить()", i);
					object НоваяСтрока = IA.Добавить();
					foreach (PropertyInfo Колонка in Колонки) {
						object V8Поле = V8Gate.V8A.Get(ComСтрокаТаблЧасти, Колонка.Name);
						object Значение = V8Gate.V8A.ConvertValueV8ToNet(V8Поле, con, Колонка.PropertyType);
						V8Gate.V8A.ReleaseComObject(V8Поле);
						Колонка.SetValue(НоваяСтрока, Значение, null);
					}
					V8Gate.V8A.ReleaseComObject(ComСтрокаТаблЧасти);
				}
				V8Gate.V8A.ReleaseComObject(ТабличнаяЧасть);
			}
			pool.ReturnDBConnection(con);
			return obj;
		}

		public override V8Object Load2(ObjectRef oRef) {
			ObjectRef Ссылка = oRef;
			V8Object obj = ObjectCache.НайтиВКеше(oRef);
			string[] ЧастьИмени = Ссылка.GetType().Name.Split(new char[] { '_' });
			string ПрефиксТаблицы = ЧастьИмени[0];  //здесь Спр или Док
			string ИмяТаблицы = ЧастьИмени[1];
			if (ПрефиксТаблицы.StartsWith("Спр")) {
				ПрефиксТаблицы = "Справочник.";
			} else if (ПрефиксТаблицы.StartsWith("Док")) {
				ПрефиксТаблицы = "Документ.";
			}
			StringBuilder ТекстЗапроса = new StringBuilder("ВЫБРАТЬ " + ИмяТаблицы + ".Представление", 2000);
			foreach (PropertyInfo fi in obj.СвойстваШапки) {
				if (fi.Name != "Ссылка") {
					Type ТипПоля = fi.PropertyType;
					string fldName = fi.Name; //.Substring(1);
					ТекстЗапроса.Append("," + ИмяТаблицы + "." + fldName);
					if (ТипПоля.IsSubclassOf(typeof(ObjectRef))) {
						ТекстЗапроса.Append("," + ИмяТаблицы + "." + fldName + ".Представление");
					}
				}
			}

			foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
				ТекстЗапроса.Append("," + ИмяТаблицы + "." + T.Name + ".(");
				IList TR = (IList)T.GetValue(obj, null);
				IV8TablePart IA = (IV8TablePart)TR;
				//if (IA.Активна()) {
				//Где-то была рекомендация вместо "" использовать string.Empty ©Andrew
				string Разделитель2 = "";
				PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				foreach (PropertyInfo Колонка in Колонки) {
					string fldName = Колонка.Name;
					ТекстЗапроса.Append(Разделитель2 + fldName);
					Разделитель2 = ",";
					if (Колонка.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
						ТекстЗапроса.Append("," + fldName + ".Представление");
					}
				}
				//}
				ТекстЗапроса.Append(")");
			}
			ТекстЗапроса.Append(" ИЗ " + ПрефиксТаблицы + ИмяТаблицы + " КАК " + ИмяТаблицы);
			ТекстЗапроса.Append(" ГДЕ	" + ИмяТаблицы + ".Ссылка = &Ссылка");

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object ComV8Ref = V8Gate.V8A.Reference(Ссылка, con);
			ПарамЗапроса[] Пар = new ПарамЗапроса[] { new ПарамЗапроса("Ссылка", ComV8Ref) };
			object result = V8Gate.V8A.ПолучитьРезультатЗапроса(con, ТекстЗапроса.ToString(), Пар);
			V8Gate.V8A.ReleaseComObject(ComV8Ref); //коряво!!!!!!!!!!!!!!!
			object Выборка = V8Gate.V8A.Get(result, "Выбрать()");
			V8Gate.V8A.ReleaseComObject(result);
			if ((bool)V8Gate.V8A.Get(Выборка, "Следующий()")) { //запрос должен вернуть ровно 1 строку
				int i = 0;
				bool ПервыйРаз = true;
				object V8Поле;
				foreach (PropertyInfo fi in obj.СвойстваШапки) {
					if (fi.Name != "Ссылка") {
						if (ПервыйРаз) { //первым идёт Представление для ссылки. Сама ссылка известна заранее
							ПервыйРаз = false;
							V8Поле = V8Gate.V8A.Get(Выборка, "Получить()", i++);
							Ссылка.Представление = (string)V8Gate.V8A.ConvertValueV8ToNet(V8Поле, con, typeof(string));
							//V8Gate.V8A.ReleaseComObject(V8Поле); не надо, это строка
						}
						V8Поле = V8Gate.V8A.Get(Выборка, "Получить()", i++);
						object Значение = V8Gate.V8A.ConvertValueV8ToNet(V8Поле, con, fi.PropertyType);
						V8Gate.V8A.ReleaseComObject(V8Поле);
						ObjectRef oref = Значение as ObjectRef;
						if (oref != null) {//за ссылочным типом в следующем поле запроса должно быть Представление
							V8Поле = V8Gate.V8A.Get(Выборка, "Получить()", i++);
              string представление = (string)V8Gate.V8A.ConvertValueV8ToNet(V8Поле, con, typeof(string));
              oref.Представление = представление == null ? string.Empty : представление;
              //V8Gate.V8A.ReleaseComObject(V8Поле); не надо, это строка
						}
						fi.SetValue(obj, Значение, null);
					}
				}
				foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
					IList TR = (IList)T.GetValue(obj, null);
					IV8TablePart IA = (IV8TablePart)TR;
					TR.Clear();
					PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
					object ComВложРезультЗапроса = V8Gate.V8A.Get(Выборка, "Получить()", i++);
					object ComВложВыборка = V8Gate.V8A.Get(ComВложРезультЗапроса, "Выбрать()");
					V8Gate.V8A.ReleaseComObject(ComВложРезультЗапроса);
					while ((bool)V8Gate.V8A.Get(ComВложВыборка, "Следующий()")) {
						object НоваяСтрока = IA.Добавить();
						int j = 0;
						foreach (PropertyInfo Колонка in Колонки) {
							V8Поле = V8Gate.V8A.Get(ComВложВыборка, "Получить()", j++);
							Type ТипПоля = Колонка.PropertyType;
							object Значение = V8Gate.V8A.ConvertValueV8ToNet(V8Поле, con, ТипПоля);
							V8Gate.V8A.ReleaseComObject(V8Поле);

							ObjectRef oref = Значение as ObjectRef;
							if (ТипПоля.IsSubclassOf(typeof(ObjectRef))) {//за ссылочным типом в следующем поле запроса должно быть Представление
								//if (oref != null) {//за ссылочным типом в следующем поле запроса должно быть Представление
								object ComResult = V8Gate.V8A.Get(ComВложВыборка, "Получить()", j++);
								string представление = (string)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(string));
								oref.Представление = представление == null ? string.Empty : представление;
								//V8Gate.V8A.ReleaseComObject(ComResult); не надо, это строка
							} else if (oref != null) {
								oref.ПолучитьПредставление();
							}

							Колонка.SetValue(НоваяСтрока, Значение, null);
						}
					}
					V8Gate.V8A.ReleaseComObject(ComВложВыборка);
				}
			}
			V8Gate.V8A.ReleaseComObject(Выборка);
			pool.ReturnDBConnection(con);
			return obj;
		}

		public override ObjectRef Записать(V8Object obj) {
			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object V8ComObj = ЗаполнитьПоляОбъекта(con, obj);
			if (obj is Справочник) {
				ЗаписатьСпр(con, obj, V8ComObj);
			}
			V8Gate.V8A.ReleaseComObject(V8ComObj);
			pool.ReturnDBConnection(con);
			return obj.Ссылка;
		}

		public override ObjectRef ЗаписатьДокумент(Документ obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object V8ComObj = ЗаполнитьПоляОбъекта(con, obj);
			ЗаписатьДок(con, obj, V8ComObj, аРежЗаписи, аРежПров);
			V8Gate.V8A.ReleaseComObject(V8ComObj);
			pool.ReturnDBConnection(con);
			return obj.Ссылка;
		}

		private object ЗаполнитьПоляОбъекта(DbConnection con, V8Object obj) {
			ObjectRef Ссылка = obj.Ссылка;
			object V8ComObj = null;
			if (!Ссылка.IsEmpty()) {
				object V8Ref = V8Gate.V8A.Reference(Ссылка, con);
				V8ComObj = V8Gate.V8A.Get(V8Ref, "ПолучитьОбъект()");
				V8Gate.V8A.ReleaseComObject(V8Ref);
			} else {
				string[] ЧастьИмени = Ссылка.GetType().Name.Split(new char[] { '_' });
				string ПрефиксТаблицы = ЧастьИмени[0];  //здесь Спр или Док
				string ИмяТаблицы = ЧастьИмени[1];
				if (ПрефиксТаблицы.StartsWith("Спр")) {
					//Справочник спр = obj as Справочник;
					PropertyInfo pi = obj.GetType().GetProperty("ЭтоГруппа");
					bool СоздатьГруппу = false;
					if (pi != null && (bool)(pi.GetValue(obj, null) ?? false)) {
						СоздатьГруппу = true;
					}
					if (СоздатьГруппу) {
						//if (спр._ЭтоГруппа ?? false) {
						ПрефиксТаблицы = "Справочники." + ИмяТаблицы + ".СоздатьГруппу()";
					} else {
						ПрефиксТаблицы = "Справочники." + ИмяТаблицы + ".СоздатьЭлемент()";
					}
				} else if (ПрефиксТаблицы.StartsWith("Док")) {
					ПрефиксТаблицы = "Документы." + ИмяТаблицы + ".СоздатьДокумент()";
				}
				V8ComObj = V8Gate.V8A.Get(con, ПрефиксТаблицы);
			}

			foreach (PropertyInfo fi in obj.СвойстваШапки) {
				if (fi.Name != "Ссылка") {
					object value = fi.GetValue(obj, null);
					if (value != null) {
						object V8Value = V8Gate.V8A.ConvertValueNetToV8(value, con);
						V8Gate.V8A.SetProp(V8ComObj, fi.Name, V8Value);
						V8Gate.V8A.ReleaseComObject(V8Value);
					}
				}
			}
			foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
				IList TR = (IList)T.GetValue(obj, null);
				IV8TablePart IA = (IV8TablePart)TR;
				//if (IA.Активна()) {
				PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				object ТабличнаяЧасть = V8Gate.V8A.Get(V8ComObj, T.Name);
				V8Gate.V8A.Get(ТабличнаяЧасть, "Очистить()");
				foreach (object Str in TR) {
					object ComСтрокаТЧ = V8Gate.V8A.Get(ТабличнаяЧасть, "Добавить()");
					foreach (PropertyInfo f in Колонки) {
						object value = V8Gate.V8A.ConvertValueNetToV8(f.GetValue(Str, null), con);
						V8Gate.V8A.SetProp(ComСтрокаТЧ, f.Name, value);
					}
					V8Gate.V8A.ReleaseComObject(ComСтрокаТЧ);
				}
				V8Gate.V8A.ReleaseComObject(ТабличнаяЧасть);
			}
			return V8ComObj;
		}

		private void ЗаписатьСпр(DbConnection con, V8Object obj, object V8Obj) {
			ObjectRef Ссылка = obj.Ссылка;
			V8Gate.V8A.Get(V8Obj, "Записать()");
			if (Ссылка.IsEmpty()) {
				object ComResult = V8Gate.V8A.Get(V8Obj, "Ссылка");
				obj.Ссылка = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, null) as V8Gate.ObjectRef;
				V8Gate.V8A.ReleaseComObject(ComResult);
				ObjectCache.Добавить(obj.Ссылка, obj);
				//if (obj is Справочник) { //на случай других объектов, кроме справочника
				//  PropertyInfo pi = obj.GetType().GetProperty("Код", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				//  object value = V8Gate.V8A.ConvertValueV8ToNet(V8Gate.V8A.Get(V8Obj, "Код"), con, null);
				//  pi.SetValue(obj, value, null);
				//}
			}
		}

		private void ЗаписатьДок(DbConnection con, Документ obj, object V8Obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
			object ComObjЗап = V8A.ConvertValueNetToV8(аРежЗаписи, con);
			object ComObjПров = V8A.ConvertValueNetToV8(аРежПров, con);
			ObjectRef Ссылка = obj.Ссылка;
			V8Gate.V8A.Get(V8Obj, "Записать()", ComObjЗап, ComObjПров);
			V8Gate.V8A.ReleaseComObject(ComObjПров);
			V8Gate.V8A.ReleaseComObject(ComObjЗап);
			if (Ссылка.IsEmpty()) {
				object ComResult = V8Gate.V8A.Get(V8Obj, "Ссылка");
				obj.Ссылка = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, null) as V8Gate.ObjectRef;
				V8Gate.V8A.ReleaseComObject(ComResult);
				ObjectCache.Добавить(obj.Ссылка, obj);
				//PropertyInfo pi = obj.GetType().GetProperty("Номер", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				//object value = V8Gate.V8A.ConvertValueV8ToNet(V8Gate.V8A.Get(V8Obj, "Номер"), con, null);
				//pi.SetValue(obj, value, null);
			}
		}

		public override T ПолучитьПоле<T>(ObjectRef oRef, string aName) {
			Type CurType = typeof(T);
			if (oRef.IsEmpty()) {
				if (CurType.Equals(typeof(string))) {
					return (T)((object)string.Empty);
				}
				return CurType.IsValueType ? default(T) : (T)Activator.CreateInstance(CurType);
			} else {
				Type UType = Nullable.GetUnderlyingType(CurType) ?? CurType;

				DbConnections pool = DbConnections.Instance;
				DbConnection con = pool.ConnectV8();
				object V8Ref = V8Gate.V8A.Reference(oRef, con);
				object ComResult = V8Gate.V8A.Get(V8Ref, aName);
				V8Gate.V8A.ReleaseComObject(V8Ref);
				object value = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, UType);
				V8Gate.V8A.ReleaseComObject(ComResult);
				pool.ReturnDBConnection(con);
				return (T)value;
			}
		}

//		internal override НайтиПоНаименованию(CatalogRef obj, string аНаим) {
//			string[] ЧастьИмени = obj.GetType().Name.Split(new char[] { '_' });
//			string ИмяТаблицы = ЧастьИмени[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "Справочники." + ИмяТаблицы + ".НайтиПоНаименованию()", аНаим);
//			CatalogRef res = (CatalogRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID = res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

//		internal override CatalogRef НайтиПоКоду(IntNumCatRef obj, int аКод) {
//			string[] ЧастьИмени = obj.GetType().Name.Split(new char[] { '_' });
//			string ИмяТаблицы = ЧастьИмени[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "Справочники." + ИмяТаблицы + ".НайтиПоКоду()", аКод);
//			CatalogRef res = (CatalogRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID=res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

//		public override CatalogRef НайтиПоКоду(StrNumCatRef obj, string аКод) {
//			string[] ЧастьИмени = obj.GetType().Name.Split(new char[] { '_' });
//			string ИмяТаблицы = ЧастьИмени[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "Справочники." + ИмяТаблицы + ".НайтиПоКоду()", аКод);
//			CatalogRef res = (CatalogRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID=res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

//		internal override StrNumDocRef НайтиПоНомеру(StrNumDocRef obj, string аНомер, DateTime аДата) {
//			string[] ЧастьИмени = obj.GetType().Name.Split(new char[] { '_' });
//			string ИмяТаблицы = ЧастьИмени[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "Документы." + ИмяТаблицы + ".НайтиПоНомеру()", аНомер, аДата);
//			StrNumDocRef res = (StrNumDocRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID=res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

//		internal override IntNumDocRef НайтиПоНомеру(IntNumDocRef obj, int аНомер, DateTime аДата) {
//			string[] ЧастьИмени = obj.GetType().Name.Split(new char[] { '_' });
//			string ИмяТаблицы = ЧастьИмени[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "Документы." + ИмяТаблицы + ".НайтиПоНомеру()", аНомер, аДата);
//			IntNumDocRef res = (IntNumDocRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID=res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

		public override Тип НайтиПоНаименованию<Тип>(string аНаим) {
			string[] ЧастьИмени = typeof(Тип).Name.Split(new char[] { '_' });
			string ИмяТаблицы = ЧастьИмени[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "Справочники." + ИмяТаблицы + ".НайтиПоНаименованию()", аНаим);
			Тип res = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID = res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}

		public override Тип НайтиПоРеквизиту<Тип>(string аИмяРеквизита, object аЗначениеРеквизита, Тип аРодитель, object аВладелец) {
			string[] ЧастьИмени = typeof(Тип).Name.Split(new char[] { '_' });
			string ИмяТаблицы = ЧастьИмени[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object ComЗначение = V8Gate.V8A.ConvertValueNetToV8(аЗначениеРеквизита, con);
			object ComРодитель = null;
			if (аРодитель != null) {
				ComРодитель = V8Gate.V8A.ConvertValueNetToV8(аРодитель, con);
			}
			object ComВладелец = null;
			if (аВладелец != null) {
				ComВладелец = V8Gate.V8A.ConvertValueNetToV8(аВладелец, con);
			}
			object ComResult = V8Gate.V8A.Get(con, "Справочники." + ИмяТаблицы + ".НайтиПоРеквизиту()", аИмяРеквизита, ComЗначение, ComРодитель, ComВладелец);
			Тип result = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
			V8Gate.V8A.ReleaseComObject(ComResult);
			V8Gate.V8A.ReleaseComObject(ComРодитель);
			V8Gate.V8A.ReleaseComObject(ComЗначение);
			pool.ReturnDBConnection(con);
			return result;
		}

		public override Тип НайтиПоКоду<Тип>(string аКод) {
			string[] ЧастьИмени = typeof(Тип).Name.Split(new char[] { '_' });
			string ИмяТаблицы = ЧастьИмени[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "Справочники." + ИмяТаблицы + ".НайтиПоКоду()", аКод);
			Тип res = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID=res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}

		public override Тип НайтиПоКоду<Тип>(int аКод) {
			string[] ЧастьИмени = typeof(Тип).Name.Split(new char[] { '_' });
			string ИмяТаблицы = ЧастьИмени[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "Справочники." + ИмяТаблицы + ".НайтиПоКоду()", аКод);
			Тип res = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID=res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}

		public override Тип НайтиПоНомеру<Тип>(string аНомер, DateTime аДата) {
			string[] ЧастьИмени = typeof(Тип).Name.Split(new char[] { '_' });
			string ИмяТаблицы = ЧастьИмени[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "Документы." + ИмяТаблицы + ".НайтиПоНомеру()", аНомер, аДата);
			Тип res = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID=res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}

		public override Тип НайтиПоНомеру<Тип>(int аНомер, DateTime аДата) {
			string[] ЧастьИмени = typeof(Тип).Name.Split(new char[] { '_' });
			string ИмяТаблицы = ЧастьИмени[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "Документы." + ИмяТаблицы + ".НайтиПоНомеру()", аНомер, аДата);
			Тип res = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID=res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}
		
		public override object[,] ВыполнитьЗапрос(string аТекстЗапроса, ПарамЗапроса[] аПараметры,
			Type[] ТипыКолонок, string[] ИменаКолонок) {
			return V8A.ВыполнитьЗапрос(аТекстЗапроса, аПараметры, ТипыКолонок, ИменаКолонок);
			//return null;
		}


		//		public override List<ТипыКолонок> ВыпЗапрос<ТипыКолонок>(string аТекстЗапроса) {
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object Res = V8A.ПолучитьРезультатЗапроса(con, аТекстЗапроса);
		//			List<ТипыКолонок> LstQ = V8A.QueryReader<ТипыКолонок>(con, Res);
		//			pool.ReturnDBConnection(con);
		//			return LstQ;
		//		}

		//		public override List<ТипыКолонок> ВыпЗапрос<ТипыКолонок>(string аТекстЗапроса, ПарамЗапроса[] аПараметры) {
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object Res = V8A.ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);
		//			List<ТипыКолонок> LstQ = V8A.QueryReader<ТипыКолонок>(con, Res);
		//			pool.ReturnDBConnection(con);
		//			return LstQ;
		//		}


	}
}
