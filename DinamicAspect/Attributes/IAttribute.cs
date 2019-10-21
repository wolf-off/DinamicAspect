using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DinamicAspect.Attributes
{
	public interface IAttribute
	{
		string StrongValidate(object newValue, object obj = null);
		object SoftValidate(object newValue, object obj = null);
	}
}
