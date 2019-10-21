using DinamicAspect.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DinamicAspectTests")]
namespace DinamicAspect
{
	/// <summary>
	/// Can create wrappers for classes inherited from BaseModel
	/// </summary>
	public class DinamicWrapper
	{
		static Dictionary<Type, Type> proxies = new Dictionary<Type, Type>();

		/// <summary>
		/// Return all wrapper types
		/// </summary>
		/// <returns>Wrappers</returns>
		static public Type[] GetWrapperTypes()
		{
			return proxies.Values.ToArray();
		}

		/// <summary>
		/// Return wrapper type for type inherited from BaseModel
		/// </summary>
		/// <param name="type">Type inherited from BaseModel</param>
		/// <returns></returns>
		static public Type GetWrapperType(Type type)
		{
			return proxies[type];
		}

		/// <summary>
		/// Check type is wrapper
		/// </summary>
		/// <param name="type">Type to check</param>
		/// <returns></returns>
		static public bool IsWrapperType(Type type)
		{
			return GetWrapperTypes().Any(e => e == type);
		}

		/// <summary>
		/// Create object of wrapper type
		///     1. Create wrapper type if it have not been created
		///     2. Create object of wrapper type
		///     3. Initialize object by correct data
		/// </summary>
		/// <typeparam name="T">Type inherited from BaseModel</typeparam>
		/// <param name="notTrack">Exclude changes from memento registering</param>
		/// <returns>Object of wrapper type</returns>
		static public T Create<T>() where T : BaseModel, new()
		{
			//Check Type has been created
			Type type;
			if (proxies.ContainsKey(typeof(T)))
			{
				type = proxies[typeof(T)];
			}
			else
			{
				AssemblyName assemblyName = new AssemblyName("GenericAssembly" + typeof(T).Name);
				AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

				ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

				TypeBuilder typeBuilder = moduleBuilder.DefineType(assemblyName.FullName
								  , TypeAttributes.Public |
								  TypeAttributes.Class |
								  TypeAttributes.AutoClass |
								  TypeAttributes.AnsiClass |
								  TypeAttributes.BeforeFieldInit |
								  TypeAttributes.AutoLayout
								  , typeof(T));

				//Copy class attributes
				foreach (var attr in typeof(T).CustomAttributes)
				{
					var attrBuilder = new CustomAttributeBuilder(attr.Constructor, attr.ConstructorArguments.Select(e => e.Value).ToArray());

					typeBuilder.SetCustomAttribute(attrBuilder);
				}

				//Copy  properties
				foreach (var property in typeof(T).GetProperties())
				{
					if (property.SetMethod != null && property.SetMethod.IsVirtual && property.SetMethod.IsPublic)
					{
						PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.HasDefault, property.PropertyType, null);

						//Copy property's attributes
						foreach (var attr in property.CustomAttributes)
						{
							var attrBuilder = new CustomAttributeBuilder(attr.Constructor,
																		 attr.ConstructorArguments.Select(e => e.Value).ToArray(),
																		 attr.NamedArguments.Select(a => a.MemberInfo).Cast<PropertyInfo>().ToArray(),
																		 attr.NamedArguments.Select(a => a.TypedValue.Value).ToArray());

							propertyBuilder.SetCustomAttribute(attrBuilder);
						}

						//Simple getter
						MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod("get_" + property.Name,
							  MethodAttributes.Public |
							  MethodAttributes.SpecialName |
							  MethodAttributes.HideBySig |
							  MethodAttributes.Virtual,
							  property.PropertyType, Type.EmptyTypes);
						ILGenerator getIl = getPropMthdBldr.GetILGenerator();

						getIl.Emit(OpCodes.Ldarg_0);
						getIl.Emit(OpCodes.Call, property.GetMethod);
						getIl.Emit(OpCodes.Ret);

						//Setter with 'Log' method calling
						MethodBuilder setPropMthdBldr = typeBuilder.DefineMethod("set_" + property.Name,
							  MethodAttributes.Public |
							  MethodAttributes.SpecialName |
							  MethodAttributes.HideBySig |
							  MethodAttributes.Virtual,
							  null, new[] { property.PropertyType });

						var setter = typeof(DinamicWrapper).GetMethod("Setter", BindingFlags.Static | BindingFlags.Public);

						ILGenerator setIl = setPropMthdBldr.GetILGenerator();

						setIl.Emit(OpCodes.Ldarg_0);
						setIl.Emit(OpCodes.Ldarg_1);
						if (property.PropertyType.IsValueType)
						{
							setIl.Emit(OpCodes.Box, property.PropertyType);
						}
						setIl.Emit(OpCodes.Ldstr, property.Name);
						setIl.Emit(OpCodes.Call, setter);

						setIl.Emit(OpCodes.Ret);

						propertyBuilder.SetGetMethod(getPropMthdBldr);
						propertyBuilder.SetSetMethod(setPropMthdBldr);

						//Backdoor for direct setter
						MethodBuilder setPureMthdBldr = typeBuilder.DefineMethod("set_Pure_" + property.Name,
							  MethodAttributes.Public, CallingConventions.Standard, null, new[] { property.PropertyType });
						ILGenerator setPureIl = setPureMthdBldr.GetILGenerator();

						setPureIl.Emit(OpCodes.Ldarg_0);
						setPureIl.Emit(OpCodes.Ldarg_1);
						setPureIl.Emit(OpCodes.Call, property.GetSetMethod());
						setPureIl.Emit(OpCodes.Ret);
					}
				}

				typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

				type = typeBuilder.CreateType();
				proxies.Add(typeof(T), type);
				Console.WriteLine("Created proxy for " + typeof(T).Name);
			}

			var obj = (T)Activator.CreateInstance(type);
			InitializeValue(obj);
			return obj;
		}

		public static void Setter(object obj, object newValue, string propertyName)
		{
			SetterWithForce(obj, newValue, propertyName);
		}
		public static void SetterWithForce(object obj, object newValue, string propertyName, bool force = false)
		{
			var value = newValue;
			var property = obj.GetType().GetProperty(propertyName);
			var baseModel = (BaseModel)obj;
			if (StrongValidate(baseModel, property, value))
			{
				var oldValue = property.GetMethod.Invoke(obj, new object[] { });
				value = SoftValidate(baseModel, property, value);
				if (oldValue != value || force)
				{
					obj.GetType().GetMethod("set_Pure_" + propertyName).Invoke(obj, new object[] { value });
					ForceValidate(baseModel, property);
					Log(baseModel, property, value, oldValue);
				}
			}
		}

		static bool StrongValidate(BaseModel obj, PropertyInfo property, object value)
		{
			var attributes = BaseModel.GetAttributes(property);
			foreach (var attribute in attributes)
			{
				var result = attribute.StrongValidate(value, obj);
				if (!string.IsNullOrWhiteSpace(result))
				{
					Console.WriteLine($"Refused: {value} => {obj.GetType().BaseType.Name.ToString()}.{property.Name}");
					Console.WriteLine(result);
					return false;
				}
			}
			return true;
		}

		static object SoftValidate(BaseModel obj, PropertyInfo property, object value)
		{
			var result = value;
			var attributes = BaseModel.GetAttributes(property);
			foreach (var attribute in attributes)
			{
				result = attribute.SoftValidate(result, obj);
			}
			return result;
		}

		static void ForceValidate(BaseModel obj, PropertyInfo changedProperty)
		{
			var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.SetMethod.IsVirtual && p.Name != changedProperty.Name);
			var dependencies = new List<Tuple<string, DependencyAttribute>>();
			foreach (var property in properties)
			{
				var attr = BaseModel.GetAttributes(property).Cast<DependencyAttribute>().FirstOrDefault(a => a != null && a.CanApply(obj));
				if (attr != null && (attr.Fields.Length == 0 || attr.Fields.Any(f => f == changedProperty.Name)))
				{
					dependencies.Add(Tuple.Create(property.Name, attr));
				}
			}
			dependencies = dependencies.OrderBy(d => d.Item2.Fields.Length).ToList();
			foreach (var dependency in dependencies)
			{
				var property = obj.GetType().GetProperty(dependency.Item1);
				property.SetValue(obj, dependency.Item2.GetDefault(property.PropertyType));
			}
		}

		static void Log(object obj, PropertyInfo property, object value, object oldValue)
		{
			((BaseModel)obj).InvokeChanged(property.Name);
		}

		static void InitializeValue(object obj)
		{
			var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var property in properties)
			{
				var value = property.GetValue(obj);
				SetterWithForce(obj, value, property.Name, true);
			}
		}
	}
}
