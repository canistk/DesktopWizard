using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	/// <summary>Keep reference for getter(property) how to copy the value of current getter.</summary>
	public class DeepCopyAttribute : System.Attribute
	{
		public string mapPrivateVariableName;
		public DeepCopyAttribute(string mapPrivateVariableName)
		{
			this.mapPrivateVariableName = mapPrivateVariableName;
		}
	}

	/// <summary>for some getter are redirect another variable's reference, ignore the copy process.</summary>
	public class DeepCopyIgnoreAttribute : System.Attribute { }
	public static class DataUtils
    {
		private const BindingFlags eventBindFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance;
		private const BindingFlags fieldBindFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance;
		private const BindingFlags propBindFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance;
		private const System.StringComparison ignore = System.StringComparison.OrdinalIgnoreCase;

		/// <summary>
		/// >> Solve server deserialize data replace will lose local event reference issue.
		/// in order to keep the data updatable event functional,
		/// process the copy the value one by one, instead of replace the current object instance.
		/// assume all field & property can be copy by value.
		/// assume event listener will be manually handle by the caller.
		/// assume all UI will be listen on <see cref="IDataUpdatable"/>
		/// Extra: coding style
		/// assume public getter had "m_" prefix naming in coding style. (e.g. public myVar, private m_myVar)
		/// incomparable private naming can be manually defined via <see cref="DeepCopyAttribute"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="other"></param>
		/// <param name="log">assign string builder for more information during copy.</param>
		public static void DeepCopy<T>(T self, T other, System.Text.StringBuilder log = null)
			where T : class
		{
			var _type = typeof(T);
			if (log != null)
				log.AppendLine($"[DeepCopy] {_type.Name}");

			var fields = new List<FieldInfo>();
			var properties = new List<PropertyInfo>();
			var events = new List<EventInfo>();
			var parentType = _type;

			/// Filter <see cref="DeepCopyIgnoreAttribute"/> across public/private field, getter, setter, event listener.
			do
			{
				var eArr = parentType.GetEvents(eventBindFlags).ToList();
				events.AddRange(eArr);

				var fArr = parentType.GetFields(fieldBindFlags);
				foreach (var field in fArr)
				{
					// such as getter/setter & event listener will be removed
					var match = events.FirstOrDefault(e => e.Name.Equals(field.Name, ignore));
					if (match != null)
					{
						if (match.GetCustomAttribute<DeepCopyIgnoreAttribute>() != null)
						{
							if (log != null) log.AppendLine($"[DeepCopyIgnore] event: {match.Name}");
							continue;
						}
					}
					else if (field.GetCustomAttribute<DeepCopyIgnoreAttribute>() != null)
					{
						if (log != null) log.AppendLine($"[DeepCopyIgnore] field: {field.Name}");
						continue;
					}
					if (fields.FindIndex(o => o.Name == field.Name) > -1)
					{
						// if (log != null) log.AppendLine($"duplicate > {field.Name}");
						continue;
					}
					fields.Add(field);
				}
				var pArr = parentType.GetProperties(propBindFlags);
				foreach (var prop in pArr)
				{
					var match = events.FirstOrDefault(e => e.Name.Equals(prop.Name, ignore));
					if (match != null && match.GetCustomAttribute<DeepCopyIgnoreAttribute>() != null)
					{
						if (log != null) log.AppendLine($"[DeepCopyIgnore] event(Getter): {match.Name}");
						continue;
					}
					else if (prop.GetCustomAttribute<DeepCopyIgnoreAttribute>() != null)
					{
						if (log != null) log.AppendLine($"[DeepCopyIgnore] property : {prop.Name}");
						continue;
					}
					if (properties.FindIndex(o => o.Name == prop.Name) > -1)
					{
						// if (log != null) log.AppendLine($"duplicate > {prop.Name}");
						continue;
					}
					properties.Add(prop);
				}
				parentType = parentType.BaseType;
			}
			while (parentType != null);

			if (log != null)
			{
				log
					.AppendLine()
					.AppendLine("Prepare to copy :")
					.AppendLine($"Public/Private field values : [{fields.Count}]")
					.AppendLine($"Getter/Setter values : [{properties.Count}]")
					.AppendLine();

			}

			// Copy value field to field
			foreach (var field in fields)
			{
				var oldValue = field.GetValue(self);
				var newValue = field.GetValue(other);
				if (field.GetCustomAttribute<DeepCopyIgnoreAttribute>() != null)
				{
					// handle on higher level
					throw new System.Exception($"This {field.Name:15} shouldn't happen, ");
				}

				try
				{
					field.SetValue(self, newValue);
					if (log != null) log.AppendLine($"clone field {field.Name:15}\t: {PrintValue(oldValue)} -> {PrintValue(newValue)}");
				}
				catch (System.Exception ex)
				{
					if (log != null) log.AppendLine($"[Fail] clone field {field.Name:15}\t:" + ex.StackTrace);
				}
			}

			// Copy value getter to setter
			foreach (var property in properties)
			{
				if (!property.CanRead)
					continue;

				object oldValue = null;

				try
				{
					oldValue = property.GetValue(self);
					if (property.CanWrite)
					{
						var value = property.GetValue(other);

						// Remark: private set can also access via reflection.
						property.SetValue(self, value);
						if (log != null) log.AppendLine($"clone property {property.Name:15}\t: {PrintValue(oldValue)} -> {PrintValue(value)}");
						continue; // setter found, and copy success.
					}
				}
				catch (System.Exception ex)
				{
					if (log != null) log.AppendLine($"[Fail] clone property {property.Name:15}\t:" + ex.StackTrace);
					continue;
				}

				// when setter access fail, try copy property value into field value.
				try
				{
					var value = property.GetValue(other);
					var copyAttr = property.GetCustomAttribute<DeepCopyAttribute>();
					var ignore = property.GetCustomAttribute<DeepCopyIgnoreAttribute>();
					if (ignore != null)
						continue;

					var pVarName = copyAttr != null && !string.IsNullOrEmpty(copyAttr.mapPrivateVariableName) ? copyAttr.mapPrivateVariableName : $"m_" + property.Name;
					var subType = property.DeclaringType; // _type
					var retry = 0;
					FieldInfo field = null;
					do
					{
						field = subType.GetField(pVarName, fieldBindFlags);
						if (field == null)
						{
							subType = subType.BaseType;
							if (subType != null)
								if (log != null) log.AppendLine($"> Retry[{retry}] - {subType.Name}.");
						}
					}
					while (subType != null && field == null && ++retry < 100);

					if (field != null)
					{
						field.SetValue(self, value);
						if (log != null) log.AppendLine($"clone property {pVarName:15}\t: {PrintValue(oldValue)} -> {PrintValue(value)}");
					}
					else
					{
						if (log != null) log.AppendLine($"> Fail to locate property {property.Name}");
					}
				}
				catch (System.Exception ex)
				{
					if (log != null) log.AppendLine(ex.Message);
				}
			}

			// auto trigger updateable event.
			//bool autoFireUpdate = _type.GetInterface(typeof(IDataUpdatable).FullName) != null;
			//if (autoFireUpdate)
			//{
			//    if (log != null) log.AppendLine("IDataUpdatable found.");
			//    EventInfo eventInfo = _type.GetEvent(nameof(IDataUpdatable.EVENT_Updated));
			//    var _invoke = eventInfo.EventHandlerType.GetMethod("Invoke");
			//    var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, self, "Invoke");
			//    eventInfo.AddEventHandler(self, handler);
			//    try
			//    {
			//        if (log != null) log.AppendLine("Event dispatch success ?");
			//        handler.DynamicInvoke();
			//    }
			//    catch (Exception ex)
			//    {
			//        if (log != null) log.AppendLine("Event dispatch fail ! >" + ex.StackTrace);
			//    }
			//    eventInfo.RemoveEventHandler(self, handler);
			//}

			string PrintValue(object value)
			{
				const string NULL = "\"Null\"";
				const string EMPTY = "\"Empty\"";

				if (value == null)
					return NULL;

				if (value is IList<object> arr1)
				{
					// int cnt = Math.Min(3, Math.Min(arr1.Count))
					var rst1 = string.Join(", ", arr1.Take(3));
					return $"{rst1}";
				}

				if (value is int || value is float || value is double || value is bool ||
					value is System.DateTime || value is System.TimeSpan)
				{
					return $"{value}";
				}

				if (value is string || value is char)
				{
					var x = value as string;
					if (string.IsNullOrEmpty(x))
						x = EMPTY;
					return $"{value}";
				}

				if (System.Nullable.GetUnderlyingType(value.GetType()) != null)
				{
					var lhsValue = (value == null) ? NULL : value.ToString();
					return $"{lhsValue}";
				}

				if (value is System.DateTime?)
				{
					var x = value as System.DateTime?;
					var dt = x.HasValue ? x.Value.ToString() : NULL;
					return $"{dt}";
				}

				var str = (value == null) ? NULL : value.ToString();
				return $"{str}";
			}
		}
	}
}