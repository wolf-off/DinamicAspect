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
	public class SimpleTests
	{
		public enum SensorType
		{
			Temperature,
			Voltage
		}

		public class Sensor : BaseModel
		{
			public virtual SensorType Type { get; set; }

			[NumberRound("Type=Temperature", "200..600", Force = "273")]
			[NumberRound("Type=Voltage", "-400..400", Force = "0")]
			public virtual decimal Value { get; set; }
		}

		[TestMethod()]
		public void Create()
		{
			var target = DinamicWrapper.Create<Sensor>();
			Assert.IsNotNull(target);
		}

		[TestMethod()]
		public void Set()
		{
			var target = DinamicWrapper.Create<Sensor>();
			target.Type = SensorType.Voltage;
			target.Value = 10;
			Assert.AreEqual(SensorType.Voltage, target.Type);
			Assert.AreEqual(10, target.Value);
		}

		[TestMethod()]
		public void SetBelow()
		{
			var target = DinamicWrapper.Create<Sensor>();
			target.Type = SensorType.Voltage;
			target.Value = -1000;
			Assert.AreEqual(-400, target.Value);
		}

		[TestMethod()]
		public void SetHigher()
		{
			var target = DinamicWrapper.Create<Sensor>();
			target.Type = SensorType.Voltage;
			target.Value = 1000;
			Assert.AreEqual(400, target.Value);
		}

		[TestMethod()]
		public void SetConsistent()
		{
			var target = DinamicWrapper.Create<Sensor>();
			target.Type = SensorType.Voltage;
			target.Type = SensorType.Temperature;
			Assert.AreEqual(273, target.Value);
		}

		[TestMethod()]
		public void InitialSet()
		{
			var target = DinamicWrapper.Create<Sensor>();
			Assert.AreEqual(SensorType.Temperature, target.Type);
			Assert.AreEqual(273, target.Value);
		}
	}
}