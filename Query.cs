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
			string ������� = aRef.GetType().Name; //base!!!!!!!!!
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
				������� = theXmlTypeAttribute.TypeName;
			}
      object ���XML = Call(connection.Connection, connection.Connection.comObject, "NewObject()", "XMLDataType", �������, URINameSpace);
      object V8��� = Call(connection.Connection, connection.Connection.comObject, "FromXMLType()", ���XML);
			ReleaseComObject(���XML);
			object V8Ref = connection.XML��������(V8���, aRef.UUID.ToString());
			ReleaseComObject(V8���);
			return V8Ref;
		}

    protected static object Invoke(ComConnection con, object obj, string �����, BindingFlags Flags, object[] args) {
      //System.Diagnostics.Trace.WriteLine(�����);
      object result;
      try {
        result = obj.GetType().InvokeMember(�����, Flags, null, obj, args);
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

    //public static object Call(DbConnection con, string �����, params object[] list) {
    //  return Call(con.connection, con.connection.comObject, �����, list);
    //}

		public static object Call(ComConnection con, object aComObj, string �����, params object[] list) {
			string[] ������ = �����.Split('.');
			int ����� = ������.Length;
			object res = aComObj;
			BindingFlags Flags;
			for (int i = 0; i < (����� - 1); i++) {
				string �������� = ������[i];
				if (��������.EndsWith("()")) {
					�������� = ��������.Substring(0, ��������.Length - 2);
					Flags = BindingFlags.InvokeMethod;
				} else {
					Flags = BindingFlags.GetProperty;
				}
				object NewRes=Invoke(con, res, ��������, Flags, null);
				if (!aComObj.Equals(res)) {
					ReleaseComObject(res);
				}
				res = NewRes;
			}
			string ����� = ������[����� - 1];
			if (�����.EndsWith("()")) {
				����� = �����.Substring(0, �����.Length - 2);
				Flags = BindingFlags.InvokeMethod;
			} else {
				Flags = BindingFlags.GetProperty;
			}
      object result = Invoke(con, res, �����, Flags, list);
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
			if (value == System.DBNull.Value) { //������������
				value = null;
			} else if (value == null) {
				value = System.DBNull.Value;    //��� ����� NULL � 1�
			} else if (value is ObjectRef) {
				value = Reference((ObjectRef)value, connection);
			//} else if (value is TypeDescription) {
			//  value = ((TypeDescription)value).GetTypeDescription(connection).comObject;
			} else if (value is Enum) {
				if ((value is ��������������������) || (value is ������������������������)) {//��������� ������������
					Type ��������������� = value.GetType();
					string ������������ = ���������������.Name + "." + value.ToString();
          value = Call(connection.Connection, connection.Connection.comObject, ������������);
				} else {
					string Namespace = string.Empty;
					string ������� = value.GetType().Name;
					XmlTypeAttribute theXmlTypeAttribute = ((XmlTypeAttribute)TypeDescriptor.GetAttributes(value)[typeof(XmlTypeAttribute)]);
					if (theXmlTypeAttribute != null) {
						Namespace = theXmlTypeAttribute.Namespace;
						if (Namespace == null) {
							Namespace = string.Empty;
						}
						������� = theXmlTypeAttribute.TypeName;
					}
          Object XMLDataType = Call(connection.Connection, connection.Connection.comObject, "NewObject()", "XMLDataType", �������, Namespace);
					Object ���1� = connection.��XML����(XMLDataType);
					ReleaseComObject(XMLDataType);
					string theString3 = value.ToString();
					if (theString3 == "EmptyRef") {
						theString3 = string.Empty;
					}
					value = connection.XML��������(���1�, theString3);
					ReleaseComObject(���1�);
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
				throw new ArgumentException("COM-������ ������� � ConvertValueNetToV8");
			}
			return value;
		}

		static readonly DateTime ����������=new DateTime(100,1,1);

    public static string ConvertValueV8ToJS(object value, DbConnection connection) {
      string result = null;
			if (value == null) {//��� ������������, ���� ��� ���������� �� System.DBNull.Value
        result = "undefined";
      } else if (value == System.DBNull.Value) { //��� NULL � 1�, ��������� ��� � null
        result = "null";
      } else if (value is bool) {
        result = ((bool)value) ? "true" : "false";
      } else if (value is string) {
        result = @"""" + (System.Web.HttpUtility.HtmlEncode((string)value)).TrimEnd().Replace(@"\", @"\\").Replace(Environment.NewLine, @"<br/>").Replace("\n", @"<br/>") + @"""";
        //result = @"""" + (System.Web.HttpUtility.UrlEncode((string)value)).TrimEnd() + @"""";
        //result = @"""" + ((string)value).TrimEnd().Replace(@"\", @"\\").Replace(@"""", @"\""") + @"""";
			} else if (value is DateTime) {
				DateTime dt = (DateTime)value;
				if (dt == ����������) {
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
        result = @"""" + System.Web.HttpUtility.HtmlEncode(connection.XML������(value)) + @"""";
      } else {
        throw new ArgumentException("Unexpected value type. Check ConvertValueV8ToJS function");
        //result = @"""" + value.ToString() + @"""";
      }
      return result;
    }

		public static object ConvertValueV8ToNet(object value, DbConnection connection, Type aNetType) {
			//������������ "��������������������" � "������������������������" �� ��������
			//������, ��� ������, ����� �� �������� �������� �� 1�
			if (value == null) {//��� ������������, ���� ��� ���������� �� System.DBNull.Value
				value = System.DBNull.Value;
			} else if (value == System.DBNull.Value) { //��� NULL � 1�, ��������� ��� � null
				value = null;
			} else if (value is double) {
				value = ((decimal)((double)value));
			} else if (value is int) {
				value = (decimal)((int)value);
			} else if (value is string) {
				value = (string)value;
			} else if (value is DateTime) {
				DateTime dt = (DateTime)value;
				if (dt == ����������) {
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
				string ��������������� = connection.XML������(value);
				bool ����������� = true;
				if (aNetType == null) {
					����������� = false;
				} else {
          //if (aNetType.IsGenericType) { //���� - ����� nullable �����. ��������, ��� ������������ �� �������
					if (aNetType.IsGenericType && aNetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) { //���� - ����� nullable �����.
						aNetType = Nullable.GetUnderlyingType(aNetType);
						//aNetType = aNetType.GetMethod("get_Value").ReturnType;
					}
					if (aNetType == typeof(object)) { //��������� ���, ������ �� ������ ���������
						����������� = false;
					}
				}
				if (!�����������) {
					object ���������XML = connection.XML������(value);
          string ������� = (string)Call(connection.Connection, ���������XML, "�������");
					ReleaseComObject(���������XML);
					if (V8TypeProvider.Instance.Dic.TryGetValue(�������, out aNetType)) {
						if (aNetType.IsEnum) {
							if (��������������� == string.Empty) {
								��������������� = "EmptyRef";
							}
							value = Enum.Parse(aNetType, ���������������);
						} else {
							object res = Activator.CreateInstance(aNetType);
							((ObjectRef)res).UUID = new Guid(���������������);
							value = res;
						}
					} /*else {
						if (string.Compare(�������, "AccumulationMovementType", true) == 0) {
							value = Enum.Parse(typeof(AccumulationMovementType), ���������������);
						} else if (string.Compare(�������, "AccountType", true) == 0) {
							value = Enum.Parse(typeof(AccountType), ���������������);
						} else if (string.Compare(�������, "AccountingMovementType", true) == 0) {
							value = Enum.Parse(typeof(AccountingMovementType), ���������������);
						}
					}*/
				} else {
					if (aNetType.IsEnum) { //������������ ����!!!!!!!!!
						if (��������������� == string.Empty) {
							��������������� = "EmptyRef";
						}
						value = Enum.Parse(aNetType, ���������������);
					} else {
						object res = Activator.CreateInstance(aNetType);
						((ObjectRef)res).UUID = new Guid(���������������);
						value = res;
					}
				}
				//throw new Exception();
			}
			return value;
		}

    //public static bool ���������������(string �������������, Type ������������, params object[] ������������) {
    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object Res = ������������������������(con, �������������, null);
    //  ReleaseComObject(Res);
    //  pool.ReturnDBConnection(con);
    //  return true;
    //}

    public static object ������������������������(DbConnection con, string �������������) {
      return ������������������������(con, �������������, null);
    }

    public static object ������������������������(DbConnection con, string �������������, ������������[] ����������) {
      object ������������ = Call(con.Connection, con.Connection.comObject, "NewObject()", "������", �������������);
      if (���������� != null) {
        foreach (������������ ��� in ����������) {
          object V8���� = ConvertValueNetToV8(���.����, con);
          Call(con.Connection, ������������, "������������������()", ���.���, V8����);
          ReleaseComObject(V8����); //� ��� ����� � �� ���� ComObject
        }
      }
      object ��� = Call(con.Connection, ������������, "���������()");
			ReleaseComObject(������������);
			return ���;
		}

		//public static List<�����������> ���������������<�����������>(string �������������)
    //  where ����������� : new() {
    //  return ���������������<�����������>(�������������, null); 		
    //}

    //public static List<�����������> ���������������<�����������>(string �������������, ������������[] ����������) 
    //  where �����������:new() {
    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object ComResult = ������������������������(con, �������������, ����������);
    //  List<�����������> lst=QueryReader<�����������>(con, ComResult);
    //  ReleaseComObject(ComResult);
    //  pool.ReturnDBConnection(con);
    //  return lst;
    //}

    //class �������� {
    //  public PropertyInfo PtyInfo;
    //  public int ������������;
    //  public string ���������;
    //  public string �������������;
    //  public bool ���������;
    //  public object ��������;
    //  public int ���������;			//������������ ������ ��� ������ � ���������� ��������
    //  public Type ���;		    	//������������ ������ ��� ������ � ���������� ��������
    //}

		const string ������������������="�������������";

    //public static List<�����������> QueryReader<�����������>(DbConnection connection, object result)
    //  where �����������:new() {

    //  List<��������> ����������� = new List<��������>();
    //  List<�����������> aResList = new List<�����������>();

    //  object ComObj������� = Get(result, "�������");
    //  int ���������������� = (int)Get(ComObj�������, "����������()");
    //  Dictionary<string,int> Dic = new Dictionary<string,int>(����������������);
    //  int[] ��������������������=new int[����������������];
    //  int ����������� = 0;
    //  for (int i = 0; i != ����������������; i++) {
    //    object ComObj������� = Get(ComObj�������, "��������()", i);
    //    string �������� = (string)Get(ComObj�������, "���");
    //    ReleaseComObject(ComObj�������);
    //    Dic[��������] = i;
    //    �������� ����=new ��������();
    //    ����.��������� = ��������.EndsWith(������������������);
    //    if (����.���������) {
    //      ����.��������� = ��������.Substring(0, ��������.Length - ������������������.Length);
    //      ��������������������[�����������] = i;
    //      �����������++;
    //      PropertyInfo pty = typeof(�����������).GetProperty(����.���������);
    //      if (pty == null) {
    //        throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���������);
    //      }
    //      ����.PtyInfo = null;
    //      if (pty.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
    //        ����.PtyInfo = pty.PropertyType.GetProperty("�������������");
    //      } else if (pty.PropertyType != typeof(object)) {
    //        throw new ArgumentException("� ������� ���� " + ����.��������� + " ������ ���� ���� ������ ��� object");
    //      }
    //    } else {
    //      ����.PtyInfo = typeof(�����������).GetProperty(��������);
    //    }
    //    �����������.Add(����);
    //    /*
    //    object ��������� = Get(�����, "�����������.����()");
    //    int �������� = (int)Get(���������, "����������()");
    //    for (int j = 0; j != ��������; j++) {
    //      object ������� = Get(���������, "��������()", j);
    //      object XML������ = Get(connection, "XML���()", �������);
    //      string � = (string)Get(XML������, "URI����������������");
    //      string ������� = (string)Get(XML������, "�������");
    //    } */
    //  }
    //  ReleaseComObject(ComObj�������);
    //  for (int i = 0; i < �����������; i++){
    //    �������� ���� = �����������[��������������������[i]];
    //    ����.������������ = Dic[����.���������];
    //    //PropertyInfo pi = �����������[����.������������].PtyInfo;
    //    //if (pi.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
    //    //  ����.PtyInfo = pi.PropertyType.GetProperty("�������������");
    //    //} else if (pi.PropertyType != typeof(object)) {
    //    //  throw new ArgumentException("� ������� ���� " + ����.��������� + " ������ ����� ��� ������ ��� object");
    //    //}
    //  }

    //  object Com������� = Get(result, "�������()");
    //  while ((bool)Get(Com�������, "���������()")) {
    //    object ��� = new �����������(); //object ��� = Activator.CreateInstance(typeof(�����������));
    //    for (int i = 0; i != ����������������; i++) {
    //      �������� ���� = �����������[i];
    //      object V8���� = Get(Com�������, "��������()", i);
    //      object �������� = ConvertValueV8ToNet(V8����, connection, ����.PtyInfo == null ? null : ����.PtyInfo.PropertyType);
    //      ReleaseComObject(V8����);
    //      if (����.���������) {
    //        ����.�������������=(string)��������;
    //      } else {
    //        ����.�������� = ��������;
    //        ����.PtyInfo.SetValue(���, ��������, null);
    //      }
    //    }
    //    for (int i = 0; i < �����������; i++) {
    //      �������� ���� = �����������[��������������������[i]];
    //      object ������ = ����.�������������;
    //      object ������ = �����������[����.������������].��������;
    //      if (����.PtyInfo != null) {
    //        ����.PtyInfo.SetValue(������, ������, null);
    //      }
    //      else {
    //        PropertyInfo prop = ������.GetType().GetProperty("�������������");
    //        if (prop != null) {
    //          prop.SetValue(������, ������, null);
    //        }
    //      }
    //    }
    //    aResList.Add((�����������)���);
    //  }
    //  ReleaseComObject(Com�������);
    //  return aResList;
    //}

		//		public static IEnumerable<�����������> �������<�����������>(DbConnection connection, object result)
		//			where �����������:new() {
		//			List<FieldInfo> ����������� = new List<FieldInfo>();
		//
		//			object ������� = Get(result, "�������");
		//			int �������� = (int)Get(�������, "����������()");
		//
		//			for (int i = 0; i != ��������; i++) {
		//				object ����� = Get(�������, "��������()", i);
		//				string �������� = (string)Get(�����, "���");
		//				FieldInfo ���� = typeof(�����������).GetField(��������);
		//				�����������.Add(����);
		//			}
		//			object ������� = Get(result, "�������()");
		//			while ((bool)Get(�������, "���������()")) {
		//				object ��� = new �����������(); //object ��� = Activator.CreateInstance(typeof(�����������));
		//				for (int i = 0; i != ��������; i++) {
		//					FieldInfo ���� = �����������[i];
		//					object �������� = ConvertValueV8ToNet(Get(�������, "��������()", i), connection, ����.FieldType);
		//					����.SetValue(���, ��������);
		//				}
		//				yield return (�����������)���;
		//			}
		//		}

    //public static object[,] ���������������(string �������������, ������������[] ����������, 
    //  Type[] �����������, string[] ������������) {
    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object ComObjQueryResult = ������������������������(con, �������������, ����������);

    //  List<��������> ����������� = new List<��������>();
    //  object ComObj������� = Get(ComObjQueryResult, "�������");
    //  int ���������������� = (int)Get(ComObj�������, "����������()");

    //  Dictionary<string, int> Dic = new Dictionary<string, int>(����������������);
    //  int[] �������������������� = new int[����������������];
    //  int ����������� = 0;
    //  int m = 0;
    //  for (int i = 0; i != ����������������; i++) {
    //    object ComObj������� = Get(ComObj�������, "��������()", i);
    //    string �������� = (string)Get(ComObj�������, "���");
    //    ReleaseComObject(ComObj�������);
    //    Dic[��������] = i;
    //    �������� ���� = new ��������();
    //    ����.��������� = ��������.EndsWith(������������������);
    //    if (!����.���������) {
    //      if (m >= �����������.Length) {
    //        throw new ArgumentException(@"������� ���� properties � ������ �������");
    //      }
    //      if (�������� != ������������[m]) {
    //        throw new ArgumentException(@"������ ���� property """ + �������� + @""", � �� """ + ������������[m] + @"""");
    //      }
    //      ����.��� = �����������[m];
    //      ����.��������� = m++;
    //    } else {
    //      ����.��������� = ��������.Substring(0, ��������.Length - ������������������.Length);
    //      ��������������������[�����������] = i;
    //      �����������++;
    //    }
    //    �����������.Add(����);
    //  }
    //  ReleaseComObject(ComObj�������);

    //  for (int i = 0; i < �����������; i++) {
    //    �������� ���� = �����������[��������������������[i]];

    //    if (!Dic.TryGetValue(����.���������, out ����.������������)) {
    //      throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���������);
    //    }
    //    //��������: 1.����������� 2.����������� !!!!!!!!!!
    //    //Type ty = �����������[����.������������].���;
    //    //����.PtyInfo = ty.GetProperty("�������������");
    //    //if (����.PtyInfo == null) {
    //    //  throw new ArgumentException("� ������� ���� " + ����.���������+" ������ ����� ��� ������");
    //    //}
    //  }
			
    //  object ������� = Get(ComObjQueryResult, "�������()");
    //  ReleaseComObject(ComObjQueryResult);
    //  int ���������� = (int)Get(�������, "����������()");
    //  object[,] ��������� = new object[����������, �����������.Length];
    //  int j=0;
    //  while ((bool)Get(�������, "���������()")) {
    //    int k=0;
    //    for (int i = 0; i != ����������������; i++) {
    //      �������� ���� = �����������[i];
    //      object V8���� = Get(�������, "��������()", i);
    //      object �������� = ConvertValueV8ToNet(V8����, con, ����.���);
    //      ReleaseComObject(V8����);  //��� ����� ���� �� ComObject
    //      if (����.���������) {
    //        ����.�������������=(string)��������;
    //      } else {
    //        ����.�������� = ��������;
    //        ���������[j, k++] = ��������;
    //      }
    //    }

    //    for (int i = 0; i < �����������; i++) {
    //      �������� ���� = �����������[��������������������[i]];
    //      object ������ = ����.�������������;
    //      object ������ = �����������[����.������������].��������;
    //      if (������ != null) {
    //        ����.PtyInfo = ������.GetType().GetProperty("�������������");
    //        if (����.PtyInfo != null) {
    //          ����.PtyInfo.SetValue(������, ������, null);
    //        }
    //      }
    //    }
    //    j++;
    //  }
    //  ReleaseComObject(�������);
    //  pool.ReturnDBConnection(con); 
    //  return ���������;	  
    //}

    #region "Andrew's ���������������"
    //****************************************************************************************************************
    //****************************************************************************************************************
    //public static object[,] ���������������(string �������������, ������������[] ����������,
    //  Type[] �����������, string[] ������������) {

    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object ComObjQueryResult = ������������������������(con, �������������, ����������);

    //  object ComObj������� = Get(ComObjQueryResult, "�������");
    //  int ���������������� = (int)Get(ComObj�������, "����������()");
    //  if (�����������.Length != ������������.Length) {
    //    throw new ArgumentException(@"������������ � ����������� �� ���������");
    //  }

    //  List<��������> ����������� = new List<��������>(����������������);

    //  Dictionary<string, ��������> �������������������� = new Dictionary<string, ��������>();
    //  List<��������> �������������������� = new List<��������>();

    //  int index = 0;
    //  while (index < ����������������) {
    //    object ComObj������� = Get(ComObj�������, "��������()", index);
    //    string �������� = (string)Get(ComObj�������, "���");
    //    ReleaseComObject(ComObj�������);

    //    �������� ���� = new ��������();
    //    ����.��������� = ��������.EndsWith(������������������);
    //    if (!����.���������) {
    //      int �������������� = �����������.Count - ��������������������.Count;
    //      if (�������� != ������������[��������������]) {
    //        throw new ArgumentException(@"������ ���� property """ + �������� + @""", � �� """ + ������������[��������������] + @"""");
    //      }
    //      ����.��� = �����������[��������������];
    //      ��������������������.Add(��������, ����);
    //    }
    //    else {
    //      ����.��������� = ��������.Substring(0, ��������.Length - ������������������.Length);
    //      ��������������������.Add(����);
    //    }
    //    �����������.Add(����);
    //    index++;
    //  }
    //  ReleaseComObject(ComObj�������);

    //  foreach (�������� ���� in ��������������������) {
    //    �������� ���������� = null;
    //    if (��������������������.TryGetValue(����.���������, out ����������)) {
    //      ����.���������� = ����������;
    //    }
    //    else {
    //      throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���������);
    //    }
    //  }

    //  object ������� = Get(ComObjQueryResult, "�������()");
    //  ReleaseComObject(ComObjQueryResult);
    //  int ���������� = (int)Get(�������, "����������()");
    //  object[,] ��������� = new object[����������, �����������.Length];
    //  int rowIndex = 0;
    //  while ((bool)Get(�������, "���������()")) {
    //    int colIndex = 0;
    //    for (int i = 0; i != ����������������; i++) {
    //      �������� ���� = �����������[i];
    //      object V8���� = Get(�������, "��������()", i);
    //      object �������� = ConvertValueV8ToNet(V8����, con, ����.���);
    //      ReleaseComObject(V8����);  //��� ����� ���� �� ComObject
    //      ����.�������� = ��������;
    //      if (!����.���������) {
    //        ���������[rowIndex, colIndex++] = ��������;
    //      }
    //    }

    //    foreach (�������� ���������� in ��������������������) {
    //      object ������ = ����������.��������;
    //      object ������ = ����������.����������.��������;
    //      if (������ != null) {
    //        PropertyInfo �������� = ������.GetType().GetProperty("�������������");
    //        if (�������� != null) {
    //          ��������.SetValue(������, ������, null);
    //          ����������.����������.PtyInfo = ��������;
    //        }
    //      }
    //    }
    //    rowIndex++;
    //  }
    //  ReleaseComObject(�������);
    //  pool.ReturnDBConnection(con);
    //  return ���������;
    //}
    //****************************************************************************************************************
    //****************************************************************************************************************
    #endregion
  }
}



