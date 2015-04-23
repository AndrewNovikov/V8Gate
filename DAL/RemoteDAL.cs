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
		
		public override ObjectRef ��������(V8Object obj) {
			DAL.Instance.Swap();
			obj.��������();
			DAL.Instance.Swap();
			return obj.������;
		}

		public override ObjectRef ����������������(�������� obj, �������������������� ����������, ������������������������ ��������) {
			DAL.Instance.Swap();
			obj.��������(����������, ��������);
			DAL.Instance.Swap();
			return obj.������;
		}

		public override T ������������<T>(ObjectRef oRef, string aName) {
			DAL.Instance.Swap();
			V8Object obj = ObjectCache.����������(oRef);
			PropertyInfo pi = oRef.����������().GetProperty(aName);
			object value = pi.GetValue(obj, null);
			DAL.Instance.Swap();
			return (T)value;
		}

		public override V8Object Load(ObjectRef oRef) {
			DAL.Instance.Swap();
			V8Object V8obj = ObjectCache.����������(oRef);
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
			V8Object V8obj = ObjectCache.����������(oRef);
			V8obj.Load2();
			DAL.Instance.Swap();
			return V8obj;
		}

//		internal override CatalogRef �������������������(CatalogRef obj, string �����) {
//			DAL.Instance.Swap();
//			obj.�������������������(�����);
//			DAL.Instance.Swap();
//			return obj;
//		}

//		internal override CatalogRef �����������(IntNumCatRef obj, int ����) {
//			DAL.Instance.Swap();
//			obj.�����������(����);
//			DAL.Instance.Swap();
//			return obj;
//		}

//		public override CatalogRef �����������(StrNumCatRef obj, string ����) {
//			DAL.Instance.Swap();
//			obj.�����������(����);
//			DAL.Instance.Swap();
//			return obj;
//		}

//		internal override StrNumDocRef �������������(StrNumDocRef obj, string ������, DateTime �����) {
//			DAL.Instance.Swap();
//			obj.�������������(������, �����);
//			DAL.Instance.Swap();
//			return obj;
//		}

//		internal override IntNumDocRef �������������(IntNumDocRef obj, int ������, DateTime �����) {
//			DAL.Instance.Swap();
//			obj.�������������(������, �����);
//			DAL.Instance.Swap();
//			return obj;
//		}

		public override ��� �������������������<���>(string �����) {
			DAL.Instance.Swap();
			��� result = DAL.Instance.CurDAL.�������������������<���>(�����);
			DAL.Instance.Swap();
			return result;
		}

		public override ��� ����������������<���>(string �������������, object ������������������, ��� ���������, object ���������) {
			DAL.Instance.Swap();
			��� result = DAL.Instance.CurDAL.����������������<���>(�������������, ������������������, ���������, ���������);
			DAL.Instance.Swap();
			return result;
		}

		public override ��� �����������<���>(string ����) {
			DAL.Instance.Swap();
			��� result = DAL.Instance.CurDAL.�����������<���>(����);
			DAL.Instance.Swap();
			return result;
		}

		public override ��� �����������<���>(int ����) {
			DAL.Instance.Swap();
			��� result = DAL.Instance.CurDAL.�����������<���>(����);
			DAL.Instance.Swap();
			return result;
		}

		public override ��� �������������<���>(string ������, DateTime �����) {
			DAL.Instance.Swap();
			��� result = DAL.Instance.CurDAL.�������������<���>(������, �����);
			DAL.Instance.Swap();
			return result;
		}

		public override ��� �������������<���>(int ������, DateTime �����) {
			DAL.Instance.Swap();
			��� result = DAL.Instance.CurDAL.�������������<���>(������, �����);
			DAL.Instance.Swap();
			return result;
		}

		public override object[,] ���������������(string �������������, ������������[] ����������, 
			Type[] �����������, string[] ������������) {
			DAL.Instance.Swap();
			object[,] result = DAL.Instance.CurDAL.���������������(�������������, ����������, �����������, ������������);
			DAL.Instance.Swap();
			return result;
			//return V8A.���������������(�������������, ����������, �����������);
		}
	}

	public static class MyProxy {

		public static List<�����������> ���������������<�����������>(string �������������) where ����������� : new() {
			return ���������������<�����������>(�������������, null);
		}

		public static List<�����������> ���������������<�����������>(string �������������, ������������[] ����������)
		where ����������� : new(){
			PropertyInfo[] ������� = typeof(�����������).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			bool[] �������� = new bool[�������.Length];
			int ����������� = 0;
			int i=0;
			foreach (PropertyInfo ���� in �������) {
				if (!Attribute.IsDefined(����, typeof(SkipAttribute))) {
					��������[i] = true;
					�����������++;
				}
				i++;
			}

			Type[] ������������ = new Type[�����������];
			string[] ������������� = new string[�����������];
			i = 0;
			int col = 0;
			foreach (PropertyInfo ���� in �������) {
				if (��������[i++]) {
					������������[col] = ����.PropertyType;
					�������������[col] = ����.Name;
					col++;
				}
			}

			object[,] lst2 = DAL.Instance.CurDAL.���������������(�������������, ����������, ������������, �������������);
			int rows = lst2.GetLength(0);
			int cols = lst2.GetLength(1);
			List<�����������> lst3 = new List<�����������>(rows);
			for (int row = 0; row < rows; row++) {
				//object[] ���=lst2[k,*];
				����������� ��� = new �����������();
				for (col = 0; col < cols; col++) {
					�������[col].SetValue(���, lst2[row, col], null);
				}
				lst3.Add(���);
			}
			return lst3;
		}
		
	}
}
