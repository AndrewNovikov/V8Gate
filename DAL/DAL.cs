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
  //      throw new Exception("В AppSettings параметр " + strDalType + @" может быть только COM или REMOTE, а не """ + strCurDal + @"""");
  //    }
  //  }
  //}


  //public interface IRemoteDAL {	 ///чтобы отличать RemoteDAL
  //}

  //public abstract class AbstractDAL : MarshalByRefObject {

  //  public abstract V8Object Load(ObjectRef oRef);

  //  public abstract V8Object Load2(ObjectRef oRef);

  //  public abstract ObjectRef Записать(V8Object obj);	//для новых объектов нужно возвращаемое значение - присвоенная Ссылка

  //  public abstract ObjectRef ЗаписатьДокумент(Документ obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров);

  //  public abstract T ПолучитьПоле<T>(ObjectRef oRef, string aName);

  //  //		internal abstract CatalogRef НайтиПоНаименованию(CatalogRef obj, string аНаим);

  //  //		internal abstract CatalogRef НайтиПоКоду(IntNumCatRef obj, int аКод);

  //  //		public abstract CatalogRef НайтиПоКоду(StrNumCatRef obj, string аКод);

  //  //		internal abstract StrNumDocRef НайтиПоНомеру(StrNumDocRef obj, string аНомер, DateTime аДата);

  //  //		internal abstract IntNumDocRef НайтиПоНомеру(IntNumDocRef obj, int аНомер, DateTime аДата);

  //  public abstract Тип НайтиПоНаименованию<Тип>(string аНаим) where Тип : CatalogRef;

  //  public abstract Тип НайтиПоРеквизиту<Тип>(string аИмяРеквизита, object аЗначениеРеквизита, Тип аРодитель, object аВладелец) where Тип : CatalogRef;

  //  public abstract Тип НайтиПоКоду<Тип>(string аКод) where Тип : StrNumCatRef;

  //  public abstract Тип НайтиПоКоду<Тип>(int аКод) where Тип : IntNumCatRef;

  //  public abstract Тип НайтиПоНомеру<Тип>(string аНомер, DateTime аДата) where Тип : StrNumDocRef;

  //  public abstract Тип НайтиПоНомеру<Тип>(int аНомер, DateTime аДата) where Тип : IntNumDocRef;

  //  //public abstract List<ТипыКолонок> ВыпЗапрос<ТипыКолонок>(string аТекстЗапроса)
  //  //  where ТипыКолонок : new();

  //  //public abstract List<ТипыКолонок> ВыпЗапрос<ТипыКолонок>(string аТекстЗапроса, ПарамЗапроса[] аПараметры)
  //  //  where ТипыКолонок : ISerializable, new();

  //  public abstract object[,] ВыполнитьЗапрос(string аТекстЗапроса, ПарамЗапроса[] аПараметры,
  //    Type[] ТипыКолонок, string[] ИменаКолонок);

  //  public abstract void ClearPool();

  //}
  #endregion

  #region "DALEngine"
  internal static class DALEngine {
    const string констПредставление = "Представление";

    private static string ПолучитьИмяТаблицы(string имяТипа) {
      int sepIndex = имяТипа.IndexOf('_');
      if (sepIndex == -1) {
        throw new ArgumentException("Тип " + имяТипа + " не содержит префикса");
      }
      return имяТипа.Substring(++sepIndex, имяТипа.Length - sepIndex);
    }

    private static void ПолучитьПрефиксИмяТаблицы(string имяТипа, out string префикс, out string имя) {
      int sepIndex = имяТипа.IndexOf('_');
      if (sepIndex == -1) {
        throw new ArgumentException("Тип " + имяТипа + " не содержит префикса");
      }
      префикс = имяТипа.Substring(0, sepIndex);
      имя = имяТипа.Substring(++sepIndex, имяТипа.Length - sepIndex);
    }

    public static string ВыполнитьЗапросJS(Func<DbConnection> getCon, string аТекстЗапроса, ПарамЗапроса[] аПараметры) {
      using (DbConnection con = getCon()) {
        object ComObjQueryResult = V8A.ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);

        object ComObjКолонки = V8A.Call(con.Connection, ComObjQueryResult, "Колонки");
        int ЧислоКолВЗапросе = (int)V8A.Call(con.Connection, ComObjКолонки, "Количество()");

        Dictionary<string, ИнфОПоле> Поля = new Dictionary<string, ИнфОПоле>(ЧислоКолВЗапросе);
        List<ИнфОПоле> ПоляПредставлений = new List<ИнфОПоле>(ЧислоКолВЗапросе / 2);

        for (int i = 0; i != ЧислоКолВЗапросе; i++) {
          object ComObjКолонка = V8A.Call(con.Connection, ComObjКолонки, "Получить()", i);
          string ИмяКолон = (string)V8A.Call(con.Connection, ComObjКолонка, "Имя");
          V8A.ReleaseComObject(ComObjКолонка);
          ИнфОПоле Поле = new ИнфОПоле();
          Поле.Индекс = i;
          if (ИмяКолон.EndsWith(констПредставление)) {
            Поле.Имя = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
            ПоляПредставлений.Add(Поле);
          } else {
            Поле.Имя = ИмяКолон;
            Поля.Add(ИмяКолон, Поле);
          }
        }
        V8A.ReleaseComObject(ComObjКолонки);

        foreach (ИнфОПоле поле in ПоляПредставлений) {
          ИнфОПоле полеСсылки;
          if (!Поля.TryGetValue(поле.Имя, out полеСсылки)) {
            throw new ArgumentException("В запросе нет поля типа ссылка с именем " + поле.Имя);
          } else {
            полеСсылки.ПолеПредставления = поле;
          }
        }

        object ComВыборка = V8A.Call(con.Connection, ComObjQueryResult, "Выбрать()");
        V8A.ReleaseComObject(ComObjQueryResult);
        StringBuilder Результат = new StringBuilder("[");
        if ((bool)V8A.Call(con.Connection, ComВыборка, "Следующий()")) {
          Результат.Append("{");
          foreach (ИнфОПоле поле in Поля.Values) {
            object V8Поле = V8A.Call(con.Connection, ComВыборка, "Получить()", поле.Индекс);
            object Значение = V8A.ConvertValueV8ToJS(V8Поле, con);
            V8A.ReleaseComObject(V8Поле);
            if (поле.ПолеПредставления != null) {
              string представление = V8A.Call(con.Connection, ComВыборка, "Получить()", поле.ПолеПредставления.Индекс) as string;
              Результат.Append(@"""" + поле.Имя + @""":{""guid"":" + Значение + @",""представление"":""" + System.Web.HttpUtility.HtmlEncode(представление) + @"""},");
            } else {
              Результат.Append(@"""" + поле.Имя + @""":" + Значение + @",");
            }
          }
          Результат.Remove(Результат.Length - 1, 1);
          Результат.Append("}");
          while ((bool)V8A.Call(con.Connection, ComВыборка, "Следующий()")) {
            Результат.Append(",{");
            foreach (ИнфОПоле поле in Поля.Values) {
              object V8Поле = V8A.Call(con.Connection, ComВыборка, "Получить()", поле.Индекс);
              object Значение = V8A.ConvertValueV8ToJS(V8Поле, con);
              V8A.ReleaseComObject(V8Поле);
              if (поле.ПолеПредставления != null) {
                string представление = V8A.Call(con.Connection, ComВыборка, "Получить()", поле.ПолеПредставления.Индекс) as string;
                Результат.Append(@"""" + поле.Имя + @""":{""guid"":" + Значение + @",""представление"":""" + System.Web.HttpUtility.HtmlEncode(представление) + @"""},");
              } else {
                Результат.Append(@"""" + поле.Имя + @""":" + Значение + @",");
              }
            }
            Результат.Remove(Результат.Length - 1, 1);
            Результат.Append("}");
          }
        }
        Результат.Append("]");
        V8A.ReleaseComObject(ComВыборка);
        return Результат.ToString();
      }
    }

    public static List<ТипРяда> ВыполнитьЗапрос<ТипРяда>(Func<DbConnection> getCon, string аТекстЗапроса, ПарамЗапроса[] аПараметры) where ТипРяда:new() {
      using (DbConnection con = getCon()) {
        object ComObjQueryResult = V8A.ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);

        object ComObjКолонки = V8A.Call(con.Connection, ComObjQueryResult, "Колонки");
        int ЧислоКолВЗапросе = (int)V8A.Call(con.Connection, ComObjКолонки, "Количество()");
        List<ИнфОПоле> ПоляЗапроса = new List<ИнфОПоле>(ЧислоКолВЗапросе);
        Dictionary<string, int> ФактИмяКол2Инд = new Dictionary<string, int>(ЧислоКолВЗапросе);
        int[] ИндексыПредставлений = new int[ЧислоКолВЗапросе];
        int КолвоПредст = 0;
        for (int i = 0; i != ЧислоКолВЗапросе; i++) {
          object ComObjКолонка = V8A.Call(con.Connection, ComObjКолонки, "Получить()", i);
          string ИмяКолон = (string)V8A.Call(con.Connection, ComObjКолонка, "Имя");
          V8A.ReleaseComObject(ComObjКолонка);
          ФактИмяКол2Инд[ИмяКолон] = i;
          ИнфОПоле Поле = new ИнфОПоле();
          Поле.ЭтоПредст = ИмяКолон.EndsWith(констПредставление);
          if (Поле.ЭтоПредст) {
            Поле.ИмяСсылки = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
            Поле.Тип = typeof(string);
            ИндексыПредставлений[КолвоПредст] = i;
            КолвоПредст++;
          } else {
            Поле.PtyInfo = typeof(ТипРяда).GetProperty(ИмяКолон);
            Поле.Тип = Поле.PtyInfo.PropertyType;
          }
          ПоляЗапроса.Add(Поле);

        }
        V8A.ReleaseComObject(ComObjКолонки);
        for (int i = 0; i < КолвоПредст; i++) {
          ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];

          if (!ФактИмяКол2Инд.TryGetValue(Поле.ИмяСсылки, out Поле.ИндексСсылки)) {
            throw new ArgumentException("В запросе нет поля типа ссылка с именем " + Поле.ИмяСсылки);
          } else {
            ИнфОПоле ПолеСсылки = ПоляЗапроса[Поле.ИндексСсылки];
            if (ПолеСсылки.Тип.IsSubclassOf(typeof(ObjectRef))) {
              Поле.PtyInfo = ПолеСсылки.Тип.GetProperty(констПредставление);
            } else if (ПолеСсылки.Тип != typeof(object)) {
              throw new ArgumentException("В запросе поле " + ПолеСсылки.ИмяСсылки + " должно быть типа ссылка или object");
            }
          }
        }

        object ComВыборка = V8A.Call(con.Connection, ComObjQueryResult, "Выбрать()");
        V8A.ReleaseComObject(ComObjQueryResult);
        List<ТипРяда> Результат = new List<ТипРяда>((int)V8A.Call(con.Connection, ComВыборка, "Количество()"));

        while ((bool)V8A.Call(con.Connection, ComВыборка, "Следующий()")) {
          object Стр = new ТипРяда(); //object Стр = Activator.CreateInstance(typeof(ТипыКолонок));
          for (int i = 0; i != ЧислоКолВЗапросе; i++) {
            ИнфОПоле Поле = ПоляЗапроса[i];
            object V8Поле = V8A.Call(con.Connection, ComВыборка, "Получить()", i);
            object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, Поле.Тип);
            V8A.ReleaseComObject(V8Поле);
            if (Поле.ЭтоПредст) {
              Поле.Представление = (string)Значение;
            } else {
              Поле.Значение = Значение;
              Поле.PtyInfo.SetValue(Стр, Значение, null);
            }
          }
          for (int i = 0; i < КолвоПредст; i++) {
            ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];
            object Предст = Поле.Представление;
            object Ссылка = ПоляЗапроса[Поле.ИндексСсылки].Значение;
            if (Ссылка != null) {
              if (Поле.PtyInfo != null) {
                Поле.PtyInfo.SetValue(Ссылка, Предст, null);
              } else {
                PropertyInfo prop = Ссылка.GetType().GetProperty(констПредставление);
                if (prop != null) {
                  prop.SetValue(Ссылка, Предст, null);
                }
              }
            }
          }
          Результат.Add((ТипРяда)Стр);
        }
        V8A.ReleaseComObject(ComВыборка);
        return Результат;
      }
    }

    public static object[,] ВыполнитьЗапрос(Func<DbConnection> getCon, string аТекстЗапроса, ПарамЗапроса[] аПараметры, Type[] ТипыКолонок, string[] ИменаКолонок) {
      using (DbConnection con = getCon()) {
        object ComObjQueryResult = V8A.ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);

        object ComObjКолонки = V8A.Call(con.Connection, ComObjQueryResult, "Колонки");
        int ЧислоКолВЗапросе = (int)V8A.Call(con.Connection, ComObjКолонки, "Количество()");
        List<ИнфОПоле> ПоляЗапроса = new List<ИнфОПоле>(ЧислоКолВЗапросе);
        Dictionary<string, int> ФактИмяКол2Инд = new Dictionary<string, int>(ЧислоКолВЗапросе);
        int[] ИндексыПредставлений = new int[ЧислоКолВЗапросе];
        int КолвоПредст = 0;
        int m = 0;
        for (int i = 0; i != ЧислоКолВЗапросе; i++) {
          object ComObjКолонка = V8A.Call(con.Connection, ComObjКолонки, "Получить()", i);
          string ИмяКолон = (string)V8A.Call(con.Connection, ComObjКолонка, "Имя");
          V8A.ReleaseComObject(ComObjКолонка);
          ФактИмяКол2Инд[ИмяКолон] = i;
          ИнфОПоле Поле = new ИнфОПоле();
          Поле.ЭтоПредст = ИмяКолон.EndsWith(констПредставление);
          if (Поле.ЭтоПредст) {
            Поле.ИмяСсылки = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
            Поле.Тип = typeof(string);
            ИндексыПредставлений[КолвоПредст] = i;
            КолвоПредст++;
          } else {
            if (m >= ТипыКолонок.Length) {
              throw new ArgumentException(@"Слишком мало properties в классе запроса");
            }
            if (ИмяКолон != ИменаКолонок[m]) {
              throw new ArgumentException(@"Должно идти property """ + ИмяКолон + @""", а не """ + ИменаКолонок[m] + @"""");
            }
            Поле.Тип = ТипыКолонок[m];
            m++;
          }
          ПоляЗапроса.Add(Поле);
        }
        V8A.ReleaseComObject(ComObjКолонки);

        for (int i = 0; i < КолвоПредст; i++) {
          ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];

          if (!ФактИмяКол2Инд.TryGetValue(Поле.ИмяСсылки, out Поле.ИндексСсылки)) {
            throw new ArgumentException("В запросе нет поля типа ссылка с именем " + Поле.ИмяСсылки);
          } else {
            ИнфОПоле ПолеСсылки = ПоляЗапроса[Поле.ИндексСсылки];
            if (ПолеСсылки.Тип.IsSubclassOf(typeof(ObjectRef))) {
              Поле.PtyInfo = ПолеСсылки.Тип.GetProperty(констПредставление);
            } else if (ПолеСсылки.Тип != typeof(object)) {
              throw new ArgumentException("В запросе поле " + ПолеСсылки.ИмяСсылки + " должно быть типа ссылка или object");
            }
          }
        }

        object ComВыборка = V8A.Call(con.Connection, ComObjQueryResult, "Выбрать()");
        V8A.ReleaseComObject(ComObjQueryResult);
        object[,] Результат = new object[(int)V8A.Call(con.Connection, ComВыборка, "Количество()"), ТипыКолонок.Length];
        int j = 0;
        while ((bool)V8A.Call(con.Connection, ComВыборка, "Следующий()")) {
          int k = 0;
          for (int i = 0; i != ЧислоКолВЗапросе; i++) {
            ИнфОПоле Поле = ПоляЗапроса[i];
            object V8Поле = V8A.Call(con.Connection, ComВыборка, "Получить()", i);
            object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, Поле.Тип);
            V8A.ReleaseComObject(V8Поле);  //это может быть не ComObject
            if (Поле.ЭтоПредст) {
              Поле.Представление = (string)Значение;
            } else {
              Поле.Значение = Значение;
              Результат[j, k++] = Значение;
            }
          }

          for (int i = 0; i < КолвоПредст; i++) {
            ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];
            object Предст = Поле.Представление;
            object Ссылка = ПоляЗапроса[Поле.ИндексСсылки].Значение;
            if (Ссылка != null) {
              if (Поле.PtyInfo != null) {
                Поле.PtyInfo.SetValue(Ссылка, Предст, null);
              } else {
                PropertyInfo prop = Ссылка.GetType().GetProperty(констПредставление);
                if (prop != null) {
                  prop.SetValue(Ссылка, Предст, null);
                }
              }
            }
          }
          j++;
        }
        V8A.ReleaseComObject(ComВыборка);
        return Результат;
      }
    }

    public static Тип НайтиПоНомеру<Тип, ТНомер>(Func<DbConnection> getCon, ТНомер аНомер, DateTime аДата) {
      string ИмяТаблицы = ПолучитьИмяТаблицы(typeof(Тип).Name);

      using (DbConnection con = getCon()) {
        object ComResult = V8Gate.V8A.Call(con.Connection, con.Connection.comObject, "Документы." + ИмяТаблицы + ".НайтиПоНомеру()", аНомер, аДата);
        Тип res = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
        V8Gate.V8A.ReleaseComObject(ComResult);
        return res;
      }
    }

    public static Тип НайтиПоКоду<Тип, ТКод>(Func<DbConnection> getCon, ТКод аКод) {
      string ИмяТаблицы = ПолучитьИмяТаблицы(typeof(Тип).Name);

      using (DbConnection con = getCon()) {
        object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "Справочники." + ИмяТаблицы + ".НайтиПоКоду()", аКод);
        Тип res = (Тип)V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
        V8A.ReleaseComObject(ComResult);
        return res;
      }
    }

    public static Тип НайтиПоНаименованию<Тип>(Func<DbConnection> getCon, string аНаим) {
      string ИмяТаблицы = ПолучитьИмяТаблицы(typeof(Тип).Name);

      using (DbConnection con = getCon()) {
        object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "Справочники." + ИмяТаблицы + ".НайтиПоНаименованию()", аНаим);
        Тип res = (Тип)V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
        V8A.ReleaseComObject(ComResult);
        return res;
      }
    }

    public static Тип НайтиПоРеквизиту<Тип>(Func<DbConnection> getCon, string аИмяРеквизита, object аЗначениеРеквизита, Тип аРодитель, object аВладелец) {
      //string[] ЧастьИмени = typeof(Тип).Name.Split(new char[] { '_' });
      //string ИмяТаблицы = ЧастьИмени[1];
      string ИмяТаблицы = ПолучитьИмяТаблицы(typeof(Тип).Name);

      using (DbConnection con = getCon()) {
        object ComЗначение = V8A.ConvertValueNetToV8(аЗначениеРеквизита, con);
        object ComРодитель = null;
        if (аРодитель != null) { //!!!!!!!!!! кривизна !!!!!!!!!!!!!!!!!!!
          ComРодитель = V8A.ConvertValueNetToV8(аРодитель, con);
        }
        object ComВладелец = null;
        if (аВладелец != null) { //!!!!!!!!!! кривизна !!!!!!!!!!!!!!!!!!!
          ComВладелец = V8A.ConvertValueNetToV8(аВладелец, con);
        }
        object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "Справочники." + ИмяТаблицы + ".НайтиПоРеквизиту()", аИмяРеквизита, ComЗначение, ComРодитель, ComВладелец);
        Тип result = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
        V8A.ReleaseComObject(ComResult);
        V8A.ReleaseComObject(ComРодитель);
        V8A.ReleaseComObject(ComЗначение);
        return result;
      }
    }

    public static T ПолучитьПоле<T>(Func<DbConnection> getCon, ObjectRef oRef, string aName) {
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

    public static TabList<T> ПолучитьТабличнуюЧасть<T>(Func<DbConnection> getCon, ObjectRef oRef, string aName) where T: ТаблЧасть, new() {
      TabList<T> result = (TabList<T>)Activator.CreateInstance(typeof(TabList<T>));

      if (!oRef.IsEmpty()) {
        using (DbConnection con = getCon()) {
          object comV8Ref = V8A.Reference(oRef, con);

          IV8TablePart IA = (IV8TablePart)result;
          PropertyInfo[] колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);

          object табличнаяЧасть = V8A.Call(con.Connection, comV8Ref, aName);
          int количество = (int)V8A.Call(con.Connection, табличнаяЧасть, "Количество()");
          //для каждой строки ТабЧасти
          for (int i = 0; i < количество; i++) {
            object comСтрокаТаблЧасти = V8A.Call(con.Connection, табличнаяЧасть, "Получить()", i);
            object новаяСтрока = IA.Добавить();
            foreach (PropertyInfo колонка in колонки) {
              object v8Поле = V8A.Call(con.Connection, comСтрокаТаблЧасти, колонка.Name);
              object значение = V8A.ConvertValueV8ToNet(v8Поле, con, колонка.PropertyType);
              V8A.ReleaseComObject(v8Поле);
              колонка.SetValue(новаяСтрока, значение, null);
            }
            V8A.ReleaseComObject(comСтрокаТаблЧасти);
          }
          V8A.ReleaseComObject(табличнаяЧасть);

          V8A.ReleaseComObject(comV8Ref);
        }
      }
      return result;
    }

    public static string ПолучитьПредставлениеЗапросом(Func<DbConnection> getCon, ObjectRef oRef) {
      using (DbConnection con = getCon()) {
        return ПолучитьПредставлениеЗапросом(con, oRef);
      }
    }

    private static string ПолучитьПредставлениеЗапросом(DbConnection con, ObjectRef oRef) {
      if (oRef.IsEmpty()) return string.Empty;

      string ИмяТаблицы = ПолучитьИмяТаблицы(oRef.GetType().Name);
      string ПрефиксТаблицы;
      if (oRef is CatalogRef) {
        ПрефиксТаблицы = "Справочник.";
      } else if (oRef is DocumentRef) {
        ПрефиксТаблицы = "Документ.";
      } else {
        throw new ArgumentException("Ссылка не является ни Справочником ни Документом");
      }

      string ТекстЗапроса = "ВЫБРАТЬ " + ИмяТаблицы + "." + констПредставление +
        " ИЗ " + ПрефиксТаблицы + ИмяТаблицы +
        " КАК " + ИмяТаблицы + " ГДЕ	" + ИмяТаблицы + ".Ссылка = &Ссылка";


      string result;
      ПарамЗапроса[] Пар = new ПарамЗапроса[] { new ПарамЗапроса("Ссылка", oRef) };
      object qResult = V8A.ПолучитьРезультатЗапроса(con, ТекстЗапроса, Пар);
      object Выборка = V8A.Call(con.Connection, qResult, "Выбрать()");
      V8A.ReleaseComObject(qResult);

      if ((bool)V8A.Call(con.Connection, Выборка, "Следующий()")) { //запрос должен вернуть ровно 1 строку
        result = (string)V8A.Call(con.Connection, Выборка, "Получить()", 0);
      } else {
        throw new ArgumentNullException("Запрос '" + ТекстЗапроса + "' не вернул представления.");
      }

      V8A.ReleaseComObject(Выборка);

      return result;
    }

    #region "Load"
    public static V8Object Load(Func<DbConnection> getCon, ObjectRef oRef) {
      V8Object obj = ObjectCache.НайтиВКеше(oRef);
      using (DbConnection con = getCon()) {
        object ComV8Ref = V8A.Reference(oRef, con);

        foreach (PropertyInfo fi in obj.СвойстваШапки) {
          if (fi.Name != "Ссылка") {
            object ComResult = V8A.Call(con.Connection, ComV8Ref, fi.Name); //.Substring(1));
            object value = V8A.ConvertValueV8ToNet(ComResult, con, fi.PropertyType);
            V8A.ReleaseComObject(ComResult);
            fi.SetValue(obj, value, null);
          }
        }
        foreach (FieldInfo T in obj.ПоляТаблЧасти) {
          IList TR = (IList)T.GetValue(obj);
          string Tname = T.Name.Remove(0, 1);
          if (TR != null) {
            TR.Clear();
          } else {
            TR = (IList)Activator.CreateInstance(T.FieldType);
          }
          IV8TablePart IA = (IV8TablePart)TR;
          PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
          object ТабличнаяЧасть = V8A.Call(con.Connection, ComV8Ref, Tname);
          int Количество = (int)V8A.Call(con.Connection, ТабличнаяЧасть, "Количество()");
          //для каждой строки ТабЧасти
          for (int i = 0; i < Количество; i++) {
            object ComСтрокаТаблЧасти = V8A.Call(con.Connection, ТабличнаяЧасть, "Получить()", i);
            object НоваяСтрока = IA.Добавить();
            foreach (PropertyInfo Колонка in Колонки) {
              object V8Поле = V8A.Call(con.Connection, ComСтрокаТаблЧасти, Колонка.Name);
              object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, Колонка.PropertyType);
              V8A.ReleaseComObject(V8Поле);
              Колонка.SetValue(НоваяСтрока, Значение, null);
            }
            V8A.ReleaseComObject(ComСтрокаТаблЧасти);
          }
          V8A.ReleaseComObject(ТабличнаяЧасть);

          obj.GetType().GetProperty(Tname).SetValue(obj, TR, null);
        }
        V8A.ReleaseComObject(ComV8Ref);
        return obj;
      }
    }

    public static V8Object Load2(Func<DbConnection> getCon, ObjectRef oRef) {
      ObjectRef Ссылка = oRef;
      V8Object obj = ObjectCache.НайтиВКеше(oRef);
      //string[] ЧастьИмени = Ссылка.GetType().Name.Split(new char[] { '_' });
      //string ПрефиксТаблицы = ЧастьИмени[0];  //здесь Спр или Док
      //string ИмяТаблицы = ЧастьИмени[1];
      string ПрефиксТаблицы;
      string ИмяТаблицы = ПолучитьИмяТаблицы(Ссылка.GetType().Name);
      //ПолучитьПрефиксИмяТаблицы(Ссылка.GetType().Name, out ПрефиксТаблицы, out ИмяТаблицы);
      //if (ПрефиксТаблицы.StartsWith("Спр")) {
      //  ПрефиксТаблицы = "Справочник.";
      //} else if (ПрефиксТаблицы.StartsWith("Док")) {
      //  ПрефиксТаблицы = "Документ.";
      //}
      if (obj is Справочник) {
        ПрефиксТаблицы = "Справочник.";
      } else if (obj is Документ) {
        ПрефиксТаблицы = "Документ.";
      } else {
        throw new ArgumentException("Ссылка не является ни Справочником ни Документом");
      }
      StringBuilder ТекстЗапроса = new StringBuilder("ВЫБРАТЬ " + ИмяТаблицы + "." + констПредставление, 2000);
      foreach (PropertyInfo fi in obj.СвойстваШапки) {
        if (fi.Name != "Ссылка") {
          Type ТипПоля = fi.PropertyType;
          string fldName = fi.Name; //.Substring(1);
          ТекстЗапроса.Append("," + ИмяТаблицы + "." + fldName);
          if (ТипПоля.IsSubclassOf(typeof(ObjectRef))) {
            ТекстЗапроса.Append("," + ИмяТаблицы + "." + fldName + "." + констПредставление);
          }
        }
      }

      //foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
      //  ТекстЗапроса.Append("," + ИмяТаблицы + "." + T.Name + ".(");
      //  IList TR = (IList)T.GetValue(obj, null);
      //  IV8TablePart IA = (IV8TablePart)TR;
      //  //if (IA.Активна()) {
      //  //Где-то была рекомендация вместо "" использовать string.Empty ©Andrew
      //  string Разделитель2 = "";
      //  PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //  foreach (PropertyInfo Колонка in Колонки) {
      //    string fldName = Колонка.Name;
      //    ТекстЗапроса.Append(Разделитель2 + fldName);
      //    Разделитель2 = ",";
      //    if (Колонка.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
      //      ТекстЗапроса.Append("," + fldName + "." + констПредставление);
      //    }
      //  }
      //  //}
      //  ТекстЗапроса.Append(")");
      //}
      foreach (FieldInfo T in obj.ПоляТаблЧасти) {
        ТекстЗапроса.Append("," + ИмяТаблицы + "." + T.Name.Remove(0, 1) + ".(");
        IV8TablePart IA = (IV8TablePart)T.GetValue(obj) ?? (IV8TablePart)Activator.CreateInstance(T.FieldType);

        string Разделитель2 = string.Empty;
        PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo Колонка in Колонки) {
          string fldName = Колонка.Name;
          ТекстЗапроса.Append(Разделитель2 + fldName);
          Разделитель2 = ",";
          if (Колонка.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
            ТекстЗапроса.Append("," + fldName + "." + констПредставление);
          }
        }
        ТекстЗапроса.Append(")");
      }
      ТекстЗапроса.Append(" ИЗ " + ПрефиксТаблицы + ИмяТаблицы + " КАК " + ИмяТаблицы);
      ТекстЗапроса.Append(" ГДЕ	" + ИмяТаблицы + ".Ссылка = &Ссылка");

      using (DbConnection con = getCon()) {
        ПарамЗапроса[] Пар = new ПарамЗапроса[] { new ПарамЗапроса("Ссылка", Ссылка) };
        object result = V8A.ПолучитьРезультатЗапроса(con, ТекстЗапроса.ToString(), Пар);
        object Выборка = V8A.Call(con.Connection, result, "Выбрать()");
        V8A.ReleaseComObject(result);
        if ((bool)V8A.Call(con.Connection, Выборка, "Следующий()")) { //запрос должен вернуть ровно 1 строку
          int i = 0;
          bool ПервыйРаз = true;
          object V8Поле;
          foreach (PropertyInfo fi in obj.СвойстваШапки) {
            if (fi.Name != "Ссылка") {
              if (ПервыйРаз) { //первым идёт Представление для ссылки. Сама ссылка известна заранее
                ПервыйРаз = false;
                V8Поле = V8A.Call(con.Connection, Выборка, "Получить()", i++);
                Ссылка.Представление = (string)V8A.ConvertValueV8ToNet(V8Поле, con, typeof(string));
                //V8A.ReleaseComObject(V8Поле); не надо, это строка
              }
              V8Поле = V8A.Call(con.Connection, Выборка, "Получить()", i++);
              object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, fi.PropertyType);
              V8A.ReleaseComObject(V8Поле);
              ObjectRef oref = Значение as ObjectRef;
              if (oref != null) {//за ссылочным типом в следующем поле запроса должно быть Представление
                V8Поле = V8A.Call(con.Connection, Выборка, "Получить()", i++);
                string представление = (string)V8A.ConvertValueV8ToNet(V8Поле, con, typeof(string));
                oref.Представление = представление ?? string.Empty;
                //V8A.ReleaseComObject(V8Поле); не надо, это строка
              }
              fi.SetValue(obj, Значение, null);
            }
          }
          foreach (FieldInfo T in obj.ПоляТаблЧасти) {
            IList TR = (IList)T.GetValue(obj);
            string Tname = T.Name.Remove(0, 1);
            if (TR != null) {
              TR.Clear();
            } else {
              TR = (IList)Activator.CreateInstance(T.FieldType);
            }
            IV8TablePart IA = (IV8TablePart)TR;
            PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            object ComВложРезультЗапроса = V8A.Call(con.Connection, Выборка, "Получить()", i++);
            object ComВложВыборка = V8A.Call(con.Connection, ComВложРезультЗапроса, "Выбрать()");
            V8A.ReleaseComObject(ComВложРезультЗапроса);
            while ((bool)V8A.Call(con.Connection, ComВложВыборка, "Следующий()")) {
              object НоваяСтрока = IA.Добавить();
              int j = 0;
              foreach (PropertyInfo Колонка in Колонки) {
                V8Поле = V8A.Call(con.Connection, ComВложВыборка, "Получить()", j++);
                Type ТипПоля = Колонка.PropertyType;
                object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, ТипПоля);
                V8A.ReleaseComObject(V8Поле);

                ObjectRef oref = Значение as ObjectRef;
                if (ТипПоля.IsSubclassOf(typeof(ObjectRef))) {//за ссылочным типом в следующем поле запроса должно быть Представление
                  //if (oref != null) {//за ссылочным типом в следующем поле запроса должно быть Представление
                  object ComResult = V8A.Call(con.Connection, ComВложВыборка, "Получить()", j++);
                  string представление = (string)V8A.ConvertValueV8ToNet(ComResult, con, typeof(string));
                  oref.Представление = представление ?? string.Empty;
                  //V8A.ReleaseComObject(ComResult); не надо, это строка
                } else if (oref != null) {
                  oref.Представление = ПолучитьПредставлениеЗапросом(con, oref);
                  //oref.ПолучитьПредставление();
                  //this.ПолучитьПоле<string>(con, oref, констПредставление);
                }

                Колонка.SetValue(НоваяСтрока, Значение, null);
              }
            }
            V8A.ReleaseComObject(ComВложВыборка);

            obj.GetType().GetProperty(Tname).SetValue(obj, TR, null);
          }
        }
        V8A.ReleaseComObject(Выборка);
        return obj;
      }
    }
    #endregion

    #region "Записать"
    private static object ЗаполнитьПоляОбъекта(DbConnection con, V8Object obj) {
      ObjectRef ссылка = obj.СсылкаВнутр;
      object v8ComObj = null;
      if (!ссылка.IsEmpty()) {
        object v8Ref = V8Gate.V8A.Reference(ссылка, con);
        v8ComObj = V8Gate.V8A.Call(con.Connection, v8Ref, "ПолучитьОбъект()");
        V8Gate.V8A.ReleaseComObject(v8Ref);
      } else {
        //string[] ЧастьИмени = Ссылка.GetType().Name.Split(new char[] { '_' });
        //string ПрефиксТаблицы = ЧастьИмени[0];  //здесь Спр или Док
        //string ИмяТаблицы = ЧастьИмени[1];
        string префиксТаблицы;
        string имяТаблицы;
        ПолучитьПрефиксИмяТаблицы(ссылка.GetType().Name, out префиксТаблицы, out имяТаблицы);
        if (префиксТаблицы.StartsWith("Спр")) {
          //Справочник спр = obj as Справочник;
          //PropertyInfo pi = obj.GetType().GetProperty("ЭтоГруппа");
          //bool создатьГруппу = false;
          FieldInfo fi = obj.GetType().GetField("ЭтоГруппа");
          bool создатьГруппу = (fi != null && (bool)(fi.GetValue(obj) ?? false));
          //if (pi != null && (bool)(pi.GetValue(obj, null) ?? false)) {
          //  создатьГруппу = true;
          //}
          if (создатьГруппу) {
            //if (спр._ЭтоГруппа ?? false) {
            префиксТаблицы = "Справочники." + имяТаблицы + ".СоздатьГруппу()";
          } else {
            префиксТаблицы = "Справочники." + имяТаблицы + ".СоздатьЭлемент()";
          }
        } else if (префиксТаблицы.StartsWith("Док")) {
          префиксТаблицы = "Документы." + имяТаблицы + ".СоздатьДокумент()";
        }
        v8ComObj = V8Gate.V8A.Call(con.Connection, con.Connection.comObject, префиксТаблицы);
      }

      foreach (FieldInfo fi in obj.ПоляШапки) {
        //Начало поля должно быть _
        string name = fi.Name.Remove(0, 1);
        //Если ссылка, то не трогать значение поля
        if (name != "Ссылка") {
          object value = fi.GetValue(obj);
          //И если поле не меняли и не загружали, тоже не изменять (v8ComObj от ПолучитьОбъект() уже имеет нужные поля)
          if (value != null) {
            object v8Value = V8Gate.V8A.ConvertValueNetToV8(value, con);
            V8Gate.V8A.SetProp(con.Connection, v8ComObj, name, v8Value);
            V8Gate.V8A.ReleaseComObject(v8Value);
          }
        }
      }
      foreach (FieldInfo T in obj.ПоляТаблЧасти) {
        IList tr = (IList)T.GetValue(obj);
        if (tr != null) {
          IV8TablePart ia = (IV8TablePart)tr;
          //if (IA.Активна()) {
          PropertyInfo[] колонки = ia.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
          object табличнаяЧасть = V8Gate.V8A.Call(con.Connection, v8ComObj, T.Name.Remove(0, 1));
          V8Gate.V8A.Call(con.Connection, табличнаяЧасть, "Очистить()");
          foreach (object str in tr) {
            object comСтрокаТч = V8Gate.V8A.Call(con.Connection, табличнаяЧасть, "Добавить()");
            foreach (PropertyInfo f in колонки) {
              object value = V8Gate.V8A.ConvertValueNetToV8(f.GetValue(str, null), con);
              V8Gate.V8A.SetProp(con.Connection, comСтрокаТч, f.Name, value);
              V8Gate.V8A.ReleaseComObject(value);
            }
            V8Gate.V8A.ReleaseComObject(comСтрокаТч);
          }
          V8Gate.V8A.ReleaseComObject(табличнаяЧасть);
        }
      }
      return v8ComObj;

      //foreach (PropertyInfo pi in obj.СвойстваШапки) {
      //  if (pi.Name != "Ссылка") {
      //    object value = pi.GetValue(obj, null);
      //    if (value != null) {
      //      object v8Value = V8Gate.V8A.ConvertValueNetToV8(value, con);
      //      V8Gate.V8A.SetProp(con.Connection, v8ComObj, pi.Name, v8Value);
      //      V8Gate.V8A.ReleaseComObject(v8Value);
      //    }
      //  }
      //}

      //foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
      //  IList tr = (IList)T.GetValue(obj, null);
      //  IV8TablePart ia = (IV8TablePart)tr;
      //  //if (IA.Активна()) {
      //  PropertyInfo[] колонки = ia.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //  object табличнаяЧасть = V8Gate.V8A.Call(con.Connection, v8ComObj, T.Name);
      //  V8Gate.V8A.Call(con.Connection, табличнаяЧасть, "Очистить()");
      //  foreach (object str in tr) {
      //    object comСтрокаТч = V8Gate.V8A.Call(con.Connection, табличнаяЧасть, "Добавить()");
      //    foreach (PropertyInfo f in колонки) {
      //      object value = V8Gate.V8A.ConvertValueNetToV8(f.GetValue(str, null), con);
      //      V8Gate.V8A.SetProp(con.Connection, comСтрокаТч, f.Name, value);
      //      V8Gate.V8A.ReleaseComObject(value);
      //    }
      //    V8Gate.V8A.ReleaseComObject(comСтрокаТч);
      //  }
      //  V8Gate.V8A.ReleaseComObject(табличнаяЧасть);
      //}
    }

    public static ObjectRef Записать(Func<DbConnection> getCon, Справочник obj) {
      using (DbConnection con = getCon()) {
        object v8ComObj = ЗаполнитьПоляОбъекта(con, obj);
        ЗаписатьСпр(con, obj, v8ComObj);
        V8A.ReleaseComObject(v8ComObj);
        return obj.СсылкаВнутр;
      }
    }

    private static void ЗаписатьСпр(DbConnection con, V8Object obj, object V8Obj) {
      ObjectRef ссылка = obj.СсылкаВнутр;
      V8Gate.V8A.Call(con.Connection, V8Obj, "Записать()");
      if (ссылка.IsEmpty()) {
        object comResult = V8Gate.V8A.Call(con.Connection, V8Obj, "Ссылка");
        obj.СсылкаВнутр = V8Gate.V8A.ConvertValueV8ToNet(comResult, con, null) as V8Gate.ObjectRef;
        V8Gate.V8A.ReleaseComObject(comResult);
        ObjectCache.Добавить(obj.СсылкаВнутр, obj);
      }
    }

    public static ObjectRef Записать(Func<DbConnection> getCon, Документ obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
      using (DbConnection con = getCon()) {
        object v8ComObj = ЗаполнитьПоляОбъекта(con, obj);
        ЗаписатьДок(con, obj, v8ComObj, аРежЗаписи, аРежПров);
        V8A.ReleaseComObject(v8ComObj);
        return obj.СсылкаВнутр;
      }
    }

    private static void ЗаписатьДок(DbConnection con, Документ obj, object V8Obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
      object comObjЗап = V8A.ConvertValueNetToV8(аРежЗаписи, con);
      object comObjПров = V8A.ConvertValueNetToV8(аРежПров, con);
      ObjectRef ссылка = obj.СсылкаВнутр;
      V8Gate.V8A.Call(con.Connection, V8Obj, "Записать()", comObjЗап, comObjПров);
      V8Gate.V8A.ReleaseComObject(comObjПров);
      V8Gate.V8A.ReleaseComObject(comObjЗап);
      if (ссылка.IsEmpty()) {
        object comResult = V8Gate.V8A.Call(con.Connection, V8Obj, "Ссылка");
        obj.СсылкаВнутр = V8Gate.V8A.ConvertValueV8ToNet(comResult, con, null) as V8Gate.ObjectRef;
        V8Gate.V8A.ReleaseComObject(comResult);
        ObjectCache.Добавить(obj.СсылкаВнутр, obj);
      }
    }

    public static void ЗаписьЖурналаРегистрации(Func<DbConnection> getCon, string событие, object данные, string коментарий) {
      using (DbConnection con = getCon()) {
        object comДанные = V8A.ConvertValueNetToV8(данные, con);
        V8A.Call(con.Connection, con.Connection.comObject, "ЗаписьЖурналаРегистрации()", new object[] { событие, null, null, comДанные, коментарий });
        V8A.ReleaseComObject(comДанные);
      }
    }
    #endregion

    #region "ИнфОПоле"
    internal class ИнфОПоле {
      public PropertyInfo PtyInfo;
      public int ИндексСсылки;
      public string ИмяСсылки;
      public string Представление;
      public bool ЭтоПредст;
      public object Значение;
      public Type Тип;

      public int Индекс;
      public string Имя;
      public ИнфОПоле ПолеПредставления;
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
        throw new ApplicationException("В AppSettings параметр " + strDalType + @" может быть только COM или REMOTE, а не """ + strDalType + @"""");
      }
    }

    //internal DbConnection ConnectV8(Guid transactionId) {
    //  try {
    //    return DbConnections.Instance.ConnectV8(transactionId);
    //  } catch (NoTransactionException) {
    //    //Если был вызван коннект с локальной переменной _transactionID (ComDAL), тогда обнулять и больше транзакции не искать
    //    //Если RemoteDAL вызовет ComDAL удалённо и передаст ему свой _transactionID, тогда _transactionID у локального ComDAL не трогать,
    //    //а _transactionID у RemoteDAL-а обнулится в своей обработке.
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
    //    throw new ApplicationException("Незакрытая транзакция. Вложенные транзакции не поддерживаются.");
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

    public abstract List<T> ВыполнитьЗапрос<T>(string аТекстЗапроса, ПарамЗапроса[] аПараметры) where T:new();
    //public List<T> ВыполнитьЗапрос<T>(string аТекстЗапроса, ПарамЗапроса[] аПараметры) where T:new() {
    //  return this.ВыполнитьЗапрос<T>(_transactionsID.GetFirst(), аТекстЗапроса, аПараметры);
    //}

    public abstract string ВыполнитьЗапросJS(string аТекстЗапроса, ПарамЗапроса[] аПараметры);
    //public string ВыполнитьЗапросJS(string аТекстЗапроса, ПарамЗапроса[] аПараметры) {
    //  return this.ВыполнитьЗапросJS(_transactionsID.GetFirst(), аТекстЗапроса, аПараметры);
    //}

    public abstract Тип НайтиПоНомеру<Тип>(string аНомер, DateTime аДата) where Тип:DocumentRef;
    public abstract Тип НайтиПоНомеру<Тип>(int аНомер, DateTime аДата) where Тип:DocumentRef;

    public abstract Тип НайтиПоКоду<Тип>(int аКод) where Тип:ObjectRef;
    //public Тип НайтиПоКоду<Тип>(int аКод) where Тип:ObjectRef {
    //  return this.НайтиПоКоду<Тип>(_transactionsID.GetFirst(), аКод);
    //}

    public abstract Тип НайтиПоКоду<Тип>(string аКод) where Тип:ObjectRef;
    //public Тип НайтиПоКоду<Тип>(string аКод) where Тип:ObjectRef {
    //  return this.НайтиПоКоду<Тип>(_transactionsID.GetFirst(), аКод);
    //}
    
    public abstract Тип НайтиПоНаименованию<Тип>(string аНаим) where Тип:ObjectRef;
    public abstract Тип НайтиПоРеквизиту<Тип>(string аИмяРеквизита, object аЗначениеРеквизита, Тип аРодитель, object аВладелец) where Тип:ObjectRef;
    public abstract T ПолучитьПоле<T>(ObjectRef oRef, string aName);
    public abstract TabList<T> ПолучитьТабличнуюЧасть<T>(ObjectRef oRef, string aName) where T:ТаблЧасть, new();
    public abstract string ПолучитьПредставлениеЗапросом(ObjectRef oRef);
    public abstract V8Object Load(ObjectRef oRef);
    public abstract V8Object Load2(ObjectRef oRef);

    public abstract ObjectRef Записать(Справочник obj);
    //public ObjectRef Записать(Справочник obj) {
    //  return this.Записать(_transactionsID.GetFirst(), obj);
    //}

    public abstract ObjectRef ЗаписатьДокумент(Документ obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров);
    public abstract void ЗаписьЖурналаРегистрации(string событие, object данные, string коментарий);
    public abstract void ЗаписьЖурналаРегистрации(string событие);
    //public abstract void TestFunc(Action<Func<int, ObjectRef>> test);

    public List<T> ВыполнитьЗапрос<T>(string аТекстЗапроса) where T:new() {
      return ВыполнитьЗапрос<T>(аТекстЗапроса, null);
    }

    public string ВыполнитьЗапросJS(string аТекстЗапроса) {
      return ВыполнитьЗапросJS(аТекстЗапроса, null);
    }

    //protected void ПолучитьПрефиксИмяТаблицы(string имяТипа, out string префикс, out string имя) {
    //  int sepIndex = имяТипа.IndexOf('_');
    //  if (sepIndex == -1) {
    //    throw new ArgumentException("Тип " + имяТипа + " не содержит префикса");
    //  }
    //  префикс = имяТипа.Substring(0, sepIndex);
    //  имя = имяТипа.Substring(++sepIndex, имяТипа.Length - sepIndex);
    //}

    //protected string ПолучитьИмяТаблицы(string имяТипа) {
    //  int sepIndex = имяТипа.IndexOf('_');
    //  if (sepIndex == -1) {
    //    throw new ArgumentException("Тип " + имяТипа + " не содержит префикса");
    //  }
    //  return имяТипа.Substring(++sepIndex, имяТипа.Length - sepIndex);
    //}

  }
  #endregion

  //****************************************************************************************************************
  //****************************************************************************************************************
  //****************************************************************************************************************

  #region "ComDAL"
  public class ComDAL:DAL {
    //const string констПредставление = "Представление";

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
    //    ComResult = V8A.Call(con, "Справочники.Проекты.НайтиПоКоду()", rin);
    //    return (ObjectRef)(V8_Custom.СпрСсылка_Проекты)V8A.ConvertValueV8ToNet(ComResult, con, typeof(V8_Custom.СпрСсылка_Проекты));
    //  });
    //  V8A.ReleaseComObject(ComResult);
    //  pool.ReturnDBConnection(con);
    //}

    //public override Guid _BeginTransaction(TimeSpan timeOut) {
    //  using (DbConnection con = ConnectV8(_transactionID)) {
    //    return con.НачатьТранзакцию(timeOut);
    //  }
    //}

    //public override void _CommitTransaction(Guid transactionID) {
    //  using (DbConnection con = ConnectV8(transactionID)) {
    //    con.ЗафиксироватьТранзакцию();
    //  }
    //}

    //public override void _RollbackTransaction(Guid transactionID) {
    //  using (DbConnection con = ConnectV8(transactionID)) {
    //    con.ОтменитьТранзакцию();
    //  }
    //}
    internal virtual Func<DbConnection> ConnectionGetter() {
      return () => DbConnections.Instance.ConnectV8(Guid.Empty);
    }

    public override string ВыполнитьЗапросJS(string аТекстЗапроса, ПарамЗапроса[] аПараметры) {
      return DALEngine.ВыполнитьЗапросJS(ConnectionGetter(), аТекстЗапроса, аПараметры);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComObjQueryResult = V8A.ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);

      //  object ComObjКолонки = V8A.Call(con.Connection, ComObjQueryResult, "Колонки");
      //  int ЧислоКолВЗапросе = (int)V8A.Call(con.Connection, ComObjКолонки, "Количество()");

      //  Dictionary<string, ИнфОПоле> Поля = new Dictionary<string, ИнфОПоле>(ЧислоКолВЗапросе);
      //  List<ИнфОПоле> ПоляПредставлений = new List<ИнфОПоле>(ЧислоКолВЗапросе / 2);

      //  for (int i = 0; i != ЧислоКолВЗапросе; i++) {
      //    object ComObjКолонка = V8A.Call(con.Connection, ComObjКолонки, "Получить()", i);
      //    string ИмяКолон = (string)V8A.Call(con.Connection, ComObjКолонка, "Имя");
      //    V8A.ReleaseComObject(ComObjКолонка);
      //    ИнфОПоле Поле = new ИнфОПоле();
      //    Поле.Индекс = i;
      //    if (ИмяКолон.EndsWith(констПредставление)) {
      //      Поле.Имя = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
      //      ПоляПредставлений.Add(Поле);
      //    } else {
      //      Поле.Имя = ИмяКолон;
      //      Поля.Add(ИмяКолон, Поле);
      //    }
      //  }
      //  V8A.ReleaseComObject(ComObjКолонки);

      //  foreach (ИнфОПоле поле in ПоляПредставлений) {
      //    ИнфОПоле полеСсылки;
      //    if (!Поля.TryGetValue(поле.Имя, out полеСсылки)) {
      //      throw new ArgumentException("В запросе нет поля типа ссылка с именем " + поле.Имя);
      //    } else {
      //      полеСсылки.ПолеПредставления = поле;
      //    }
      //  }

      //  object ComВыборка = V8A.Call(con.Connection, ComObjQueryResult, "Выбрать()");
      //  V8A.ReleaseComObject(ComObjQueryResult);
      //  StringBuilder Результат = new StringBuilder("[");
      //  if ((bool)V8A.Call(con.Connection, ComВыборка, "Следующий()")) {
      //    Результат.Append("{");
      //    foreach (ИнфОПоле поле in Поля.Values) {
      //      object V8Поле = V8A.Call(con.Connection, ComВыборка, "Получить()", поле.Индекс);
      //      object Значение = V8A.ConvertValueV8ToJS(V8Поле, con);
      //      V8A.ReleaseComObject(V8Поле);
      //      if (поле.ПолеПредставления != null) {
      //        string представление = V8A.Call(con.Connection, ComВыборка, "Получить()", поле.ПолеПредставления.Индекс) as string;
      //        Результат.Append(@"""" + поле.Имя + @""":{""guid"":" + Значение + @",""представление"":""" + System.Web.HttpUtility.HtmlEncode(представление) + @"""},");
      //      } else {
      //        Результат.Append(@"""" + поле.Имя + @""":" + Значение + @",");
      //      }
      //    }
      //    Результат.Remove(Результат.Length - 1, 1);
      //    Результат.Append("}");
      //    while ((bool)V8A.Call(con.Connection, ComВыборка, "Следующий()")) {
      //      Результат.Append(",{");
      //      foreach (ИнфОПоле поле in Поля.Values) {
      //        object V8Поле = V8A.Call(con.Connection, ComВыборка, "Получить()", поле.Индекс);
      //        object Значение = V8A.ConvertValueV8ToJS(V8Поле, con);
      //        V8A.ReleaseComObject(V8Поле);
      //        if (поле.ПолеПредставления != null) {
      //          string представление = V8A.Call(con.Connection, ComВыборка, "Получить()", поле.ПолеПредставления.Индекс) as string;
      //          Результат.Append(@"""" + поле.Имя + @""":{""guid"":" + Значение + @",""представление"":""" + System.Web.HttpUtility.HtmlEncode(представление) + @"""},");
      //        } else {
      //          Результат.Append(@"""" + поле.Имя + @""":" + Значение + @",");
      //        }
      //      }
      //      Результат.Remove(Результат.Length - 1, 1);
      //      Результат.Append("}");
      //    }
      //  }
      //  Результат.Append("]");
      //  V8A.ReleaseComObject(ComВыборка);
      //  return Результат.ToString();
      //}
    }

    public override List<ТипРяда> ВыполнитьЗапрос<ТипРяда>(string аТекстЗапроса, ПарамЗапроса[] аПараметры) {
      return DALEngine.ВыполнитьЗапрос<ТипРяда>(ConnectionGetter(), аТекстЗапроса, аПараметры);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComObjQueryResult = V8A.ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);

      //  object ComObjКолонки = V8A.Call(con.Connection, ComObjQueryResult, "Колонки");
      //  int ЧислоКолВЗапросе = (int)V8A.Call(con.Connection, ComObjКолонки, "Количество()");
      //  List<ИнфОПоле> ПоляЗапроса = new List<ИнфОПоле>(ЧислоКолВЗапросе);
      //  Dictionary<string, int> ФактИмяКол2Инд = new Dictionary<string, int>(ЧислоКолВЗапросе);
      //  int[] ИндексыПредставлений = new int[ЧислоКолВЗапросе];
      //  int КолвоПредст = 0;
      //  for (int i = 0; i != ЧислоКолВЗапросе; i++) {
      //    object ComObjКолонка = V8A.Call(con.Connection, ComObjКолонки, "Получить()", i);
      //    string ИмяКолон = (string)V8A.Call(con.Connection, ComObjКолонка, "Имя");
      //    V8A.ReleaseComObject(ComObjКолонка);
      //    ФактИмяКол2Инд[ИмяКолон] = i;
      //    ИнфОПоле Поле = new ИнфОПоле();
      //    Поле.ЭтоПредст = ИмяКолон.EndsWith(констПредставление);
      //    if (Поле.ЭтоПредст) {
      //      Поле.ИмяСсылки = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
      //      Поле.Тип = typeof(string);
      //      ИндексыПредставлений[КолвоПредст] = i;
      //      КолвоПредст++;
      //    } else {
      //      Поле.PtyInfo = typeof(ТипРяда).GetProperty(ИмяКолон);
      //      Поле.Тип = Поле.PtyInfo.PropertyType;
      //    }
      //    ПоляЗапроса.Add(Поле);

      //  }
      //  V8A.ReleaseComObject(ComObjКолонки);
      //  for (int i = 0; i < КолвоПредст; i++) {
      //    ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];

      //    if (!ФактИмяКол2Инд.TryGetValue(Поле.ИмяСсылки, out Поле.ИндексСсылки)) {
      //      throw new ArgumentException("В запросе нет поля типа ссылка с именем " + Поле.ИмяСсылки);
      //    } else {
      //      ИнфОПоле ПолеСсылки = ПоляЗапроса[Поле.ИндексСсылки];
      //      if (ПолеСсылки.Тип.IsSubclassOf(typeof(ObjectRef))) {
      //        Поле.PtyInfo = ПолеСсылки.Тип.GetProperty(констПредставление);
      //      } else if (ПолеСсылки.Тип != typeof(object)) {
      //        throw new ArgumentException("В запросе поле " + ПолеСсылки.ИмяСсылки + " должно быть типа ссылка или object");
      //      }
      //    }
      //  }

      //  object ComВыборка = V8A.Call(con.Connection, ComObjQueryResult, "Выбрать()");
      //  V8A.ReleaseComObject(ComObjQueryResult);
      //  List<ТипРяда> Результат = new List<ТипРяда>((int)V8A.Call(con.Connection, ComВыборка, "Количество()"));

      //  while ((bool)V8A.Call(con.Connection, ComВыборка, "Следующий()")) {
      //    object Стр = new ТипРяда(); //object Стр = Activator.CreateInstance(typeof(ТипыКолонок));
      //    for (int i = 0; i != ЧислоКолВЗапросе; i++) {
      //      ИнфОПоле Поле = ПоляЗапроса[i];
      //      object V8Поле = V8A.Call(con.Connection, ComВыборка, "Получить()", i);
      //      object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, Поле.Тип);
      //      V8A.ReleaseComObject(V8Поле);
      //      if (Поле.ЭтоПредст) {
      //        Поле.Представление = (string)Значение;
      //      } else {
      //        Поле.Значение = Значение;
      //        Поле.PtyInfo.SetValue(Стр, Значение, null);
      //      }
      //    }
      //    for (int i = 0; i < КолвоПредст; i++) {
      //      ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];
      //      object Предст = Поле.Представление;
      //      object Ссылка = ПоляЗапроса[Поле.ИндексСсылки].Значение;
      //      if (Ссылка != null) {
      //        if (Поле.PtyInfo != null) {
      //          Поле.PtyInfo.SetValue(Ссылка, Предст, null);
      //        } else {
      //          PropertyInfo prop = Ссылка.GetType().GetProperty(констПредставление);
      //          if (prop != null) {
      //            prop.SetValue(Ссылка, Предст, null);
      //          }
      //        }
      //      }
      //    }
      //    Результат.Add((ТипРяда)Стр);
      //  }
      //  V8A.ReleaseComObject(ComВыборка);
      //  return Результат;
      //}
    }

    public object[,] ВыполнитьЗапрос(string аТекстЗапроса, ПарамЗапроса[] аПараметры, Type[] ТипыКолонок, string[] ИменаКолонок) {
      return DALEngine.ВыполнитьЗапрос(ConnectionGetter(), аТекстЗапроса, аПараметры, ТипыКолонок, ИменаКолонок);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComObjQueryResult = V8A.ПолучитьРезультатЗапроса(con, аТекстЗапроса, аПараметры);

      //  object ComObjКолонки = V8A.Call(con.Connection, ComObjQueryResult, "Колонки");
      //  int ЧислоКолВЗапросе = (int)V8A.Call(con.Connection, ComObjКолонки, "Количество()");
      //  List<ИнфОПоле> ПоляЗапроса = new List<ИнфОПоле>(ЧислоКолВЗапросе);
      //  Dictionary<string, int> ФактИмяКол2Инд = new Dictionary<string, int>(ЧислоКолВЗапросе);
      //  int[] ИндексыПредставлений = new int[ЧислоКолВЗапросе];
      //  int КолвоПредст = 0;
      //  int m = 0;
      //  for (int i = 0; i != ЧислоКолВЗапросе; i++) {
      //    object ComObjКолонка = V8A.Call(con.Connection, ComObjКолонки, "Получить()", i);
      //    string ИмяКолон = (string)V8A.Call(con.Connection, ComObjКолонка, "Имя");
      //    V8A.ReleaseComObject(ComObjКолонка);
      //    ФактИмяКол2Инд[ИмяКолон] = i;
      //    ИнфОПоле Поле = new ИнфОПоле();
      //    Поле.ЭтоПредст = ИмяКолон.EndsWith(констПредставление);
      //    if (Поле.ЭтоПредст) {
      //      Поле.ИмяСсылки = ИмяКолон.Substring(0, ИмяКолон.Length - констПредставление.Length);
      //      Поле.Тип = typeof(string);
      //      ИндексыПредставлений[КолвоПредст] = i;
      //      КолвоПредст++;
      //    } else {
      //      if (m >= ТипыКолонок.Length) {
      //        throw new ArgumentException(@"Слишком мало properties в классе запроса");
      //      }
      //      if (ИмяКолон != ИменаКолонок[m]) {
      //        throw new ArgumentException(@"Должно идти property """ + ИмяКолон + @""", а не """ + ИменаКолонок[m] + @"""");
      //      }
      //      Поле.Тип = ТипыКолонок[m];
      //      m++;
      //    }
      //    ПоляЗапроса.Add(Поле);
      //  }
      //  V8A.ReleaseComObject(ComObjКолонки);

      //  for (int i = 0; i < КолвоПредст; i++) {
      //    ИнфОПоле Поле = ПоляЗапроса[ИндексыПредставлений[i]];

      //    if (!ФактИмяКол2Инд.TryGetValue(Поле.ИмяСсылки, out Поле.ИндексСсылки)) {
      //      throw new ArgumentException("В запросе нет поля типа ссылка с именем " + Поле.ИмяСсылки);
      //    } else {
      //      ИнфОПоле ПолеСсылки = ПоляЗапроса[Поле.ИндексСсылки];
      //      if (ПолеСсылки.Тип.IsSubclassOf(typeof(ObjectRef))) {
      //        Поле.PtyInfo = ПолеСсылки.Тип.GetProperty(констПредставление);
      //      } else if (ПолеСсылки.Тип != typeof(object)) {
      //        throw new ArgumentException("В запросе поле " + ПолеСсылки.ИмяСсылки + " должно быть типа ссылка или object");
      //      }
      //    }
      //  }

      //  object ComВыборка = V8A.Call(con.Connection, ComObjQueryResult, "Выбрать()");
      //  V8A.ReleaseComObject(ComObjQueryResult);
      //  object[,] Результат = new object[(int)V8A.Call(con.Connection, ComВыборка, "Количество()"), ТипыКолонок.Length];
      //  int j = 0;
      //  while ((bool)V8A.Call(con.Connection, ComВыборка, "Следующий()")) {
      //    int k = 0;
      //    for (int i = 0; i != ЧислоКолВЗапросе; i++) {
      //      ИнфОПоле Поле = ПоляЗапроса[i];
      //      object V8Поле = V8A.Call(con.Connection, ComВыборка, "Получить()", i);
      //      object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, Поле.Тип);
      //      V8A.ReleaseComObject(V8Поле);  //это может быть не ComObject
      //      if (Поле.ЭтоПредст) {
      //        Поле.Представление = (string)Значение;
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
      //        if (Поле.PtyInfo != null) {
      //          Поле.PtyInfo.SetValue(Ссылка, Предст, null);
      //        } else {
      //          PropertyInfo prop = Ссылка.GetType().GetProperty(констПредставление);
      //          if (prop != null) {
      //            prop.SetValue(Ссылка, Предст, null);
      //          }
      //        }
      //      }
      //    }
      //    j++;
      //  }
      //  V8A.ReleaseComObject(ComВыборка);
      //  return Результат;
      //}
    }

    protected Тип НайтиПоНомеру<Тип, ТНомер>(ТНомер аНомер, DateTime аДата) {
      return DALEngine.НайтиПоНомеру<Тип, ТНомер>(ConnectionGetter(), аНомер, аДата);
      //string ИмяТаблицы = ПолучитьИмяТаблицы(typeof(Тип).Name);

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComResult = V8Gate.V8A.Call(con.Connection, con.Connection.comObject, "Документы." + ИмяТаблицы + ".НайтиПоНомеру()", аНомер, аДата);
      //  Тип res = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
      //  V8Gate.V8A.ReleaseComObject(ComResult);
      //  return res;
      //}
    }

    public override Тип НайтиПоНомеру<Тип>(int аНомер, DateTime аДата) {
      return this.НайтиПоНомеру<Тип, int>(аНомер, аДата);
    }

    public override Тип НайтиПоНомеру<Тип>(string аНомер, DateTime аДата) {
      return this.НайтиПоНомеру<Тип, string>(аНомер, аДата);
    }

    protected Тип НайтиПоКоду<Тип, ТКод>(ТКод аКод) {
      return DALEngine.НайтиПоКоду<Тип, ТКод>(ConnectionGetter(), аКод);
      //string ИмяТаблицы = ПолучитьИмяТаблицы(typeof(Тип).Name);

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "Справочники." + ИмяТаблицы + ".НайтиПоКоду()", аКод);
      //  Тип res = (Тип)V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
      //  V8A.ReleaseComObject(ComResult);
      //  return res;
      //}
    }

    public override Тип НайтиПоКоду<Тип>(int аКод) {
      return this.НайтиПоКоду<Тип, int>(аКод);
    }

    public override Тип НайтиПоКоду<Тип>(string аКод) {
      return this.НайтиПоКоду<Тип, string>(аКод);
    }

    public override Тип НайтиПоНаименованию<Тип>(string аНаим) {
      return DALEngine.НайтиПоНаименованию<Тип>(ConnectionGetter(), аНаим);
      //string ИмяТаблицы = ПолучитьИмяТаблицы(typeof(Тип).Name);

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "Справочники." + ИмяТаблицы + ".НайтиПоНаименованию()", аНаим);
      //  Тип res = (Тип)V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
      //  V8A.ReleaseComObject(ComResult);
      //  return res;
      //}
    }

    public override Тип НайтиПоРеквизиту<Тип>(string аИмяРеквизита, object аЗначениеРеквизита, Тип аРодитель, object аВладелец) {
      return DALEngine.НайтиПоРеквизиту(ConnectionGetter(), аИмяРеквизита, аЗначениеРеквизита, аРодитель, аВладелец);
      //string ИмяТаблицы = ПолучитьИмяТаблицы(typeof(Тип).Name);

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComЗначение = V8A.ConvertValueNetToV8(аЗначениеРеквизита, con);
      //  object ComРодитель = null;
      //  if (аРодитель != null) { //!!!!!!!!!! кривизна !!!!!!!!!!!!!!!!!!!
      //    ComРодитель = V8A.ConvertValueNetToV8(аРодитель, con);
      //  }
      //  object ComВладелец = null;
      //  if (аВладелец != null) { //!!!!!!!!!! кривизна !!!!!!!!!!!!!!!!!!!
      //    ComВладелец = V8A.ConvertValueNetToV8(аВладелец, con);
      //  }
      //  object ComResult = V8A.Call(con.Connection, con.Connection.comObject, "Справочники." + ИмяТаблицы + ".НайтиПоРеквизиту()", аИмяРеквизита, ComЗначение, ComРодитель, ComВладелец);
      //  Тип result = (Тип)V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, typeof(Тип));
      //  V8A.ReleaseComObject(ComResult);
      //  V8A.ReleaseComObject(ComРодитель);
      //  V8A.ReleaseComObject(ComЗначение);
      //  return result;
      //}
    }

    public override T ПолучитьПоле<T>(ObjectRef oRef, string aName) {
      return DALEngine.ПолучитьПоле<T>(ConnectionGetter(), oRef, aName);
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

    public override TabList<T> ПолучитьТабличнуюЧасть<T>(ObjectRef oRef, string aName) {
      return DALEngine.ПолучитьТабличнуюЧасть<T>(ConnectionGetter(), oRef, aName);
    }

    public override string ПолучитьПредставлениеЗапросом(ObjectRef oRef) {
      if (oRef.IsEmpty()) return string.Empty;
      return DALEngine.ПолучитьПредставлениеЗапросом(ConnectionGetter(), oRef);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  return ПолучитьПредставлениеЗапросом(con, oRef);
      //}
    }

    //private string ПолучитьПредставлениеЗапросом(DbConnection con, ObjectRef oRef) {
    //  if (oRef.IsEmpty()) return string.Empty;

    //  string ИмяТаблицы = ПолучитьИмяТаблицы(oRef.GetType().Name);
    //  string ПрефиксТаблицы;
    //  if (oRef is CatalogRef) {
    //    ПрефиксТаблицы = "Справочник.";
    //  } else if (oRef is DocumentRef) {
    //    ПрефиксТаблицы = "Документ.";
    //  } else {
    //    throw new ArgumentException("Ссылка не является ни Справочником ни Документом");
    //  }

    //  string ТекстЗапроса = "ВЫБРАТЬ " + ИмяТаблицы + "." + констПредставление +
    //    " ИЗ " + ПрефиксТаблицы + ИмяТаблицы +
    //    " КАК " + ИмяТаблицы + " ГДЕ	" + ИмяТаблицы + ".Ссылка = &Ссылка";

    //  string result;

    //  ПарамЗапроса[] Пар = new ПарамЗапроса[] { new ПарамЗапроса("Ссылка", oRef) };
    //  object qResult = V8A.ПолучитьРезультатЗапроса(con, ТекстЗапроса, Пар);
    //  object Выборка = V8A.Call(con.Connection, qResult, "Выбрать()");
    //  V8A.ReleaseComObject(qResult);

    //  if ((bool)V8A.Call(con.Connection, Выборка, "Следующий()")) { //запрос должен вернуть ровно 1 строку
    //    result = (string)V8A.Call(con.Connection, Выборка, "Получить()", 0);
    //  } else {
    //    throw new ArgumentNullException("Запрос '" + ТекстЗапроса + "' не вернул представления.");
    //  }

    //  V8A.ReleaseComObject(Выборка);

    //  return result;
    //}

    public override V8Object Load(ObjectRef oRef) {
      return DALEngine.Load(ConnectionGetter(), oRef);
      //V8Object obj = ObjectCache.НайтиВКеше(oRef);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComV8Ref = V8A.Reference(oRef, con);

      //  foreach (PropertyInfo fi in obj.СвойстваШапки) {
      //    if (fi.Name != "Ссылка") {
      //      object ComResult = V8A.Call(con.Connection, ComV8Ref, fi.Name); //.Substring(1));
      //      object value = V8A.ConvertValueV8ToNet(ComResult, con, fi.PropertyType);
      //      V8A.ReleaseComObject(ComResult);
      //      fi.SetValue(obj, value, null);
      //    }
      //  }
      //  foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
      //    IList TR = (IList)T.GetValue(obj, null);
      //    IV8TablePart IA = (IV8TablePart)TR;
      //    //if (IA.Активна()) {
      //    TR.Clear();
      //    PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //    object ТабличнаяЧасть = V8A.Call(con.Connection, ComV8Ref, T.Name);
      //    V8A.ReleaseComObject(ComV8Ref);
      //    int Количество = (int)V8A.Call(con.Connection, ТабличнаяЧасть, "Количество()");
      //    //для каждой строки ТабЧасти
      //    for (int i = 0; i < Количество; i++) {
      //      object ComСтрокаТаблЧасти = V8A.Call(con.Connection, ТабличнаяЧасть, "Получить()", i);
      //      object НоваяСтрока = IA.Добавить();
      //      foreach (PropertyInfo Колонка in Колонки) {
      //        object V8Поле = V8A.Call(con.Connection, ComСтрокаТаблЧасти, Колонка.Name);
      //        object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, Колонка.PropertyType);
      //        V8A.ReleaseComObject(V8Поле);
      //        Колонка.SetValue(НоваяСтрока, Значение, null);
      //      }
      //      V8A.ReleaseComObject(ComСтрокаТаблЧасти);
      //    }
      //    V8A.ReleaseComObject(ТабличнаяЧасть);
      //  }
      //  return obj;
      //}
    }

    public override V8Object Load2(ObjectRef oRef) {
      return DALEngine.Load2(ConnectionGetter(), oRef);
      //ObjectRef Ссылка = oRef;
      //V8Object obj = ObjectCache.НайтиВКеше(oRef);
      ////string[] ЧастьИмени = Ссылка.GetType().Name.Split(new char[] { '_' });
      ////string ПрефиксТаблицы = ЧастьИмени[0];  //здесь Спр или Док
      ////string ИмяТаблицы = ЧастьИмени[1];
      //string ПрефиксТаблицы;
      //string ИмяТаблицы = ПолучитьИмяТаблицы(Ссылка.GetType().Name);
      ////ПолучитьПрефиксИмяТаблицы(Ссылка.GetType().Name, out ПрефиксТаблицы, out ИмяТаблицы);
      ////if (ПрефиксТаблицы.StartsWith("Спр")) {
      ////  ПрефиксТаблицы = "Справочник.";
      ////} else if (ПрефиксТаблицы.StartsWith("Док")) {
      ////  ПрефиксТаблицы = "Документ.";
      ////}
      //if (obj is Справочник) {
      //  ПрефиксТаблицы = "Справочник.";
      //} else if (obj is Документ) {
      //  ПрефиксТаблицы = "Документ.";
      //} else {
      //  throw new ArgumentException("Ссылка не является ни Справочником ни Документом");
      //}
      //StringBuilder ТекстЗапроса = new StringBuilder("ВЫБРАТЬ " + ИмяТаблицы + "." + констПредставление, 2000);
      //foreach (PropertyInfo fi in obj.СвойстваШапки) {
      //  if (fi.Name != "Ссылка") {
      //    Type ТипПоля = fi.PropertyType;
      //    string fldName = fi.Name; //.Substring(1);
      //    ТекстЗапроса.Append("," + ИмяТаблицы + "." + fldName);
      //    if (ТипПоля.IsSubclassOf(typeof(ObjectRef))) {
      //      ТекстЗапроса.Append("," + ИмяТаблицы + "." + fldName + "." + констПредставление);
      //    }
      //  }
      //}

      //foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
      //  ТекстЗапроса.Append("," + ИмяТаблицы + "." + T.Name + ".(");
      //  IList TR = (IList)T.GetValue(obj, null);
      //  IV8TablePart IA = (IV8TablePart)TR;
      //  //if (IA.Активна()) {
      //  //Где-то была рекомендация вместо "" использовать string.Empty ©Andrew
      //  string Разделитель2 = "";
      //  PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //  foreach (PropertyInfo Колонка in Колонки) {
      //    string fldName = Колонка.Name;
      //    ТекстЗапроса.Append(Разделитель2 + fldName);
      //    Разделитель2 = ",";
      //    if (Колонка.PropertyType.IsSubclassOf(typeof(ObjectRef))) {
      //      ТекстЗапроса.Append("," + fldName + "." + констПредставление);
      //    }
      //  }
      //  //}
      //  ТекстЗапроса.Append(")");
      //}
      //ТекстЗапроса.Append(" ИЗ " + ПрефиксТаблицы + ИмяТаблицы + " КАК " + ИмяТаблицы);
      //ТекстЗапроса.Append(" ГДЕ	" + ИмяТаблицы + ".Ссылка = &Ссылка");

      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  ПарамЗапроса[] Пар = new ПарамЗапроса[] { new ПарамЗапроса("Ссылка", Ссылка) };
      //  object result = V8A.ПолучитьРезультатЗапроса(con, ТекстЗапроса.ToString(), Пар);
      //  object Выборка = V8A.Call(con.Connection, result, "Выбрать()");
      //  V8A.ReleaseComObject(result);
      //  if ((bool)V8A.Call(con.Connection, Выборка, "Следующий()")) { //запрос должен вернуть ровно 1 строку
      //    int i = 0;
      //    bool ПервыйРаз = true;
      //    object V8Поле;
      //    foreach (PropertyInfo fi in obj.СвойстваШапки) {
      //      if (fi.Name != "Ссылка") {
      //        if (ПервыйРаз) { //первым идёт Представление для ссылки. Сама ссылка известна заранее
      //          ПервыйРаз = false;
      //          V8Поле = V8A.Call(con.Connection, Выборка, "Получить()", i++);
      //          Ссылка.Представление = (string)V8A.ConvertValueV8ToNet(V8Поле, con, typeof(string));
      //          //V8A.ReleaseComObject(V8Поле); не надо, это строка
      //        }
      //        V8Поле = V8A.Call(con.Connection, Выборка, "Получить()", i++);
      //        object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, fi.PropertyType);
      //        V8A.ReleaseComObject(V8Поле);
      //        ObjectRef oref = Значение as ObjectRef;
      //        if (oref != null) {//за ссылочным типом в следующем поле запроса должно быть Представление
      //          V8Поле = V8A.Call(con.Connection, Выборка, "Получить()", i++);
      //          string представление = (string)V8A.ConvertValueV8ToNet(V8Поле, con, typeof(string));
      //          oref.Представление = представление ?? string.Empty;
      //          //V8A.ReleaseComObject(V8Поле); не надо, это строка
      //        }
      //        fi.SetValue(obj, Значение, null);
      //      }
      //    }
      //    foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
      //      IList TR = (IList)T.GetValue(obj, null);
      //      IV8TablePart IA = (IV8TablePart)TR;
      //      TR.Clear();
      //      PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
      //      object ComВложРезультЗапроса = V8A.Call(con.Connection, Выборка, "Получить()", i++);
      //      object ComВложВыборка = V8A.Call(con.Connection, ComВложРезультЗапроса, "Выбрать()");
      //      V8A.ReleaseComObject(ComВложРезультЗапроса);
      //      while ((bool)V8A.Call(con.Connection, ComВложВыборка, "Следующий()")) {
      //        object НоваяСтрока = IA.Добавить();
      //        int j = 0;
      //        foreach (PropertyInfo Колонка in Колонки) {
      //          V8Поле = V8A.Call(con.Connection, ComВложВыборка, "Получить()", j++);
      //          Type ТипПоля = Колонка.PropertyType;
      //          object Значение = V8A.ConvertValueV8ToNet(V8Поле, con, ТипПоля);
      //          V8A.ReleaseComObject(V8Поле);

      //          ObjectRef oref = Значение as ObjectRef;
      //          if (ТипПоля.IsSubclassOf(typeof(ObjectRef))) {//за ссылочным типом в следующем поле запроса должно быть Представление
      //            //if (oref != null) {//за ссылочным типом в следующем поле запроса должно быть Представление
      //            object ComResult = V8A.Call(con.Connection, ComВложВыборка, "Получить()", j++);
      //            string представление = (string)V8A.ConvertValueV8ToNet(ComResult, con, typeof(string));
      //            oref.Представление = представление == null ? string.Empty : представление;
      //            //V8A.ReleaseComObject(ComResult); не надо, это строка
      //          } else if (oref != null) {
      //            oref.Представление = ПолучитьПредставлениеЗапросом(con, oref);
      //            //oref.ПолучитьПредставление();
      //            //this.ПолучитьПоле<string>(con, oref, констПредставление);
      //          }

      //          Колонка.SetValue(НоваяСтрока, Значение, null);
      //        }
      //      }
      //      V8A.ReleaseComObject(ComВложВыборка);
      //    }
      //  }
      //  V8A.ReleaseComObject(Выборка);
      //  return obj;
      //}
    }

    //private void WriteRequest(StringBuilder writer, ObjectRef reference) {
    //  string ПрефиксТаблицы;
    //  string ИмяТаблицы;
    //  ПолучитьПрефиксИмяТаблицы(reference.GetType().Name, out ПрефиксТаблицы, out ИмяТаблицы);
    //  if (ПрефиксТаблицы.StartsWith("Спр")) {
    //    ПрефиксТаблицы = "Справочник.";
    //  } else if (ПрефиксТаблицы.StartsWith("Док")) {
    //    ПрефиксТаблицы = "Документ.";
    //  } else throw new ArgumentException("Ссылка непонятной таблицы - " + reference.GetType().Name + " GUID=" + reference.UUID);

    //  writer.Append("ВЫБРАТЬ " + ИмяТаблицы + "." + констПредставление);

    //  writer.Append(" ИЗ " + ПрефиксТаблицы + ИмяТаблицы + " КАК " + ИмяТаблицы);
    //  writer.Append(" ГДЕ	" + ИмяТаблицы + ".Ссылка = &Ссылка");
    //}

    public override ObjectRef Записать(Справочник obj) {
      return DALEngine.Записать(ConnectionGetter(), obj);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object V8ComObj = ЗаполнитьПоляОбъекта(con, obj);
      //  //if (obj is Справочник) {
      //  ЗаписатьСпр(con, obj, V8ComObj);
      //  //}
      //  V8A.ReleaseComObject(V8ComObj);
      //  return obj.Ссылка;
      //}
    }

    public override ObjectRef ЗаписатьДокумент(Документ obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
      return DALEngine.Записать(ConnectionGetter(), obj, аРежЗаписи, аРежПров);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object V8ComObj = ЗаполнитьПоляОбъекта(con, obj);
      //  ЗаписатьДок(con, obj, V8ComObj, аРежЗаписи, аРежПров);
      //  V8A.ReleaseComObject(V8ComObj);
      //  return obj.Ссылка;
      //}
    }

    //private void ЗаписатьДок(DbConnection con, Документ obj, object V8Obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
    //  object ComObjЗап = V8A.ConvertValueNetToV8(аРежЗаписи, con);
    //  object ComObjПров = V8A.ConvertValueNetToV8(аРежПров, con);
    //  ObjectRef Ссылка = obj.Ссылка;
    //  V8Gate.V8A.Call(con.Connection, V8Obj, "Записать()", ComObjЗап, ComObjПров);
    //  V8Gate.V8A.ReleaseComObject(ComObjПров);
    //  V8Gate.V8A.ReleaseComObject(ComObjЗап);
    //  if (Ссылка.IsEmpty()) {
    //    object ComResult = V8Gate.V8A.Call(con.Connection, V8Obj, "Ссылка");
    //    obj.Ссылка = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, null) as V8Gate.ObjectRef;
    //    V8Gate.V8A.ReleaseComObject(ComResult);
    //    ObjectCache.Добавить(obj.Ссылка, obj);
    //  }
    //}

    //private object ЗаполнитьПоляОбъекта(DbConnection con, V8Object obj) {
    //  ObjectRef Ссылка = obj.Ссылка;
    //  object V8ComObj = null;
    //  if (!Ссылка.IsEmpty()) {
    //    object V8Ref = V8Gate.V8A.Reference(Ссылка, con);
    //    V8ComObj = V8Gate.V8A.Call(con.Connection, V8Ref, "ПолучитьОбъект()");
    //    V8Gate.V8A.ReleaseComObject(V8Ref);
    //  } else {
    //    //string[] ЧастьИмени = Ссылка.GetType().Name.Split(new char[] { '_' });
    //    //string ПрефиксТаблицы = ЧастьИмени[0];  //здесь Спр или Док
    //    //string ИмяТаблицы = ЧастьИмени[1];
    //    string ПрефиксТаблицы;
    //    string ИмяТаблицы;
    //    ПолучитьПрефиксИмяТаблицы(Ссылка.GetType().Name, out ПрефиксТаблицы, out ИмяТаблицы);
    //    if (ПрефиксТаблицы.StartsWith("Спр")) {
    //      //Справочник спр = obj as Справочник;
    //      PropertyInfo pi = obj.GetType().GetProperty("ЭтоГруппа");
    //      bool СоздатьГруппу = false;
    //      if (pi != null && (bool)(pi.GetValue(obj, null) ?? false)) {
    //        СоздатьГруппу = true;
    //      }
    //      if (СоздатьГруппу) {
    //        //if (спр._ЭтоГруппа ?? false) {
    //        ПрефиксТаблицы = "Справочники." + ИмяТаблицы + ".СоздатьГруппу()";
    //      } else {
    //        ПрефиксТаблицы = "Справочники." + ИмяТаблицы + ".СоздатьЭлемент()";
    //      }
    //    } else if (ПрефиксТаблицы.StartsWith("Док")) {
    //      ПрефиксТаблицы = "Документы." + ИмяТаблицы + ".СоздатьДокумент()";
    //    }
    //    V8ComObj = V8Gate.V8A.Call(con.Connection, con.Connection.comObject, ПрефиксТаблицы);
    //  }

    //  foreach (PropertyInfo fi in obj.СвойстваШапки) {
    //    if (fi.Name != "Ссылка") {
    //      object value = fi.GetValue(obj, null);
    //      if (value != null) {
    //        object V8Value = V8Gate.V8A.ConvertValueNetToV8(value, con);
    //        V8Gate.V8A.SetProp(con.Connection, V8ComObj, fi.Name, V8Value);
    //        V8Gate.V8A.ReleaseComObject(V8Value);
    //      }
    //    }
    //  }
    //  foreach (PropertyInfo T in obj.СвойстваТаблЧасти) {
    //    IList TR = (IList)T.GetValue(obj, null);
    //    IV8TablePart IA = (IV8TablePart)TR;
    //    //if (IA.Активна()) {
    //    PropertyInfo[] Колонки = IA.ПолучитьСтуктуруКолонок().GetProperties(BindingFlags.Public | BindingFlags.Instance);
    //    object ТабличнаяЧасть = V8Gate.V8A.Call(con.Connection, V8ComObj, T.Name);
    //    V8Gate.V8A.Call(con.Connection, ТабличнаяЧасть, "Очистить()");
    //    foreach (object Str in TR) {
    //      object ComСтрокаТЧ = V8Gate.V8A.Call(con.Connection, ТабличнаяЧасть, "Добавить()");
    //      foreach (PropertyInfo f in Колонки) {
    //        object value = V8Gate.V8A.ConvertValueNetToV8(f.GetValue(Str, null), con);
    //        V8Gate.V8A.SetProp(con.Connection, ComСтрокаТЧ, f.Name, value);
    //        V8Gate.V8A.ReleaseComObject(value);
    //      }
    //      V8Gate.V8A.ReleaseComObject(ComСтрокаТЧ);
    //    }
    //    V8Gate.V8A.ReleaseComObject(ТабличнаяЧасть);
    //  }
    //  return V8ComObj;
    //}

    //private void ЗаписатьСпр(DbConnection con, V8Object obj, object V8Obj) {
    //  ObjectRef Ссылка = obj.Ссылка;
    //  V8Gate.V8A.Call(con.Connection, V8Obj, "Записать()");
    //  if (Ссылка.IsEmpty()) {
    //    object ComResult = V8Gate.V8A.Call(con.Connection, V8Obj, "Ссылка");
    //    obj.Ссылка = V8Gate.V8A.ConvertValueV8ToNet(ComResult, con, null) as V8Gate.ObjectRef;
    //    V8Gate.V8A.ReleaseComObject(ComResult);
    //    ObjectCache.Добавить(obj.Ссылка, obj);
    //    //if (obj is Справочник) { //на случай других объектов, кроме справочника
    //    //  PropertyInfo pi = obj.GetType().GetProperty("Код", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
    //    //  object value = V8Gate.V8A.ConvertValueV8ToNet(V8Gate.V8A.Get(V8Obj, "Код"), con, null);
    //    //  pi.SetValue(obj, value, null);
    //    //}
    //  }
    //}

    public override void ЗаписьЖурналаРегистрации(string событие) {
      ЗаписьЖурналаРегистрации(событие, null, null);
    }

    public override void ЗаписьЖурналаРегистрации(string событие, object данные, string коментарий) {
      DALEngine.ЗаписьЖурналаРегистрации(ConnectionGetter(), событие, данные, коментарий);
      //using (DbConnection con = ConnectV8(_transactionID)) {
      //  object ComДанные = V8A.ConvertValueNetToV8(данные, con);
      //  //V8A.Invoke(con.connection.comObject, "ЗаписьЖурналаРегистрации", BindingFlags.InvokeMethod, new object[] { событие, null, null, ComДанные, коментарий });
      //  V8A.Call(con.Connection, con.Connection.comObject, "ЗаписьЖурналаРегистрации()", new object[] { событие, null, null, ComДанные, коментарий });
      //  V8A.ReleaseComObject(ComДанные);
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
      if (remoteURL == null) throw new ApplicationException("В AppSettings параметр " + _configRemoteURLStr + " не задан");
      _dal = (ComDAL)Activator.GetObject(typeof(ComDAL), remoteURL);
    }

    public override void ClearPool() {
      _dal.ClearPool();
    }

    public override ConnectionsInfo GetConnectionsInfo {
      get { return _dal.GetConnectionsInfo; }
    }

    public override string ВыполнитьЗапросJS(string аТекстЗапроса, ПарамЗапроса[] аПараметры) {
      return _dal.ВыполнитьЗапросJS(аТекстЗапроса, аПараметры);
    }

    public override List<T> ВыполнитьЗапрос<T>(string аТекстЗапроса, ПарамЗапроса[] аПараметры) {
      PropertyInfo[] всеПоля = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
      bool[] нужнПоле = new bool[всеПоля.Length];
      int числоНужных = 0;
      int i = 0;
      foreach (PropertyInfo поле in всеПоля) {
        if (!Attribute.IsDefined(поле, typeof(SkipAttribute))) {
          нужнПоле[i] = true;
          числоНужных++;
        }
        i++;
      }

      Type[] мТипыКолонок = new Type[числоНужных];
      string[] мИменаКолонок = new string[числоНужных];
      i = 0;
      int col = 0;
      foreach (PropertyInfo поле in всеПоля) {
        if (нужнПоле[i++]) {
          мТипыКолонок[col] = поле.PropertyType;
          мИменаКолонок[col] = поле.Name;
          col++;
        }
      }

      object[,] lst2 = _dal.ВыполнитьЗапрос(аТекстЗапроса, аПараметры, мТипыКолонок, мИменаКолонок);
      int rows = lst2.GetLength(0);
      int cols = lst2.GetLength(1);
      List<T> lst3 = new List<T>(rows);
      for (int row = 0; row < rows; row++) {
        T стр = new T();
        for (col = 0; col < cols; col++) {
          всеПоля[col].SetValue(стр, lst2[row, col], null); //а как же, когда ВсеПоля.Length != ЧислоНужных ?!!!
        }
        lst3.Add(стр);
      }
      return lst3;
    }

    public override Тип НайтиПоНаименованию<Тип>(string аНаим) {
      return _dal.НайтиПоНаименованию<Тип>(аНаим);
    }

    public override Тип НайтиПоНомеру<Тип>(int аНомер, DateTime аДата) {
      return _dal.НайтиПоНомеру<Тип>(аНомер, аДата);
    }

    public override Тип НайтиПоНомеру<Тип>(string аНомер, DateTime аДата) {
      return _dal.НайтиПоНомеру<Тип>(аНомер, аДата);
    }

    public override Тип НайтиПоКоду<Тип>(int аКод) {
      return _dal.НайтиПоКоду<Тип>(аКод);
    }

    public override Тип НайтиПоКоду<Тип>(string аКод) {
      return _dal.НайтиПоКоду<Тип>(аКод);
    }

    public override Тип НайтиПоРеквизиту<Тип>(string аИмяРеквизита, object аЗначениеРеквизита, Тип аРодитель, object аВладелец) {
      return _dal.НайтиПоРеквизиту<Тип>(аИмяРеквизита, аЗначениеРеквизита, аРодитель, аВладелец);
    }

    public override T ПолучитьПоле<T>(ObjectRef oRef, string aName) {
      return _dal.ПолучитьПоле<T>(oRef, aName);
    }

    public override TabList<T> ПолучитьТабличнуюЧасть<T>(ObjectRef oRef, string aName) {
      return _dal.ПолучитьТабличнуюЧасть<T>(oRef, aName);
    }

    public override string ПолучитьПредставлениеЗапросом(ObjectRef oRef) {
      return _dal.ПолучитьПредставлениеЗапросом(oRef);
    }

    public override V8Object Load(ObjectRef oRef) {
      return _dal.Load(oRef);
    }

    public override V8Object Load2(ObjectRef oRef) {
      return _dal.Load2(oRef);
    }

    public override ObjectRef Записать(Справочник obj) {
      return _dal.Записать(obj);
    }

    public override ObjectRef ЗаписатьДокумент(Документ obj, РежимЗаписиДокумента аРежЗаписи, РежимПроведенияДокумента аРежПров) {
      return _dal.ЗаписатьДокумент(obj, аРежЗаписи, аРежПров);
    }

    public override void ЗаписьЖурналаРегистрации(string событие) {
      _dal.ЗаписьЖурналаРегистрации(событие);
    }

    public override void ЗаписьЖурналаРегистрации(string событие, object данные, string коментарий) {
      _dal.ЗаписьЖурналаРегистрации(событие, данные, коментарий);
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
        throw new ApplicationException("Незакрытая транзакция. Вложенные транзакции не поддерживаются.");
      }
      using (DbConnection con = DbConnections.Instance.ConnectV8(_transactionID)) {
        _transactionID = con.НачатьТранзакцию(TimeSpan.MaxValue);
      }
    }

    public void CommitTransaction() {
      using (DbConnection con = DbConnections.Instance.ConnectV8(_transactionID)) {
        con.ЗафиксироватьТранзакцию();
        _transactionID = Guid.Empty;
      }
    }

    public void RollbackTransaction() {
      using (DbConnection con = DbConnections.Instance.ConnectV8(_transactionID)) {
        con.ОтменитьТранзакцию();
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
