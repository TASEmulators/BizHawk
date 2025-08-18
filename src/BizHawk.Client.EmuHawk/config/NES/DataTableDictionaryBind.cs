﻿using System.Collections.Generic;
using System.Data;

namespace BizHawk.Client.EmuHawk
{
	public class DataTableDictionaryBind<TKey, TValue>
	{
		public DataTable Table { get; private set; }

		private IDictionary<TKey, TValue> Dictionary { get; }

		public bool WasModified { get; private set; }

		public DataTableDictionaryBind(IDictionary<TKey, TValue> dictionary)
		{
			Dictionary = dictionary;
			CreateTable();
		}

		private void CreateTable()
		{
			Table = new DataTable();
			Table.Columns.Add("Key", typeof(TKey));
			Table.Columns.Add("Value", typeof(TValue));
			foreach (var (k, v) in Dictionary) Table.Rows.Add(k, v);

			Table.RowChanged += Table_RowChanged;
			WasModified = false;
		}

		private void Table_RowChanged(object sender, DataRowChangeEventArgs e)
		{
			var key = (TKey)e.Row[0];
			var value = (TValue)e.Row[1];

			switch (e.Action)
			{
				case DataRowAction.Add:
					if (Dictionary.ContainsKey(key))
					{
						e.Row.RejectChanges();
					}
					else
					{
						Dictionary.Add(key, value);
						WasModified = true;
					}
					break;
				case DataRowAction.Change:
					Dictionary[key] = value;
					WasModified = true;
					break;
				case DataRowAction.Delete:
					Dictionary.Remove(key);
					WasModified = true;
					break;
			}
		}
	}
}
