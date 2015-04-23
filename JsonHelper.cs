using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace V8Gate {
  internal static class JsonHelper {
    public static void DeserializeV8(object target, SerializationInfo info, StreamingContext context) {
      foreach (SerializationEntry field in info) {
        Type fieldType = target.GetType();
        FieldInfo fi = fieldType.GetField('_' + field.Name, BindingFlags.Instance | BindingFlags.NonPublic);
        PropertyInfo pi = fieldType.GetProperty(field.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (fi != null && pi != null) {
          object value;
          try {
            value = DeserializeValue(field.Value, pi.PropertyType);
          } catch (Exception exc) {
            throw new SerializationException("Error on '" + field.Name + "' deserialization of type '" + field.ObjectType.FullName + "' of value '" + field.Value + "'.", exc);
          }
          fi.SetValue(target, value);
        }
      }
    }

    public static object DeserializeValue(object token, Type target) {
      object result;
      JValue _jValue = token as JValue;
      JArray _jArray;
      JObject _jObject;
      if (_jValue != null) {
        result = JsonToNET(_jValue.Value, target);
      } else if ((_jObject = token as JObject) != null) {
        result = DeserializeObjectRef(target, _jObject);
      } else if ((_jArray = token as JArray) != null) {
        result = DeserializeTablePart(target, _jArray);
      } else {
        result = token;
      }
      return result;
    }

    private static object DeserializeTablePart(Type target, JArray value) {
      if (!IsSupportGenericInterface(typeof(IList<>), target)) throw new InvalidCastException("Property if array type '" + target.FullName + "' doesn't implement IList interface");

      Type itemType = target.GetGenericArguments()[0];
      IList list = Activator.CreateInstance(target) as IList;
      foreach (JToken arrayElement in value.Children()) {
        list.Add(ISerializableCreator(itemType, CollectSerializationInfo(itemType, arrayElement)));
      }
      return list;
    }

    private static object DeserializeObjectRef(Type target, JObject value) {
      object result;
      JToken typeInfo;
      if (target == typeof(object) && value.TryGetValue("T", out typeInfo)) {
        Type refType = Type.GetType(typeInfo.Value<string>(), true);
        result = ISerializableCreator(refType, CollectSerializationInfo(target, value));
      } else {
        result = ISerializableCreator(target, CollectSerializationInfo(target, value));
      }
      return result;
    }

    private static SerializationInfo CollectSerializationInfo(Type objectType, JToken value) {
      SerializationInfo serializationInfo = new SerializationInfo(objectType, new FormatterConverter());
      foreach (JToken token in value.Children()) {
        switch (token.Type) {
          case JTokenType.Property:
            JProperty prop = token as JProperty;
            if (prop == null) throw new ArgumentException("Not property occured for " + objectType.FullName);
            serializationInfo.AddValue(prop.Name, prop.Value);
            break;
          default:
            throw new NotImplementedException("GetSerialization is not implemented for " + token.Type);
        }
      }
      return serializationInfo;
    }

    public static object JsonToNET(object value, Type target) {
      if (target == typeof(Guid)) {
        string sValue = value as string;
        if (sValue != null) {
          return new Guid(sValue);
        }
      } else if (target == typeof(decimal)) {
        if (value is long) {
          return (decimal)(long)value;
        } else if (value is float) {
          return (decimal)(float)value;
        } else if (value is double) {
          return (decimal)(double)value;
        }
      } else if (target == typeof(double)) {
        if (value is long) {
          return (double)(long)value;
        } else if (value is float) {
          return (double)(float)value;
        }
      } else if (target == typeof(float)) {
        if (value is long) {
          return (float)(long)value;
        }
      } else if (target == typeof(DateTime)) {
        string sValue = value as string;
        if (sValue != null) {
          //Javascript возвращает мало-значный год, если он меньше 1000 ... гад
          int yearLength = sValue.IndexOf('-');
          if (yearLength < 4) {
            sValue = (new String('0', 4 - yearLength)) + sValue;
          }
          //Иногда Javascript возвращает время в формате 2007-07-22T11:22:33.000Z иногда 2007-07-22T11:22:33.00Z. ms не нужны
          int pointPosition = sValue.IndexOf('.');
          if (pointPosition > -1) {
            sValue = sValue.Substring(0, pointPosition) + 'Z';
          }
          DateTime dValue = DateTime.ParseExact(sValue, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
          if (dValue != DateTime.MinValue) {
            dValue = TimeZone.CurrentTimeZone.ToLocalTime(DateTime.SpecifyKind(dValue, DateTimeKind.Utc));
          }
          value = dValue;
        }
      } else if (typeof(Enum).IsAssignableFrom(target)) {
        string sValue = value as string;
        if (sValue != null) {
          value = Enum.Parse(target, sValue);
        } else {
          throw new ArgumentException("Cannot parse enum.");
        }
      }
      return value;
    }

    private static bool IsSupportGenericInterface(Type genericInterfaceType, Type type) {
      foreach (Type interfaceType in type.GetInterfaces()) {
        if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericInterfaceType) {
          return true;
        }
      }
      return false;
    }

    private static object ISerializableCreator(Type objectType, SerializationInfo info) {
      if (!typeof(ISerializable).IsAssignableFrom(objectType)) throw new InvalidCastException("Property if type '" + objectType.FullName + "' doesn't implement ISerializable");

      ConstructorInfo constructorInfo = objectType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
      if (constructorInfo != null) {
        return constructorInfo.Invoke(new object[] { info, null });
      }
      throw new ArgumentException("No serialization constructor found.");
    }
  }
}
