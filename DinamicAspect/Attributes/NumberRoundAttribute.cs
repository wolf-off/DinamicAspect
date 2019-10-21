using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DinamicAspect.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class NumberRoundAttribute : DependencyAttribute
	{
		public decimal Min { get; set; }
		public decimal Max { get; set; }
		public decimal Step { get; set; }

		protected override void ExtractFields()
		{
			if (Values.Length == 1)
			{
				if (Values[0].Contains(".."))
				{
					try
					{
						var minmax = Values[0].Split(new[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
						var min = decimal.Parse(minmax[0], new CultureInfo("en-US"));
						decimal max;
						decimal step = 1;
						if (minmax[1].Contains('/'))
						{
							var stepmax = minmax[1].Split('/');
							max = decimal.Parse(stepmax[0], new CultureInfo("en-US"));
							step = decimal.Parse(stepmax[1], new CultureInfo("en-US"));
						}
						else
						{
							max = decimal.Parse(minmax[1], new CultureInfo("en-US"));
						}
						Min = min;
						Max = max;
						Step = step;
					}
					catch (Exception)
					{
						Console.WriteLine("Fail on Available Int value parsing");
					}
				}
				else
				{

					if (decimal.TryParse(Values[0], out decimal minmax))
					{
						Min = minmax;
						Max = minmax;
						Step = 0;
					}
				}
			}
			else
			{
				throw new NotImplementedException("not support several ranges");
			}
		}

		public NumberRoundAttribute(string[] fields, string[][] conditions, string[] values) : base(fields, conditions, values) { }
		public NumberRoundAttribute(string[] fields, string[][] conditions, string values) : base(fields, conditions, values) { }
		public NumberRoundAttribute(string values) : base(values) { }
		public NumberRoundAttribute(string conditions, string values) : base(conditions, values) { }

		public override string StrongValidate(object newValue, object obj = null)
		{			
			return null;
		}

		public override object SoftValidate(object newValue, object obj = null)
		{
			if (CanApply(obj))
			{
				try
				{
					var value = Convert.ToDecimal(newValue);
					if (value < Min)
					{
						return Min;
					}
					if (value > Max)
					{
						return Max;
					}
				}
				catch (Exception)
				{
					return Min;
				}

			}
			return newValue;
		}

		public override object GetDefault(Type type)
		{
			if (type == typeof(int))
			{
				return Force == null ? Convert.ToInt32(Min) : int.Parse(Force);
			}
			if (type == typeof(double))
			{
				return Force == null ? Convert.ToDouble(Min) : double.Parse(Force);
			}
			if (type == typeof(byte))
			{
				return Force == null ? Convert.ToByte(Min) : byte.Parse(Force);
			}
			return Force == null ? Min : decimal.Parse(Force, new CultureInfo("en-US"));
		}

		public override bool IsReadonly
		{
			get
			{
				return Min >= Max;
			}
		}
	}
}
