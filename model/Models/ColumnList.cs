﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {
	public class ColumnList {
		private readonly List<Column> _mItems = new List<Column>();

		public ReadOnlyCollection<Column> Items => _mItems.AsReadOnly();

		public void Add(Column c) {
			_mItems.Add(c);
		}

		public void Remove(Column c) {
			_mItems.Remove(c);
		}

		public Column Find(string name) {
			return _mItems.FirstOrDefault(c => c.Name == name);
		}

		public string Script() {
			var text = new StringBuilder();
			var columns = _mItems.Where(c => c.IsColumnSet == false).ToList();
			foreach (var c in columns) { 
				text.Append("   " + c.ScriptCreate());
				if (_mItems.IndexOf(c) < _mItems.Count - 1) {
					text.AppendLine(",");
				} else {
					text.AppendLine();
				}
			}
			foreach (var c in _mItems.Where(c => c.IsColumnSet)) {
				text.AppendLine(string.Format("   {0} [xml] COLUMN_SET FOR ALL_SPARSE_COLUMNS", c.Name));
			}

			return text.ToString();
		}
	}
}
