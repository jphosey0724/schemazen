﻿using System;
using System.Text;

namespace SchemaZen.Library.Models {
	public class Column {
		public Default Default { get; set; }
		public Identity Identity { get; set; }
		public bool IsNullable { get; set; }
		public int Length { get; set; }
		public string Name { get; set; }
		public int Position { get; set; }
		public byte Precision { get; set; }
		public int Scale { get; set; }
		public string Type { get; set; }
		public string ComputedDefinition { get; set; }
		public string CollationName { get; set; }
		public bool IsRowGuidCol { get; set; }
		public bool IsColumnSet { get; set; }
		public bool IsSparse { get; set; }
		public Table Table { get; set; }
		public Column() { }

		public Column(string name, string type, bool nullable, Default defaultValue) {
			Name = name;
			Type = type;
			Default = defaultValue;
			IsNullable = nullable;
		}

		public Column(string name, string type, int length, bool nullable, Default defaultValue)
			: this(name, type, nullable, defaultValue) {
			Length = length;
		}

		public Column(string name, string type, byte precision, int scale, bool nullable,
			Default defaultValue)
			: this(name, type, nullable, defaultValue) {
			Precision = precision;
			Scale = scale;
		}

		public bool IncludeDefaultConstraint => Default != null && Default.ScriptInline;

		private string IsNullableText {
			get {
				if (IsNullable || IsSparse) {
					return (IsSparse ? "SPARSE " : string.Empty) + "NULL";
				} else {
					return "NOT NULL";
				}
			}
		}

		private string IsPersistedText {
			get {
				return Persisted ? $"PERSISTED {(IsNullable ? string.Empty : $"NOT NULL")}" : string.Empty;
			}
		}

		public string DefaultText {
			get {
				if (Default == null || !string.IsNullOrEmpty(ComputedDefinition)) return "";
				return "\r\n      " + Default.ScriptCreate();
			}
		}

		public string IdentityText {
			get {
				if (Identity == null) return "";
				return "\r\n      " + Identity.Script();
			}
		}

		public string RowGuidColText => IsRowGuidCol ? " ROWGUIDCOL " : string.Empty;

		public bool Persisted { get; set; }

		private string CollationText {
			get {
				return !string.IsNullOrEmpty(CollationName) ? $"COLLATE {CollationName} " : string.Empty;
			}
		}

		public ColumnDiff Compare(Column c) {
			return new ColumnDiff(this, c);
		}

		private string ScriptBase() {
			if (!string.IsNullOrEmpty(ComputedDefinition)) {
				return $"[{Name}] AS {ComputedDefinition} {IsPersistedText}";
			}

			var val = new StringBuilder($"[{Name}] [{Type}]");

			switch (Type) {
				case "bigint":
				case "bit":
				case "date":
				case "datetime":
				case "datetime2":
				case "datetimeoffset":
				case "float":
				case "hierarchyid":
				case "image":
				case "int":
				case "money":
				case "ntext":
				case "real":
				case "smalldatetime":
				case "smallint":
				case "smallmoney":
				case "sql_variant":
				case "text":
				case "time":
				case "timestamp":
				case "tinyint":
				case "uniqueidentifier":
				case "geography":
				case "xml":
				case "sysname":
					val.Append($" {CollationText}{IsNullableText}");
					if (IncludeDefaultConstraint) val.Append(DefaultText);
					if (Identity != null) val.Append(IdentityText);
					if (IsRowGuidCol) val.Append(RowGuidColText);
					return val.ToString();

				case "binary":
				case "char":
				case "nchar":
				case "nvarchar":
				case "varbinary":
				case "varchar":
					if ((Type == "nchar" || Type == "nvarchar") && Length > 0) {
						Length /= 2;
					}
					var lengthString = Length.ToString();
					if (lengthString == "-1") lengthString = "max";
					val.Append($"({lengthString}) {CollationText}{IsNullableText}");
					if (IncludeDefaultConstraint) val.Append(DefaultText);
					return val.ToString();

				case "decimal":
				case "numeric":
					val.Append($"({Precision},{Scale}) {IsNullableText}");
					if (IncludeDefaultConstraint) val.Append(DefaultText);
					if (Identity != null) val.Append(IdentityText);
					return val.ToString();

				default:
					throw new NotSupportedException("Error scripting column " + Name +
						". SQL data type " + Type + " is not supported.");
			}
		}

		public string ScriptCreate() {
			return ScriptBase();
		}

		public string ScriptAlter() {
			return ScriptBase();
		}

		internal static Type SqlTypeToNativeType(string sqlType) {
			switch (sqlType.ToLower()) {
				case "bit":
					return typeof(bool);
				case "datetime":
				case "smalldatetime":
					return typeof(DateTime);
				case "int":
					return typeof(int);
				case "uniqueidentifier":
					return typeof(Guid);
				case "binary":
				case "varbinary":
				case "image":
					return typeof(byte[]);
				default:
					return typeof(string);
			}
		}

		public Type SqlTypeToNativeType() {
			return SqlTypeToNativeType(Type);
		}
	}

	public class ColumnDiff {
		public Column Source { get; set; }
		public Column Target { get; set; }

		public ColumnDiff(Column target, Column source) {
			Source = source;
			Target = target;
		}

		public bool IsDiff => IsDiffBase || DefaultIsDiff;

		private bool IsDiffBase => Source.IsNullable != Target.IsNullable ||
			Source.Length != Target.Length ||
			Source.Position != Target.Position || Source.Type != Target.Type ||
			Source.Precision != Target.Precision ||
			Source.Scale != Target.Scale ||
			Source.ComputedDefinition != Target.ComputedDefinition ||
			Source.Persisted != Target.Persisted;

		public bool DefaultIsDiff => Source.DefaultText != Target.DefaultText;

		public bool OnlyDefaultIsDiff => DefaultIsDiff && !IsDiffBase;

		/*public string Script() {
			return Target.Script();
		}*/
	}
}
