using Microsoft.VisualStudio.TestTools.UnitTesting;
using DinamicAspect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DinamicAspect.Attributes;

namespace DinamicAspect.Tests
{
	[TestClass()]
	public class DoubleTests
	{
		public enum SensorType
		{
			Temperature,
			Voltage
		}

		public class Sensor : BaseModel
		{
			public virtual SensorType Type { get; set; }

			[NumberRound("Type=Temperature", "1..100", Force = "1")]
			[NumberRound("Type=Voltage", "0..1", Force = "1")]
			public virtual decimal Multiply { get; set; }

			[NumberRound("Type=Temperature", "200..600", Force = "273")]
			[NumberRound("Type=Voltage;Multiply=1", "-400..400", Force = "0")]
			[NumberRound("Type=Voltage;Multiply=0", "0..1", Force = "0")]
			public virtual decimal Value { get; set; }
		}

		[TestMethod()]
		public void InitialSet()
		{
			var target = DinamicWrapper.Create<Sensor>();
			Assert.AreEqual(SensorType.Temperature, target.Type);
			Assert.AreEqual(1, target.Multiply);
			Assert.AreEqual(273, target.Value);
		}

		[TestMethod()]
		public void Set()
		{
			var target = DinamicWrapper.Create<Sensor>();
			target.Type = SensorType.Voltage;
			Assert.AreEqual(SensorType.Voltage, target.Type);
			Assert.AreEqual(1, target.Multiply);
			Assert.AreEqual(0, target.Value);
		}
	}
}