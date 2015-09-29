using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text;

namespace V8Gate {
	[System.Serializable()]
	[System.Xml.Serialization.XmlType(TypeName = "EnumRef.AccountingMovementType", Namespace = "http://v8.1c.ru/data")]
	public enum AccountingMovementType {
		Debit = 0,
		Credit = 1
	}

	[System.Serializable()]
	[System.Xml.Serialization.XmlType(TypeName = "EnumRef.AccountType", Namespace = "http://v8.1c.ru/data")]
	public enum AccountType {
		Active = 0,
		Passive = 1,
		ActivePassive = 2
	}

	[System.Serializable()]
	[System.Xml.Serialization.XmlType(TypeName = "EnumRef.AccumulationMovementType", Namespace = "http://v8.1c.ru/data")]
	public enum AccumulationMovementType {
		Receipt = 0,
		Expense = 1
	}

	[System.Serializable()]
	[System.Xml.Serialization.XmlType(TypeName = "EnumRef.AllowedLength", Namespace = "http://v8.1c.ru/data")]
	public enum AllowedLength {
		Fixed = 0,
		Variable = 1
	}

	[System.Serializable()]
	[System.Xml.Serialization.XmlType(TypeName = "EnumRef.AllowedSign", Namespace = "http://v8.1c.ru/data")]
	public enum AllowedSign {
		Any = 0,
		Nonnegative = 1
	}

	[System.Serializable()]
	[System.Xml.Serialization.XmlType(TypeName = "EnumRef.DateFractions", Namespace = "http://v8.1c.ru/data")]
	public enum DateFractions {
		Date = 0,
		Time = 1,
		DateTime = 2
	}

	public enum РежимЗаписиДокумента {
		Запись,
		ОтменаПроведения,
		Проведение
	}

	public enum РежимПроведенияДокумента {
		Неоперативный,
		Оперативный
	}

}



