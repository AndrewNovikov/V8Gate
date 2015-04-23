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
			V8Object obj = ObjectCache.����������(oRef);
			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object ComV8Ref = V8Gate.V8A.Reference(oRef, con);

			foreach (PropertyInfo fi in obj.�������������) {
				if (fi.Name != "������") {
					object ComResult = V8Gate.V8A.Get(ComV8Ref, fi.Name); //.Substring(1));
					object value = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, fi.PropertyType);
					V8Gate.V8A.ReleaseComObject(ComResult);
					fi.SetValue(obj, value, null);
				}
			}
			foreach (PropertyInfo T in obj.�����������������) {
				IList TR = (IList)T.GetValue(obj, null);
				IV8TablePart IA = (IV8TablePart)TR;
				//if (IA.�������()) {
				TR.Clear();
				PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				object �������������� = V8Gate.V8A.Get(ComV8Ref, T.Name);
				V8Gate.V8A.ReleaseComObject(ComV8Ref);
				int ���������� = (int)V8Gate.V8A.Get(��������������, "����������()");
				//��� ������ ������ ��������
				for (int i = 0; i < ����������; i++) {
					object Com��������������� = V8Gate.V8A.Get(��������������, "��������()", i);
					object ����������� = IA.��������();
					foreach (PropertyInfo ������� in �������) {
						object V8���� = V8Gate.V8A.Get(Com���������������, �������.Name);
						object �������� = V8Gate.V8A.ConvertValueV8ToNet(V8����, con, �������.PropertyType);
						V8Gate.V8A.ReleaseComObject(V8����);
						�������.SetValue(�����������, ��������, null);
					}
					V8Gate.V8A.ReleaseComObject(Com���������������);
				}
				V8Gate.V8A.ReleaseComObject(��������������);
			}
			pool.ReturnDBConnection(con);
			return obj;
		}

		public override V8Object Load2(ObjectRef oRef) {
			ObjectRef ������ = oRef;
			V8Object obj = ObjectCache.����������(oRef);
			string[] ���������� = ������.GetType().Name.Split(new char[] { '_' });
			string �������������� = ����������[0];  //����� ��� ��� ���
			string ���������� = ����������[1];
			if (��������������.StartsWith("���")) {
				�������������� = "����������.";
			} else if (��������������.StartsWith("���")) {
				�������������� = "��������.";
			}
			StringBuilder ������������ = new StringBuilder("������� " + ���������� + ".�������������", 2000);
			foreach (PropertyInfo fi in obj.�������������) {
				if (fi.Name != "������") {
					Type ������� = fi.PropertyType;
					string fldName = fi.Name; //.Substring(1);
					������������.Append("," + ���������� + "." + fldName);
					if (�������.IsSubclassOf(typeof(ObjectRef))) {
						������������.Append("," + ���������� + "." + fldName + ".�������������");
					}
				}
			}

			foreach (PropertyInfo T in obj.�����������������) {
				������������.Append("," + ���������� + "." + T.Name + ".(");
				IList TR = (IList)T.GetValue(obj, null);
				IV8TablePart IA = (IV8TablePart)TR;
				//if (IA.�������()) {
				//���-�� ���� ������������ ������ "" ������������ string.Empty �Andrew
				string �����������2 = "";
				PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				foreach (PropertyInfo ������� in �������) {
					string fldName = �������.Name;
					������������.Append(�����������2 + fldName);
					�����������2 = ",";
					if (�������.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
						������������.Append("," + fldName + ".�������������");
					}
				}
				//}
				������������.Append(")");
			}
			������������.Append(" �� " + �������������� + ���������� + " ��� " + ����������);
			������������.Append(" ���	" + ���������� + ".������ = &������");

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object ComV8Ref = V8Gate.V8A.Reference(������, con);
			������������[] ��� = new ������������[] { new ������������("������", ComV8Ref) };
			object result = V8Gate.V8A.������������������������(con, ������������.ToString(), ���);
			V8Gate.V8A.ReleaseComObject(ComV8Ref); //������!!!!!!!!!!!!!!!
			object ������� = V8Gate.V8A.Get(result, "�������()");
			V8Gate.V8A.ReleaseComObject(result);
			if ((bool)V8Gate.V8A.Get(�������, "���������()")) { //������ ������ ������� ����� 1 ������
				int i = 0;
				bool ��������� = true;
				object V8����;
				foreach (PropertyInfo fi in obj.�������������) {
					if (fi.Name != "������") {
						if (���������) { //������ ��� ������������� ��� ������. ���� ������ �������� �������
							��������� = false;
							V8���� = V8Gate.V8A.Get(�������, "��������()", i++);
							������.������������� = (string)V8Gate.V8A.ConvertValueV8ToNet(V8����, con, typeof(string));
							//V8Gate.V8A.ReleaseComObject(V8����); �� ����, ��� ������
						}
						V8���� = V8Gate.V8A.Get(�������, "��������()", i++);
						object �������� = V8Gate.V8A.ConvertValueV8ToNet(V8����, con, fi.PropertyType);
						V8Gate.V8A.ReleaseComObject(V8����);
						ObjectRef oref = �������� as ObjectRef;
						if (oref != null) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
							V8���� = V8Gate.V8A.Get(�������, "��������()", i++);
              string ������������� = (string)V8Gate.V8A.ConvertValueV8ToNet(V8����, con, typeof(string));
              oref.������������� = ������������� == null ? string.Empty : �������������;
              //V8Gate.V8A.ReleaseComObject(V8����); �� ����, ��� ������
						}
						fi.SetValue(obj, ��������, null);
					}
				}
				foreach (PropertyInfo T in obj.�����������������) {
					IList TR = (IList)T.GetValue(obj, null);
					IV8TablePart IA = (IV8TablePart)TR;
					TR.Clear();
					PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
					object Com������������������ = V8Gate.V8A.Get(�������, "��������()", i++);
					object Com����������� = V8Gate.V8A.Get(Com������������������, "�������()");
					V8Gate.V8A.ReleaseComObject(Com������������������);
					while ((bool)V8Gate.V8A.Get(Com�����������, "���������()")) {
						object ����������� = IA.��������();
						int j = 0;
						foreach (PropertyInfo ������� in �������) {
							V8���� = V8Gate.V8A.Get(Com�����������, "��������()", j++);
							Type ������� = �������.PropertyType;
							object �������� = V8Gate.V8A.ConvertValueV8ToNet(V8����, con, �������);
							V8Gate.V8A.ReleaseComObject(V8����);

							ObjectRef oref = �������� as ObjectRef;
							if (�������.IsSubclassOf(typeof(ObjectRef))) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
								//if (oref != null) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
								object ComResult = V8Gate.V8A.Get(Com�����������, "��������()", j++);
								string ������������� = (string)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(string));
								oref.������������� = ������������� == null ? string.Empty : �������������;
								//V8Gate.V8A.ReleaseComObject(ComResult); �� ����, ��� ������
							} else if (oref != null) {
								oref.���������������������();
							}

							�������.SetValue(�����������, ��������, null);
						}
					}
					V8Gate.V8A.ReleaseComObject(Com�����������);
				}
			}
			V8Gate.V8A.ReleaseComObject(�������);
			pool.ReturnDBConnection(con);
			return obj;
		}

		public override ObjectRef ��������(V8Object obj) {
			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object V8ComObj = ��������������������(con, obj);
			if (obj is ����������) {
				�����������(con, obj, V8ComObj);
			}
			V8Gate.V8A.ReleaseComObject(V8ComObj);
			pool.ReturnDBConnection(con);
			return obj.������;
		}

		public override ObjectRef ����������������(�������� obj, �������������������� ����������, ������������������������ ��������) {
			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object V8ComObj = ��������������������(con, obj);
			�����������(con, obj, V8ComObj, ����������, ��������);
			V8Gate.V8A.ReleaseComObject(V8ComObj);
			pool.ReturnDBConnection(con);
			return obj.������;
		}

		private object ��������������������(DbConnection con, V8Object obj) {
			ObjectRef ������ = obj.������;
			object V8ComObj = null;
			if (!������.IsEmpty()) {
				object V8Ref = V8Gate.V8A.Reference(������, con);
				V8ComObj = V8Gate.V8A.Get(V8Ref, "��������������()");
				V8Gate.V8A.ReleaseComObject(V8Ref);
			} else {
				string[] ���������� = ������.GetType().Name.Split(new char[] { '_' });
				string �������������� = ����������[0];  //����� ��� ��� ���
				string ���������� = ����������[1];
				if (��������������.StartsWith("���")) {
					//���������� ��� = obj as ����������;
					PropertyInfo pi = obj.GetType().GetProperty("���������");
					bool ������������� = false;
					if (pi != null && (bool)(pi.GetValue(obj, null) ?? false)) {
						������������� = true;
					}
					if (�������������) {
						//if (���._��������� ?? false) {
						�������������� = "�����������." + ���������� + ".�������������()";
					} else {
						�������������� = "�����������." + ���������� + ".��������������()";
					}
				} else if (��������������.StartsWith("���")) {
					�������������� = "���������." + ���������� + ".���������������()";
				}
				V8ComObj = V8Gate.V8A.Get(con, ��������������);
			}

			foreach (PropertyInfo fi in obj.�������������) {
				if (fi.Name != "������") {
					object value = fi.GetValue(obj, null);
					if (value != null) {
						object V8Value = V8Gate.V8A.ConvertValueNetToV8(value, con);
						V8Gate.V8A.SetProp(V8ComObj, fi.Name, V8Value);
						V8Gate.V8A.ReleaseComObject(V8Value);
					}
				}
			}
			foreach (PropertyInfo T in obj.�����������������) {
				IList TR = (IList)T.GetValue(obj, null);
				IV8TablePart IA = (IV8TablePart)TR;
				//if (IA.�������()) {
				PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				object �������������� = V8Gate.V8A.Get(V8ComObj, T.Name);
				V8Gate.V8A.Get(��������������, "��������()");
				foreach (object Str in TR) {
					object Com�������� = V8Gate.V8A.Get(��������������, "��������()");
					foreach (PropertyInfo f in �������) {
						object value = V8Gate.V8A.ConvertValueNetToV8(f.GetValue(Str, null), con);
						V8Gate.V8A.SetProp(Com��������, f.Name, value);
					}
					V8Gate.V8A.ReleaseComObject(Com��������);
				}
				V8Gate.V8A.ReleaseComObject(��������������);
			}
			return V8ComObj;
		}

		private void �����������(DbConnection con, V8Object obj, object V8Obj) {
			ObjectRef ������ = obj.������;
			V8Gate.V8A.Get(V8Obj, "��������()");
			if (������.IsEmpty()) {
				object ComResult = V8Gate.V8A.Get(V8Obj, "������");
				obj.������ = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, null) as V8Gate.ObjectRef;
				V8Gate.V8A.ReleaseComObject(ComResult);
				ObjectCache.��������(obj.������, obj);
				//if (obj is ����������) { //�� ������ ������ ��������, ����� �����������
				//  PropertyInfo pi = obj.GetType().GetProperty("���", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				//  object value = V8Gate.V8A.ConvertValueV8ToNet(V8Gate.V8A.Get(V8Obj, "���"), con, null);
				//  pi.SetValue(obj, value, null);
				//}
			}
		}

		private void �����������(DbConnection con, �������� obj, object V8Obj, �������������������� ����������, ������������������������ ��������) {
			object ComObj��� = V8A.ConvertValueNetToV8(����������, con);
			object ComObj���� = V8A.ConvertValueNetToV8(��������, con);
			ObjectRef ������ = obj.������;
			V8Gate.V8A.Get(V8Obj, "��������()", ComObj���, ComObj����);
			V8Gate.V8A.ReleaseComObject(ComObj����);
			V8Gate.V8A.ReleaseComObject(ComObj���);
			if (������.IsEmpty()) {
				object ComResult = V8Gate.V8A.Get(V8Obj, "������");
				obj.������ = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, null) as V8Gate.ObjectRef;
				V8Gate.V8A.ReleaseComObject(ComResult);
				ObjectCache.��������(obj.������, obj);
				//PropertyInfo pi = obj.GetType().GetProperty("�����", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				//object value = V8Gate.V8A.ConvertValueV8ToNet(V8Gate.V8A.Get(V8Obj, "�����"), con, null);
				//pi.SetValue(obj, value, null);
			}
		}

		public override T ������������<T>(ObjectRef oRef, string aName) {
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

//		internal override �������������������(CatalogRef obj, string �����) {
//			string[] ���������� = obj.GetType().Name.Split(new char[] { '_' });
//			string ���������� = ����������[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "�����������." + ���������� + ".�������������������()", �����);
//			CatalogRef res = (CatalogRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID = res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

//		internal override CatalogRef �����������(IntNumCatRef obj, int ����) {
//			string[] ���������� = obj.GetType().Name.Split(new char[] { '_' });
//			string ���������� = ����������[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "�����������." + ���������� + ".�����������()", ����);
//			CatalogRef res = (CatalogRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID=res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

//		public override CatalogRef �����������(StrNumCatRef obj, string ����) {
//			string[] ���������� = obj.GetType().Name.Split(new char[] { '_' });
//			string ���������� = ����������[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "�����������." + ���������� + ".�����������()", ����);
//			CatalogRef res = (CatalogRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID=res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

//		internal override StrNumDocRef �������������(StrNumDocRef obj, string ������, DateTime �����) {
//			string[] ���������� = obj.GetType().Name.Split(new char[] { '_' });
//			string ���������� = ����������[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "���������." + ���������� + ".�������������()", ������, �����);
//			StrNumDocRef res = (StrNumDocRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID=res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

//		internal override IntNumDocRef �������������(IntNumDocRef obj, int ������, DateTime �����) {
//			string[] ���������� = obj.GetType().Name.Split(new char[] { '_' });
//			string ���������� = ����������[1];
//
//			DbConnections pool = DbConnections.Instance;
//			DbConnection con = pool.ConnectV8();
//			//object V8Ref = V8Gate.V8A.Reference(obj, con);
//			object ComResult = V8Gate.V8A.Get(con, "���������." + ���������� + ".�������������()", ������, �����);
//			IntNumDocRef res = (IntNumDocRef)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, obj.GetType());
//			V8Gate.V8A.ReleaseComObject(ComResult);
//			//obj.UUID=res.UUID;
//			pool.ReturnDBConnection(con);
//			return res;
//		}

		public override ��� �������������������<���>(string �����) {
			string[] ���������� = typeof(���).Name.Split(new char[] { '_' });
			string ���������� = ����������[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "�����������." + ���������� + ".�������������������()", �����);
			��� res = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID = res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}

		public override ��� ����������������<���>(string �������������, object ������������������, ��� ���������, object ���������) {
			string[] ���������� = typeof(���).Name.Split(new char[] { '_' });
			string ���������� = ����������[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			object Com�������� = V8Gate.V8A.ConvertValueNetToV8(������������������, con);
			object Com�������� = null;
			if (��������� != null) {
				Com�������� = V8Gate.V8A.ConvertValueNetToV8(���������, con);
			}
			object Com�������� = null;
			if (��������� != null) {
				Com�������� = V8Gate.V8A.ConvertValueNetToV8(���������, con);
			}
			object ComResult = V8Gate.V8A.Get(con, "�����������." + ���������� + ".����������������()", �������������, Com��������, Com��������, Com��������);
			��� result = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
			V8Gate.V8A.ReleaseComObject(ComResult);
			V8Gate.V8A.ReleaseComObject(Com��������);
			V8Gate.V8A.ReleaseComObject(Com��������);
			pool.ReturnDBConnection(con);
			return result;
		}

		public override ��� �����������<���>(string ����) {
			string[] ���������� = typeof(���).Name.Split(new char[] { '_' });
			string ���������� = ����������[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "�����������." + ���������� + ".�����������()", ����);
			��� res = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID=res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}

		public override ��� �����������<���>(int ����) {
			string[] ���������� = typeof(���).Name.Split(new char[] { '_' });
			string ���������� = ����������[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "�����������." + ���������� + ".�����������()", ����);
			��� res = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID=res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}

		public override ��� �������������<���>(string ������, DateTime �����) {
			string[] ���������� = typeof(���).Name.Split(new char[] { '_' });
			string ���������� = ����������[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "���������." + ���������� + ".�������������()", ������, �����);
			��� res = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID=res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}

		public override ��� �������������<���>(int ������, DateTime �����) {
			string[] ���������� = typeof(���).Name.Split(new char[] { '_' });
			string ���������� = ����������[1];

			DbConnections pool = DbConnections.Instance;
			DbConnection con = pool.ConnectV8();
			//object V8Ref = V8Gate.V8A.Reference(obj, con);
			object ComResult = V8Gate.V8A.Get(con, "���������." + ���������� + ".�������������()", ������, �����);
			��� res = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
			V8Gate.V8A.ReleaseComObject(ComResult);
			//obj.UUID=res.UUID;
			pool.ReturnDBConnection(con);
			return res;
		}
		
		public override object[,] ���������������(string �������������, ������������[] ����������,
			Type[] �����������, string[] ������������) {
			return V8A.���������������(�������������, ����������, �����������, ������������);
			//return null;
		}


		//		public override List<�����������> ���������<�����������>(string �������������) {
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object Res = V8A.������������������������(con, �������������);
		//			List<�����������> LstQ = V8A.QueryReader<�����������>(con, Res);
		//			pool.ReturnDBConnection(con);
		//			return LstQ;
		//		}

		//		public override List<�����������> ���������<�����������>(string �������������, ������������[] ����������) {
		//			DbConnections pool = DbConnections.Instance;
		//			DbConnection con = pool.ConnectV8();
		//			object Res = V8A.������������������������(con, �������������, ����������);
		//			List<�����������> LstQ = V8A.QueryReader<�����������>(con, Res);
		//			pool.ReturnDBConnection(con);
		//			return LstQ;
		//		}


	}
}
