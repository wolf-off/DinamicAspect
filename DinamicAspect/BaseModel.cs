using DinamicAspect.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DinamicAspect
{
	public class BaseModel
	{
		public event Action<string> Changed;

		internal bool forceValidation = true;

		internal void InvokeChanged(string property)
		{
			Changed?.Invoke(property);
		}

		static readonly Dictionary<PropertyInfo, IAttribute[]> attributes = new Dictionary<PropertyInfo, IAttribute[]>();

		/// <summary>
		/// All attributes of property (cached)
		/// </summary>
		/// <param name="info">Property info</param>
		/// <returns>Attributes of property</returns>
		public static IAttribute[] GetAttributes(PropertyInfo info)
		{
			if (attributes.ContainsKey(info))
			{
				return attributes[info];
			}
			else
			{
				var result = info.GetCustomAttributes(true).Cast<IAttribute>().Where(e => e != null).ToArray();
				foreach (var attr in result)
				{
					if (attr is DependencyAttribute dependencyAttribute)
					{
						dependencyAttribute.Init(info.DeclaringType);
					}
				}
				attributes.Add(info, result);
				return result;
			}
		}
	}
}