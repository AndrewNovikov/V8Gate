using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace V8Gate {

  #region "some old stuff"
  //[System.Diagnostics.DebuggerStepThrough()]
  //public sealed class DAL {
  //  private AbstractDAL _CurDAL;
  //  private AbstractDAL _AltDAL;

  //  public AbstractDAL CurDAL {
  //    get { return _CurDAL; }
  //    set { _CurDAL = value; }
  //  }

  //  public AbstractDAL AltDAL {
  //    set { _AltDAL = value; }
  //  }

  //  public void Swap() {
  //    AbstractDAL temp = _CurDAL;
  //    _CurDAL = _AltDAL;
  //    AltDAL = temp;
  //  }
  //  static readonly DAL instance = new DAL();

  //  // Explicit static constructor to tell C# compiler
  //  // not to mark type as beforefieldinit
  //  static DAL() {
  //  }

  //  private DAL() {
  //    CurDAL = GetDal("1C_CurDal");
  //    AltDAL = GetDal("1C_AltDal");
  //  }

  //  public static DAL Instance {
  //    get {
  //      return instance;
  //    }
  //  }

  //  private AbstractDAL GetDal(string strDalType) {
  //    string strCurDal = System.Configuration.ConfigurationManager.AppSettings[strDalType];
  //    if (strCurDal == null) {
  //      strCurDal = "";
  //    } else {
  //      strCurDal = strCurDal.ToUpper();
  //    }
  //    if (strCurDal == "COM") {
  //      return new ComDAL();
  //    } else if (strCurDal == "REMOTE") {
  //      return new RemoteDAL();
  //    } else {
  //      throw new Exception("� AppSettings �������� " + strDalType + @" ����� ���� ������ COM ��� REMOTE, � �� """ + strCurDal + @"""");
  //    }
  //  }
  //}


  //public interface IRemoteDAL {	 ///����� �������� RemoteDAL
  //}

  //public abstract class AbstractDAL : MarshalByRefObject {

  //  public abstract V8Object Load(ObjectRef oRef);

  //  public abstract V8Object Load2(ObjectRef oRef);

  //  public abstract ObjectRef ��������(V8Object obj);	//��� ����� �������� ����� ������������ �������� - ����������� ������

  //  public abstract ObjectRef ����������������(�������� obj, �������������������� ����������, ������������������������ ��������);

  //  public abstract T ������������<T>(ObjectRef oRef, string aName);

  //  //		internal abstract CatalogRef �������������������(CatalogRef obj, string �����);

  //  //		internal abstract CatalogRef �����������(IntNumCatRef obj, int ����);

  //  //		public abstract CatalogRef �����������(StrNumCatRef obj, string ����);

  //  //		internal abstract StrNumDocRef �������������(StrNumDocRef obj, string ������, DateTime �����);

  //  //		internal abstract IntNumDocRef �������������(IntNumDocRef obj, int ������, DateTime �����);

  //  public abstract ��� �������������������<���>(string �����) where ��� : CatalogRef;

  //  public abstract ��� ����������������<���>(string �������������, object ������������������, ��� ���������, object ���������) where ��� : CatalogRef;

  //  public abstract ��� �����������<���>(string ����) where ��� : StrNumCatRef;

  //  public abstract ��� �����������<���>(int ����) where ��� : IntNumCatRef;

  //  public abstract ��� �������������<���>(string ������, DateTime �����) where ��� : StrNumDocRef;

  //  public abstract ��� �������������<���>(int ������, DateTime �����) where ��� : IntNumDocRef;

  //  //public abstract List<�����������> ���������<�����������>(string �������������)
  //  //  where ����������� : new();

  //  //public abstract List<�����������> ���������<�����������>(string �������������, ������������[] ����������)
  //  //  where ����������� : ISerializable, new();

  //  public abstract object[,] ���������������(string �������������, ������������[] ����������,
  //    Type[] �����������, string[] ������������);

  //  public abstract void ClearPool();

  //}
  #endregion

  #region "DALEngine"
  internal static class DALEngine {
    const string ������������������ = "�������������";

    private static string ������������������(string �������) {
      int sepIndex = �������.IndexOf('_');
      if (sepIndex == -1) {
        throw new ArgumentException("��� " + ������� + " �� �������� ��������");
      }
      return �������.Substring(++sepIndex, �������.Length - sepIndex);
    }

    private static void �������������������������(string �������, out string �������, out string ���) {
      int sepIndex = �������.IndexOf('_');
      if (sepIndex == -1) {
        throw new ArgumentException("��� " + ������� + " �� �������� ��������");
      }
      ������� = �������.Substring(0, sepIndex);
      ��� = �������.Substring(++sepIndex, �������.Length - sepIndex);
    }

    public static string ���������������JS(Func<DbConnection> getCon, string �������������, ������������[] ����������) {
      using (DbConnection con = getCon()) {
        object ComObjQueryResult = V8A.������������������������(con, �������������, ����������);

        object ComObj������� = V8A.Call(con.Connection, ComObjQueryResult, "�������");
        int ���������������� = (int)V8A.Call(con.Connection, ComObj�������, "����������()");

        Dictionary<string, ��������> ���� = new Dictionary<string, ��������>(����������������);
        List<��������> ����������������� = new List<��������>(���������������� / 2);

        for (int i = 0; i != ����������������; i++) {
          object ComObj������� = V8A.Call(con.Connection, ComObj�������, "��������()", i);
          string �������� = (string)V8A.Call(con.Connection, ComObj�������, "���");
          V8A.ReleaseComObject(ComObj�������);
          �������� ���� = new ��������();
          ����.������ = i;
          if (��������.EndsWith(������������������)) {
            ����.��� = ��������.Substring(0, ��������.Length - ������������������.Length);
            �����������������.Add(����);
          } else {
            ����.��� = ��������;
            ����.Add(��������, ����);
          }
        }
        V8A.ReleaseComObject(ComObj�������);

        foreach (�������� ���� in �����������������) {
          �������� ����������;
          if (!����.TryGetValue(����.���, out ����������)) {
            throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���);
          } else {
            ����������.����������������� = ����;
          }
        }

        object Com������� = V8A.Call(con.Connection, ComObjQueryResult, "�������()");
        V8A.ReleaseComObject(ComObjQueryResult);
        StringBuilder ��������� = new StringBuilder("[");
        if ((bool)V8A.Call(con.Connection, Com�������, "���������()")) {
          ���������.Append("{");
          foreach (�������� ���� in ����.Values) {
            object V8���� = V8A.Call(con.Connection, Com�������, "��������()", ����.������);
            object �������� = V8A.ConvertValueV8ToJS(V8����, con);
            V8A.ReleaseComObject(V8����);
            if (����.����������������� != null) {
              string ������������� = V8A.Call(con.Connection, Com�������, "��������()", ����.�����������������.������) as string;
              ���������.Append(@"""" + ����.��� + @""":{""guid"":" + �������� + @",""�������������"":""" + System.Web.HttpUtility.HtmlEncode(�������������) + @"""},");
            } else {
              ���������.Append(@"""" + ����.��� + @""":" + �������� + @",");
            }
          }
          ���������.Remove(���������.Length - 1, 1);
          ���������.Append("}");
          while ((bool)V8A.Call(con.Connection, Com�������, "���������()")) {
            ���������.Append(",{");
            foreach (�������� ���� in ����.Values) {
              object V8���� = V8A.Call(con.Connection, Com�������, "��������()", ����.������);
              object �������� = V8A.ConvertValueV8ToJS(V8����, con);
              V8A.ReleaseComObject(V8����);
              if (����.����������������� != null) {
                string ������������� = V8A.Call(con.Connection, Com�������, "��������()", ����.�����������������.������) as string;
                ���������.Append(@"""" + ����.��� + @""":{""guid"":" + �������� + @",""�������������"":""" + System.Web.HttpUtility.HtmlEncode(�������������) + @"""},");
              } else {
                ���������.Append(@"""" + ����.��� + @""":" + �������� + @",");
              }
            }
            ���������.Remove(���������.Length - 1, 1);
            ���������.Append("}");
          }
        }
        ���������.Append("]");
        V8A.ReleaseComObject(Com�������);
        return ���������.ToString();
      }
    }

    public static List<�������> ���������������<�������>(Func<DbConnection> getCon, string �������������, ������������[] ����������) where �������:new() {
      using (DbConnection con = getCon()) {
        object ComObjQueryResult = V8A.������������������������(con, �������������, ����������);

        object ComObj������� = V8A.Call(con.Connection, ComObjQueryResult, "�������");
        int ���������������� = (int)V8A.Call(con.Connection, ComObj�������, "����������()");
        List<��������> ����������� = new List<��������>(����������������);
        Dictionary<string, int> ����������2��� = new Dictionary<string, int>(����������������);
        int[] �������������������� = new int[����������������];
        int ����������� = 0;
        for (int i = 0; i != ����������������; i++) {
          object ComObj������� = V8A.Call(con.Connection, ComObj�������, "��������()", i);
          string �������� = (string)V8A.Call(con.Connection, ComObj�������, "���");
          V8A.ReleaseComObject(ComObj�������);
          ����������2���[��������] = i;
          �������� ���� = new ��������();
          ����.��������� = ��������.EndsWith(������������������);
          if (����.���������) {
            ����.��������� = ��������.Substring(0, ��������.Length - ������������������.Length);
            ����.��� = typeof(string);
            ��������������������[�����������] = i;
            �����������++;
          } else {
            ����.PtyInfo = typeof(�������).GetProperty(��������);
            ����.��� = ����.PtyInfo.PropertyType;
          }
          �����������.Add(����);

        }
        V8A.ReleaseComObject(ComObj�������);
        for (int i = 0; i < �����������; i++) {
          �������� ���� = �����������[��������������������[i]];

          if (!����������2���.TryGetValue(����.���������, out ����.������������)) {
            throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���������);
          } else {
            �������� ���������� = �����������[����.������������];
            if (����������.���.IsSubclassOf(typeof(ObjectRef))) {
              ����.PtyInfo = ����������.���.GetProperty(������������������);
            } else if (����������.��� != typeof(object)) {
              throw new ArgumentException("� ������� ���� " + ����������.��������� + " ������ ���� ���� ������ ��� object");
            }
          }
        }

        object Com������� = V8A.Call(con.Connection, ComObjQueryResult, "�������()");
        V8A.ReleaseComObject(ComObjQueryResult);
        List<�������> ��������� = new List<�������>((int)V8A.Call(con.Connection, Com�������, "����������()"));

        while ((bool)V8A.Call(con.Connection, Com�������, "���������()")) {
          object ��� = new �������(); //object ��� = Activator.CreateInstance(typeof(�����������));
          for (int i = 0; i != ����������������; i++) {
            �������� ���� = �����������[i];
            object V8���� = V8A.Call(con.Connection, Com�������, "��������()", i);
            object �������� = V8A.ConvertValueV8ToNet(V8����, con, ����.���);
            V8A.ReleaseComObject(V8����);
            if (����.���������) {
              ����.������������� = (string)��������;
            } else {
              ����.�������� = ��������;
              ����.PtyInfo.SetValue(���, ��������, null);
            }
          }
          for (int i = 0; i < �����������; i++) {
            �������� ���� = �����������[��������������������[i]];
            object ������ = ����.�������������;
            object ������ = �����������[����.������������].��������;
            if (������ != null) {
              if (����.PtyInfo != null) {
                ����.PtyInfo.SetValue(������, ������, null);
              } else {
                PropertyInfo prop = ������.GetType().GetProperty(������������������);
                if (prop != null) {
                  prop.SetValue(������, ������, null);
                }
              }
            }
          }
          ���������.Add((�������)���);
        }
        V8A.ReleaseComObject(Com�������);
        return ���������;
      }
    }

    public static object[,] ���������������(Func<DbConnection> getCon, string �������������, ������������[] ����������, Type[] �����������, string[] ������������) {
      using (DbConnection con = getCon()) {
        object ComObjQueryResult = V8A.������������������������(con, �������������, ����������);

        object ComObj������� = V8A.Call(con.Connection, ComObjQueryResult, "�������");
        int ���������������� = (int)V8A.Call(con.Connection, ComObj�������, "����������()");
        List<��������> ����������� = new List<��������>(����������������);
        Dictionary<string, int> ����������2��� = new Dictionary<string, int>(����������������);
        int[] �������������������� = new int[����������������];
        int ����������� = 0;
        int m = 0;
        for (int i = 0; i != ����������������; i++) {
          object ComObj������� = V8A.Call(con.Connection, ComObj�������, "��������()", i);
          string �������� = (string)V8A.Call(con.Connection, ComObj�������, "���");
          V8A.ReleaseComObject(ComObj�������);
          ����������2���[��������] = i;
          �������� ���� = new ��������();
          ����.��������� = ��������.EndsWith(������������������);
          if (����.���������) {
            ����.��������� = ��������.Substring(0, ��������.Length - ������������������.Length);
            ����.��� = typeof(string);
            ��������������������[�����������] = i;
            �����������++;
          } else {
            if (m >= �����������.Length) {
              throw new ArgumentException(@"������� ���� properties � ������ �������");
            }
            if (�������� != ������������[m]) {
              throw new ArgumentException(@"������ ���� property """ + �������� + @""", � �� """ + ������������[m] + @"""");
            }
            ����.��� = �����������[m];
            m++;
          }
          �����������.Add(����);
        }
        V8A.ReleaseComObject(ComObj�������);

        for (int i = 0; i < �����������; i++) {
          �������� ���� = �����������[��������������������[i]];

          if (!����������2���.TryGetValue(����.���������, out ����.������������)) {
            throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���������);
          } else {
            �������� ���������� = �����������[����.������������];
            if (����������.���.IsSubclassOf(typeof(ObjectRef))) {
              ����.PtyInfo = ����������.���.GetProperty(������������������);
            } else if (����������.��� != typeof(object)) {
              throw new ArgumentException("� ������� ���� " + ����������.��������� + " ������ ���� ���� ������ ��� object");
            }
          }
        }

        object Com������� = V8A.Call(con.Connection, ComObjQueryResult, "�������()");
        V8A.ReleaseComObject(ComObjQueryResult);
        object[,] ��������� = new object[(int)V8A.Call(con.Connection, Com�������, "����������()"), �����������.Length];
        int j = 0;
        while ((bool)V8A.Call(con.Connection, Com�������, "���������()")) {
          int k = 0;
          for (int i = 0; i != ����������������; i++) {
            �������� ���� = �����������[i];
            object V8���� = V8A.Call(con.Connection, Com�������, "��������()", i);
            object �������� = V8A.ConvertValueV8ToNet(V8����, con, ����.���);
            V8A.ReleaseComObject(V8����);  //��� ����� ���� �� ComObject
            if (����.���������) {
              ����.������������� = (string)��������;
            } else {
              ����.�������� = ��������;
              ���������[j, k++] = ��������;
            }
          }

          for (int i = 0; i < �����������; i++) {
            �������� ���� = �����������[��������������������[i]];
            object ������ = ����.�������������;
            object ������ = �����������[����.������������].��������;
            if (������ != null) {
              if (����.PtyInfo != null) {
                ����.PtyInfo.SetValue(������, ������, null);
              } else {
                PropertyInfo prop = ������.GetType().GetProperty(������������������);
                if (prop != null) {
                  prop.SetValue(������, ������, null);
                }
              }
            }
          }
          j++;
        }
        V8A.ReleaseComObject(Com�������);
        return ���������;
      }
    }

    public static ��� �������������<���, ������>(Func<DbConnection> getCon, ������ ������, DateTime �����) {
      string ���������� = ������������������(typeof(���).Name);

      using (DbConnection con = getCon()) {
        object ComResult = V8Gate.V8A.Call(con.Connection, con.Connection.comObject, "���������." + ���������� + ".�������������()", ������, �����);
        ��� res = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
        V8Gate.V8A.ReleaseComObject(ComResult);
        return res;
      }
    }

    public static ��� �����������<���, ����>(Func<DbConnection> getCon, ���� ����) {
      string ���������� = ������������������(typeof(���).Name);

      using (DbConnection con = getCon()) {
        object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "�����������." + ���������� + ".�����������()", ����);
        ��� res = (���)V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
        V8A.ReleaseComObject(ComResult);
        return res;
      }
    }

    public static ��� �������������������<���>(Func<DbConnection> getCon, string �����) {
      string ���������� = ������������������(typeof(���).Name);

      using (DbConnection con = getCon()) {
        object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "�����������." + ���������� + ".�������������������()", �����);
        ��� res = (���)V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
        V8A.ReleaseComObject(ComResult);
        return res;
      }
    }

    public static ��� ����������������<���>(Func<DbConnection> getCon, string �������������, object ������������������, ��� ���������, object ���������) {
      //string[] ���������� = typeof(���).Name.Split(new char[] { '_' });
      //string ���������� = ����������[1];
      string ���������� = ������������������(typeof(���).Name);

      using (DbConnection con = getCon()) {
        object Com�������� = V8A.ConvertValueNetToV8(������������������, con);
        object Com�������� = null;
        if (��������� != null) { //!!!!!!!!!! �������� !!!!!!!!!!!!!!!!!!!
          Com�������� = V8A.ConvertValueNetToV8(���������, con);
        }
        object Com�������� = null;
        if (��������� != null) { //!!!!!!!!!! �������� !!!!!!!!!!!!!!!!!!!
          Com�������� = V8A.ConvertValueNetToV8(���������, con);
        }
        object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "�����������." + ���������� + ".����������������()", �������������, Com��������, Com��������, Com��������);
        ��� result = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
        V8A.ReleaseComObject(ComResult);
        V8A.ReleaseComObject(Com��������);
        V8A.ReleaseComObject(Com��������);
        return result;
      }
    }

    public static T ������������<T>(Func<DbConnection> getCon, ObjectRef oRef, string aName) {
      Type CurType = typeof(T);
      if (oRef.IsEmpty()) {
        if (CurType.Equals(typeof(string))) {
          return (T)((object)string.Empty);
        }
        return CurType.IsValueType ? default(T) : (T)Activator.CreateInstance(CurType);
      } else {
        Type UType = Nullable.GetUnderlyingType(CurType) ?? CurType;

        using (DbConnection con = getCon()) {
          object V8Ref = V8A.Reference(oRef, con);
          object ComResult = V8A.Call(con.Connection, V8Ref, aName);
          V8A.ReleaseComObject(V8Ref);
          object value = V8A.ConvertValueV8ToNet(ComResult, con, UType);
          V8A.ReleaseComObject(ComResult);
          return (T)value;
        }
      }
    }

    public static TabList<T> ����������������������<T>(Func<DbConnection> getCon, ObjectRef oRef, string aName) where T: ���������, new() {
      TabList<T> result = (TabList<T>)Activator.CreateInstance(typeof(TabList<T>));

      if (!oRef.IsEmpty()) {
        using (DbConnection con = getCon()) {
          object comV8Ref = V8A.Reference(oRef, con);

          IV8TablePart IA = (IV8TablePart)result;
          PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);

          object �������������� = V8A.Call(con.Connection, comV8Ref, aName);
          int ���������� = (int)V8A.Call(con.Connection, ��������������, "����������()");
          //��� ������ ������ ��������
          for (int i = 0; i < ����������; i++) {
            object com��������������� = V8A.Call(con.Connection, ��������������, "��������()", i);
            object ����������� = IA.��������();
            foreach (PropertyInfo ������� in �������) {
              object v8���� = V8A.Call(con.Connection, com���������������, �������.Name);
              object �������� = V8A.ConvertValueV8ToNet(v8����, con, �������.PropertyType);
              V8A.ReleaseComObject(v8����);
              �������.SetValue(�����������, ��������, null);
            }
            V8A.ReleaseComObject(com���������������);
          }
          V8A.ReleaseComObject(��������������);

          V8A.ReleaseComObject(comV8Ref);
        }
      }
      return result;
    }

    public static string �����������������������������(Func<DbConnection> getCon, ObjectRef oRef) {
      using (DbConnection con = getCon()) {
        return �����������������������������(con, oRef);
      }
    }

    private static string �����������������������������(DbConnection con, ObjectRef oRef) {
      if (oRef.IsEmpty()) return string.Empty;

      string ���������� = ������������������(oRef.GetType().Name);
      string ��������������;
      if (oRef is CatalogRef) {
        �������������� = "����������.";
      } else if (oRef is DocumentRef) {
        �������������� = "��������.";
      } else {
        throw new ArgumentException("������ �� �������� �� ������������ �� ����������");
      }

      string ������������ = "������� " + ���������� + "." + ������������������ +
        " �� " + �������������� + ���������� +
        " ��� " + ���������� + " ���	" + ���������� + ".������ = &������";


      string result;
      ������������[] ��� = new ������������[] { new ������������("������", oRef) };
      object qResult = V8A.������������������������(con, ������������, ���);
      object ������� = V8A.Call(con.Connection, qResult, "�������()");
      V8A.ReleaseComObject(qResult);

      if ((bool)V8A.Call(con.Connection, �������, "���������()")) { //������ ������ ������� ����� 1 ������
        result = (string)V8A.Call(con.Connection, �������, "��������()", 0);
      } else {
        throw new ArgumentNullException("������ '" + ������������ + "' �� ������ �������������.");
      }

      V8A.ReleaseComObject(�������);

      return result;
    }

    #region "Load"
    public static V8Object Load(Func<DbConnection> getCon, ObjectRef oRef) {
      V8Object obj = ObjectCache.����������(oRef);
      using (DbConnection con = getCon()) {
        object ComV8Ref = V8A.Reference(oRef, con);

        foreach (PropertyInfo fi in obj.�������������) {
          if (fi.Name != "������") {
            object ComResult = V8A.Call(con.Connection, ComV8Ref, fi.Name); //.Substring(1));
            object value = V8A.ConvertValueV8ToNet(ComResult, con, fi.PropertyType);
            V8A.ReleaseComObject(ComResult);
            fi.SetValue(obj, value, null);
          }
        }
        foreach (FieldInfo T in obj.�������������) {
          IList TR = (IList)T.GetValue(obj);
          string Tname = T.Name.Remove(0, 1);
          if (TR != null) {
            TR.Clear();
          } else {
            TR = (IList)Activator.CreateInstance(T.FieldType);
          }
          IV8TablePart IA = (IV8TablePart)TR;
          PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
          object �������������� = V8A.Call(con.Connection, ComV8Ref, Tname);
          int ���������� = (int)V8A.Call(con.Connection, ��������������, "����������()");
          //��� ������ ������ ��������
          for (int i = 0; i < ����������; i++) {
            object Com��������������� = V8A.Call(con.Connection, ��������������, "��������()", i);
            object ����������� = IA.��������();
            foreach (PropertyInfo ������� in �������) {
              object V8���� = V8A.Call(con.Connection, Com���������������, �������.Name);
              object �������� = V8A.ConvertValueV8ToNet(V8����, con, �������.PropertyType);
              V8A.ReleaseComObject(V8����);
              �������.SetValue(�����������, ��������, null);
            }
            V8A.ReleaseComObject(Com���������������);
          }
          V8A.ReleaseComObject(��������������);

          obj.GetType().GetProperty(Tname).SetValue(obj, TR, null);
        }
        V8A.ReleaseComObject(ComV8Ref);
        return obj;
      }
    }

    public static V8Object Load2(Func<DbConnection> getCon, ObjectRef oRef) {
      ObjectRef ������ = oRef;
      V8Object obj = ObjectCache.����������(oRef);
      //string[] ���������� = ������.GetType().Name.Split(new char[] { '_' });
      //string �������������� = ����������[0];  //����� ��� ��� ���
      //string ���������� = ����������[1];
      string ��������������;
      string ���������� = ������������������(������.GetType().Name);
      //�������������������������(������.GetType().Name, out ��������������, out ����������);
      //if (��������������.StartsWith("���")) {
      //  �������������� = "����������.";
      //} else if (��������������.StartsWith("���")) {
      //  �������������� = "��������.";
      //}
      if (obj is ����������) {
        �������������� = "����������.";
      } else if (obj is ��������) {
        �������������� = "��������.";
      } else {
        throw new ArgumentException("������ �� �������� �� ������������ �� ����������");
      }
      StringBuilder ������������ = new StringBuilder("������� " + ���������� + "." + ������������������, 2000);
      foreach (PropertyInfo fi in obj.�������������) {
        if (fi.Name != "������") {
          Type ������� = fi.PropertyType;
          string fldName = fi.Name; //.Substring(1);
          ������������.Append("," + ���������� + "." + fldName);
          if (�������.IsSubclassOf(typeof(ObjectRef))) {
            ������������.Append("," + ���������� + "." + fldName + "." + ������������������);
          }
        }
      }

      //foreach (PropertyInfo T in obj.�����������������) {
      //  ������������.Append("," + ���������� + "." + T.Name + ".(");
      //  IList TR = (IList)T.GetValue(obj, null);
      //  IV8TablePart IA = (IV8TablePart)TR;
      //  //if (IA.�������()) {
      //  //���-�� ���� ������������ ������ "" ������������ string.Empty �Andrew
      //  string �����������2 = "";
      //  PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //  foreach (PropertyInfo ������� in �������) {
      //    string fldName = �������.Name;
      //    ������������.Append(�����������2 + fldName);
      //    �����������2 = ",";
      //    if (�������.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
      //      ������������.Append("," + fldName + "." + ������������������);
      //    }
      //  }
      //  //}
      //  ������������.Append(")");
      //}
      foreach (FieldInfo T in obj.�������������) {
        ������������.Append("," + ���������� + "." + T.Name.Remove(0, 1) + ".(");
        IV8TablePart IA = (IV8TablePart)T.GetValue(obj) ?? (IV8TablePart)Activator.CreateInstance(T.FieldType);

        string �����������2 = string.Empty;
        PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo ������� in �������) {
          string fldName = �������.Name;
          ������������.Append(�����������2 + fldName);
          �����������2 = ",";
          if (�������.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
            ������������.Append("," + fldName + "." + ������������������);
          }
        }
        ������������.Append(")");
      }
      ������������.Append(" �� " + �������������� + ���������� + " ��� " + ����������);
      ������������.Append(" ���	" + ���������� + ".������ = &������");

      using (DbConnection con = getCon()) {
        ������������[] ��� = new ������������[] { new ������������("������", ������) };
        object result = V8A.������������������������(con, ������������.ToString(), ���);
        object ������� = V8A.Call(con.Connection, result, "�������()");
        V8A.ReleaseComObject(result);
        if ((bool)V8A.Call(con.Connection, �������, "���������()")) { //������ ������ ������� ����� 1 ������
          int i = 0;
          bool ��������� = true;
          object V8����;
          foreach (PropertyInfo fi in obj.�������������) {
            if (fi.Name != "������") {
              if (���������) { //������ ��� ������������� ��� ������. ���� ������ �������� �������
                ��������� = false;
                V8���� = V8A.Call(con.Connection, �������, "��������()", i++);
                ������.������������� = (string)V8A.ConvertValueV8ToNet(V8����, con, typeof(string));
                //V8A.ReleaseComObject(V8����); �� ����, ��� ������
              }
              V8���� = V8A.Call(con.Connection, �������, "��������()", i++);
              object �������� = V8A.ConvertValueV8ToNet(V8����, con, fi.PropertyType);
              V8A.ReleaseComObject(V8����);
              ObjectRef oref = �������� as ObjectRef;
              if (oref != null) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
                V8���� = V8A.Call(con.Connection, �������, "��������()", i++);
                string ������������� = (string)V8A.ConvertValueV8ToNet(V8����, con, typeof(string));
                oref.������������� = ������������� ?? string.Empty;
                //V8A.ReleaseComObject(V8����); �� ����, ��� ������
              }
              fi.SetValue(obj, ��������, null);
            }
          }
          foreach (FieldInfo T in obj.�������������) {
            IList TR = (IList)T.GetValue(obj);
            string Tname = T.Name.Remove(0, 1);
            if (TR != null) {
              TR.Clear();
            } else {
              TR = (IList)Activator.CreateInstance(T.FieldType);
            }
            IV8TablePart IA = (IV8TablePart)TR;
            PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            object Com������������������ = V8A.Call(con.Connection, �������, "��������()", i++);
            object Com����������� = V8A.Call(con.Connection, Com������������������, "�������()");
            V8A.ReleaseComObject(Com������������������);
            while ((bool)V8A.Call(con.Connection, Com�����������, "���������()")) {
              object ����������� = IA.��������();
              int j = 0;
              foreach (PropertyInfo ������� in �������) {
                V8���� = V8A.Call(con.Connection, Com�����������, "��������()", j++);
                Type ������� = �������.PropertyType;
                object �������� = V8A.ConvertValueV8ToNet(V8����, con, �������);
                V8A.ReleaseComObject(V8����);

                ObjectRef oref = �������� as ObjectRef;
                if (�������.IsSubclassOf(typeof(ObjectRef))) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
                  //if (oref != null) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
                  object ComResult = V8A.Call(con.Connection, Com�����������, "��������()", j++);
                  string ������������� = (string)V8A.ConvertValueV8ToNet(ComResult, con, typeof(string));
                  oref.������������� = ������������� ?? string.Empty;
                  //V8A.ReleaseComObject(ComResult); �� ����, ��� ������
                } else if (oref != null) {
                  oref.������������� = �����������������������������(con, oref);
                  //oref.���������������������();
                  //this.������������<string>(con, oref, ������������������);
                }

                �������.SetValue(�����������, ��������, null);
              }
            }
            V8A.ReleaseComObject(Com�����������);

            obj.GetType().GetProperty(Tname).SetValue(obj, TR, null);
          }
        }
        V8A.ReleaseComObject(�������);
        return obj;
      }
    }
    #endregion

    #region "��������"
    private static object ��������������������(DbConnection con, V8Object obj) {
      ObjectRef ������ = obj.�����������;
      object v8ComObj = null;
      if (!������.IsEmpty()) {
        object v8Ref = V8Gate.V8A.Reference(������, con);
        v8ComObj = V8Gate.V8A.Call(con.Connection, v8Ref, "��������������()");
        V8Gate.V8A.ReleaseComObject(v8Ref);
      } else {
        //string[] ���������� = ������.GetType().Name.Split(new char[] { '_' });
        //string �������������� = ����������[0];  //����� ��� ��� ���
        //string ���������� = ����������[1];
        string ��������������;
        string ����������;
        �������������������������(������.GetType().Name, out ��������������, out ����������);
        if (��������������.StartsWith("���")) {
          //���������� ��� = obj as ����������;
          //PropertyInfo pi = obj.GetType().GetProperty("���������");
          //bool ������������� = false;
          FieldInfo fi = obj.GetType().GetField("���������");
          bool ������������� = (fi != null && (bool)(fi.GetValue(obj) ?? false));
          //if (pi != null && (bool)(pi.GetValue(obj, null) ?? false)) {
          //  ������������� = true;
          //}
          if (�������������) {
            //if (���._��������� ?? false) {
            �������������� = "�����������." + ���������� + ".�������������()";
          } else {
            �������������� = "�����������." + ���������� + ".��������������()";
          }
        } else if (��������������.StartsWith("���")) {
          �������������� = "���������." + ���������� + ".���������������()";
        }
        v8ComObj = V8Gate.V8A.Call(con.Connection, con.Connection.comObject, ��������������);
      }

      foreach (FieldInfo fi in obj.���������) {
        //������ ���� ������ ���� _
        string name = fi.Name.Remove(0, 1);
        //���� ������, �� �� ������� �������� ����
        if (name != "������") {
          object value = fi.GetValue(obj);
          //� ���� ���� �� ������ � �� ���������, ���� �� �������� (v8ComObj �� ��������������() ��� ����� ������ ����)
          if (value != null) {
            object v8Value = V8Gate.V8A.ConvertValueNetToV8(value, con);
            V8Gate.V8A.SetProp(con.Connection, v8ComObj, name, v8Value);
            V8Gate.V8A.ReleaseComObject(v8Value);
          }
        }
      }
      foreach (FieldInfo T in obj.�������������) {
        IList tr = (IList)T.GetValue(obj);
        if (tr != null) {
          IV8TablePart ia = (IV8TablePart)tr;
          //if (IA.�������()) {
          PropertyInfo[] ������� = ia.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
          object �������������� = V8Gate.V8A.Call(con.Connection, v8ComObj, T.Name.Remove(0, 1));
          V8Gate.V8A.Call(con.Connection, ��������������, "��������()");
          foreach (object str in tr) {
            object com�������� = V8Gate.V8A.Call(con.Connection, ��������������, "��������()");
            foreach (PropertyInfo f in �������) {
              object value = V8Gate.V8A.ConvertValueNetToV8(f.GetValue(str, null), con);
              V8Gate.V8A.SetProp(con.Connection, com��������, f.Name, value);
              V8Gate.V8A.ReleaseComObject(value);
            }
            V8Gate.V8A.ReleaseComObject(com��������);
          }
          V8Gate.V8A.ReleaseComObject(��������������);
        }
      }
      return v8ComObj;

      //foreach (PropertyInfo pi in obj.�������������) {
      //  if (pi.Name != "������") {
      //    object value = pi.GetValue(obj, null);
      //    if (value != null) {
      //      object v8Value = V8Gate.V8A.ConvertValueNetToV8(value, con);
      //      V8Gate.V8A.SetProp(con.Connection, v8ComObj, pi.Name, v8Value);
      //      V8Gate.V8A.ReleaseComObject(v8Value);
      //    }
      //  }
      //}

      //foreach (PropertyInfo T in obj.�����������������) {
      //  IList tr = (IList)T.GetValue(obj, null);
      //  IV8TablePart ia = (IV8TablePart)tr;
      //  //if (IA.�������()) {
      //  PropertyInfo[] ������� = ia.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //  object �������������� = V8Gate.V8A.Call(con.Connection, v8ComObj, T.Name);
      //  V8Gate.V8A.Call(con.Connection, ��������������, "��������()");
      //  foreach (object str in tr) {
      //    object com�������� = V8Gate.V8A.Call(con.Connection, ��������������, "��������()");
      //    foreach (PropertyInfo f in �������) {
      //      object value = V8Gate.V8A.ConvertValueNetToV8(f.GetValue(str, null), con);
      //      V8Gate.V8A.SetProp(con.Connection, com��������, f.Name, value);
      //      V8Gate.V8A.ReleaseComObject(value);
      //    }
      //    V8Gate.V8A.ReleaseComObject(com��������);
      //  }
      //  V8Gate.V8A.ReleaseComObject(��������������);
      //}
    }

    public static ObjectRef ��������(Func<DbConnection> getCon, ���������� obj) {
      using (DbConnection con = getCon()) {
        object v8ComObj = ��������������������(con, obj);
        �����������(con, obj, v8ComObj);
        V8A.ReleaseComObject(v8ComObj);
        return obj.�����������;
      }
    }

    private static void �����������(DbConnection con, V8Object obj, object V8Obj) {
      ObjectRef ������ = obj.�����������;
      V8Gate.V8A.Call(con.Connection, V8Obj, "��������()");
      if (������.IsEmpty()) {
        object comResult = V8Gate.V8A.Call(con.Connection, V8Obj, "������");
        obj.����������� = V8Gate.V8A.ConvertValueV8ToNet(comResult, con, null) as V8Gate.ObjectRef;
        V8Gate.V8A.ReleaseComObject(comResult);
        ObjectCache.��������(obj.�����������, obj);
      }
    }

    public static ObjectRef ��������(Func<DbConnection> getCon, �������� obj, �������������������� ����������, ������������������������ ��������) {
      using (DbConnection con = getCon()) {
        object v8ComObj = ��������������������(con, obj);
        �����������(con, obj, v8ComObj, ����������, ��������);
        V8A.ReleaseComObject(v8ComObj);
        return obj.�����������;
      }
    }

    private static void �����������(DbConnection con, �������� obj, object V8Obj, �������������������� ����������, ������������������������ ��������) {
      object comObj��� = V8A.ConvertValueNetToV8(����������, con);
      object comObj���� = V8A.ConvertValueNetToV8(��������, con);
      ObjectRef ������ = obj.�����������;
      V8Gate.V8A.Call(con.Connection, V8Obj, "��������()", comObj���, comObj����);
      V8Gate.V8A.ReleaseComObject(comObj����);
      V8Gate.V8A.ReleaseComObject(comObj���);
      if (������.IsEmpty()) {
        object comResult = V8Gate.V8A.Call(con.Connection, V8Obj, "������");
        obj.����������� = V8Gate.V8A.ConvertValueV8ToNet(comResult, con, null) as V8Gate.ObjectRef;
        V8Gate.V8A.ReleaseComObject(comResult);
        ObjectCache.��������(obj.�����������, obj);
      }
    }

    public static void ������������������������(Func<DbConnection> getCon, string �������, object ������, string ����������) {
      using (DbConnection con = getCon()) {
        object com������ = V8A.ConvertValueNetToV8(������, con);
        V8A.Call(con.Connection, con.Connection.comObject, "������������������������()", new object[] { �������, null, null, com������, ���������� });
        V8A.ReleaseComObject(com������);
      }
    }
    #endregion

    #region "��������"
    internal class �������� {
      public PropertyInfo PtyInfo;
      public int ������������;
      public string ���������;
      public string �������������;
      public bool ���������;
      public object ��������;
      public Type ���;

      public int ������;
      public string ���;
      public �������� �����������������;
    }
    #endregion
  }
  #endregion

  //****************************************************************************************************************
  //****************************************************************************************************************
  //****************************************************************************************************************

  #region "DAL"
  public abstract class DAL:MarshalByRefObject {
    //protected internal Guid _transactionID;
    //protected internal TransactionsIdStack _transactionsID = new TransactionsIdStack();
    static readonly DAL _instance;

    public static DAL Instance {
      [System.Diagnostics.DebuggerHidden]
      get {
        return _instance;
      }
    }

    static DAL() {
      string strDalType = System.Configuration.ConfigurationManager.AppSettings["1C_DALType"];
      if (strDalType != null) {
        strDalType = strDalType.ToUpper();
      }

      if (strDalType == "COM") {
        _instance = new ComDAL();
      } else if (strDalType == "REMOTE") {
        _instance = new RemoteDAL();
      } else {
        throw new ApplicationException("� AppSettings �������� " + strDalType + @" ����� ���� ������ COM ��� REMOTE, � �� """ + strDalType + @"""");
      }
    }

    //internal DbConnection ConnectV8(Guid transactionId) {
    //  try {
    //    return DbConnections.Instance.ConnectV8(transactionId);
    //  } catch (NoTransactionException) {
    //    //���� ��� ������ ������� � ��������� ���������� _transactionID (ComDAL), ����� �������� � ������ ���������� �� ������
    //    //���� RemoteDAL ������� ComDAL ������� � �������� ��� ���� _transactionID, ����� _transactionID � ���������� ComDAL �� �������,
    //    //� _transactionID � RemoteDAL-� ��������� � ����� ���������.
    //    if (transactionId == _transactionID) {
    //      _transactionID = Guid.Empty;
    //    }
    //    throw;
    //  }
    //}

    public abstract void ClearPool();
    public abstract ConnectionsInfo GetConnectionsInfo { get; }

    //public abstract Guid _BeginTransaction(TimeSpan timeOut);
    //public abstract void _CommitTransaction(Guid transactionID);
    //public abstract void _RollbackTransaction(Guid transactionID);

    //public void BeginTransaction(int seconds) {
    //  BeginTransaction(new TimeSpan(0, 0, seconds));
    //}
    //public void BeginTransaction(TimeSpan timeOut) {
    //  if (_transactionID != Guid.Empty) {
    //    throw new ApplicationException("���������� ����������. ��������� ���������� �� ��������������.");
    //  }
    //  //_transactionsID.Add(this._BeginTransaction(timeOut));
    //  _transactionID = this._BeginTransaction(timeOut);
    //}

    //public void CommitTransaction() {
    //  //if (_transactionsID.Count == 1) {
    //  //  this._CommitTransaction(_transactionsID.Pop());
    //  //} else if (_transactionsID.Count != 0) {
    //  //  _transactionsID.Pop();
    //  //}
    //  this._CommitTransaction(_transactionID);
    //  _transactionID = Guid.Empty;
    //}

    //public void RollbackTransaction() {
    //  this._RollbackTransaction(_transactionID);
    //  _transactionID = Guid.Empty;
    //}

    //Was made for GarbageCollection of the connections pool of DBConnections class
    //to roll back and release alone transactions by timeout
    //internal void RollbackTransaction(long transactionID) {
    //  int tIndex = _transactionsID.IndexOf(transactionID);
    //  if (tIndex == 0) {
    //    this._RollbackTransaction(transactionID);
    //    _transactionsID.Clear();
    //  } else if (tIndex != -1) {
    //    _transactionsID.RemoveAt(tIndex);
    //  }
    //}
    //public void RollbackTransaction() {
    //  if (_transactionsID.Count == 1) {
    //    this._RollbackTransaction(_transactionsID.Pop());
    //  } else if (_transactionsID.Count != 0) {
    //    _transactionsID.Pop();
    //  }
    //  //this._RollbackTransaction(_transactionID);
    //  //_transactionID = 0;
    //}

    public abstract List<T> ���������������<T>(string �������������, ������������[] ����������) where T:new();
    //public List<T> ���������������<T>(string �������������, ������������[] ����������) where T:new() {
    //  return this.���������������<T>(_transactionsID.GetFirst(), �������������, ����������);
    //}

    public abstract string ���������������JS(string �������������, ������������[] ����������);
    //public string ���������������JS(string �������������, ������������[] ����������) {
    //  return this.���������������JS(_transactionsID.GetFirst(), �������������, ����������);
    //}

    public abstract ��� �������������<���>(string ������, DateTime �����) where ���:DocumentRef;
    public abstract ��� �������������<���>(int ������, DateTime �����) where ���:DocumentRef;

    public abstract ��� �����������<���>(int ����) where ���:ObjectRef;
    //public ��� �����������<���>(int ����) where ���:ObjectRef {
    //  return this.�����������<���>(_transactionsID.GetFirst(), ����);
    //}

    public abstract ��� �����������<���>(string ����) where ���:ObjectRef;
    //public ��� �����������<���>(string ����) where ���:ObjectRef {
    //  return this.�����������<���>(_transactionsID.GetFirst(), ����);
    //}
    
    public abstract ��� �������������������<���>(string �����) where ���:ObjectRef;
    public abstract ��� ����������������<���>(string �������������, object ������������������, ��� ���������, object ���������) where ���:ObjectRef;
    public abstract T ������������<T>(ObjectRef oRef, string aName);
    public abstract TabList<T> ����������������������<T>(ObjectRef oRef, string aName) where T:���������, new();
    public abstract string �����������������������������(ObjectRef oRef);
    public abstract V8Object Load(ObjectRef oRef);
    public abstract V8Object Load2(ObjectRef oRef);

    public abstract ObjectRef ��������(���������� obj);
    //public ObjectRef ��������(���������� obj) {
    //  return this.��������(_transactionsID.GetFirst(), obj);
    //}

    public abstract ObjectRef ����������������(�������� obj, �������������������� ����������, ������������������������ ��������);
    public abstract void ������������������������(string �������, object ������, string ����������);
    public abstract void ������������������������(string �������);
    //public abstract void TestFunc(Action<Func<int, ObjectRef>> test);

    public List<T> ���������������<T>(string �������������) where T:new() {
      return ���������������<T>(�������������, null);
    }

    public string ���������������JS(string �������������) {
      return ���������������JS(�������������, null);
    }

    //protected void �������������������������(string �������, out string �������, out string ���) {
    //  int sepIndex = �������.IndexOf('_');
    //  if (sepIndex == -1) {
    //    throw new ArgumentException("��� " + ������� + " �� �������� ��������");
    //  }
    //  ������� = �������.Substring(0, sepIndex);
    //  ��� = �������.Substring(++sepIndex, �������.Length - sepIndex);
    //}

    //protected string ������������������(string �������) {
    //  int sepIndex = �������.IndexOf('_');
    //  if (sepIndex == -1) {
    //    throw new ArgumentException("��� " + ������� + " �� �������� ��������");
    //  }
    //  return �������.Substring(++sepIndex, �������.Length - sepIndex);
    //}

  }
  #endregion

  //****************************************************************************************************************
  //****************************************************************************************************************
  //****************************************************************************************************************

  #region "ComDAL"
  public class ComDAL:DAL {
    //const string ������������������ = "�������������";

    public ComDAL() {
    }

    public override void ClearPool() {
      DbConnections.Instance.Clear();
    }

    public override ConnectionsInfo GetConnectionsInfo {
      get { return DbConnections.Instance.GetConnectionsInfo; }
    }

    //public override void TestFunc(Action<Func<int, ObjectRef>> test) {
    //  DbConnections pool = DbConnections.Instance;
    //  DbConnection con = pool.ConnectV8();
    //  object ComResult = null;
    //  test((rin) => {
    //    ComResult = V8A.Call(con, "�����������.�������.�����������()", rin);
    //    return (ObjectRef)(V8_Custom.���������_�������)V8A.ConvertValueV8ToNet(ComResult, con, typeof(V8_Custom.���������_�������));
    //  });
    //  V8A.ReleaseComObject(ComResult);
    //  pool.ReturnDBConnection(con);
    //}

    //public override Guid _BeginTransaction(TimeSpan timeOut) {
    //  using (DbConnection con = ConnectV8(_transactionID)) {
    //    return con.����������������(timeOut);
    //  }
    //}

    //public override void _CommitTransaction(Guid transactionID) {
    //  using (DbConnection con = ConnectV8(transactionID)) {
    //    con.�����������������������();
    //  }
    //}

    //public override void _RollbackTransaction(Guid transactionID) {
    //  using (DbConnection con = ConnectV8(transactionID)) {
    //    con.������������������();
    //  }
    //}
    internal virtual Func<DbConnection> ConnectionGetter() {
      return () => DbConnections.Instance.ConnectV8(Guid.Empty);
    }

    public override string ���������������JS(string �������������, ������������[] ����������) {
      return DALEngine.���������������JS(ConnectionGetter(), �������������, ����������);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComObjQueryResult = V8A.������������������������(con, �������������, ����������);

      //  object ComObj������� = V8A.Call(con.Connection, ComObjQueryResult, "�������");
      //  int ���������������� = (int)V8A.Call(con.Connection, ComObj�������, "����������()");

      //  Dictionary<string, ��������> ���� = new Dictionary<string, ��������>(����������������);
      //  List<��������> ����������������� = new List<��������>(���������������� / 2);

      //  for (int i = 0; i != ����������������; i++) {
      //    object ComObj������� = V8A.Call(con.Connection, ComObj�������, "��������()", i);
      //    string �������� = (string)V8A.Call(con.Connection, ComObj�������, "���");
      //    V8A.ReleaseComObject(ComObj�������);
      //    �������� ���� = new ��������();
      //    ����.������ = i;
      //    if (��������.EndsWith(������������������)) {
      //      ����.��� = ��������.Substring(0, ��������.Length - ������������������.Length);
      //      �����������������.Add(����);
      //    } else {
      //      ����.��� = ��������;
      //      ����.Add(��������, ����);
      //    }
      //  }
      //  V8A.ReleaseComObject(ComObj�������);

      //  foreach (�������� ���� in �����������������) {
      //    �������� ����������;
      //    if (!����.TryGetValue(����.���, out ����������)) {
      //      throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���);
      //    } else {
      //      ����������.����������������� = ����;
      //    }
      //  }

      //  object Com������� = V8A.Call(con.Connection, ComObjQueryResult, "�������()");
      //  V8A.ReleaseComObject(ComObjQueryResult);
      //  StringBuilder ��������� = new StringBuilder("[");
      //  if ((bool)V8A.Call(con.Connection, Com�������, "���������()")) {
      //    ���������.Append("{");
      //    foreach (�������� ���� in ����.Values) {
      //      object V8���� = V8A.Call(con.Connection, Com�������, "��������()", ����.������);
      //      object �������� = V8A.ConvertValueV8ToJS(V8����, con);
      //      V8A.ReleaseComObject(V8����);
      //      if (����.����������������� != null) {
      //        string ������������� = V8A.Call(con.Connection, Com�������, "��������()", ����.�����������������.������) as string;
      //        ���������.Append(@"""" + ����.��� + @""":{""guid"":" + �������� + @",""�������������"":""" + System.Web.HttpUtility.HtmlEncode(�������������) + @"""},");
      //      } else {
      //        ���������.Append(@"""" + ����.��� + @""":" + �������� + @",");
      //      }
      //    }
      //    ���������.Remove(���������.Length - 1, 1);
      //    ���������.Append("}");
      //    while ((bool)V8A.Call(con.Connection, Com�������, "���������()")) {
      //      ���������.Append(",{");
      //      foreach (�������� ���� in ����.Values) {
      //        object V8���� = V8A.Call(con.Connection, Com�������, "��������()", ����.������);
      //        object �������� = V8A.ConvertValueV8ToJS(V8����, con);
      //        V8A.ReleaseComObject(V8����);
      //        if (����.����������������� != null) {
      //          string ������������� = V8A.Call(con.Connection, Com�������, "��������()", ����.�����������������.������) as string;
      //          ���������.Append(@"""" + ����.��� + @""":{""guid"":" + �������� + @",""�������������"":""" + System.Web.HttpUtility.HtmlEncode(�������������) + @"""},");
      //        } else {
      //          ���������.Append(@"""" + ����.��� + @""":" + �������� + @",");
      //        }
      //      }
      //      ���������.Remove(���������.Length - 1, 1);
      //      ���������.Append("}");
      //    }
      //  }
      //  ���������.Append("]");
      //  V8A.ReleaseComObject(Com�������);
      //  return ���������.ToString();
      //}
    }

    public override List<�������> ���������������<�������>(string �������������, ������������[] ����������) {
      return DALEngine.���������������<�������>(ConnectionGetter(), �������������, ����������);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComObjQueryResult = V8A.������������������������(con, �������������, ����������);

      //  object ComObj������� = V8A.Call(con.Connection, ComObjQueryResult, "�������");
      //  int ���������������� = (int)V8A.Call(con.Connection, ComObj�������, "����������()");
      //  List<��������> ����������� = new List<��������>(����������������);
      //  Dictionary<string, int> ����������2��� = new Dictionary<string, int>(����������������);
      //  int[] �������������������� = new int[����������������];
      //  int ����������� = 0;
      //  for (int i = 0; i != ����������������; i++) {
      //    object ComObj������� = V8A.Call(con.Connection, ComObj�������, "��������()", i);
      //    string �������� = (string)V8A.Call(con.Connection, ComObj�������, "���");
      //    V8A.ReleaseComObject(ComObj�������);
      //    ����������2���[��������] = i;
      //    �������� ���� = new ��������();
      //    ����.��������� = ��������.EndsWith(������������������);
      //    if (����.���������) {
      //      ����.��������� = ��������.Substring(0, ��������.Length - ������������������.Length);
      //      ����.��� = typeof(string);
      //      ��������������������[�����������] = i;
      //      �����������++;
      //    } else {
      //      ����.PtyInfo = typeof(�������).GetProperty(��������);
      //      ����.��� = ����.PtyInfo.PropertyType;
      //    }
      //    �����������.Add(����);

      //  }
      //  V8A.ReleaseComObject(ComObj�������);
      //  for (int i = 0; i < �����������; i++) {
      //    �������� ���� = �����������[��������������������[i]];

      //    if (!����������2���.TryGetValue(����.���������, out ����.������������)) {
      //      throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���������);
      //    } else {
      //      �������� ���������� = �����������[����.������������];
      //      if (����������.���.IsSubclassOf(typeof(ObjectRef))) {
      //        ����.PtyInfo = ����������.���.GetProperty(������������������);
      //      } else if (����������.��� != typeof(object)) {
      //        throw new ArgumentException("� ������� ���� " + ����������.��������� + " ������ ���� ���� ������ ��� object");
      //      }
      //    }
      //  }

      //  object Com������� = V8A.Call(con.Connection, ComObjQueryResult, "�������()");
      //  V8A.ReleaseComObject(ComObjQueryResult);
      //  List<�������> ��������� = new List<�������>((int)V8A.Call(con.Connection, Com�������, "����������()"));

      //  while ((bool)V8A.Call(con.Connection, Com�������, "���������()")) {
      //    object ��� = new �������(); //object ��� = Activator.CreateInstance(typeof(�����������));
      //    for (int i = 0; i != ����������������; i++) {
      //      �������� ���� = �����������[i];
      //      object V8���� = V8A.Call(con.Connection, Com�������, "��������()", i);
      //      object �������� = V8A.ConvertValueV8ToNet(V8����, con, ����.���);
      //      V8A.ReleaseComObject(V8����);
      //      if (����.���������) {
      //        ����.������������� = (string)��������;
      //      } else {
      //        ����.�������� = ��������;
      //        ����.PtyInfo.SetValue(���, ��������, null);
      //      }
      //    }
      //    for (int i = 0; i < �����������; i++) {
      //      �������� ���� = �����������[��������������������[i]];
      //      object ������ = ����.�������������;
      //      object ������ = �����������[����.������������].��������;
      //      if (������ != null) {
      //        if (����.PtyInfo != null) {
      //          ����.PtyInfo.SetValue(������, ������, null);
      //        } else {
      //          PropertyInfo prop = ������.GetType().GetProperty(������������������);
      //          if (prop != null) {
      //            prop.SetValue(������, ������, null);
      //          }
      //        }
      //      }
      //    }
      //    ���������.Add((�������)���);
      //  }
      //  V8A.ReleaseComObject(Com�������);
      //  return ���������;
      //}
    }

    public object[,] ���������������(string �������������, ������������[] ����������, Type[] �����������, string[] ������������) {
      return DALEngine.���������������(ConnectionGetter(), �������������, ����������, �����������, ������������);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComObjQueryResult = V8A.������������������������(con, �������������, ����������);

      //  object ComObj������� = V8A.Call(con.Connection, ComObjQueryResult, "�������");
      //  int ���������������� = (int)V8A.Call(con.Connection, ComObj�������, "����������()");
      //  List<��������> ����������� = new List<��������>(����������������);
      //  Dictionary<string, int> ����������2��� = new Dictionary<string, int>(����������������);
      //  int[] �������������������� = new int[����������������];
      //  int ����������� = 0;
      //  int m = 0;
      //  for (int i = 0; i != ����������������; i++) {
      //    object ComObj������� = V8A.Call(con.Connection, ComObj�������, "��������()", i);
      //    string �������� = (string)V8A.Call(con.Connection, ComObj�������, "���");
      //    V8A.ReleaseComObject(ComObj�������);
      //    ����������2���[��������] = i;
      //    �������� ���� = new ��������();
      //    ����.��������� = ��������.EndsWith(������������������);
      //    if (����.���������) {
      //      ����.��������� = ��������.Substring(0, ��������.Length - ������������������.Length);
      //      ����.��� = typeof(string);
      //      ��������������������[�����������] = i;
      //      �����������++;
      //    } else {
      //      if (m >= �����������.Length) {
      //        throw new ArgumentException(@"������� ���� properties � ������ �������");
      //      }
      //      if (�������� != ������������[m]) {
      //        throw new ArgumentException(@"������ ���� property """ + �������� + @""", � �� """ + ������������[m] + @"""");
      //      }
      //      ����.��� = �����������[m];
      //      m++;
      //    }
      //    �����������.Add(����);
      //  }
      //  V8A.ReleaseComObject(ComObj�������);

      //  for (int i = 0; i < �����������; i++) {
      //    �������� ���� = �����������[��������������������[i]];

      //    if (!����������2���.TryGetValue(����.���������, out ����.������������)) {
      //      throw new ArgumentException("� ������� ��� ���� ���� ������ � ������ " + ����.���������);
      //    } else {
      //      �������� ���������� = �����������[����.������������];
      //      if (����������.���.IsSubclassOf(typeof(ObjectRef))) {
      //        ����.PtyInfo = ����������.���.GetProperty(������������������);
      //      } else if (����������.��� != typeof(object)) {
      //        throw new ArgumentException("� ������� ���� " + ����������.��������� + " ������ ���� ���� ������ ��� object");
      //      }
      //    }
      //  }

      //  object Com������� = V8A.Call(con.Connection, ComObjQueryResult, "�������()");
      //  V8A.ReleaseComObject(ComObjQueryResult);
      //  object[,] ��������� = new object[(int)V8A.Call(con.Connection, Com�������, "����������()"), �����������.Length];
      //  int j = 0;
      //  while ((bool)V8A.Call(con.Connection, Com�������, "���������()")) {
      //    int k = 0;
      //    for (int i = 0; i != ����������������; i++) {
      //      �������� ���� = �����������[i];
      //      object V8���� = V8A.Call(con.Connection, Com�������, "��������()", i);
      //      object �������� = V8A.ConvertValueV8ToNet(V8����, con, ����.���);
      //      V8A.ReleaseComObject(V8����);  //��� ����� ���� �� ComObject
      //      if (����.���������) {
      //        ����.������������� = (string)��������;
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
      //        if (����.PtyInfo != null) {
      //          ����.PtyInfo.SetValue(������, ������, null);
      //        } else {
      //          PropertyInfo prop = ������.GetType().GetProperty(������������������);
      //          if (prop != null) {
      //            prop.SetValue(������, ������, null);
      //          }
      //        }
      //      }
      //    }
      //    j++;
      //  }
      //  V8A.ReleaseComObject(Com�������);
      //  return ���������;
      //}
    }

    protected ��� �������������<���, ������>(������ ������, DateTime �����) {
      return DALEngine.�������������<���, ������>(ConnectionGetter(), ������, �����);
      //string ���������� = ������������������(typeof(���).Name);

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComResult = V8Gate.V8A.Call(con.Connection, con.Connection.comObject, "���������." + ���������� + ".�������������()", ������, �����);
      //  ��� res = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
      //  V8Gate.V8A.ReleaseComObject(ComResult);
      //  return res;
      //}
    }

    public override ��� �������������<���>(int ������, DateTime �����) {
      return this.�������������<���, int>(������, �����);
    }

    public override ��� �������������<���>(string ������, DateTime �����) {
      return this.�������������<���, string>(������, �����);
    }

    protected ��� �����������<���, ����>(���� ����) {
      return DALEngine.�����������<���, ����>(ConnectionGetter(), ����);
      //string ���������� = ������������������(typeof(���).Name);

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "�����������." + ���������� + ".�����������()", ����);
      //  ��� res = (���)V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
      //  V8A.ReleaseComObject(ComResult);
      //  return res;
      //}
    }

    public override ��� �����������<���>(int ����) {
      return this.�����������<���, int>(����);
    }

    public override ��� �����������<���>(string ����) {
      return this.�����������<���, string>(����);
    }

    public override ��� �������������������<���>(string �����) {
      return DALEngine.�������������������<���>(ConnectionGetter(), �����);
      //string ���������� = ������������������(typeof(���).Name);

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "�����������." + ���������� + ".�������������������()", �����);
      //  ��� res = (���)V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
      //  V8A.ReleaseComObject(ComResult);
      //  return res;
      //}
    }

    public override ��� ����������������<���>(string �������������, object ������������������, ��� ���������, object ���������) {
      return DALEngine.����������������(ConnectionGetter(), �������������, ������������������, ���������, ���������);
      //string ���������� = ������������������(typeof(���).Name);

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object Com�������� = V8A.ConvertValueNetToV8(������������������, con);
      //  object Com�������� = null;
      //  if (��������� != null) { //!!!!!!!!!! �������� !!!!!!!!!!!!!!!!!!!
      //    Com�������� = V8A.ConvertValueNetToV8(���������, con);
      //  }
      //  object Com�������� = null;
      //  if (��������� != null) { //!!!!!!!!!! �������� !!!!!!!!!!!!!!!!!!!
      //    Com�������� = V8A.ConvertValueNetToV8(���������, con);
      //  }
      //  object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "�����������." + ���������� + ".����������������()", �������������, Com��������, Com��������, Com��������);
      //  ��� result = (���)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(���));
      //  V8A.ReleaseComObject(ComResult);
      //  V8A.ReleaseComObject(Com��������);
      //  V8A.ReleaseComObject(Com��������);
      //  return result;
      //}
    }

    public override T ������������<T>(ObjectRef oRef, string aName) {
      return DALEngine.������������<T>(ConnectionGetter(), oRef, aName);
      //Type CurType = typeof(T);
      //if (oRef.IsEmpty()) {
      //  if (CurType.Equals(typeof(string))) {
      //    return (T)((object)string.Empty);
      //  }
      //  return CurType.IsValueType ? default(T) : (T)Activator.CreateInstance(CurType);
      //} else {
      //  Type UType = Nullable.GetUnderlyingType(CurType) ?? CurType;

      //  using (DbConnection con = ConnectV8(_transactionID)) {
      //    object V8Ref = V8A.Reference(oRef, con);
      //    object ComResult = V8A.Call(con.Connection, V8Ref, aName);
      //    V8A.ReleaseComObject(V8Ref);
      //    object value = V8A.ConvertValueV8ToNet(ComResult, con, UType);
      //    V8A.ReleaseComObject(ComResult);
      //    return (T)value;
      //  }
      //}
    }

    public override TabList<T> ����������������������<T>(ObjectRef oRef, string aName) {
      return DALEngine.����������������������<T>(ConnectionGetter(), oRef, aName);
    }

    public override string �����������������������������(ObjectRef oRef) {
      if (oRef.IsEmpty()) return string.Empty;
      return DALEngine.�����������������������������(ConnectionGetter(), oRef);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  return �����������������������������(con, oRef);
      //}
    }

    //private string �����������������������������(DbConnection con, ObjectRef oRef) {
    //  if (oRef.IsEmpty()) return string.Empty;

    //  string ���������� = ������������������(oRef.GetType().Name);
    //  string ��������������;
    //  if (oRef is CatalogRef) {
    //    �������������� = "����������.";
    //  } else if (oRef is DocumentRef) {
    //    �������������� = "��������.";
    //  } else {
    //    throw new ArgumentException("������ �� �������� �� ������������ �� ����������");
    //  }

    //  string ������������ = "������� " + ���������� + "." + ������������������ +
    //    " �� " + �������������� + ���������� +
    //    " ��� " + ���������� + " ���	" + ���������� + ".������ = &������";

    //  string result;

    //  ������������[] ��� = new ������������[] { new ������������("������", oRef) };
    //  object qResult = V8A.������������������������(con, ������������, ���);
    //  object ������� = V8A.Call(con.Connection, qResult, "�������()");
    //  V8A.ReleaseComObject(qResult);

    //  if ((bool)V8A.Call(con.Connection, �������, "���������()")) { //������ ������ ������� ����� 1 ������
    //    result = (string)V8A.Call(con.Connection, �������, "��������()", 0);
    //  } else {
    //    throw new ArgumentNullException("������ '" + ������������ + "' �� ������ �������������.");
    //  }

    //  V8A.ReleaseComObject(�������);

    //  return result;
    //}

    public override V8Object Load(ObjectRef oRef) {
      return DALEngine.Load(ConnectionGetter(), oRef);
      //V8Object obj = ObjectCache.����������(oRef);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComV8Ref = V8A.Reference(oRef, con);

      //  foreach (PropertyInfo fi in obj.�������������) {
      //    if (fi.Name != "������") {
      //      object ComResult = V8A.Call(con.Connection, ComV8Ref, fi.Name); //.Substring(1));
      //      object value = V8A.ConvertValueV8ToNet(ComResult, con, fi.PropertyType);
      //      V8A.ReleaseComObject(ComResult);
      //      fi.SetValue(obj, value, null);
      //    }
      //  }
      //  foreach (PropertyInfo T in obj.�����������������) {
      //    IList TR = (IList)T.GetValue(obj, null);
      //    IV8TablePart IA = (IV8TablePart)TR;
      //    //if (IA.�������()) {
      //    TR.Clear();
      //    PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //    object �������������� = V8A.Call(con.Connection, ComV8Ref, T.Name);
      //    V8A.ReleaseComObject(ComV8Ref);
      //    int ���������� = (int)V8A.Call(con.Connection, ��������������, "����������()");
      //    //��� ������ ������ ��������
      //    for (int i = 0; i < ����������; i++) {
      //      object Com��������������� = V8A.Call(con.Connection, ��������������, "��������()", i);
      //      object ����������� = IA.��������();
      //      foreach (PropertyInfo ������� in �������) {
      //        object V8���� = V8A.Call(con.Connection, Com���������������, �������.Name);
      //        object �������� = V8A.ConvertValueV8ToNet(V8����, con, �������.PropertyType);
      //        V8A.ReleaseComObject(V8����);
      //        �������.SetValue(�����������, ��������, null);
      //      }
      //      V8A.ReleaseComObject(Com���������������);
      //    }
      //    V8A.ReleaseComObject(��������������);
      //  }
      //  return obj;
      //}
    }

    public override V8Object Load2(ObjectRef oRef) {
      return DALEngine.Load2(ConnectionGetter(), oRef);
      //ObjectRef ������ = oRef;
      //V8Object obj = ObjectCache.����������(oRef);
      ////string[] ���������� = ������.GetType().Name.Split(new char[] { '_' });
      ////string �������������� = ����������[0];  //����� ��� ��� ���
      ////string ���������� = ����������[1];
      //string ��������������;
      //string ���������� = ������������������(������.GetType().Name);
      ////�������������������������(������.GetType().Name, out ��������������, out ����������);
      ////if (��������������.StartsWith("���")) {
      ////  �������������� = "����������.";
      ////} else if (��������������.StartsWith("���")) {
      ////  �������������� = "��������.";
      ////}
      //if (obj is ����������) {
      //  �������������� = "����������.";
      //} else if (obj is ��������) {
      //  �������������� = "��������.";
      //} else {
      //  throw new ArgumentException("������ �� �������� �� ������������ �� ����������");
      //}
      //StringBuilder ������������ = new StringBuilder("������� " + ���������� + "." + ������������������, 2000);
      //foreach (PropertyInfo fi in obj.�������������) {
      //  if (fi.Name != "������") {
      //    Type ������� = fi.PropertyType;
      //    string fldName = fi.Name; //.Substring(1);
      //    ������������.Append("," + ���������� + "." + fldName);
      //    if (�������.IsSubclassOf(typeof(ObjectRef))) {
      //      ������������.Append("," + ���������� + "." + fldName + "." + ������������������);
      //    }
      //  }
      //}

      //foreach (PropertyInfo T in obj.�����������������) {
      //  ������������.Append("," + ���������� + "." + T.Name + ".(");
      //  IList TR = (IList)T.GetValue(obj, null);
      //  IV8TablePart IA = (IV8TablePart)TR;
      //  //if (IA.�������()) {
      //  //���-�� ���� ������������ ������ "" ������������ string.Empty �Andrew
      //  string �����������2 = "";
      //  PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //  foreach (PropertyInfo ������� in �������) {
      //    string fldName = �������.Name;
      //    ������������.Append(�����������2 + fldName);
      //    �����������2 = ",";
      //    if (�������.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
      //      ������������.Append("," + fldName + "." + ������������������);
      //    }
      //  }
      //  //}
      //  ������������.Append(")");
      //}
      //������������.Append(" �� " + �������������� + ���������� + " ��� " + ����������);
      //������������.Append(" ���	" + ���������� + ".������ = &������");

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  ������������[] ��� = new ������������[] { new ������������("������", ������) };
      //  object result = V8A.������������������������(con, ������������.ToString(), ���);
      //  object ������� = V8A.Call(con.Connection, result, "�������()");
      //  V8A.ReleaseComObject(result);
      //  if ((bool)V8A.Call(con.Connection, �������, "���������()")) { //������ ������ ������� ����� 1 ������
      //    int i = 0;
      //    bool ��������� = true;
      //    object V8����;
      //    foreach (PropertyInfo fi in obj.�������������) {
      //      if (fi.Name != "������") {
      //        if (���������) { //������ ��� ������������� ��� ������. ���� ������ �������� �������
      //          ��������� = false;
      //          V8���� = V8A.Call(con.Connection, �������, "��������()", i++);
      //          ������.������������� = (string)V8A.ConvertValueV8ToNet(V8����, con, typeof(string));
      //          //V8A.ReleaseComObject(V8����); �� ����, ��� ������
      //        }
      //        V8���� = V8A.Call(con.Connection, �������, "��������()", i++);
      //        object �������� = V8A.ConvertValueV8ToNet(V8����, con, fi.PropertyType);
      //        V8A.ReleaseComObject(V8����);
      //        ObjectRef oref = �������� as ObjectRef;
      //        if (oref != null) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
      //          V8���� = V8A.Call(con.Connection, �������, "��������()", i++);
      //          string ������������� = (string)V8A.ConvertValueV8ToNet(V8����, con, typeof(string));
      //          oref.������������� = ������������� ?? string.Empty;
      //          //V8A.ReleaseComObject(V8����); �� ����, ��� ������
      //        }
      //        fi.SetValue(obj, ��������, null);
      //      }
      //    }
      //    foreach (PropertyInfo T in obj.�����������������) {
      //      IList TR = (IList)T.GetValue(obj, null);
      //      IV8TablePart IA = (IV8TablePart)TR;
      //      TR.Clear();
      //      PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //      object Com������������������ = V8A.Call(con.Connection, �������, "��������()", i++);
      //      object Com����������� = V8A.Call(con.Connection, Com������������������, "�������()");
      //      V8A.ReleaseComObject(Com������������������);
      //      while ((bool)V8A.Call(con.Connection, Com�����������, "���������()")) {
      //        object ����������� = IA.��������();
      //        int j = 0;
      //        foreach (PropertyInfo ������� in �������) {
      //          V8���� = V8A.Call(con.Connection, Com�����������, "��������()", j++);
      //          Type ������� = �������.PropertyType;
      //          object �������� = V8A.ConvertValueV8ToNet(V8����, con, �������);
      //          V8A.ReleaseComObject(V8����);

      //          ObjectRef oref = �������� as ObjectRef;
      //          if (�������.IsSubclassOf(typeof(ObjectRef))) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
      //            //if (oref != null) {//�� ��������� ����� � ��������� ���� ������� ������ ���� �������������
      //            object ComResult = V8A.Call(con.Connection, Com�����������, "��������()", j++);
      //            string ������������� = (string)V8A.ConvertValueV8ToNet(ComResult, con, typeof(string));
      //            oref.������������� = ������������� == null ? string.Empty : �������������;
      //            //V8A.ReleaseComObject(ComResult); �� ����, ��� ������
      //          } else if (oref != null) {
      //            oref.������������� = �����������������������������(con, oref);
      //            //oref.���������������������();
      //            //this.������������<string>(con, oref, ������������������);
      //          }

      //          �������.SetValue(�����������, ��������, null);
      //        }
      //      }
      //      V8A.ReleaseComObject(Com�����������);
      //    }
      //  }
      //  V8A.ReleaseComObject(�������);
      //  return obj;
      //}
    }

    //private void WriteRequest(StringBuilder writer, ObjectRef reference) {
    //  string ��������������;
    //  string ����������;
    //  �������������������������(reference.GetType().Name, out ��������������, out ����������);
    //  if (��������������.StartsWith("���")) {
    //    �������������� = "����������.";
    //  } else if (��������������.StartsWith("���")) {
    //    �������������� = "��������.";
    //  } else throw new ArgumentException("������ ���������� ������� - " + reference.GetType().Name + " GUID=" + reference.UUID);

    //  writer.Append("������� " + ���������� + "." + ������������������);

    //  writer.Append(" �� " + �������������� + ���������� + " ��� " + ����������);
    //  writer.Append(" ���	" + ���������� + ".������ = &������");
    //}

    public override ObjectRef ��������(���������� obj) {
      return DALEngine.��������(ConnectionGetter(), obj);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object V8ComObj = ��������������������(con, obj);
      //  //if (obj is ����������) {
      //  �����������(con, obj, V8ComObj);
      //  //}
      //  V8A.ReleaseComObject(V8ComObj);
      //  return obj.������;
      //}
    }

    public override ObjectRef ����������������(�������� obj, �������������������� ����������, ������������������������ ��������) {
      return DALEngine.��������(ConnectionGetter(), obj, ����������, ��������);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object V8ComObj = ��������������������(con, obj);
      //  �����������(con, obj, V8ComObj, ����������, ��������);
      //  V8A.ReleaseComObject(V8ComObj);
      //  return obj.������;
      //}
    }

    //private void �����������(DbConnection con, �������� obj, object V8Obj, �������������������� ����������, ������������������������ ��������) {
    //  object ComObj��� = V8A.ConvertValueNetToV8(����������, con);
    //  object ComObj���� = V8A.ConvertValueNetToV8(��������, con);
    //  ObjectRef ������ = obj.������;
    //  V8Gate.V8A.Call(con.Connection, V8Obj, "��������()", ComObj���, ComObj����);
    //  V8Gate.V8A.ReleaseComObject(ComObj����);
    //  V8Gate.V8A.ReleaseComObject(ComObj���);
    //  if (������.IsEmpty()) {
    //    object ComResult = V8Gate.V8A.Call(con.Connection, V8Obj, "������");
    //    obj.������ = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, null) as V8Gate.ObjectRef;
    //    V8Gate.V8A.ReleaseComObject(ComResult);
    //    ObjectCache.��������(obj.������, obj);
    //  }
    //}

    //private object ��������������������(DbConnection con, V8Object obj) {
    //  ObjectRef ������ = obj.������;
    //  object V8ComObj = null;
    //  if (!������.IsEmpty()) {
    //    object V8Ref = V8Gate.V8A.Reference(������, con);
    //    V8ComObj = V8Gate.V8A.Call(con.Connection, V8Ref, "��������������()");
    //    V8Gate.V8A.ReleaseComObject(V8Ref);
    //  } else {
    //    //string[] ���������� = ������.GetType().Name.Split(new char[] { '_' });
    //    //string �������������� = ����������[0];  //����� ��� ��� ���
    //    //string ���������� = ����������[1];
    //    string ��������������;
    //    string ����������;
    //    �������������������������(������.GetType().Name, out ��������������, out ����������);
    //    if (��������������.StartsWith("���")) {
    //      //���������� ��� = obj as ����������;
    //      PropertyInfo pi = obj.GetType().GetProperty("���������");
    //      bool ������������� = false;
    //      if (pi != null && (bool)(pi.GetValue(obj, null) ?? false)) {
    //        ������������� = true;
    //      }
    //      if (�������������) {
    //        //if (���._��������� ?? false) {
    //        �������������� = "�����������." + ���������� + ".�������������()";
    //      } else {
    //        �������������� = "�����������." + ���������� + ".��������������()";
    //      }
    //    } else if (��������������.StartsWith("���")) {
    //      �������������� = "���������." + ���������� + ".���������������()";
    //    }
    //    V8ComObj = V8Gate.V8A.Call(con.Connection, con.Connection.comObject, ��������������);
    //  }

    //  foreach (PropertyInfo fi in obj.�������������) {
    //    if (fi.Name != "������") {
    //      object value = fi.GetValue(obj, null);
    //      if (value != null) {
    //        object V8Value = V8Gate.V8A.ConvertValueNetToV8(value, con);
    //        V8Gate.V8A.SetProp(con.Connection, V8ComObj, fi.Name, V8Value);
    //        V8Gate.V8A.ReleaseComObject(V8Value);
    //      }
    //    }
    //  }
    //  foreach (PropertyInfo T in obj.�����������������) {
    //    IList TR = (IList)T.GetValue(obj, null);
    //    IV8TablePart IA = (IV8TablePart)TR;
    //    //if (IA.�������()) {
    //    PropertyInfo[] ������� = IA.�����������������������().GetProperties(BindingFlags.Public | BindingFlags.Instance);
    //    object �������������� = V8Gate.V8A.Call(con.Connection, V8ComObj, T.Name);
    //    V8Gate.V8A.Call(con.Connection, ��������������, "��������()");
    //    foreach (object Str in TR) {
    //      object Com�������� = V8Gate.V8A.Call(con.Connection, ��������������, "��������()");
    //      foreach (PropertyInfo f in �������) {
    //        object value = V8Gate.V8A.ConvertValueNetToV8(f.GetValue(Str, null), con);
    //        V8Gate.V8A.SetProp(con.Connection, Com��������, f.Name, value);
    //        V8Gate.V8A.ReleaseComObject(value);
    //      }
    //      V8Gate.V8A.ReleaseComObject(Com��������);
    //    }
    //    V8Gate.V8A.ReleaseComObject(��������������);
    //  }
    //  return V8ComObj;
    //}

    //private void �����������(DbConnection con, V8Object obj, object V8Obj) {
    //  ObjectRef ������ = obj.������;
    //  V8Gate.V8A.Call(con.Connection, V8Obj, "��������()");
    //  if (������.IsEmpty()) {
    //    object ComResult = V8Gate.V8A.Call(con.Connection, V8Obj, "������");
    //    obj.������ = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, null) as V8Gate.ObjectRef;
    //    V8Gate.V8A.ReleaseComObject(ComResult);
    //    ObjectCache.��������(obj.������, obj);
    //    //if (obj is ����������) { //�� ������ ������ ��������, ����� �����������
    //    //  PropertyInfo pi = obj.GetType().GetProperty("���", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
    //    //  object value = V8Gate.V8A.ConvertValueV8ToNet(V8Gate.V8A.Get(V8Obj, "���"), con, null);
    //    //  pi.SetValue(obj, value, null);
    //    //}
    //  }
    //}

    public override void ������������������������(string �������) {
      ������������������������(�������, null, null);
    }

    public override void ������������������������(string �������, object ������, string ����������) {
      DALEngine.������������������������(ConnectionGetter(), �������, ������, ����������);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object Com������ = V8A.ConvertValueNetToV8(������, con);
      //  //V8A.Invoke(con.connection.comObject, "������������������������", BindingFlags.InvokeMethod, new object[] { �������, null, null, Com������, ���������� });
      //  V8A.Call(con.Connection, con.Connection.comObject, "������������������������()", new object[] { �������, null, null, Com������, ���������� });
      //  V8A.ReleaseComObject(Com������);
      //}
    }
  }
  #endregion

  //****************************************************************************************************************
  //****************************************************************************************************************
  //****************************************************************************************************************

  #region "RemoteDAL"
  public class RemoteDAL:DAL {
    readonly ComDAL _dal;

    public RemoteDAL() {
      TcpChannel channel = new TcpChannel(0);
      ChannelServices.RegisterChannel(channel, false);
      const string _configRemoteURLStr = "RemoteDALurl";
      string remoteURL = System.Configuration.ConfigurationManager.AppSettings[_configRemoteURLStr];
      if (remoteURL == null) throw new ApplicationException("� AppSettings �������� " + _configRemoteURLStr + " �� �����");
      _dal = (ComDAL)Activator.GetObject(typeof(ComDAL), remoteURL);
    }

    public override void ClearPool() {
      _dal.ClearPool();
    }

    public override ConnectionsInfo GetConnectionsInfo {
      get { return _dal.GetConnectionsInfo; }
    }

    public override string ���������������JS(string �������������, ������������[] ����������) {
      return _dal.���������������JS(�������������, ����������);
    }

    public override List<T> ���������������<T>(string �������������, ������������[] ����������) {
      PropertyInfo[] ������� = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
      bool[] �������� = new bool[�������.Length];
      int ����������� = 0;
      int i = 0;
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

      object[,] lst2 = _dal.���������������(�������������, ����������, ������������, �������������);
      int rows = lst2.GetLength(0);
      int cols = lst2.GetLength(1);
      List<T> lst3 = new List<T>(rows);
      for (int row = 0; row < rows; row++) {
        T ��� = new T();
        for (col = 0; col < cols; col++) {
          �������[col].SetValue(���, lst2[row, col], null); //� ��� ��, ����� �������.Length != ����������� ?!!!
        }
        lst3.Add(���);
      }
      return lst3;
    }

    public override ��� �������������������<���>(string �����) {
      return _dal.�������������������<���>(�����);
    }

    public override ��� �������������<���>(int ������, DateTime �����) {
      return _dal.�������������<���>(������, �����);
    }

    public override ��� �������������<���>(string ������, DateTime �����) {
      return _dal.�������������<���>(������, �����);
    }

    public override ��� �����������<���>(int ����) {
      return _dal.�����������<���>(����);
    }

    public override ��� �����������<���>(string ����) {
      return _dal.�����������<���>(����);
    }

    public override ��� ����������������<���>(string �������������, object ������������������, ��� ���������, object ���������) {
      return _dal.����������������<���>(�������������, ������������������, ���������, ���������);
    }

    public override T ������������<T>(ObjectRef oRef, string aName) {
      return _dal.������������<T>(oRef, aName);
    }

    public override TabList<T> ����������������������<T>(ObjectRef oRef, string aName) {
      return _dal.����������������������<T>(oRef, aName);
    }

    public override string �����������������������������(ObjectRef oRef) {
      return _dal.�����������������������������(oRef);
    }

    public override V8Object Load(ObjectRef oRef) {
      return _dal.Load(oRef);
    }

    public override V8Object Load2(ObjectRef oRef) {
      return _dal.Load2(oRef);
    }

    public override ObjectRef ��������(���������� obj) {
      return _dal.��������(obj);
    }

    public override ObjectRef ����������������(�������� obj, �������������������� ����������, ������������������������ ��������) {
      return _dal.����������������(obj, ����������, ��������);
    }

    public override void ������������������������(string �������) {
      _dal.������������������������(�������);
    }

    public override void ������������������������(string �������, object ������, string ����������) {
      _dal.������������������������(�������, ������, ����������);
    }
  }
  #endregion

  //****************************************************************************************************************
  //****************************************************************************************************************
  //****************************************************************************************************************

  #region "TransactionDAL"
  public class TransactionDAL:ComDAL, IDisposable {
    private bool _disposed = false;
    private Guid _transactionID;

    internal override Func<DbConnection> ConnectionGetter() {
      return () => DbConnections.Instance.ConnectV8(_transactionID);
    }

    #region "Transaction"
    public void BeginTransaction() {
      if (_transactionID != Guid.Empty) {
        throw new ApplicationException("���������� ����������. ��������� ���������� �� ��������������.");
      }
      using (DbConnection con = DbConnections.Instance.ConnectV8(_transactionID)) {
        _transactionID = con.����������������(TimeSpan.MaxValue);
      }
    }

    public void CommitTransaction() {
      using (DbConnection con = DbConnections.Instance.ConnectV8(_transactionID)) {
        con.�����������������������();
        _transactionID = Guid.Empty;
      }
    }

    public void RollbackTransaction() {
      using (DbConnection con = DbConnections.Instance.ConnectV8(_transactionID)) {
        con.������������������();
        _transactionID = Guid.Empty;
      }
    }
    #endregion

    #region "implementing IDisposable"
    protected virtual void Dispose(bool disposing) {
      if (!this._disposed) {
        if (disposing) {
          if (_transactionID != Guid.Empty) {
            RollbackTransaction();
          }
        }
      }
      _disposed = true;
    }

    public virtual void Dispose() {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~TransactionDAL() {
      this.Dispose(false);
    }
    #endregion

  }
  #endregion

  //****************************************************************************************************************
  //****************************************************************************************************************
  //****************************************************************************************************************
}
