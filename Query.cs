using System;
using System.Xml.Serialization;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using V8_TypeProvider;
using System.Threading;
using System.Globalization;
//using ConnectionsPool;

namespace V8Gate {
	
	internal class V8A {

		public static int ComCounter = 0;

		public static void ReleaseComObject(object obj) {
			//int i;
			if ((obj != null) && Marshal.IsComObject(obj)) {
				//if (V81.TraceComObject.m_traceComObjectEnabled) {
				//  i = (V81.TraceComObject.m_traceComObjectArrayObj.Count - 1);
				//  while (i >= 0) {
				//    if (V81.TraceComObject.m_traceComObjectArrayObj[i] != obj) {
				//      i--;
				//      continue;
				//    }
				//    V81.TraceComObject.m_traceComObjectArrayObj.RemoveAt(i);
				//    V81.TraceComObject.m_traceComObjectStack.RemoveAt(i);
				//    break;
				//  }
				//}
				Marshal.ReleaseComObject(obj);

#if (DEBUG)
        Interlocked.Decrement(ref ComCounter);
#endif
			}
		}

		public static object Reference(ObjectRef aRef, DbConnection connection) {
			string URINameSpace = string.Empty;
			string ИмяТипа = aRef.GetType().Name; //base!!!!!!!!!
			Type RefType = aRef.GetType();
			object[] aar = RefType.GetCustomAttributes(typeof(XmlTypeAttribute), true);
			XmlTypeAttribute theXmlTypeAttribute=null;
			if (aar.Length != 0) {
				theXmlTypeAttribute = (XmlTypeAttribute)aar[0];
			//}
			////XmlTypeAttribute theXmlTypeAttribute = ((XmlTypeAttribute)TypeDescriptor.GetAttributes(aRef)[typeof(XmlTypeAttribute)]);
			//if (theXmlTypeAttribute != null) {
				URINameSpace = theXmlTypeAttribute.Namespace;
				if (URINameSpace == null) {
					URINameSpace = string.Empty;
				}
				ИмяТипа = theXmlTypeAttribute.TypeName;
			}
      object ТипXML = Call(connection.Connection, connection.Connection.comObject, "NewObject()", "XMLDataType", ИмяТипа, URINameSpace);
      object V8Тип = Call(connection.Connection, connection.Connection.comObject, "FromXMLType()", ТипXML);
			ReleaseComObject(ТипXML);
			object V8Ref = connection.XMLЗначение(V8Тип, aRef.UUID.ToString());
			ReleaseComObject(V8Тип);
			return V8Ref;
		}

    protected static object Invoke(ComConnection con, object obj, string Метод, BindingFlags Flags, object[] args) {
      //System.Diagnostics.Trace.WriteLine(Метод);
      object result;
      try {
        result = obj.GetType().InvokeMember(Метод, Flags, null, obj, args);
//#if (DEBUG)
//        if ((result != null) && Marshal.IsComObject(result)) {
//          Interlocked.Increment(ref ComCounter);
//        }
//#endif
      } catch (TargetInvocationException e) {
        con.IsDirty = true;
        throw e;
      }
      return result;
    }

    //public static object Call(DbConnection con, string Метод, params object[] list) {
    //  return Call(con.connection, con.connection.comObject, Метод, list);
    //}

		public static object Call(ComConnection con, object aComObj, string Метод, params object[] list) {
			string[] Методы = Метод.Split('.');
			int Всего = Методы.Length;
			object res = aComObj;
			BindingFlags Flags;
			for (int i = 0; i < (Всего - 1); i++) {
				string ТекМетод = Методы[i];
				if (ТекМетод.EndsWith("()")) {
					ТекМетод = ТекМетод.Substring(0, ТекМетод.Length - 2);
					Flags = BindingFlags.InvokeMethod;
				} else {
					Flags = BindingFlags.GetProperty;
				}
				object NewRes=Invoke(con, res, ТекМетод, Flags, null);
				if (!aComObj.Equals(res)) {
					ReleaseComObject(res);
				}
				res = NewRes;
			}
			string Хвост = Методы[Всего - 1];
			if (Хвост.EndsWith("()")) {
				Хвост = Хвост.Substring(0, Хвост.Length - 2);
				Flags = BindingFlags.InvokeMethod;
			} else {
				Flags = BindingFlags.GetProperty;
			}
      object result = Invoke(con, res, Хвост, Flags, list);
			if (!aComObj.Equals(res)) {
				ReleaseComObject(res);
			}
			return result;
		}

		//		public static object GetProp(object ComObj, string Property){
		//			object prop=ComObj.GetType().InvokeMember(Property, BindingFlags.GetProperty, null, ComObj, null); 	
		//			if (prop == System.DBNull.Value) {
		//				return prop.ToString();
		//			} else {
		//				return ComObj.GetType().InvokeMember(Property, BindingFlags.GetProperty, null, ComObj, null); 	
		//			}
		//		}

		public static object SetProp(ComConnection con, object ComObj, string Property, object Val) {
      return Invoke(con, ComObj, Property, BindingFlags.SetProperty, new object[] { Val });
		}

		public static object ConvertValueNetToV8(object value, DbConnection connection) {
			//string theString;
			//string theString2;
			//XmlTypeAttribute theXmlTypeAttribute;
			//string theString3;
			//Array theArray;
			//ComObject theComObject3;
			//object theObject2;
			//object[] theObjectArray;
			if (value == System.DBNull.Value) { //неопределено
				value = null;
			} else if (value == null) {
				value = System.DBNull.Value;    //это будет NULL в 1С
			} else if (value is ObjectRef) {
				value = Reference((ObjectRef)value, connection);
			//} else if (value is TypeDescription) {
			//  value = ((TypeDescription)value).GetTypeDescription(connection).comObject;
			} else if (value is Enum) {
				if ((value is РежимЗаписиДокумента) || (value is РежимПроведенияДокумента)) {//системные перечисления
					Type ТипПеречисления = value.GetType();
					string ИмяСистПереч = ТипПеречисления.Name + "." + value.ToString();
          value = Call(connection.Connection, connection.Connection.comObject, ИмяСистПереч);
				} else {
					string Namespace = string.Empty;
					string ИмяТипа = value.GetType().Name;
					XmlTypeAttribute theXmlTypeAttribute = ((XmlTypeAttribute)TypeDescriptor.GetAttributes(value)[typeof(XmlTypeAttribute)]);
					if (theXmlTypeAttribute != null) {
						Namespace = theXmlTypeAttribute.Namespace;
						if (Namespace == null) {
							Namespace = string.Empty;
						}
						ИмяТипа = theXmlTypeAttribute.TypeName;
					}
          Object XMLDataType = Call(connection.Connection, connection.Connection.comObject, "NewObject()", "XMLDataType", ИмяТипа, Namespace);
					Object Тип1С = connection.ИзXMLТипа(XMLDataType);
					ReleaseComObject(XMLDataType);
					string theString3 = value.ToString();
					if (theString3 == "EmptyRef") {
						theString3 = string.Empty;
					}
					value = connection.XMLЗначение(Тип1С, theString3);
					ReleaseComObject(Тип1С);
					return value;
				}
			} else if (value is Array) {
        object ComObjArray = Call(connection.Connection, connection.Connection.comObject, "NewObject()", "Array");
				foreach (object item in (Array)value) {
					object ComObjItem = ConvertValueNetToV8(item, connection);
          Call(connection.Connection, ComObjArray, "Add()", ComObjItem);
					ReleaseComObject(ComObjItem);
				}
				value = ComObjArray; 
			} else if (value is DateTime) {
				if (((DateTime)value).Year < 100){
				//if ((((DateTime)value).Year == 1) && ((((DateTime)value).Month == 1) && (((DateTime)value).Day == 1))) {
					value = new DateTime(100, 1, 1);
				}
			} else if (value is int) {
				value = ((decimal)((int)value));
			} else if (value is double) {
				value = ((decimal)((double)value));
			//} else if (value is ComObject) {
			//  value = ((ComObject)value).comObject;
			} else if (Marshal.IsComObject(value)) {
				throw new ArgumentException("COM-объект передан в ConvertValueNetToV8");
			}
			return value;
		}

		static readonly DateTime ПустаяДата=new DateTime(100,1,1);

    public static string ConvertValueV8ToJS(object value, DbConnection connection) {
      string result = null;
			if (value == null) {//это Неопределено, пока оно заменяется на System.DBNull.Value
        result = "undefined";
      } else if (value == System.DBNull.Value) { //это NULL в 1С, переводим его в null
        result = "null";
      } else if (value is bool) {
        result = ((bool)value) ? "true" : "false";
      } else if (value is string) {
        result = @"""" + (System.Web.HttpUtility.HtmlEncode((string)value)).TrimEnd().Replace(@"\", @"\\").Replace(Environment.NewLine, @"<br/>").Replace("\n", @"<br/>") + @"""";
        //result = @"""" + (System.Web.HttpUtility.UrlEncode((string)value)).TrimEnd() + @"""";
        //result = @"""" + ((string)value).TrimEnd().Replace(@"\", @"\\").Replace(@"""", @"\""") + @"""";
			} else if (value is DateTime) {
				DateTime dt = (DateTime)value;
				if (dt == ПустаяДата) {
          result = "null";
				} else {
          result = "new Date(" + dt.Year + "," + (dt.Month - 1) + "," + dt.Day + ")";
          //result = "/Date("+dt.Ticks+")/";
				}
      } else if (value is int) {// || value is uint || value is long || value is ulong || value is short || value is ushort || value is byte || value is sbyte) {
        result = value.ToString();
      } else if (value is double) {// || value is decimal || value is float) {
        result = ((IFormattable)value).ToString(null, new CultureInfo("en-US", false));
      } else if (Marshal.IsComObject(value)) {
        result = @"""" + System.Web.HttpUtility.HtmlEncode(connection.XMLСтрока(value)) + @"""";
      } else {
        throw new ArgumentException("Unexpected value type. Check ConvertValueV8ToJS function");
        //result = @"""" + value.ToString() + @"""";
      }
      return result;
    }

		public static object ConvertValueV8ToNet(object value, DbConnection connection, Type aNetType) {
			//Перечисления "РежимЗаписиДокумента" и "РежимПроведенияДокумента" не работают
			//Видимо, нет случая, когда их придется получать из 1С
			if (value == null) {//это Неопределено, пока оно заменяется на System.DBNull.Value
				value = System.DBNull.Value;
			} else if (value == System.DBNull.Value) { //это NULL в 1С, переводим его в null
				value = null;
			} else if (value is double) {
				value = ((decimal)((double)value));
			} else if (value is int) {
				value = (decimal)((int)value);
			} else if (value is string) {
				value = (string)value;
			} else if (value is DateTime) {
				DateTime dt = (DateTime)value;
				if (dt == ПустаяДата) {
					value = DateTime.MinValue;
				} else {
					value = dt;
				}
			} else if (value is bool) {
				value = (bool)value;
			//} else if (Marshal.IsComObject(value)){
			//  Marshal.ReleaseComObject(value);
			//  value = null;
			} else {
				string ЗначениеСтрокой = connection.XMLСтрока(value);
				bool ТипИзвестен = true;
				if (aNetType == null) {
					ТипИзвестен = false;
				} else {
          //if (aNetType.IsGenericType) { //Цель - отлов nullable типов. Возможно, это неправильный их признак
					if (aNetType.IsGenericType && aNetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) { //Цель - отлов nullable типов.
						aNetType = Nullable.GetUnderlyingType(aNetType);
						//aNetType = aNetType.GetMethod("get_Value").ReturnType;
					}
					if (aNetType == typeof(object)) { //составной тип, анализ по полной программе
						ТипИзвестен = false;
					}
				}
				if (!ТипИзвестен) {
					object ТипДанныхXML = connection.XMLТипЗнч(value);
          string ИмяТипа = (string)Call(connection.Connection, ТипДанныхXML, "ИмяТипа");
					ReleaseComObject(ТипДанныхXML);
					if (V8TypeProvider.Instance.Dic.TryGetValue(ИмяТипа, out aNetType)) {
						if (aNetType.IsEnum) {
							if (ЗначениеСтрокой == string.Empty) {
								ЗначениеСтрокой = "EmptyRef";
							}
							value = Enum.Parse(aNetType, ЗначениеСтрокой);
						} else {
							object res = Activator.CreateInstance(aNetType);
							((ObjectRef)res).UUID = new Guid(ЗначениеСтрокой);
							value = res;
						}
					} /*else {
						if (string.Compare(ИмяТипа, "AccumulationMovementType", true) == 0) {
							value = Enum.Parse(typeof(AccumulationMovementType), ЗначениеСтрокой);
						} else if (string.Compare(ИмяТипа, "AccountType", true) == 0) {
							value = Enum.Parse(typeof(AccountType), ЗначениеСтрокой);
						} else if (string.Compare(ИмяТипа, "AccountingMovementType", true) == 0) {
							value = Enum.Parse(typeof(AccountingMovementType), ЗначениеСтрокой);
						}
					}*/
				} else {
					if (aNetType.IsEnum) { //дублирование кода!!!!!!!!!
						if (ЗначениеСтрокой == string.Empty) {
							ЗначениеСтрокой = "EmptyRef";
						}
						value = Enum.Parse(aNetType, ЗначениеСтрокой);
					} else {
						object res = Activator.CreateInstance(aNetType);
						((ObjectRef)res).UUID = new Guid(ЗначениеСтрокой);
						value = res;
					}
				}
				//throw new Exception();
			}
			return value;
		}

    //public static bool ПроверитьЗапрос(string аТекстЗапроса, Type аТипыКолонок, params object[] аПараИмяЗнач) {
    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object Res = ПолучитьРезультатЗапроса(con, аТекстЗапроса, null);
    //  ReleaseComObject(Res);
    //  pool.ReturnDBConnection(con);
    //  return true;
    //}

    public static object ПолучитьРезультатЗапроса(DbConnection con, string аТекстЗапроса) {
      return ПолучитьРезультатЗапроса(con, аТекстЗапроса, null);
    }

    public static object ПолучитьРезультатЗапроса(DbConnection con, string аТекстЗапроса, ПарамЗапроса[] аПараметры) {
      object ОбъектЗапрос = Call(con.Connection, con.Connection.comObject, "NewObject()", "Запрос", аТекстЗапроса);
      if (аПараметры != null) {
        foreach (ПарамЗапроса Пар in аПараметры) {
          object V8Знач = ConvertValueNetToV8(Пар.Знач, con);
          Call(con.Connection, ОбъектЗапрос, "УстановитьПараметр()", Пар.Имя, V8Знач);
          ReleaseComObject(V8Знач); //а это может и не быть ComObject
        }
      }
      object Рез = Call(con.Connection, ОбъектЗапрос, "Выполнить()");
			ReleaseComObject(ОбъектЗапрос);
			return Рез;
		}

		//public static List<ТипыКолонок> ВыполнитьЗапрос<ТипыКолонок>(string аТекстЗапроса)
    //  where ТипыКолонок : new() {
    //  return ВыполнитьЗапрос<ТипыКолонок>(аТекстЗапроса, null); 		
    //}

    //public static List<ТипыКолонок> ВыполнитьЗапрос<ТипыКолонок>(string аТекстЗапроса, ПарамЗапроса[] аПараметры) 
    //  where ТипыКолонок:new() {
    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object ComResult = ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);
    //  List<ТипыКолонок> lst=QueryReader<ТипыКолонок>(con, ComResult);
    //  ReleaseComObject(ComResult);
    //  pool.ReturnDBConnection(con);
    //  return lst;
    //}

    //class ИнфОПоле {
    //  public PropertyInfo PtyInfo;
    //  public int ИндексСсылки;
    //  public string ИмяСсылки;
    //  public string Представление;
    //  public bool ЭтоПредст;
    //  public object Значение;
    //  public int ИндексРез;			//используется только при работе с квадратным массивом
    //  public Type Тип;		    	//используется только при работе с квадратным массивом
    //}

		const string констПредставление="Представление";

    //public static List<ТипыКолонок> QueryReader<ТипыКолонок>(DbConnection connection, object result)
    //  where ТипыКолонок:new() {

    //  List<ИнфОПоле> ПоляЗапроса = new List<ИнфОПоле>();
    //  List<ТипыКолонок> aResList = new List<ТипыКолонок>();

    //  object ComObjКолонки = Get(result, "Колонки");
    //  int ЧислоКолВЗапросе = (int)Get(ComObjКолонки, "Количество()");
    //  Dictionary<string,int> Dic = new Dictionary<string,int>(ЧислоКолВЗапросе);
    //  int[] ИндексыПредставлений=new int[ЧислоКолВЗапросе];
    //  int КолвоПредст = 0;
    //  for (int i = 0; i != ЧислоКолВЗапросе; i++) {
    //    object ComObjКолонка = Get(ComObjКолонки, "Получить()", i);
    //    string ИмяКолон = (string)Get(ComObjКолонка, "Имя");
    //    ReleaseComObject(ComObjКолонка);
    //    Dic[ИмяКолон] = i;
    //    ИнфОПоле Поле=new ИнфОПоле();
    //    Поле.ЭтоПредст = ИмяКолон.EndsWith(констПредставление);
    //    if (Поле.ЭтоПредст) {
    //      Поле.ИмяСсылки = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
    //      ИндексыПредставлений[КолвоПредст] = i;
    //      КолвоПредст++;
    //      PropertyInfo pty = typeof(ТипыКолонок).GetProperty(Поле.ИмяСсылки);
    //      if (pty == null) {
    //        throw new ArgumentException("В запросе нет поля типа ссылка с именем " + Поле.ИмяСсылки);
    //      }
    //      Поле.PtyInfo = null;
    //      if (pty.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
    //        Поле.PtyInfo = pty.PropertyType.GetProperty("Представление");
    //      } else if (pty.PropertyType != typeof(object)) {
    //        throw new ArgumentException("В запросе поле " + Поле.ИмяСсылки + " должно быть типа ссылка или object");
    //      }
    //    } else {
    //      Поле.PtyInfo = typeof(ТипыКолонок).GetProperty(ИмяКолон);
    //    }
    //    ПоляЗапроса.Add(Поле);
    //    /*
    //    object ТипыКолон = Get(Колон, "ТипЗначения.Типы()");
    //    int КолТипов = (int)Get(ТипыКолон, "Количество()");
    //    for (int j = 0; j != КолТипов; j++) {
    //      object ОдинТип = Get(ТипыКолон, "Получить()", j);
    //      object XMLСтрукт = Get(connection, "XMLТип()", ОдинТип);
    //      string А = (string)Get(XMLСтрукт, "URIПространстваИмен");
    //      string ИмяТипа = (string)Get(XMLСтрукт, "ИмяТипа");
    //    } */
    //  }
    //  ReleaseComObject(ComObjКолонки);
    //  for (int i = 0; i < КолвоПредст; i++){
    //    ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];
    //    Поле.ИндексСсылки = Dic[Поле.ИмяСсылки];
    //    //PropertyInfo pi = ПоляЗапроса[Поле.ИндексСсылки].PtyInfo;
    //    //if (pi.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
    //    //  Поле.PtyInfo = pi.PropertyType.GetProperty("Представление");
    //    //} else if (pi.PropertyType != typeof(object)) {
    //    //  throw new ArgumentException("В запросе поле " + Поле.ИмяСсылки + " должно иметь тип ссылка или object");
    //    //}
    //  }

    //  object ComВыборка = Get(result, "Выбрать()");
    //  while ((bool)Get(ComВыборка, "Следующий()")) {
    //    object Стр = new ТипыКолонок(); //object Стр = Activator.CreateInstance(typeof(ТипыКолонок));
    //    for (int i = 0; i != ЧислоКолВЗапросе; i++) {
    //      ИнфОПоле Поле = ПоляЗапроса[i];
    //      object V8Поле = Get(ComВыборка, "Получить()", i);
    //      object Значение = ConvertValueV8ToNet(V8Поле, connection, Поле.PtyInfo == null ? null : Поле.PtyInfo.PropertyType);
    //      ReleaseComObject(V8Поле);
    //      if (Поле.ЭтоПредст) {
    //        Поле.Представление=(string)Значение;
    //      } else {
    //        Поле.Значение = Значение;
    //        Поле.PtyInfo.SetValue(Стр, Значение, null);
    //      }
    //    }
    //    for (int i = 0; i < КолвоПредст; i++) {
    //      ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];
    //      object Предст = Поле.Представление;
    //      object Ссылка = ПоляЗапроса[Поле.ИндексСсылки].Значение;
    //      if (Поле.PtyInfo != null) {
    //        Поле.PtyInfo.SetValue(Ссылка, Предст, null);
    //      }
    //      else {
    //        PropertyInfo prop = Ссылка.GetType().GetProperty("Представление");
    //        if (prop != null) {
    //          prop.SetValue(Ссылка, Предст, null);
    //        }
    //      }
    //    }
    //    aResList.Add((ТипыКолонок)Стр);
    //  }
    //  ReleaseComObject(ComВыборка);
    //  return aResList;
    //}

		//		public static IEnumerable<ТипыКолонок> Выбрать<ТипыКолонок>(DbConnection connection, object result)
		//			where ТипыКолонок:new() {
		//			List<FieldInfo> ПоляЗапроса = new List<FieldInfo>();
		//
		//			object Колонки = Get(result, "Колонки");
		//			int ЧислоКол = (int)Get(Колонки, "Количество()");
		//
		//			for (int i = 0; i != ЧислоКол; i++) {
		//				object Колон = Get(Колонки, "Получить()", i);
		//				string ИмяКолон = (string)Get(Колон, "Имя");
		//				FieldInfo Поле = typeof(ТипыКолонок).GetField(ИмяКолон);
		//				ПоляЗапроса.Add(Поле);
		//			}
		//			object Выборка = Get(result, "Выбрать()");
		//			while ((bool)Get(Выборка, "Следующий()")) {
		//				object Стр = new ТипыКолонок(); //object Стр = Activator.CreateInstance(typeof(ТипыКолонок));
		//				for (int i = 0; i != ЧислоКол; i++) {
		//					FieldInfo Поле = ПоляЗапроса[i];
		//					object Значение = ConvertValueV8ToNet(Get(Выборка, "Получить()", i), connection, Поле.FieldType);
		//					Поле.SetValue(Стр, Значение);
		//				}
		//				yield return (ТипыКолонок)Стр;
		//			}
		//		}

    //public static object[,] ВыполнитьЗапрос(string аТекстЗапроса, ПарамЗапроса[] аПараметры, 
    //  Type[] ТипыКолонок, string[] ИменаКолонок) {
    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object ComObjQueryResult = ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);

    //  List<ИнфОПоле> ПоляЗапроса = new List<ИнфОПоле>();
    //  object ComObjКолонки = Get(ComObjQueryResult, "Колонки");
    //  int ЧислоКолВЗапросе = (int)Get(ComObjКолонки, "Количество()");

    //  Dictionary<string, int> Dic = new Dictionary<string, int>(ЧислоКолВЗапросе);
    //  int[] ИндексыПредставлений = new int[ЧислоКолВЗапросе];
    //  int КолвоПредст = 0;
    //  int m = 0;
    //  for (int i = 0; i != ЧислоКолВЗапросе; i++) {
    //    object ComObjКолонка = Get(ComObjКолонки, "Получить()", i);
    //    string ИмяКолон = (string)Get(ComObjКолонка, "Имя");
    //    ReleaseComObject(ComObjКолонка);
    //    Dic[ИмяКолон] = i;
    //    ИнфОПоле Поле = new ИнфОПоле();
    //    Поле.ЭтоПредст = ИмяКолон.EndsWith(констПредставление);
    //    if (!Поле.ЭтоПредст) {
    //      if (m >= ТипыКолонок.Length) {
    //        throw new ArgumentException(@"Слишком мало properties в классе запроса");
    //      }
    //      if (ИмяКолон != ИменаКолонок[m]) {
    //        throw new ArgumentException(@"Должно идти property """ + ИмяКолон + @""", а не """ + ИменаКолонок[m] + @"""");
    //      }
    //      Поле.Тип = ТипыКолонок[m];
    //      Поле.ИндексРез = m++;
    //    } else {
    //      Поле.ИмяСсылки = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
    //      ИндексыПредставлений[КолвоПредст] = i;
    //      КолвоПредст++;
    //    }
    //    ПоляЗапроса.Add(Поле);
    //  }
    //  ReleaseComObject(ComObjКолонки);

    //  for (int i = 0; i < КолвоПредст; i++) {
    //    ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];

    //    if (!Dic.TryGetValue(Поле.ИмяСсылки, out Поле.ИндексСсылки)) {
    //      throw new ArgumentException("В запросе нет поля типа ссылка с именем " + Поле.ИмяСсылки);
    //    }
    //    //Добавить: 1.Диагностику 2.Оптимизацию !!!!!!!!!!
    //    //Type ty = ПоляЗапроса[Поле.ИндексСсылки].Тип;
    //    //Поле.PtyInfo = ty.GetProperty("Представление");
    //    //if (Поле.PtyInfo == null) {
    //    //  throw new ArgumentException("В запросе поле " + Поле.ИмяСсылки+" должно иметь тип ссылка");
    //    //}
    //  }
			
    //  object Выборка = Get(ComObjQueryResult, "Выбрать()");
    //  ReleaseComObject(ComObjQueryResult);
    //  int Количество = (int)Get(Выборка, "Количество()");
    //  object[,] Результат = new object[Количество, ТипыКолонок.Length];
    //  int j=0;
    //  while ((bool)Get(Выборка, "Следующий()")) {
    //    int k=0;
    //    for (int i = 0; i != ЧислоКолВЗапросе; i++) {
    //      ИнфОПоле Поле = ПоляЗапроса[i];
    //      object V8Поле = Get(Выборка, "Получить()", i);
    //      object Значение = ConvertValueV8ToNet(V8Поле, con, Поле.Тип);
    //      ReleaseComObject(V8Поле);  //это может быть не ComObject
    //      if (Поле.ЭтоПредст) {
    //        Поле.Представление=(string)Значение;
    //      } else {
    //        Поле.Значение = Значение;
    //        Результат[j, k++] = Значение;
    //      }
    //    }

    //    for (int i = 0; i < КолвоПредст; i++) {
    //      ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];
    //      object Предст = Поле.Представление;
    //      object Ссылка = ПоляЗапроса[Поле.ИндексСсылки].Значение;
    //      if (Ссылка != null) {
    //        Поле.PtyInfo = Ссылка.GetType().GetProperty("Представление");
    //        if (Поле.PtyInfo != null) {
    //          Поле.PtyInfo.SetValue(Ссылка, Предст, null);
    //        }
    //      }
    //    }
    //    j++;
    //  }
    //  ReleaseComObject(Выборка);
    //  pool.ReturnDBConnection(con); 
    //  return Результат;	  
    //}

    #region "Andrew's ВыполнитьЗапрос"
    //****************************************************************************************************************
    //****************************************************************************************************************
    //public static object[,] ВыполнитьЗапрос(string аТекстЗапроса, ПарамЗапроса[] аПараметры,
    //  Type[] ТипыКолонок, string[] ИменаКолонок) {

    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object ComObjQueryResult = ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);

    //  object ComObjКолонки = Get(ComObjQueryResult, "Колонки");
    //  int ЧислоКолВЗапросе = (int)Get(ComObjКолонки, "Количество()");
    //  if (ТипыКолонок.Length != ИменаКолонок.Length) {
    //    throw new ArgumentException(@"ИменаКолонок и ТипыКолонок не совпадают");
    //  }

    //  List<ИнфОПоле> ПоляЗапроса = new List<ИнфОПоле>(ЧислоКолВЗапросе);

    //  Dictionary<string, ИнфОПоле> ИмяПоляСсылкиЗапроса = new Dictionary<string, ИнфОПоле>();
    //  List<ИнфОПоле> ПредставленияЗапроса = new List<ИнфОПоле>();

    //  int index = 0;
    //  while (index < ЧислоКолВЗапросе) {
    //    object ComObjКолонка = Get(ComObjКолонки, "Получить()", index);
    //    string ИмяКолон = (string)Get(ComObjКолонка, "Имя");
    //    ReleaseComObject(ComObjКолонка);

    //    ИнфОПоле Поле = new ИнфОПоле();
    //    Поле.ЭтоПредст = ИмяКолон.EndsWith(констПредставление);
    //    if (!Поле.ЭтоПредст) {
    //      int ссылПолеИндекс = ПоляЗапроса.Count - ПредставленияЗапроса.Count;
    //      if (ИмяКолон != ИменаКолонок[ссылПолеИндекс]) {
    //        throw new ArgumentException(@"Должно идти property """ + ИмяКолон + @""", а не """ + ИменаКолонок[ссылПолеИндекс] + @"""");
    //      }
    //      Поле.Тип = ТипыКолонок[ссылПолеИндекс];
    //      ИмяПоляСсылкиЗапроса.Add(ИмяКолон, Поле);
    //    }
    //    else {
    //      Поле.ИмяСсылки = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
    //      ПредставленияЗапроса.Add(Поле);
    //    }
    //    ПоляЗапроса.Add(Поле);
    //    index++;
    //  }
    //  ReleaseComObject(ComObjКолонки);

    //  foreach (ИнфОПоле поле in ПредставленияЗапроса) {
    //    ИнфОПоле полеСсылки = null;
    //    if (ИмяПоляСсылкиЗапроса.TryGetValue(поле.ИмяСсылки, out полеСсылки)) {
    //      поле.ПолеСсылки = полеСсылки;
    //    }
    //    else {
    //      throw new ArgumentException("В запросе нет поля типа ссылка с именем " + поле.ИмяСсылки);
    //    }
    //  }

    //  object Выборка = Get(ComObjQueryResult, "Выбрать()");
    //  ReleaseComObject(ComObjQueryResult);
    //  int Количество = (int)Get(Выборка, "Количество()");
    //  object[,] Результат = new object[Количество, ТипыКолонок.Length];
    //  int rowIndex = 0;
    //  while ((bool)Get(Выборка, "Следующий()")) {
    //    int colIndex = 0;
    //    for (int i = 0; i != ЧислоКолВЗапросе; i++) {
    //      ИнфОПоле Поле = ПоляЗапроса[i];
    //      object V8Поле = Get(Выборка, "Получить()", i);
    //      object Значение = ConvertValueV8ToNet(V8Поле, con, Поле.Тип);
    //      ReleaseComObject(V8Поле);  //это может быть не ComObject
    //      Поле.Значение = Значение;
    //      if (!Поле.ЭтоПредст) {
    //        Результат[rowIndex, colIndex++] = Значение;
    //      }
    //    }

    //    foreach (ИнфОПоле полеПредст in ПредставленияЗапроса) {
    //      object предст = полеПредст.Значение;
    //      object ссылка = полеПредст.ПолеСсылки.Значение;
    //      if (ссылка != null) {
    //        PropertyInfo свойство = ссылка.GetType().GetProperty("Представление");
    //        if (свойство != null) {
    //          свойство.SetValue(ссылка, предст, null);
    //          полеПредст.ПолеСсылки.PtyInfo = свойство;
    //        }
    //      }
    //    }
    //    rowIndex++;
    //  }
    //  ReleaseComObject(Выборка);
    //  pool.ReturnDBConnection(con);
    //  return Результат;
    //}
    //****************************************************************************************************************
    //****************************************************************************************************************
    #endregion
  }
}



