using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DinamicAspect.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public abstract class DependencyAttribute : Attribute, IAttribute
	{
		public string[] Fields { get; set; }
		public string[][] Conditions { get; set; }
		public string[] Values { get; set; }
		public string Force { get; set; } = null;

		private PropertyInfo[] _fields;
		private object[][] _conditions;

		public abstract string StrongValidate(object newValue, object obj = null);
		public abstract object SoftValidate(object newValue, object obj = null);

		public abstract object GetDefault(Type type);

		public virtual bool IsReadonly
		{
			get
			{
				return false;
			}
		}

		protected virtual void ExtractFields()
		{
		}

		public DependencyAttribute(string[] fields, string[][] conditions, string[] values, string force = null)
		{
			Fields = fields;
			Conditions = conditions;
			Values = values;
			Force = force;
			ExtractFields();
		}

		public DependencyAttribute(string[] fields, string[][] conditions, string values, string force = null)
			: this(fields, conditions, values == null ? null : values.Split(',').Select(s => s.Trim()).ToArray(), force) { }

		public DependencyAttribute(string values)
			: this(new string[0], new string[0][], values) { }

		public DependencyAttribute(string[] values)
			: this(new string[0], new string[0][], values) { }

		public DependencyAttribute(string conditions, string values)
			: this(ExtractFields(conditions), ExtractConditions(conditions), values) { }

		private static string[] ExtractFields(string conditions)
		{
			if (string.IsNullOrWhiteSpace(conditions)) return new string[0];
			return conditions.Split(';').Select(s => ExtractField(s.Trim())).ToArray();
		}
		private static string ExtractField(string conditions)
		{
			return conditions.Split('=')[0].Trim();
		}
		private static string[][] ExtractConditions(string conditions)
		{
			if (string.IsNullOrWhiteSpace(conditions)) return new string[0][];
			return conditions.Split(';').Select(s => ExtractCondition(s.Trim())).ToArray();
		}
		private static string[] ExtractCondition(string conditions)
		{
			if (string.IsNullOrWhiteSpace(conditions)) return new string[0];
			var values = conditions.Split('=')[1].Trim();
			return values.Split(',').Select(s => s.Trim()).ToArray();
		}

		public void Init(Type type)
		{
			_fields = Fields.Select(f => type.GetProperty(f)).ToArray();
			_conditions = new object[_fields.Length][];

			for (int i = 0; i < _fields.Length; i++)
			{
				if (_fields[i].PropertyType.IsEnum)
				{
					_conditions[i] = Conditions[i].Select(e => Enum.Parse(_fields[i].PropertyType, e)).ToArray();
				}
				else if (_fields[i].PropertyType == typeof(decimal))
				{
					_conditions[i] = Conditions[i].Select(e => decimal.Parse(e)).Cast<object>().ToArray();
				}
				else
				{
					_conditions[i] = Conditions[i];
				}
			}
		}

		public bool CanApply(object obj)
		{
			var fl = true;
			for (int i = 0; i < Fields.Count(); i++)
			{
				fl = fl && CanApplyCondition(obj, _fields[i], _conditions[i]);
			}
			return fl;
		}

		internal bool CanApplyCondition(object obj, PropertyInfo field, object[] conditions)
		{
			var value = field.GetValue(obj);
			return conditions.Any(c => c.Equals(value));
		}
	}
}
