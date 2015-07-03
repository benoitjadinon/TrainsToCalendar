using System;
using Android.App;
using Android.OS;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Java.Lang;
using Android.Runtime;
using System.Runtime.Serialization;

namespace Trains2Calendar
{
	public static class BundleExtensions
	{
		const string jSONSERIALIZED = "__JSONSERIALIZED__";

		static List<IMapItem> typesMapping;

		static BundleExtensions() {
			typesMapping = new List<IMapItem>{
				new TypeMap<bool>  ((b, i, v) => b.PutBoolean(i, v), (b, i) => b.GetBoolean(i)),
				new TypeMap<int>   ((b, i, v) => b.PutInt(i, v),     (b, i) => b.GetInt(i)),
				new TypeMap<string>((b, i, v) => b.PutString(i, v),  (b, i) => b.GetString(i)),
			};
		}


		interface IMapItem {
			void Put(Bundle b, string name, object value, out bool success);
			object Get(Bundle b, Type t, string name, out bool success);
		}

		class TypeMap<T> : IMapItem 
		{
			readonly Action<Bundle, string, T> PutExpr;
			readonly Func<Bundle, string, T> GetExpr;

			public TypeMap (Action<Bundle, string, T> put, Func<Bundle, string, T> get)
			{
				this.PutExpr = put;
				this.GetExpr = get;
			}

			#region IMapItem implementation

			public void Put (Bundle b, string name, object value, out bool success)
			{
				if (value is T) {
					PutExpr (b, name, (T)value);
					success = true;
					return;
				}
				success = false;
			}

			public object Get (Bundle b, Type t, string name, out bool success)
			{
				if (t == typeof(T)){
					var val = (T)GetExpr.Invoke(b, name);
					success = true;
					return val;
				}
				success = false;
				return null;
			}

			#endregion
		}


		public static T GetValue<T> (this Bundle bundle, string propName, T defaultValue = default(T))
		{
			if (bundle != null) {
				bool hasConverted = false;

				if (bundle.ContainsKey (propName)) {

					//TODO: return smth so we can break the loop ?
					foreach (var listItem in typesMapping) {
						var res = listItem.Get(bundle, typeof(T), propName, out hasConverted);
						if (hasConverted)
							return (T)res;
					}

				} else if (bundle.ContainsKey (jSONSERIALIZED + propName)) {
					//return jsonconvert.from(bundle.GetString(jSONSERIALIZED+propName));
				}
			}

			return defaultValue;
		}

		public static T GetValue<T> (this Bundle bundle, Expression<Func<T>> property, T defaultValue = default(T))
		{
			return GetValue(bundle, GetExpressionName<T>(property.Body), defaultValue);
		}

		public static void FillValue<T> (this Bundle bundle, Expression<Action<T>> property, T defaultValue = default(T))
		{
			property.Compile().Invoke(GetValue(bundle, GetExpressionName<T>(property.Body), defaultValue));
		}

		public static Bundle PutValue<T> (this Bundle bundle, string propName, T value)
		{
			if (bundle == null)
				bundle = new Bundle ();

			bool hasConverted = false;

			foreach (var listItem in typesMapping) {
				listItem.Put (bundle, propName, value, out hasConverted);
				if (hasConverted) {
					return bundle;
				}
			}

			if (!hasConverted) {
				throw new NotSupportedException (string.Format ("type {0} not supported for param {1}", typeof(T).Name, propName));
				//bundle.PutString(jSONSERIALIZED + propName, Json.Convert(value));
			}

			return bundle;
		}

		public static Bundle PutValue<T> (this Bundle bundle, Expression<Func<T>> property)
		{
			bundle.PutValue(GetExpressionName<T>(property.Body), property.Compile()());
			return bundle;
		}

		public static Bundle Fill<T> (Bundle bundle, string key, T value, Expression<Func<Bundle, Action<string, T>>> put)//, Expression<Func<string, T>> get = null)
		{
			put.Compile().Invoke(bundle).Invoke(key, value);
			return bundle;
		}



		static string GetExpressionName<T> (Expression exp)
		{
			if (exp is MemberExpression)
				return ((MemberExpression)exp).Member.Name;

			// TODO: better handling
			throw new InvalidCastException();
		}


		public static TConvertType DoConvert<TConvertType>(object convertValue, out bool hasConverted)
		{
		    hasConverted = false;
		    var converted = default(TConvertType);
		    try
		    {
		        converted = (TConvertType) Convert.ChangeType(convertValue, typeof(TConvertType));
		        hasConverted = true;
		    }
		    catch (InvalidCastException)
		    {
		    }
		    catch (ArgumentNullException)
		    {
		    }
		    catch (FormatException)
		    {
		    }
		    catch (OverflowException)
		    {
		    }

		    return converted;
		}
	}
}

